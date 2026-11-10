using System.Collections;
using UnityEngine;
using TMPro;
using TLC.Narrative;

namespace TLC.UI
{
    /// <summary>
    /// عرض السطر: آلة كاتبة + ضغطة تكمل الكتابة + ضغطة تانية تقدّم.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DialoguePresenter : MonoBehaviour
    {
        [Header("مراجع")]
        [SerializeField] StoryRunner runner;
        [SerializeField] TMP_Text speakerLabel;
        [SerializeField] TMP_Text bodyLabel;
        [SerializeField] GameObject tapToContinueHint;
        [SerializeField] CanvasGroup choiceGroup;

        [Header("السرعة")]
        [SerializeField] float charsPerSecond = 42f;
        [SerializeField] float holdAfterTyping = 0f;

        CanvasGroup _group;
        Coroutine _typing;
        bool _isTyping;

        void Awake() => _group = GetComponent<CanvasGroup>();

        void OnEnable()
        {
            runner.OnLine    += HandleLine;
            runner.OnChoices += HandleChoices;
            runner.OnEnding  += HandleEnding;
        }

        void OnDisable()
        {
            runner.OnLine    -= HandleLine;
            runner.OnChoices -= HandleChoices;
            runner.OnEnding  -= HandleEnding;
        }

        void HandleLine(StoryNode node)
        {
            SetChoiceVisible(false);
            ShowPanel(true);

            bool isNarration = string.IsNullOrEmpty(node.speaker);

            if (speakerLabel)
            {
                speakerLabel.gameObject.SetActive(!isNarration);
                speakerLabel.text = node.speaker;
            }

            if (bodyLabel)
            {
                bodyLabel.fontStyle = isNarration ? FontStyles.Italic : FontStyles.Normal;
                bodyLabel.color = isNarration
                    ? new Color(0.78f, 0.78f, 0.80f)
                    : Color.white;
            }

            if (_typing != null) StopCoroutine(_typing);
            _typing = StartCoroutine(TypeLine(node.text));
        }

        void HandleChoices(StoryNode node, System.Collections.Generic.List<ChoiceOption> options)
        {
            ShowPanel(false);
            SetChoiceVisible(true);
            // العارض بتاع الخيارات بيملا القائمة
        }

        void HandleEnding(StoryNode node)
        {
            ShowPanel(false);
            SetChoiceVisible(false);
        }

        IEnumerator TypeLine(string full)
        {
            _isTyping = true;
            if (tapToContinueHint) tapToContinueHint.SetActive(false);

            if (bodyLabel)
            {
                bodyLabel.text = full;
                bodyLabel.maxVisibleCharacters = 0;
                bodyLabel.ForceMeshUpdate();

                int total = bodyLabel.textInfo.characterCount;
                float t = 0f;

                while (bodyLabel.maxVisibleCharacters < total)
                {
                    t += Time.deltaTime * charsPerSecond;
                    bodyLabel.maxVisibleCharacters = Mathf.FloorToInt(t);
                    yield return null;
                }
                bodyLabel.maxVisibleCharacters = total;
            }

            _isTyping = false;
            if (holdAfterTyping > 0f) yield return new WaitForSeconds(holdAfterTyping);
            if (tapToContinueHint) tapToContinueHint.SetActive(true);
        }

        /// <summary>تُربط بـ Button أو منطقة ضغط في الشاشة.</summary>
        public void OnTap()
        {
            if (_isTyping)
            {
                // اقفل الكتابة فوراً
                if (_typing != null) StopCoroutine(_typing);
                if (bodyLabel)
                {
                    bodyLabel.ForceMeshUpdate();
                    bodyLabel.maxVisibleCharacters = bodyLabel.textInfo.characterCount;
                }
                _isTyping = false;
                if (tapToContinueHint) tapToContinueHint.SetActive(true);
                return;
            }

            runner.Advance();
        }

        void ShowPanel(bool v)
        {
            if (_group)
            {
                _group.alpha = v ? 1f : 0f;
                _group.blocksRaycasts = v;
                _group.interactable = v;
            }
        }

        void SetChoiceVisible(bool v)
        {
            if (!choiceGroup) return;
            choiceGroup.alpha = v ? 1f : 0f;
            choiceGroup.blocksRaycasts = v;
            choiceGroup.interactable = v;
        }
    }
}
