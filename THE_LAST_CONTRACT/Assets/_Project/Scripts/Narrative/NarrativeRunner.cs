using System;
using System.Collections.Generic;
using UnityEngine;
using TLC.Core;

namespace TLC.Narrative
{
    /// <summary>
    /// مشغّل القصة. بيمشي على العقد، بيطبّق الآثار، وبيبلّغ الواجهة والعالم.
    /// السرد مش عارف حاجة عن الـ 3D — والعالم مش عارف حاجة عن القصة.
    /// الرابط الوحيد: Events + NotifyInteraction().
    /// </summary>
    public class NarrativeRunner : MonoBehaviour
    {
        public const string ActWaitInteract = "wait_interact";
        public const string ActCinematic   = "cinematic";
        public const string ActAudio       = "audio";
        public const string ActSave        = "save";

        GameState _state;
        CompiledStory _story;

        public GameState State => _state;
        public CompiledStory Story => _story;

        public event Action<StoryNode> LineShown;
        public event Action<StoryNode, List<ChoiceOption>> ChoicesShown;
        public event Action<StoryNode> EndingReached;
        public event Action<string, string> EventRaised;    // action, param

        public bool IsWaitingForInput { get; private set; }
        public bool IsBlocked { get; private set; }          // أثناء سينمائية أو انتظار تفاعل

        List<ChoiceOption> _currentChoices;
        string _pendingInteractId;
        StoryNode _pendingEvent;

        // ────────────────────────── التهيئة ──────────────────────────

        public bool Initialize(GameState state, string json)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));

            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[TLC] نص القصة فاضي.");
                return false;
            }

            _story = CompiledStory.FromJson(json);

            if (_story.Document == null)
            {
                Debug.LogError("[TLC] فشل تحليل JSON القصة.");
                return false;
            }

            if (_story.CorruptionCount > 0)
                Debug.LogWarning($"[TLC] القصة فيها {_story.CorruptionCount} مشكلة — راجع اللوج فوق.");

            return true;
        }

        public void Begin()
        {
            if (_story == null) { Debug.LogError("[TLC] NarrativeRunner مش متهيّئ."); return; }
            GoTo(_story.Document.entry);
        }

        /// <summary>يحمّل فصل جديد ويصفّر مرجع السرد — نفس المشغّل، قصة تانية.</summary>
        public bool SwitchStory(string json)
        {
            _currentChoices = null;
            _pendingEvent = null;
            _pendingInteractId = null;
            IsWaitingForInput = false;
            IsBlocked = false;
            return Initialize(_state, json);
        }

        /// <summary>يبدأ السرد من عقدة بعينها (نقاط دخول الفصول).</summary>
        public void BeginAt(string nodeId)
        {
            if (_story == null) { Debug.LogError("[TLC] مفيش قصة محمّلة."); return; }
            GoTo(nodeId);
        }

        public void ResumeFromSave()
        {
            if (_state == null || string.IsNullOrEmpty(_state.CurrentNodeId)) { Begin(); return; }
            GoTo(_state.CurrentNodeId);
        }

        // ────────────────────────── المشي على الشجرة ──────────────────────────

        void GoTo(string nodeId)
        {
            var node = _story?.Get(nodeId);
            if (node == null)
            {
                Debug.LogError($"[TLC] العقدة '{nodeId}' مش موجودة — السرد وقف.");
                return;
            }

            _state.CurrentNodeId = nodeId;

            if (node.require != null && !node.require.IsMet(_state))
            {
                if (!string.IsNullOrEmpty(node.elseNext)) { GoTo(node.elseNext); return; }
                Debug.LogWarning($"[TLC] العقدة '{nodeId}' شرطها مش متحقق ومفيش elseNext.");
                return;
            }

            node.onEnter?.Apply(_state);

            switch (node.Kind)
            {
                case NodeKind.Line:    ShowLine(node); break;
                case NodeKind.Choice:  ShowChoices(node); break;
                case NodeKind.Branch:  EvaluateBranch(node); break;
                case NodeKind.Event:   RaiseEvent(node); break;
                case NodeKind.Ending:  ShowEnding(node); break;
            }
        }

        void ShowLine(StoryNode node)
        {
            IsWaitingForInput = true;
            IsBlocked = false;
            _currentChoices = null;
            LineShown?.Invoke(node);
        }

        void ShowChoices(StoryNode node)
        {
            _currentChoices = new List<ChoiceOption>();
            if (node.options != null)
                foreach (var o in node.options)
                    if (o.IsAvailable(_state)) _currentChoices.Add(o);

            if (_currentChoices.Count == 0)
            {
                Debug.LogWarning($"[TLC] عقدة قرار '{node.id}' مفيش فيها خيار متاح → next.");
                GoTo(node.next);
                return;
            }

            IsWaitingForInput = true;
            IsBlocked = false;
            ChoicesShown?.Invoke(node, _currentChoices);
        }

        void EvaluateBranch(StoryNode node)
        {
            if (node.options != null)
                foreach (var b in node.options)
                    if (b.IsAvailable(_state)) { GoTo(b.next); return; }

            GoTo(node.next);
        }

        void RaiseEvent(StoryNode node)
        {
            switch (node.action)
            {
                case ActWaitInteract:
                    _pendingInteractId = node.param;
                    _pendingEvent = node;
                    IsWaitingForInput = false;
                    IsBlocked = true;
                    EventRaised?.Invoke(node.action, node.param);
                    break;

                case ActCinematic:
                case ActAudio:
                    _pendingEvent = node;
                    IsWaitingForInput = false;
                    IsBlocked = true;
                    EventRaised?.Invoke(node.action, node.param);
                    break;

                default:
                    EventRaised?.Invoke(node.action, node.param);
                    GoTo(node.next);
                    break;
            }
        }

        void ShowEnding(StoryNode node)
        {
            IsWaitingForInput = false;
            IsBlocked = true;
            _state.RecordDecision($"ENDING::{node.endingId}");
            EndingReached?.Invoke(node);
        }

        // ────────────────────────── مدخلات ──────────────────────────

        /// <summary>اللاعب ضغط للمتابعة.</summary>
        public void Advance()
        {
            if (!IsWaitingForInput || _currentChoices != null) return;
            var n = _story.Get(_state.CurrentNodeId);
            if (n != null && !string.IsNullOrEmpty(n.next)) GoTo(n.next);
        }

        public void Choose(int index)
        {
            if (_currentChoices == null) return;
            if (index < 0 || index >= _currentChoices.Count) return;

            var opt = _currentChoices[index];
            _currentChoices = null;
            IsWaitingForInput = false;

            opt.effect?.Apply(_state);

            if (!string.IsNullOrEmpty(opt.decisionId))
                _state.RecordDecision(opt.decisionId);

            SaveSystem.Save(SaveSystem.AutoSlot, _state);
            GoTo(opt.next);
        }

        /// <summary>بينده الـ hotspot في العالم لما اللاعب يتفاعل.</summary>
        public void NotifyInteraction(string hotspotId)
        {
            if (!IsBlocked || _pendingEvent == null) return;
            if (_pendingEvent.action != ActWaitInteract) return;
            if (_pendingInteractId != hotspotId) return;

            var n = _pendingEvent;
            _pendingEvent = null;
            _pendingInteractId = null;
            IsBlocked = false;
            GoTo(n.next);
        }

        /// <summary>بينده مدير السينمائية لما اللقطة تخلص.</summary>
        public void ResumeAfterEvent()
        {
            if (_pendingEvent == null) return;
            var n = _pendingEvent;
            _pendingEvent = null;
            IsBlocked = false;
            GoTo(n.next);
        }

        public string PendingInteractionId => _pendingInteractId;
    }
}
