using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace TLC.UI
{
    /// <summary>
    /// بناء الواجهة بالكود بالكامل — عشان المشروع يشتغل من أول Play
    /// من غير prefabs ولا مراجع ناقصة.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Color Gold      = new Color(0.85f, 0.71f, 0.42f);
        public static readonly Color Ink       = new Color(0.03f, 0.035f, 0.045f);
        public static readonly Color Paper     = new Color(0.92f, 0.90f, 0.87f);
        public static readonly Color Muted     = new Color(0.62f, 0.60f, 0.57f);

        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();

            // Screen Space - Camera بدل Overlay:
            //  • بيسمح بتصوير الواجهة جوه RenderTexture (اختبار بصري حقيقي)
            //  • بيخلي الواجهة تدخل في تأثيرات ما بعد المعالجة (Bloom/Vignette) فتبان جزء من الصورة
            // لو مفيش كاميرا في المشهد بنرجع لـOverlay كخطة بديلة.
            var cam = Camera.main;
            if (cam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 0.45f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        /// <summary>
        /// تمريرة سلامة على التخطيط: أي عنصر نص اتظبط بمقاس صفر أو سالب (سبب ظهور
        /// النص رأسياً أو مقصوصًا في المنيو والحوار) بيتمدد على مساحة الأب.
        /// بترجع عدد العناصر اللي اتصححت — وبتتسجل في الـConsole.
        /// </summary>
        public static int FixBrokenTextLayouts()
        {
            int fixedCount = 0;
            var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in texts)
            {
                if (t == null) continue;
                var rt = t.rectTransform;
                if (rt == null) continue;
                // ملاحظة مهمة: لو الأغلال (anchors) متمدّدة أصلًا (min != max)،
                // فـsizeDelta = 0 دي حالة سليمة مش عطل. الغلط الحقيقي هو عنصر
                // مثبّت بنقطة (anchorMin == anchorMax) ومقاسه صفر/سالب.
                bool stretched = rt.anchorMin != rt.anchorMax;
                if (stretched) continue;

                if (rt.sizeDelta.x <= 1f || rt.sizeDelta.y <= 1f || rt.rect.width <= 1f || rt.rect.height <= 1f)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    fixedCount++;
                }
            }
            if (fixedCount > 0) Debug.Log($"[TLC/UI] صلّحت تخطيط {fixedCount} عنصر نص (مقاس صفر/سالب)");
            return fixedCount;
        }

        /// <summary>
        /// بيربط كل الكانفسات في المشهد بكاميرا معيّنة كـScreen Space - Camera.
        ///
        /// ليه؟ Screen Space - Overlay بيترسم بعد الكاميرا على الشاشة مباشرة،
        /// فمش بيظهر في أي RenderTexture ولا في لقطة كاميرا. ولما الكاميرا بتتحوّل
        /// لتصوير في RenderTexture (اختبار بصري، أو شاشة داخل اللعبة، أو Cinemachine)
        /// الواجهة بتختفي. Screen Space - Camera بيدخل في مسار الرسم الطبيعي
        /// وبيفضل ظاهر في كل الحالات، وكمان بيستفيد من البلوم والفينييت.
        /// </summary>
        public static int BindAllCanvasesToCamera(Camera cam, float planeDistance = 0.6f)
        {
            if (cam == null) return 0;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            int n = 0;
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (c.renderMode == RenderMode.WorldSpace) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = planeDistance;
                c.targetDisplay = cam.targetDisplay;
                n++;
            }
            return n;
        }

        /// <summary>حاوية بتحترم النوتش وشرائط النظام على الموبايل.</summary>
        public static RectTransform CreateSafeArea(Canvas canvas)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<SafeAreaFitter>();
            return rt;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static RectTransform CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            Color? background = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.GetComponent<Image>();
            img.color = background ?? new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = background.HasValue && background.Value.a > 0.01f;

            return rt;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string content,
            float fontSize, TextAlignmentOptions alignment, Color? color = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.color = color ?? Paper;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = true;
#if UNITY_6000_0_OR_NEWER
            tmp.textWrappingMode = TextWrappingModes.Normal;
#endif
            tmp.raycastTarget = false;
            // التشكيل والـBidi بيتعملوا في ArabicShaper قبل التمرير لـTMP،
            // فبنقفل RTL بتاع TMP (اللي بيعكس من غير تشكيل) عشان ما يحصلش عكس مزدوج.
            tmp.isRightToLeftText = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            Settings.ArabicFont.Apply(tmp); // خط Amiri تلقائياً
            go.AddComponent<Settings.ArabicTextBinder>();

            return tmp;
        }

        public static Button CreateButton(Transform parent, string name, string label,
            Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.09f, 0.10f, 0.12f, 0.92f);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f);
            colors.pressedColor = new Color(0.85f, 0.71f, 0.42f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.targetGraphic = img;

            var text = CreateText(rt, "Label", label, fontSize, TextAlignmentOptions.Center, Paper);
            text.rectTransform.offsetMin = new Vector2(18f, 6f);
            text.rectTransform.offsetMax = new Vector2(-18f, -6f);

            return btn;
        }
    }
}
