using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TLC.Narrative;

namespace TLC.UI
{
    /// <summary>
    /// واجهة الحوار: سطر + اسم المتكلم + آلة كاتبة + أزرار قرارات + عنوان فصل +
    /// شاشة نهاية. بتتبني بالكود وقت التشغيل.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("الإعدادات")]
        [SerializeField] float charsPerSecond = 40f;

        NarrativeRunner _runner;

        Canvas _canvas;
        RectTransform _safeRoot;
        RectTransform _dialogueRoot;
        TextMeshProUGUI _speakerLabel;
        TextMeshProUGUI _bodyLabel;
        TextMeshProUGUI _continueHint;
        Image _vignette;

        RectTransform _choiceRoot;
        TextMeshProUGUI _choicePrompt;
        readonly List<Button> _choiceButtons = new List<Button>();

        RectTransform _chapterCard;
        TextMeshProUGUI _chapterLabel;

        RectTransform _endingRoot;
        TextMeshProUGUI _endingTitle, _endingBody;

        Coroutine _typing;
        bool _isTyping;
        string _fullText = "";

        public bool IsTyping => _isTyping;

        // ────────────────────────── البناء ──────────────────────────

        public void Build(NarrativeRunner runner)
        {
            _runner = runner;

            // منع تكرار الواجهة: GameBootstrap بينادي Build بعد كل تبديل فصل،
            // فلو الكانفس موجود أصلًا بندمّره الأول — وإلا بيطلع زرار «التالي»
            // أكتر من مرة والنصوص تتراكب فوق بعضها.
            foreach (var old in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (old != null && old.name == "UI_Dialogue") UnityEngine.Object.Destroy(old.gameObject);

            UIFactory.EnsureEventSystem();
            _canvas = UIFactory.CreateCanvas("UI_Dialogue", 10);
            _safeRoot = UIFactory.CreateSafeArea(_canvas);

            // تظليل سفلي لقراءة أوضح
            var scrim = UIFactory.CreatePanel(_canvas.transform, "Scrim",
                new Vector2(0f, 0f), new Vector2(1f, 0.42f),
                Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0.78f));
            scrim.GetComponent<Image>().raycastTarget = false;

            // صندوق الحوار
            _dialogueRoot = UIFactory.CreatePanel(_safeRoot, "Dialogue",
                new Vector2(0f, 0f), new Vector2(1f, 0.40f),
                new Vector2(60f, 70f), new Vector2(-60f, -40f),
                new Color(0.03f, 0.035f, 0.05f, 0.82f));

            _speakerLabel = UIFactory.CreateText(_dialogueRoot, "Speaker", "", 40f,
                TextAlignmentOptions.TopRight, UIFactory.Gold);
            _speakerLabel.rectTransform.anchorMin = new Vector2(0f, 0.80f);
            _speakerLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _speakerLabel.rectTransform.offsetMin = Vector2.zero;
            _speakerLabel.rectTransform.offsetMax = Vector2.zero;

            _bodyLabel = UIFactory.CreateText(_dialogueRoot, "Body", "", 40f,
                TextAlignmentOptions.TopRight, UIFactory.Paper);
            _bodyLabel.rectTransform.anchorMin = new Vector2(0f, 0.10f);
            _bodyLabel.rectTransform.anchorMax = new Vector2(1f, 0.80f);
            _bodyLabel.rectTransform.offsetMin = Vector2.zero;
            _bodyLabel.rectTransform.offsetMax = Vector2.zero;
            _bodyLabel.lineSpacing = 6f;
            // هوامش داخلية عشان النص ما يلزقش في حدود الصندوق
            _bodyLabel.rectTransform.offsetMin = new Vector2(28f, 0f);
            _bodyLabel.rectTransform.offsetMax = new Vector2(-28f, 0f);
            _bodyLabel.enableWordWrapping = true;
#if UNITY_6000_0_OR_NEWER
            _bodyLabel.textWrappingMode = TextWrappingModes.Normal;
#endif
            _bodyLabel.overflowMode = TextOverflowModes.Truncate;

            _continueHint = UIFactory.CreateText(_dialogueRoot, "Hint", "اضغط للمتابعة »", 30f,
                TextAlignmentOptions.BottomLeft, new Color(0.85f, 0.71f, 0.42f, 0.8f));
            _continueHint.rectTransform.anchorMin = new Vector2(0f, 0f);
            _continueHint.rectTransform.anchorMax = new Vector2(0.5f, 0.12f);
            _continueHint.rectTransform.offsetMin = Vector2.zero;
            _continueHint.rectTransform.offsetMax = Vector2.zero;

            BuildChoiceArea();
            BuildChapterCard();
            BuildEndingScreen();

            HideDialogue();
            HideChoices();

            runner.LineShown    += OnLine;
            runner.ChoicesShown += OnChoices;
            runner.EndingReached += OnEnding;
        }

        void BuildChoiceArea()
        {
            _choiceRoot = UIFactory.CreatePanel(_safeRoot, "Choices",
                new Vector2(0f, 0.05f), new Vector2(1f, 0.62f),
                new Vector2(70f, 40f), new Vector2(-70f, -40f));

            _choicePrompt = UIFactory.CreateText(_choiceRoot, "Prompt", "", 38f,
                TextAlignmentOptions.Bottom, UIFactory.Muted);
            _choicePrompt.rectTransform.anchorMin = new Vector2(0f, 0.72f);
            _choicePrompt.rectTransform.anchorMax = new Vector2(1f, 1f);
            _choicePrompt.rectTransform.offsetMin = Vector2.zero;
            _choicePrompt.rectTransform.offsetMax = Vector2.zero;

            // الـVerticalLayoutGroup بيحكم في ارتفاع الأبناء، فلازم ارتفاع مفضّل
            // للنص وإلا بيطلع بارتفاع صفر (كان السؤال بيختفي فوق الاختيارات).
            var promptLayout = _choicePrompt.gameObject.AddComponent<LayoutElement>();
            promptLayout.minHeight = 56f;
            promptLayout.preferredHeight = 64f;
            promptLayout.flexibleHeight = 0f;

            var vlg = _choiceRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 18f;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.padding = new RectOffset(0, 0, 0, 0);
        }

        void BuildChapterCard()
        {
            _chapterCard = UIFactory.CreatePanel(_safeRoot, "ChapterCard",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, UIFactory.Ink);
            _chapterCard.GetComponent<Image>().raycastTarget = false;

            _chapterLabel = UIFactory.CreateText(_chapterCard, "Label", "", 64f,
                TextAlignmentOptions.Center, UIFactory.Gold);

            _chapterCard.gameObject.SetActive(false);
        }

        void BuildEndingScreen()
        {
            _endingRoot = UIFactory.CreatePanel(_safeRoot, "Ending",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.02f, 0.025f, 0.03f, 0.97f));
            _endingRoot.gameObject.SetActive(false);

            _endingTitle = UIFactory.CreateText(_endingRoot, "Title", "", 66f,
                TextAlignmentOptions.Top, UIFactory.Gold);
            _endingTitle.rectTransform.anchorMin = new Vector2(0f, 0.72f);
            _endingTitle.rectTransform.anchorMax = new Vector2(1f, 0.92f);
            _endingTitle.rectTransform.offsetMin = new Vector2(80f, 0f);
            _endingTitle.rectTransform.offsetMax = new Vector2(-80f, 0f);

            _endingBody = UIFactory.CreateText(_endingRoot, "Body", "", 38f,
                TextAlignmentOptions.Top, UIFactory.Paper);
            _endingBody.rectTransform.anchorMin = new Vector2(0f, 0.15f);
            _endingBody.rectTransform.anchorMax = new Vector2(1f, 0.70f);
            _endingBody.rectTransform.offsetMin = new Vector2(80f, 0f);
            _endingBody.rectTransform.offsetMax = new Vector2(-80f, 0f);
            _endingBody.lineSpacing = 14f;
        }

        // ────────────────────────── الأحداث ──────────────────────────

        void OnLine(StoryNode node)
        {
            HideChoices();
            _endingRoot.gameObject.SetActive(false);
            ShowDialogue();

            bool narration = string.IsNullOrEmpty(node.speaker);

            _speakerLabel.text = narration ? "" : DisplayName(node.speaker);
            _speakerLabel.gameObject.SetActive(!narration);
            _bodyLabel.color = narration ? new Color(0.80f, 0.78f, 0.75f) : UIFactory.Paper;
            _bodyLabel.fontStyle = narration ? FontStyles.Italic : FontStyles.Normal;

            StartTyping(node.text ?? "");
        }

        void OnChoices(StoryNode node, List<ChoiceOption> options)
        {
            HideDialogue();
            ClearChoices();

            _choicePrompt.text = string.IsNullOrEmpty(node.text) ? "" : node.text;
            _choiceRoot.gameObject.SetActive(true);

            for (int i = 0; i < options.Count; i++)
            {
                int captured = i;
                var btn = UIFactory.CreateButton(_choiceRoot, $"Choice{i}",
                    options[i].text ?? "…", new Vector2(0f, 96f), 36f);

                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 96f;
                le.preferredHeight = 96f;

                btn.onClick.AddListener(() =>
                {
                    ClearChoices();
                    _runner.Choose(captured);
                });

                _choiceButtons.Add(btn);
            }
        }

        void OnEnding(StoryNode node)
        {
            HideDialogue();
            HideChoices();

            _endingRoot.gameObject.SetActive(true);
            _endingTitle.text = node.endingTitle ?? "النهاية";
            _endingBody.text = node.endingBody ?? node.text ?? "";
        }

        // ────────────────────────── الآلة الكاتبة ──────────────────────────

        void StartTyping(string text)
        {
            _fullText = text;
            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeRoutine(text));
        }

        IEnumerator TypeRoutine(string text)
        {
            _isTyping = true;
            _continueHint.enabled = false;
            _bodyLabel.text = text;
            _bodyLabel.ForceMeshUpdate();

            int total = _bodyLabel.textInfo.characterCount;

            // النص العربي (بعد التشكيل والترتيب البصري) بيترسم من الشمال لليمين،
            // لكن بدايته المنطقية على اليمين. فلو كشفنا الحروف بـmaxVisibleCharacters
            // (من أول السلسلة) هيبدأ يبان من آخر الجملة — غلط. الحل: نفتح نافذة
            // من آخر السلسلة بـfirstVisibleCharacter ونحرّكها، فتظهر الجملة
            // من أولها (يمين) لآخرها (شمال) زي الكتابة الحقيقية.
            bool visual = TLC.Settings.ArabicShaper.IsAlreadyVisual(text);

            if (visual)
            {
                _bodyLabel.firstVisibleCharacter = total;
                _bodyLabel.maxVisibleCharacters = total;
            }
            else
            {
                _bodyLabel.maxVisibleCharacters = 0;
            }

            float progress = 0f;

            while (progress < total)
            {
                progress += charsPerSecond * Time.unscaledDeltaTime;
                int shown = Mathf.FloorToInt(progress);
                if (visual) _bodyLabel.firstVisibleCharacter = Mathf.Max(0, total - shown);
                else        _bodyLabel.maxVisibleCharacters = shown;
                yield return null;
            }

            _bodyLabel.firstVisibleCharacter = 0;
            _bodyLabel.maxVisibleCharacters = total;
            _isTyping = false;
            _continueHint.enabled = true;
        }

        /// <summary>بينده زر/ضغطة المتابعة.</summary>
        public void OnAdvancePressed()
        {
            if (_isTyping)
            {
                if (_typing != null) StopCoroutine(_typing);
                _isTyping = false;
                _bodyLabel.ForceMeshUpdate();
                _bodyLabel.firstVisibleCharacter = 0;
                _bodyLabel.maxVisibleCharacters = _bodyLabel.textInfo.characterCount;
                _continueHint.enabled = true;
                return;
            }

            if (_runner != null) _runner.Advance();
        }

        // ────────────────────────── كارت الفصل ──────────────────────────

        public IEnumerator PlayChapterCard(string title, float seconds)
        {
            HideDialogue();
            HideChoices();
            _chapterLabel.text = title;
            _chapterCard.gameObject.SetActive(true);
            yield return new WaitForSeconds(seconds);
            _chapterCard.gameObject.SetActive(false);
        }

        // ────────────────────────── مساعدات ──────────────────────────

        void ShowDialogue()
        {
            _dialogueRoot.gameObject.SetActive(true);
            _continueHint.enabled = true;
        }

        void HideDialogue() => _dialogueRoot.gameObject.SetActive(false);

        void HideChoices() => _choiceRoot.gameObject.SetActive(false);

        void ClearChoices()
        {
            foreach (var b in _choiceButtons) if (b != null) Destroy(b.gameObject);
            _choiceButtons.Clear();
        }

        static string DisplayName(string key)
        {
            switch (key)
            {
                case "adam":   return "آدم";
                case "murad":  return "مراد";
                case "laila":  return "ليلى";
                case "yassin": return "ياسين";
                case "yusuf":  return "يوسف";
                case "caller": return "متصل مجهول";
                default:       return key;
            }
        }
    }
}
