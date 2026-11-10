using System;
using System.Collections.Generic;
using UnityEngine;
using TLC.Core;

namespace TLC.Narrative
{
    public enum NodeKind
    {
        Line = 0,       // سطر حوار
        Choice = 1,     // قرار
        Branch = 2,     // تفريع شرطي
        Event = 3,      // حدث: انتظار تفاعل / سينمائية / صوت
        Ending = 4      // نهاية
    }

    [Serializable]
    public class StatRequirement
    {
        public string key;
        public string op = ">=";
        public float value;

        public bool IsMet(GameState s)
        {
            float v = s.Get(key);
            switch (op)
            {
                case ">=": return v >= value;
                case "<=": return v <= value;
                case ">":  return v > value;
                case "<":  return v < value;
                case "==": return Mathf.Abs(v - value) < 0.001f;
                case "!=": return Mathf.Abs(v - value) >= 0.001f;
                default:   return false;
            }
        }
    }

    [Serializable]
    public class Condition
    {
        public string[] allFlags  = Array.Empty<string>();
        public string[] anyFlags  = Array.Empty<string>();
        public string[] noneFlags = Array.Empty<string>();
        public StatRequirement[] stats = Array.Empty<StatRequirement>();

        public bool IsMet(GameState s)
        {
            if (s == null) return false;

            if (allFlags != null)
                foreach (var f in allFlags) if (!s.HasFlag(f)) return false;

            if (noneFlags != null)
                foreach (var f in noneFlags) if (s.HasFlag(f)) return false;

            if (anyFlags != null && anyFlags.Length > 0)
            {
                bool any = false;
                foreach (var f in anyFlags) { if (s.HasFlag(f)) { any = true; break; } }
                if (!any) return false;
            }

            if (stats != null)
                foreach (var r in stats) if (!r.IsMet(s)) return false;

            return true;
        }
    }

    [Serializable]
    public class StatEffect
    {
        public string key;
        public float add;
        public float set;
        public bool useSet;

        public void Apply(GameState s)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (useSet) s.SetStat(key, set);
            else s.AddStat(key, add);
        }
    }

    [Serializable]
    public class Effect
    {
        public StatEffect[] stats = Array.Empty<StatEffect>();
        public string[] setFlags = Array.Empty<string>();
        public string[] clearFlags = Array.Empty<string>();

        public void Apply(GameState s)
        {
            if (s == null) return;
            if (stats != null) foreach (var m in stats) m.Apply(s);
            if (setFlags != null) foreach (var f in setFlags) s.SetFlag(f);
            if (clearFlags != null) foreach (var f in clearFlags) s.ClearFlag(f);
        }
    }

    [Serializable]
    public class ChoiceOption
    {
        public string id;
        public string text;
        public string next;

        public Effect effect;
        public Condition require;
        public string decisionId;   // يتسجّل في سجل القرارات

        public bool IsAvailable(GameState s) => require == null || require.IsMet(s);
    }

    [Serializable]
    public class StoryNode
    {
        public string id;
        public int type;

        public string speaker;
        public string text;
        public string next;

        public Effect onEnter;
        public Condition require;
        public string elseNext;

        public ChoiceOption[] options = Array.Empty<ChoiceOption>();

        // Event
        public string action;   // "wait_interact" | "cinematic" | "audio" | "save"
        public string param;

        // Ending
        public string endingId;
        public string endingTitle;
        public string endingBody;

        public string act;
        public string camera;   // اسم اللقطة السينمائية

        public NodeKind Kind => (NodeKind)type;
    }

    [Serializable]
    public class StoryDocument
    {
        public int schemaVersion = 1;
        public string id;
        public string title;
        public string entry;
        public StoryNode[] nodes = Array.Empty<StoryNode>();
    }

    /// <summary>فهرس سريع + تحقق من سلامة الشجرة وقت التحميل.</summary>
    public class CompiledStory
    {
        readonly Dictionary<string, StoryNode> _index = new Dictionary<string, StoryNode>();

        public StoryDocument Document { get; private set; }
        public int CorruptionCount { get; private set; }

        public static CompiledStory FromJson(string json)
        {
            var doc = JsonUtility.FromJson<StoryDocument>(json);
            var story = new CompiledStory { Document = doc };
            if (doc?.nodes != null)
                foreach (var n in doc.nodes)
                    if (!string.IsNullOrEmpty(n.id)) story._index[n.id] = n;
            story.Validate();
            return story;
        }

        void Validate()
        {
            CorruptionCount = 0;
            if (Document?.nodes == null) { CorruptionCount++; return; }

            foreach (var n in Document.nodes)
            {
                if (!string.IsNullOrEmpty(n.next) && !_index.ContainsKey(n.next))
                {
                    Debug.LogError($"[TLC/Story] العقدة '{n.id}' بتشاور على '{n.next}' — مش موجودة.");
                    CorruptionCount++;
                }
                if (n.options != null)
                    foreach (var o in n.options)
                        if (!string.IsNullOrEmpty(o.next) && !_index.ContainsKey(o.next))
                        {
                            Debug.LogError($"[TLC/Story] خيار في '{n.id}' بيشاور على '{o.next}' — مش موجود.");
                            CorruptionCount++;
                        }
            }

            if (!string.IsNullOrEmpty(Document.entry) && !_index.ContainsKey(Document.entry))
            {
                Debug.LogError($"[TLC/Story] نقطة البداية '{Document.entry}' مش موجودة.");
                CorruptionCount++;
            }
        }

        public StoryNode Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _index.TryGetValue(id, out var n) ? n : null;
        }

        public IEnumerable<StoryNode> Nodes => _index.Values;
    }
}
