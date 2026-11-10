// 4.4 RECOVERY — SaveController.cs — **مُعاد حرفيًا** (نصّه محفوظ من نفس الجلسة)
using System;
using UnityEngine;
using TLC.Core;
using TLC.Narrative;

namespace TLC.Bootstrap
{
    /// <summary>
    /// سياسة الحفظ كاملة في مكان واحد:
    /// • وقت اللعب المتراكم.
    /// • الحفظ التلقائي في نقاط آمنة (بعد قرار / كل ١٢ سطر / بعد انتقال فصل / عند النهاية).
    /// • منع الحفظ اليدوي وقت السينمائية أو أي حالة غير مستقرة (لكن مش وقت القائمة).
    /// • صورة مصغّرة + بيانات الخانة (فصل/مشهد/سطر/وقت).
    /// </summary>
    public class SaveController : MonoBehaviour
    {
        public const float AutosaveMinGap = 20f;
        public const int ThumbWidth = 320;
        public const int ThumbHeight = 180;

        GameState _state;
        NarrativeRunner _runner;
        Camera _camera;
        Cinematics.CinematicDirector _cinematics;
        UI.MenuSystem _menu;

        Func<string> _chapterId, _chapterTitle, _locationName;
        Func<bool> _cutsceneActive;

        float _sinceAutosave;
        int _autosaves;
        string _lastReason;

        public bool Paused { get; set; }
        public float Playtime => _state != null ? _state.PlaytimeSeconds : 0f;
        public int AutosaveCount => _autosaves;
        public string LastSaveReason => _lastReason;
        public bool LastSaveSucceeded { get; private set; }

        public void Bind(GameState state, NarrativeRunner runner, Camera cam,
                         Cinematics.CinematicDirector cinematics, UI.MenuSystem menu,
                         Func<string> chapterId, Func<string> chapterTitle,
                         Func<string> locationName, Func<bool> cutsceneActive)
        {
            _state = state; _runner = runner; _camera = cam;
            _cinematics = cinematics; _menu = menu;
            _chapterId = chapterId; _chapterTitle = chapterTitle;
            _locationName = locationName; _cutsceneActive = cutsceneActive;
        }

        /// <summary>كل إطار — بيجمع وقت اللعب (وقت حقيقي مش متأثر بالإيقاف).</summary>
        public void Tick(float unscaledDelta)
        {
            if (_state == null || Paused) return;
            _state.PlaytimeSeconds += unscaledDelta;
            _sinceAutosave += unscaledDelta;
        }

        public bool IsSafePoint(out string why) => IsSafePoint(out why, false);

        /// <summary>
        /// نفس الفحص مع تجاهل القائمة/الإيقاف — الحفظ اليدوي بيتنادى من القائمة نفسها،
        /// فوجودها مفتوحة مش خطر. الخطر: سينمائية مكتوبة/انتقال/كروت/بلوك سرد.
        /// </summary>
        public bool IsSafePoint(out string why, bool ignoreOverlays)
        {
            why = "آمن";

            if (_state == null) { why = "مفيش حالة"; return false; }
            if (Paused && !ignoreOverlays) { why = "اللعبة موقوفة"; return false; }

            // ملاحظة: لقطات كاميرا الحوار بتشتغل كل سطر ⇒ مش علامة على انتقال غير مستقر.
            if (_cutsceneActive != null && _cutsceneActive())
            {
                why = "حالة غير مستقرة (سينمائية/انتقال/كروت)";
                return false;
            }

            if (_menu != null && _menu.IsOpen && !ignoreOverlays)
            {
                why = "القائمة مفتوحة";
                return false;
            }

            return true;
        }

        /// <summary>حفظ يدوي في خانة (1..3).</summary>
        public bool ManualSave(int slot, out string message)
        {
            if (!SaveManager.IsManual(slot))
            {
                message = "الخانة دي مش للحفظ اليدوي";
                return false;
            }

            if (!IsSafePoint(out string why, true))
            {
                message = "مينفعش تحفظ دلوقتي: " + why;
                LastSaveSucceeded = false;
                _lastReason = "manual-blocked:" + why;
                Debug.Log($"[TLC/Save] ✘ حفظ يدوي مرفوض — {why}");
                return false;
            }

            bool ok = Write(slot, "manual");
            message = ok ? $"اتحفظت في الخانة {UI.MenuModel.SlotLabel(slot)}" : "الحفظ فشل — شوف اللوج";
            return ok;
        }

        /// <summary>حفظ تلقائي في نقطة آمنة (مع فاصل زمني أدنى).</summary>
        public bool Autosave(string reason, bool force = false)
        {
            if (!force && _sinceAutosave < AutosaveMinGap) return false;
            if (!IsSafePoint(out string why))
            {
                Debug.Log($"[TLC/Save] ⏸ حفظ تلقائي مؤجّل ({reason}): {why}");
                _lastReason = "auto-skipped:" + why;
                return false;
            }

            bool ok = Write(SaveManager.AutoSlot, "auto:" + reason);
            if (ok) { _sinceAutosave = 0f; _autosaves++; }
            return ok;
        }

        /// <summary>حفظ من غير فحص سلامة — عند النهاية وانتقال الفصل.</summary>
        public bool SaveUnsafe(string checkpoint, int slot = SaveManager.AutoSlot)
        {
            bool ok = Write(slot, checkpoint);
            if (ok) _sinceAutosave = 0f;
            return ok;
        }

        bool Write(int slot, string checkpoint)
        {
            var meta = new SaveMeta
            {
                chapterId = _chapterId != null ? _chapterId() : _state.ChapterId,
                chapterTitle = _chapterTitle != null ? _chapterTitle() : null,
                locationName = _locationName != null ? _locationName() : null,
                nodeLabel = NodeLabel(),
                endingId = _state.EndingId,
                checkpoint = checkpoint,
                isAuto = slot == SaveManager.AutoSlot,
                thumbnailPng = CaptureThumbnail()
            };

            bool ok = SaveManager.Save(slot, _state, meta);
            LastSaveSucceeded = ok;
            _lastReason = checkpoint;
            return ok;
        }

        string NodeLabel()
        {
            if (_runner == null || _runner.Story == null) return null;
            var node = _runner.Story.Get(_state.CurrentNodeId);
            if (node == null) return null;
            return !string.IsNullOrEmpty(node.text) ? node.text : node.endingTitle;
        }

        /// <summary>صورة مصغّرة صغيرة للخانة (1280×720 مش محتاجينها كلها).</summary>
        public byte[] CaptureThumbnail()
        {
            if (_camera == null) return null;
            RenderTexture rt = null, prev = null;
            try
            {
                rt = RenderTexture.GetTemporary(ThumbWidth, ThumbHeight, 16);
                prev = _camera.targetTexture;
                _camera.targetTexture = rt;
                _camera.Render();

                var active = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(ThumbWidth, ThumbHeight, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, ThumbWidth, ThumbHeight), 0, 0);
                tex.Apply();
                RenderTexture.active = active;

                var png = tex.EncodeToPNG();
                Destroy(tex);
                return png;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TLC/Save] تصوير الخانة فشل: {e.Message}");
                return null;
            }
            finally
            {
                if (_camera != null) _camera.targetTexture = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
