using System;
using System.Collections.Generic;
using UnityEngine;
using TLC.Core;

namespace TLC.Narrative
{
    /// <summary>
    /// مشغّل السرد: بيمشي على عقد القصة، بيطبّق الآثار، وبيبلّغ الواجهة.
    /// الواجهة بتستدعي Advance() و Choose(i) — ومش عارفة حاجة عن شكل الشجرة.
    /// </summary>
    public class StoryRunner : MonoBehaviour
    {
        [Header("القصة")]
        [SerializeField] StoryAsset story;
        [SerializeField] bool autoSaveEveryNode = false;
        [SerializeField] int autoSaveSlot = SaveSystem.AutoSlot;

        [Header("مخرجات للأحداث")]
        [Tooltip("لو مفعّل، العقد Event بتوقف السرد لحين ما الحدث يخلص")]
        [SerializeField] bool blockOnEvents = true;

        public GameState State { get; private set; } = new();
        public StoryAsset Story => story;

        /// <summary>سطر جديد معروض — speaker فاضي يعني رواية.</summary>
        public event Action<StoryNode> OnLine;

        /// <summary>قرار معروض. اللاعب يرد بـ Choose(index).</summary>
        public event Action<StoryNode, List<ChoiceOption>> OnChoices;

        /// <summary>حدث خارج السرد (سينمائية، تحميل مشهد، QTE...).</summary>
        public event Action<string, string> OnEvent;

        /// <summary>وصلنا لنهاية.</summary>
        public event Action<StoryNode> OnEnding;

        public bool IsWaitingForInput { get; private set; }

        List<ChoiceOption> _currentOptions;

        void Awake()   { GameStateBus.Register(State); }
        void OnDestroy(){ GameStateBus.Unregister(State); }

        // ---------------- دورة الحياة ----------------

        public void Begin()
        {
            if (story == null) { Debug.LogError("[TLC] مفيش StoryAsset متسند."); return; }
            State.Reset();
            ApplyInitialStats();
            GoTo(story.Data.entry);
        }

        public void BeginFromSave(int slot)
        {
            if (story == null) { Debug.LogError("[TLC] مفيش StoryAsset متسند."); return; }
            if (!SaveSystem.Load(slot, State)) { Begin(); return; }
            GoTo(State.nodeId);
        }

        void ApplyInitialStats()
        {
            // القيم الابتدائية لشبكة العلاقات — مطابقة لدستور الشخصيات
            State.SetStat("bond.ziad", 60f);
            State.SetStat("trust.nadine", 10f);
            State.SetStat("fear.lotfy", 40f);
            State.SetStat("adam.resolve", 50f);
            State.SetStat("adam.ghost", 0f);
            State.SetStat("evidence.count", 0f);
        }

        // ---------------- المشي على الشجرة ----------------

        public void GoTo(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return;

            var node = story.Get(nodeId);
            if (node == null)
            {
                Debug.LogError($"[TLC] العقدة '{nodeId}' مش موجودة. السرد وقف.");
                return;
            }

            State.storyId = story.Data.id;
            State.nodeId  = nodeId;
            State.MarkVisited(nodeId);

            // شرط الدخول: لو مش متحقق، نروح للبديل
            if (node.require != null && !node.require.IsMet(State))
            {
                if (!string.IsNullOrEmpty(node.elseNext)) { GoTo(node.elseNext); return; }
                Debug.LogWarning($"[TLC] العقدة '{nodeId}' شرطها مش متحقق ومفيش elseNext — تخطّي.");
                return;
            }

            node.onEnter?.Apply(State);

            if (autoSaveEveryNode) SaveSystem.Save(autoSaveSlot, State);

            switch (node.type)
            {
                case NodeType.Line:
                    ShowLine(node);
                    break;

                case NodeType.Choice:
                    ShowChoices(node);
                    break;

                case NodeType.Branch:
                    EvaluateBranches(node);
                    break;

                case NodeType.Event:
                    FireEvent(node);
                    break;

                case NodeType.End:
                    IsWaitingForInput = false;
                    OnEnding?.Invoke(node);
                    break;
            }
        }

        void ShowLine(StoryNode node)
        {
            IsWaitingForInput = true;
            _currentOptions = null;
            OnLine?.Invoke(node);
        }

        void ShowChoices(StoryNode node)
        {
            _currentOptions = new List<ChoiceOption>();
            foreach (var o in node.options)
                if (o.IsAvailable(State)) _currentOptions.Add(o);

            if (_currentOptions.Count == 0)
            {
                Debug.LogWarning($"[TLC] عقدة القرار '{node.id}' مفيش فيها خيار متاح. رايحة لـ next.");
                GoTo(node.next);
                return;
            }

            IsWaitingForInput = true;
            OnChoices?.Invoke(node, _currentOptions);
        }

        void EvaluateBranches(StoryNode node)
        {
            foreach (var b in node.branches)
                if (b.require == null || b.require.IsMet(State)) { GoTo(b.next); return; }

            GoTo(node.next); // الافتراضي
        }

        void FireEvent(StoryNode node)
        {
            // الحدث بيطبّق آثاره الأول — يسمح لـ QTE إنه يحدد نتيجة قبل يبدأ
            IsWaitingForInput = blockOnEvents;
            OnEvent?.Invoke(node.action, node.param);
            if (blockOnEvents) _pendingEventNode = node;
            else GoTo(node.next);
        }

        StoryNode _pendingEventNode;

        /// <summary>بينده الـ event handler لما يخلص (مثلاً بعد سينمائية).</summary>
        public void ResumeAfterEvent()
        {
            if (_pendingEventNode == null) return;
            var n = _pendingEventNode;
            _pendingEventNode = null;
            IsWaitingForInput = false;
            GoTo(n.next);
        }

        // ---------------- مدخلات اللاعب ----------------

        /// <summary>اللاعب ضغط على الشاشة يكمل السطر.</summary>
        public void Advance()
        {
            if (!IsWaitingForInput) return;
            if (_currentOptions != null) return;   // لازم يختار

            GoTo(State.nodeId != null ? story.Get(State.nodeId)?.next : null);
        }

        /// <summary>اللاعب اختار خيار.</summary>
        public void Choose(int index)
        {
            if (_currentOptions == null) return;
            if (index < 0 || index >= _currentOptions.Count) return;

            var opt = _currentOptions[_currentOptions.Count > index ? index : 0];
            _currentOptions = null;
            IsWaitingForInput = false;

            opt.effect?.Apply(State);
            if (opt.once) opt.consumed = true;

            SaveSystem.Save(SaveSystem.AutoSlot, State); // نقطة قرار = نقطة حفظ
            GoTo(opt.next);
        }
    }
}
