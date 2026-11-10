using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TLC.Narrative;

namespace TLC.UI
{
    /// <summary>
    /// يبني أزرار القرارات ديناميكياً من العقدة.
    /// الزر بيوضّح المتغيّر اللي هيتأثر — اختياري، اقفله لو عايز اللاعب يختار بالحس.
    /// </summary>
    public class ChoicePresenter : MonoBehaviour
    {
        [Header("مراجع")]
        [SerializeField] StoryRunner runner;
        [SerializeField] RectTransform container;
        [SerializeField] Button buttonPrefab;         // داخله TMP_Text
        [SerializeField] CanvasGroup group;

        [Header("الإعدادات")]
        [Tooltip("لو مفعّل، بيظهر تحت كل خيار أثر المتغيّر (زي Detroit)")]
        [SerializeField] bool showConsequences = false;
        [SerializeField] float appearDelay = 0.15f;

        readonly List<Button> _spawned = new();

        void OnEnable()  => runner.OnChoices += Build;
        void OnDisable() => runner.OnChoices -= Build;

        void Build(StoryNode node, List<ChoiceOption> options)
        {
            Clear();
            StartCoroutine(BuildRoutine(options));
        }

        System.Collections.IEnumerator BuildRoutine(List<ChoiceOption> options)
        {
            yield return new WaitForSeconds(appearDelay);

            for (int i = 0; i < options.Count; i++)
            {
                int captured = i;
                var opt = options[i];

                var btn = Instantiate(buttonPrefab, container);
                btn.gameObject.SetActive(true);

                var label = btn.GetComponentInChildren<TMP_Text>();
                if (label)
                {
                    label.text = opt.text;
                    if (showConsequences && opt.effect != null && opt.effect.stats != null)
                        label.text += "\n" + DescribeEffects(opt.effect);
                }

                btn.onClick.AddListener(() => { Clear(); runner.Choose(captured); });
                _spawned.Add(btn);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        static string DescribeEffects(Effect e)
        {
            if (e.stats == null || e.stats.Length == 0) return "";
            var sb = new System.Text.StringBuilder("<size=70%><i>");
            foreach (var m in e.stats)
            {
                float d = m.useSet ? 0 : m.add;
                if (Mathf.Approximately(d, 0f)) continue;
                string sign = d > 0 ? "+" : "";
                sb.Append($"{m.key} {sign}{d:0}   ");
            }
            sb.Append("</i></size>");
            return sb.ToString();
        }

        void Clear()
        {
            foreach (var b in _spawned) if (b) Destroy(b.gameObject);
            _spawned.Clear();
        }
    }
}
