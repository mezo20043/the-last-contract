using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLC.Core
{
    /// <summary>
    /// الحالة الوحيدة القابلة للحفظ: علاقات + أعلام + سجل قرارات.
    /// كل الأنظمة التانية بتقرأ من هنا وبتكتب هنا — مفيش حالة مبعوتة في المشهد.
    ///
    /// المتغيّرات الستة المطلوبة في التصميم:
    ///   Trust_Murad, Trust_Laila, Trust_Yassin,
    ///   Family_Truth, Yusuf_Connection, Organization_Suspicion
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const float StatMin = 0f;
        public const float StatMax = 100f;

        public const string TrustMurad   = "Trust_Murad";
        public const string TrustLaila   = "Trust_Laila";
        public const string TrustYassin  = "Trust_Yassin";
        public const string FamilyTruth  = "Family_Truth";
        public const string YusufConn    = "Yusuf_Connection";
        public const string OrgSuspicion = "Organization_Suspicion";

        public static readonly string[] AllStats =
        {
            TrustMurad, TrustLaila, TrustYassin, FamilyTruth, YusufConn, OrgSuspicion
        };

        readonly Dictionary<string, float> _stats = new Dictionary<string, float>();
        readonly HashSet<string> _flags = new HashSet<string>();
        readonly List<string> _decisions = new List<string>();

        public string ChapterId;
        public string CurrentNodeId;

        public event Action<string, float> StatChanged;
        public event Action<string> FlagSet;
        public event Action<string> DecisionRecorded;

        public IReadOnlyDictionary<string, float> Stats => _stats;
        public IReadOnlyCollection<string> Flags => _flags;
        public IReadOnlyList<string> Decisions => _decisions;

        // ────────────────────────── متغيّرات ──────────────────────────

        public float Get(string key) => _stats.TryGetValue(key, out var v) ? v : 0f;

        public void SetStat(string key, float value)
        {
            float c = Mathf.Clamp(value, StatMin, StatMax);
            _stats[key] = c;
            StatChanged?.Invoke(key, c);
        }

        public void AddStat(string key, float delta) => SetStat(key, Get(key) + delta);

        // ────────────────────────── أعلام ──────────────────────────

        public bool HasFlag(string flag) => !string.IsNullOrEmpty(flag) && _flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (_flags.Add(flag)) FlagSet?.Invoke(flag);
        }

        public void ClearFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag)) _flags.Remove(flag);
        }

        public bool HasAllFlags(string[] flags)
        {
            if (flags == null) return true;
            foreach (var f in flags) if (!HasFlag(f)) return false;
            return true;
        }

        // ────────────────────────── قرارات ──────────────────────────

        public void RecordDecision(string decisionId)
        {
            if (string.IsNullOrEmpty(decisionId)) return;
            if (_decisions.Contains(decisionId)) return;
            _decisions.Add(decisionId);
            DecisionRecorded?.Invoke(decisionId);
        }

        public bool DecidedBefore(string decisionId) => _decisions.Contains(decisionId);

        // ────────────────────────── دورة الحياة ──────────────────────────

        public void ResetToDefaults()
        {
            _stats.Clear();
            _flags.Clear();
            _decisions.Clear();
            ChapterId = null;
            CurrentNodeId = null;

            SetStat(TrustMurad, 55f);
            SetStat(TrustLaila, 40f);
            SetStat(TrustYassin, 20f);
            SetStat(FamilyTruth, 0f);
            SetStat(YusufConn, 0f);
            SetStat(OrgSuspicion, 25f);
        }

        // ── A1: ربط العقدة بفصلها (شبكة أمان للتوافق مع الحفظ القديم) ──
        // الحقل ChapterId بيُكتب من GameBootstrap وقت الحفظ. لو حفظ قديم/تالف جه
        // بلا ChapterId، بنستنتج الفصل من بادئة العقدة (c1_ / ch2_ / ch3_) —
        // كل العقد في الفصول التلاتة بتتبع البادئة دي، وما فيهاش تعارض.
        public static readonly string[] ChapterIds = { "chapter01", "chapter02", "chapter03" };

        /// <summary>"ch2_c3" → "chapter02" · أي حاجة غير معروفة → null (بدون تخمين).</summary>
        public static string ChapterOfNodeId(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            if (nodeId.StartsWith("c1_"))  return ChapterIds[0];
            if (nodeId.StartsWith("ch2_")) return ChapterIds[1];
            if (nodeId.StartsWith("ch3_")) return ChapterIds[2];
            return null;
        }

        // ────────────────────────── حفظ ──────────────────────────

        public GameStateDto ToDto()
        {
            var keys = new List<string>(_stats.Keys);
            var vals = new List<float>(keys.Count);
            foreach (var k in keys) vals.Add(_stats[k]);

            return new GameStateDto
            {
                chapterId = ChapterId,
                chapterOfNode = ChapterOfNodeId(CurrentNodeId), // A1
                currentNodeId = CurrentNodeId,
                statKeys = keys,
                statValues = vals,
                flagList = new List<string>(_flags),
                decisions = new List<string>(_decisions),
                savedAtUtc = DateTime.UtcNow.ToString("o")
            };
        }

        public void FromDto(GameStateDto d)
        {
            ResetToDefaults();
            if (d == null) return;

            ChapterId = d.chapterId;
            CurrentNodeId = d.currentNodeId;

            if (d.statKeys != null && d.statValues != null)
            {
                int n = Mathf.Min(d.statKeys.Count, d.statValues.Count);
                for (int i = 0; i < n; i++) _stats[d.statKeys[i]] = d.statValues[i];
            }

            if (d.flagList != null) foreach (var f in d.flagList) _flags.Add(f);
            if (d.decisions != null) _decisions.AddRange(d.decisions);
        }
    }

    [Serializable]
    public class GameStateDto
    {
        public int version = 1;
        public string chapterId;

        // A1: الفصل المستنتَج من العقدة. اختياري تمامًا:
        //   · الحفظ القديم (بلا الحقل) ⇒ JsonUtility بيسيبه null ⇒ الكود بيتجاهله.
        //   · الحفظ الجديد ⇒ بيتكتب دايمًا من CurrentNodeId.
        //   · version زي ما هي (1) عشان الأكواد القديمة ما ترفضش الحفظ.
        public string chapterOfNode;

        public string currentNodeId;
        public List<string> statKeys = new List<string>();
        public List<float> statValues = new List<float>();
        public List<string> flagList = new List<string>();
        public List<string> decisions = new List<string>();
        public string savedAtUtc;
    }
}
