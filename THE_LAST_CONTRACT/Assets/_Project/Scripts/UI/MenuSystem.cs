using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TLC.Settings;

namespace TLC.UI
{
    /// <summary>
    /// رسم القوائم. المنطق في MenuModel، والرسم هنا — المنفصلين عن بعض
    /// عشان التنقّل يتختبر آلياً من غير Unity.
    /// كل حاجة بتتبني بالكود، مفيش prefabs ناقصة.
    /// </summary>
    public class MenuSystem : MonoBehaviour
    {
        Canvas _canvas;
        RectTransform _root;
        RectTransform _buttonArea;
        TextMeshProUGUI _title;
        TextMeshProUGUI _subtitle;
        TextMeshProUGUI _hint;

        readonly MenuModel _model = new MenuModel();
        MenuContext _ctx = new MenuContext();
        readonly List<GameObject> _spawned = new List<GameObject>();

        public MenuModel Model => _model;
        public MenuContext Context => _ctx;
        public bool IsOpen { get; private set; }
        public event Action<MenuAction, int> ActionRequested;

        // ─────────────────────── البناء ───────────────────────

        public void Build()
        {
            if (_canvas != null) return;

            UIFactory.EnsureEventSystem();
            _canvas = UIFactory.CreateCanvas("MenuCanvas", 200);
            _canvas.transform.SetParent(transform, false);

            _root = UIFactory.CreateSafeArea(_canvas);
            _root.name = "MenuSafeArea";

            // خلفية داكنة بتغطي اللعب
            var bg = UIFactory.CreatePanel(_root, "Backdrop",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.025f, 0.035f, 0.97f));
            bg.SetAsFirstSibling();

            // شريط ذهبي رفيع — هوية بصرية موحّدة
            var bar = UIFactory.CreatePanel(_root, "GoldRule",
                new Vector2(0.18f, 0.5f), new Vector2(0.82f, 0.5f),
                Vector2.zero, new Vector2(0f, 2f), UIFactory.Gold);
            var barRt = bar.GetComponent<RectTransform>();
            if (barRt != null) barRt.anchoredPosition = new Vector2(0f, 120f);

            _title = UIFactory.CreateText(_root, "Title", Localization.Get("game.title"),
                108f, TextAlignmentOptions.Center, UIFactory.Paper);
            Place(_title.rectTransform, 0.5f, 1f, new Vector2(0f, -150f), new Vector2(1100f, 190f));

            _subtitle = UIFactory.CreateText(_root, "Subtitle", Localization.Get("game.subtitle"),
                34f, TextAlignmentOptions.Center, UIFactory.Muted);
            Place(_subtitle.rectTransform, 0.5f, 1f, new Vector2(0f, -300f), new Vector2(1000f, 110f));

            _buttonArea = UIFactory.CreatePanel(_root, "Buttons",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, null);

            _hint = UIFactory.CreateText(_root, "Hint", "",
                26f, TextAlignmentOptions.Center, UIFactory.Muted);
            Place(_hint.rectTransform, 0.5f, 0f, new Vector2(0f, 70f), new Vector2(960f, 90f));

            _model.Changed += Refresh;
            Localization.Changed += OnLanguageChanged;
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

        /// <summary>الاتجاه: عربي = RTL، إنجليزي = LTR.</summary>
        public void ApplyLanguageDirection()
        {
            // التشكيل/الـBidi بيتعملوا في ArabicShaper + ArabicTextBinder،
            // فـRTL بتاع TMP لازم يفضل مقفول دايمًا مهما كانت اللغة.
            if (_title != null) _title.isRightToLeftText = false;
            if (_subtitle != null) _subtitle.isRightToLeftText = false;
            if (_hint != null) _hint.isRightToLeftText = false;
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>())
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

            _model.Reset(screen);
            IsOpen = true;
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void ShowPause()
        {
            if (_canvas == null) Build();
            if (_canvas == null) return;
            _model.Reset(UIScreen.Pause);
            IsOpen = true;
            _canvas.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            IsOpen = false;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        public void Back()
        {
            _model.HardwareBack();
            if (_model.Current == UIScreen.Title) return;   // بنفضل في العنوان
            Refresh();
        }

        public void Refresh()
        {
            if (_buttonArea == null) return;

            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();

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

            var entries = _model.Build(_ctx);
            ApplyDynamicSuffix(entries);

            float spacing = isTitle ? 118f : 108f;
            // أول زر لازم يبقى تحت العنوان الفرعي (y = -300 من أعلى الشاشة)
            float startY = isTitle ? 420f : 200f;

            // لو فيه عناصر كتير (النهايات) نحوّل لشبكة
            bool grid = entries.Count > 7;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                string label = Localization.Get(e.Key);
                if (!string.IsNullOrEmpty(e.Suffix)) label += "  ·  " + e.Suffix;
                if (e.Action == MenuAction.SaveToSlot)
                    label = Localization.Get("pause.save") + " " + (e.Value + 1);
                if (e.Action == MenuAction.LoadFromSlot)
                    label = Localization.Get("pause.load") + " " + (e.Value + 1);

                var btn = UIFactory.CreateButton(_buttonArea, "Entry" + i, label,
                    new Vector2(760f, 92f), 38f);

                var rt = btn.GetComponent<RectTransform>();
                if (grid)
                {
                    int col = i % 2;
                    int row = i / 2;
                    Place(rt, 0.5f, 1f,
                        new Vector2(col == 0 ? -190f : 190f, -(80f + row * 104f)),
                        new Vector2(360f, 92f));
                }
                else
                {
                    Place(rt, 0.5f, 1f, new Vector2(0f, -(startY + i * spacing)),
                        new Vector2(760f, 92f));
                }

                btn.interactable = e.Enabled;

                var captured = e;
                btn.onClick.AddListener(() => OnEntryClicked(captured));

                var canvasGroup = btn.gameObject.GetComponent<CanvasGroup>();
                if (!e.Enabled && canvasGroup != null) canvasGroup.alpha = 0.4f;

                _spawned.Add(btn.gameObject);
            }
        }

        void ApplyDynamicSuffix(List<MenuEntry> entries)
        {
            foreach (var e in entries)
            {
                if (e.Key == "settings.graphics" && string.IsNullOrEmpty(e.Suffix))
                {
                    e.Suffix = Localization.Get("settings.quality." +
                        QualityLabel(QualityManager.Instance != null
                            ? (int)QualityManager.Instance.CurrentTier : 1));
                }
                else if (e.Key == "settings.look")
                {
                    e.Suffix = Settings.ControlSettings.LookLabel;
                }
                else if (e.Key == "settings.stick")
                {
                    e.Suffix = Settings.ControlSettings.StickLabel;
                }
                else if (e.Key == "settings.stickSide")
                {
                    e.Suffix = Localization.Get(Settings.ControlSettings.StickSide ==
                        Settings.JoystickSide.Left
                        ? "settings.stickSide.left" : "settings.stickSide.right");
                }
                else if (e.Key == "settings.invert")
                {
                    e.Suffix = Localization.Get(Settings.ControlSettings.InvertY
                        ? "settings.on" : "settings.off");
                }
                else if (e.Key == "settings.autoRun")
                {
                    e.Suffix = Localization.Get(Settings.ControlSettings.AutoRun
                        ? "settings.on" : "settings.off");
                }
                else if (e.Key == "settings.language" && string.IsNullOrEmpty(e.Suffix))
                {
                    e.Suffix = Localization.Get(Localization.IsRtl
                        ? "settings.language.arabic" : "settings.language.english");
                }
                else if (e.Key == "save.slot")
                {
                    e.Suffix = Core.SaveSystem.Exists(e.Value)
                        ? Core.SaveSystem.Describe(e.Value)
                        : Localization.Get("save.empty");
                }
            }
        }

        static string QualityLabel(int tier) =>
            tier <= 0 ? "low" : tier == 1 ? "medium" : "high";

        void OnEntryClicked(MenuEntry entry)
        {
            if (!entry.Enabled) return;

            var director = Audio.AudioDirector.Instance;
            if (director != null) director.PlayClick();

            switch (entry.Action)
            {
                case MenuAction.OpenChapters: _model.Open(UIScreen.Chapters); break;
                case MenuAction.OpenSettings: _model.Open(UIScreen.Settings); break;
                case MenuAction.OpenEndings:  _model.Open(UIScreen.Endings); break;
                case MenuAction.OpenSaveLoad: _model.Open(UIScreen.SaveLoad); break;
                case MenuAction.Back:         Back(); break;
                default: break;
            }

            ActionRequested?.Invoke(entry.Action, entry.Value);

            if (entry.Action == MenuAction.Back ||
                entry.Action == MenuAction.OpenChapters ||
                entry.Action == MenuAction.OpenSettings ||
                entry.Action == MenuAction.OpenEndings ||
                entry.Action == MenuAction.OpenSaveLoad)
                Refresh();
        }
    }
}
