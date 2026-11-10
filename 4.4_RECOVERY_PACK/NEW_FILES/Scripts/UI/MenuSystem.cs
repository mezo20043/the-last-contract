// 4.4 RECOVERY — MenuSystem.cs — **مُعاد حرفيًا** (النسخة النهائية: تخطيط مُ tighten + DebugClick)
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TLC.Settings;

namespace TLC.UI
{
    /// <summary>
    /// رسم القوائم. المنطق في MenuModel، والرسم هنا — عشان التنقّل يتختبر آلياً.
    /// كل حاجة بتتبني بالكود، مفيش prefabs ناقصة.
    /// </summary>
    public class MenuSystem : MonoBehaviour
    {
        Canvas _canvas;
        RectTransform _root;
        RectTransform _buttonArea;
        RectTransform _confirmRoot;
        TextMeshProUGUI _title;
        TextMeshProUGUI _subtitle;
        TextMeshProUGUI _hint;
        TextMeshProUGUI _confirmTitle;
        TextMeshProUGUI _confirmBody;

        readonly MenuModel _model = new MenuModel();
        MenuContext _ctx = new MenuContext();
        readonly List<GameObject> _spawned = new List<GameObject>();

        public MenuModel Model => _model;
        public MenuContext Context => _ctx;
        public bool IsOpen { get; private set; }
        public event Action<MenuAction, int> ActionRequested;

        public IReadOnlyList<MenuEntry> RenderedEntries => _rendered;
        readonly List<MenuEntry> _rendered = new List<MenuEntry>();
        readonly List<Button> _buttons = new List<Button>();

        // ─────────────────────── البناء ───────────────────────

        public void Build()
        {
            if (_canvas != null) return;

            UIFactory.EnsureEventSystem();
            _canvas = UIFactory.CreateCanvas("MenuCanvas", 200);
            _canvas.transform.SetParent(transform, false);

            _root = UIFactory.CreateSafeArea(_canvas);
            _root.name = "MenuSafeArea";

            var bg = UIFactory.CreatePanel(_root, "Backdrop",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.025f, 0.035f, 0.97f));
            bg.SetAsFirstSibling();

            _title = UIFactory.CreateText(_root, "Title", Localization.Get("game.title"),
                68f, TextAlignmentOptions.Center, UIFactory.Paper);
            Place(_title.rectTransform, 0.5f, 1f, new Vector2(0f, -95f), new Vector2(1150f, 110f));

            _subtitle = UIFactory.CreateText(_root, "Subtitle", Localization.Get("game.subtitle"),
                24f, TextAlignmentOptions.Center, UIFactory.Muted);
            Place(_subtitle.rectTransform, 0.5f, 1f, new Vector2(0f, -172f), new Vector2(1040f, 56f));

            var bar = UIFactory.CreatePanel(_root, "GoldRule",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(760f, 2f), UIFactory.Gold);
            var barRt = bar.GetComponent<RectTransform>();
            if (barRt != null) barRt.anchoredPosition = new Vector2(0f, -214f);

            _buttonArea = UIFactory.CreatePanel(_root, "Buttons",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

            _hint = UIFactory.CreateText(_root, "Hint", "",
                20f, TextAlignmentOptions.Center, UIFactory.Muted);
            Place(_hint.rectTransform, 0.5f, 0f, new Vector2(0f, 42f), new Vector2(1000f, 48f));

            BuildConfirm();

            _model.Changed += Refresh;
            Localization.Changed += OnLanguageChanged;
        }

        void BuildConfirm()
        {
            _confirmRoot = UIFactory.CreatePanel(_root, "Confirm",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.72f));

            var box = UIFactory.CreatePanel(_confirmRoot, "Box",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(780f, 360f), new Color(0.06f, 0.065f, 0.08f, 0.98f));

            _confirmTitle = UIFactory.CreateText(box.transform, "Title", "", 38f,
                TextAlignmentOptions.Center, UIFactory.Gold);
            Place(_confirmTitle.rectTransform, 0.5f, 1f, new Vector2(0f, -60f), new Vector2(700f, 70f));

            _confirmBody = UIFactory.CreateText(box.transform, "Body", "", 28f,
                TextAlignmentOptions.Center, UIFactory.Paper);
            Place(_confirmBody.rectTransform, 0.5f, 0.5f, new Vector2(0f, 10f), new Vector2(700f, 150f));

            _confirmRoot.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            _model.Changed -= Refresh;
            Localization.Changed -= OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            ApplyLanguageDirection();
            Refresh();
        }

        /// <summary>الاتجاه: عربي = RTL عبر ArabicShaper (TMP RTL مقفول عشان ما يحصلش عكس مزدوج).</summary>
        public void ApplyLanguageDirection()
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                t.isRightToLeftText = false;
        }

        static void Place(RectTransform rt, float ax, float ay, Vector2 offset, Vector2 sizeDelta)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
            rt.sizeDelta = sizeDelta;
        }

        // ─────────────────────── العرض ───────────────────────

        public void SetContext(MenuContext ctx)
        {
            if (ctx != null) _ctx = ctx;
        }

        public void SetContext(Action<MenuContext> mutate)
        {
            mutate?.Invoke(_ctx);
        }

        public void Show(UIScreen screen = UIScreen.Title)
        {
            if (_canvas == null) Build();
            if (_canvas == null) return;

            _ctx.PendingConfirm = MenuAction.None;
            _model.Reset(screen);
            IsOpen = true;
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void ShowPause()
        {
            if (_canvas == null) Build();
            if (_canvas == null) return;

            _ctx.PendingConfirm = MenuAction.None;
            _model.Reset(UIScreen.Pause);
            IsOpen = true;
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            IsOpen = false;
            _ctx.PendingConfirm = MenuAction.None;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        public void Back()
        {
            if (_ctx.PendingConfirm != MenuAction.None) { _ctx.PendingConfirm = MenuAction.None; Refresh(); return; }
            _model.HardwareBack();
            if (_model.Current == UIScreen.Title) return;
            Refresh();
        }

        public void Refresh()
        {
            if (_buttonArea == null) return;

            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
            _rendered.Clear();
            _buttons.Clear();

            var screen = _model.Current;
            bool isTitle = screen == UIScreen.Title;

            if (_title != null)
            {
                _title.gameObject.SetActive(isTitle);
                _title.text = Localization.Get("game.title");
            }
            if (_subtitle != null)
            {
                _subtitle.gameObject.SetActive(isTitle);
                _subtitle.text = Localization.Get("game.subtitle");
            }
            if (_hint != null)
                _hint.text = Localization.IsRtl
                    ? "اسحب للكاميرا · عصا للحركة · زر للتفاعل"
                    : "Drag to look · Stick to move · Button to interact";

            bool confirming = _ctx.PendingConfirm != MenuAction.None;
            if (_confirmRoot != null)
            {
                _confirmRoot.gameObject.SetActive(confirming);
                if (confirming)
                {
                    _confirmTitle.text = _ctx.ConfirmTitle ?? "متأكد؟";
                    _confirmBody.text = _ctx.ConfirmBody ?? "";
                }
            }

            var entries = _model.Build(_ctx);
            ApplyDynamicSuffix(entries);
            _rendered.AddRange(entries);

            // العنوان بياخد ~٢٣٠ بكسل فوق، فالقائمة بتبدأ تحته
            float y = isTitle ? 252f : 168f;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];

                if (e.IsHeader)
                {
                    var head = UIFactory.CreateText(_buttonArea, "Header" + i, Localization.Get(e.Key),
                        26f, TextAlignmentOptions.Center, UIFactory.Muted);
                    Place(head.rectTransform, 0.5f, 1f, new Vector2(0f, -y), new Vector2(760f, 40f));
                    _spawned.Add(head.gameObject);
                    y += 46f;
                    continue;
                }

                string label = Localization.Get(e.Key);
                if (!string.IsNullOrEmpty(e.Suffix)) label += "  ·  " + e.Suffix;

                float rowH = e.IsSlotRow ? 86f : 66f;
                var btn = UIFactory.CreateButton(_buttonArea, "Entry" + i, label,
                    new Vector2(760f, rowH), e.IsSlotRow ? 30f : 34f);

                var rt = btn.GetComponent<RectTransform>();
                Place(rt, 0.5f, 1f, new Vector2(0f, -(y + rowH * 0.5f)), new Vector2(760f, rowH));

                if (!string.IsNullOrEmpty(e.Detail))
                {
                    var detail = UIFactory.CreateText(rt, "Detail", e.Detail, 20f,
                        TextAlignmentOptions.Center, UIFactory.Muted);
                    detail.rectTransform.anchorMin = new Vector2(0f, 0f);
                    detail.rectTransform.anchorMax = new Vector2(1f, 0.42f);
                    detail.rectTransform.offsetMin = new Vector2(16f, 4f);
                    detail.rectTransform.offsetMax = new Vector2(-16f, 0f);

                    var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (lbl != null)
                    {
                        lbl.rectTransform.anchorMin = new Vector2(0f, 0.40f);
                        lbl.rectTransform.anchorMax = new Vector2(1f, 1f);
                        lbl.fontSize = 28f;
                    }
                }

                if (e.IsSlotRow) AddThumbnail(rt, e.Value);
                if (e.Action == MenuAction.LoadFromSlot && e.Value > 0) AddDeleteButton(rt, e.Value);

                btn.interactable = e.Enabled;

                var captured = e;
                btn.onClick.AddListener(() => OnEntryClicked(captured));

                var canvasGroup = btn.gameObject.GetComponent<CanvasGroup>();
                if (!e.Enabled || e.Locked)
                {
                    if (canvasGroup == null) canvasGroup = btn.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = e.Locked ? 0.35f : 0.5f;
                }

                _spawned.Add(btn.gameObject);
                _buttons.Add(btn);
                y += rowH + 8f;

                if (y > 940f) break;   // الشاشة ٧٢٠ — بنوقف قبل الحد الأدنى
            }
        }

        /// <summary>للاختبارات: يضغط أول زرار مطابق للفعل.</summary>
        public bool DebugClick(MenuAction action)
        {
            var entries = _model.Build(_ctx);
            for (int i = 0; i < entries.Count && i < _buttons.Count; i++)
            {
                if (entries[i].Action != action) continue;
                var b = _buttons[i];
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                Debug.Log($"[TLC/Menu] ضغط '{entries[i].Key}' ({action})");
                b.onClick.Invoke();
                return true;
            }
            return false;
        }

        public int DebugButtonCount => _buttons.Count;

        void AddDeleteButton(RectTransform row, int menuSlotIndex)
        {
            var del = UIFactory.CreateButton(row, "Delete",
                Localization.Get("save.delete"), new Vector2(110f, 46f), 20f);
            var rt = del.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-10f, 0f);

            int captured = menuSlotIndex;
            del.onClick.AddListener(() => ActionRequested?.Invoke(MenuAction.DeleteSlot, captured));
            _spawned.Add(del.gameObject);
        }

        void AddThumbnail(RectTransform row, int slotMenuIndex)
        {
            int slot = slotMenuIndex + 1;    // القائمة بتبعت 0..2 = الخانات اليدوية A/B/C
            var path = Core.SaveManager.ThumbPathFor(slot);
            if (!System.IO.File.Exists(path)) return;

            try
            {
                var bytes = System.IO.File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!tex.LoadImage(bytes)) { Destroy(tex); return; }

                var go = new GameObject("Thumb", typeof(RectTransform), typeof(RawImage));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(row, false);
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(10f, 0f);
                rt.sizeDelta = new Vector2(120f, 68f);

                var raw = go.GetComponent<RawImage>();
                raw.texture = tex;
                raw.raycastTarget = false;

                _spawned.Add(go);
            }
            catch (Exception e) { Debug.LogWarning($"[TLC/Menu] صورة الخانة مش قابلة للقراءة: {e.Message}"); }
        }

        void ApplyDynamicSuffix(List<MenuEntry> entries)
        {
            foreach (var e in entries)
            {
                if (e.IsHeader) continue;

                switch (e.Key)
                {
                    case "settings.graphics":
                        e.Suffix = Localization.Get("settings.quality." + QualityLabel(
                            QualityManager.Instance != null ? (int)QualityManager.Instance.CurrentTier : (int)QualityManager.SavedTier(1)));
                        break;
                    case "settings.look":     e.Suffix = ControlSettings.LookLabel; break;
                    case "settings.stick":    e.Suffix = ControlSettings.StickLabel; break;
                    case "settings.stickSide":
                        e.Suffix = Localization.Get(ControlSettings.StickSide == JoystickSide.Left
                            ? "settings.stickSide.left" : "settings.stickSide.right");
                        break;
                    case "settings.invert":
                        e.Suffix = Localization.Get(ControlSettings.InvertY ? "settings.on" : "settings.off");
                        break;
                    case "settings.autoRun":
                        e.Suffix = Localization.Get(ControlSettings.AutoRun ? "settings.on" : "settings.off");
                        break;
                    case "settings.language":
                        e.Suffix = Localization.Get(Localization.IsRtl ? "settings.language.arabic" : "settings.language.english");
                        break;
                    case "settings.master": e.Suffix = "◄ " + AudioLevels.Label("master") + " ►"; break;
                    case "settings.music":  e.Suffix = "◄ " + AudioLevels.Label("music") + " ►"; break;
                    case "settings.sfx":    e.Suffix = "◄ " + AudioLevels.Label("sfx") + " ►"; break;
                    case "settings.voice":  e.Suffix = "◄ " + AudioLevels.Label("voice") + " ►"; break;
                }
            }
        }

        static string QualityLabel(int tier) =>
            tier <= 0 ? "low" : tier == 1 ? "medium" : "high";

        void OnEntryClicked(MenuEntry entry)
        {
            if (!entry.Enabled && entry.Action != MenuAction.None) return;

            var director = Audio.AudioDirector.Instance;
            if (director != null) director.PlayClick();

            switch (entry.Action)
            {
                case MenuAction.OpenChapters:   _model.Open(UIScreen.Chapters); break;
                case MenuAction.OpenSettings:   _model.Open(UIScreen.Settings); break;
                case MenuAction.OpenEndings:    _model.Open(UIScreen.Endings); break;
                case MenuAction.OpenSaveLoad:   _model.Open(UIScreen.SaveLoad); break;
                case MenuAction.OpenExtras:     _model.Open(UIScreen.Extras); break;
                case MenuAction.OpenCredits:    _model.Open(UIScreen.Credits); break;
                case MenuAction.OpenCharacters: _model.Open(UIScreen.Characters); break;
                case MenuAction.OpenRecords:    _model.Open(UIScreen.Records); break;
                case MenuAction.OpenCinematics: _model.Open(UIScreen.Cinematics); break;
                case MenuAction.Cancel:
                    _ctx.PendingConfirm = MenuAction.None;
                    Refresh();
                    return;
                case MenuAction.Back:           Back(); return;
                default: break;
            }

            ActionRequested?.Invoke(entry.Action, entry.Value);

            if (entry.Action == MenuAction.OpenChapters || entry.Action == MenuAction.OpenSettings ||
                entry.Action == MenuAction.OpenEndings || entry.Action == MenuAction.OpenSaveLoad ||
                entry.Action == MenuAction.OpenExtras || entry.Action == MenuAction.OpenCredits ||
                entry.Action == MenuAction.OpenCharacters || entry.Action == MenuAction.OpenRecords ||
                entry.Action == MenuAction.OpenCinematics)
                Refresh();
        }
    }
}
