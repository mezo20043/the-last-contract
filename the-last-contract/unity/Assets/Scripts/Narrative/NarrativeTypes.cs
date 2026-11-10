using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLC.Narrative
{
    public enum NodeType { Line, Choice, Branch, Event, End }

    /// <summary>شرط واحد على متغيّر رقمي (ثقة، خوف، يقين...).</summary>
    [Serializable]
    public class StatReq
    {
        public string key;              // "trust.nadine"
        public string op = ">=";        // >=  <=  >  <  ==  !=
        public float value;

        public bool IsMet(TLC.Core.GameState s)
        {
            float v = s.Get(key);
            return op switch
            {
                ">=" => v >= value,
                "<=" => v <= value,
                ">"  => v >  value,
                "<"  => v <  value,
                "==" => Mathf.Approximately(v, value),
                "!=" => !Mathf.Approximately(v, value),
                _    => false
            };
        }
    }

    /// <summary>مجموعة شروط — كلها لازم تتحقق.</summary>
    [Serializable]
    public class Condition
    {
        public string[] allFlags  = Array.Empty<string>();
        public string[] anyFlags  = Array.Empty<string>();
        public string[] noneFlags = Array.Empty<string>();
        public StatReq[] stats    = Array.Empty<StatReq>();

        public bool IsEmpty =>
            allFlags.Length == 0 && anyFlags.Length == 0 &&
            noneFlags.Length == 0 && stats.Length == 0;

        public bool IsMet(TLC.Core.GameState s)
        {
            if (IsEmpty) return true;

            foreach (var f in allFlags)  if (!s.HasFlag(f)) return false;
            foreach (var f in noneFlags) if (s.HasFlag(f))  return false;

            if (anyFlags.Length > 0)
            {
                bool any = false;
                foreach (var f in anyFlags) if (s.HasFlag(f)) { any = true; break; }
                if (!any) return false;
            }

            foreach (var r in stats) if (!r.IsMet(s)) return false;
            return true;
        }
    }

    /// <summary>تعديل واحد على متغيّر.</summary>
    [Serializable]
    public class StatMod
    {
        public string key;
        public float add;          // إضافة (تستخدم لو useSet = false)
        public float set;          // تعيين مباشر
        public bool useSet;
    }

    /// <summary>ما يحدث عند المرور على عقدة أو اختيار.</summary>
    [Serializable]
    public class Effect
    {
        public string[] setFlags   = Array.Empty<string>();
        public string[] clearFlags = Array.Empty<string>();
        public StatMod[] stats     = Array.Empty<StatMod>();

        public void Apply(TLC.Core.GameState s)
        {
            foreach (var f in setFlags)   s.SetFlag(f);
            foreach (var f in clearFlags) s.ClearFlag(f);

            foreach (var m in stats)
            {
                if (m.useSet) s.SetStat(m.key, m.set);
                else          s.AddStat(m.key, m.add);
            }
        }
    }

    /// <summary>خيار واحد في قائمة قرارات.</summary>
    [Serializable]
    public class ChoiceOption
    {
        public string id;
        [TextArea(1, 3)] public string text;
        public string next;

        public Effect effect;
        public Condition require;    // الخيار ما يظهرش غير لو اتحقق
        public Condition lockout;    // الخيار يختفي لو اتحقق
        public bool once;            // يظهر مرة واحدة في اللعبة

        [NonSerialized] public bool consumed;

        public bool IsAvailable(TLC.Core.GameState s)
        {
            if (once && consumed) return false;
            if (require != null && !require.IsMet(s)) return false;
            if (lockout != null && lockout.IsMet(s)) return false;
            return true;
        }
    }

    [Serializable]
    public class StoryNode
    {
        public string id;
        public NodeType type = NodeType.Line;

        [Header("Line")]
        public string speaker;                  // فاضي = رواية
        [TextArea(2, 6)] public string text;
        public string next;

        [Header("منطق مشترك")]
        public Effect onEnter;
        public Condition require;               // لو مش متحقق → elseNext
        public string elseNext;

        [Header("Choice")]
        public List<ChoiceOption> options = new();

        [Header("Branch")]
        public List<ChoiceOption> branches = new();

        [Header("Event")]
        public string action;                   // "cinematic" / "loadScene" / "qte" / "camera"
        public string param;

        [Header("End")]
        public string endingId;
        public string endingTitle;
        [TextArea(2, 5)] public string endingBody;
    }

    [CreateAssetMenu(menuName = "TLC/Story Asset", fileName = "Story")]
    public class StoryAsset : ScriptableObject
    {
        public TextAsset json;

        [NonSerialized] StoryDocument _doc;
        [NonSerialized] Dictionary<string, StoryNode> _index;

        public StoryDocument Doc
        {
            get
            {
                if (_doc == null)
                {
                    _doc = JsonUtility.FromJson<StoryDocument>(json.text);
                    BuildIndex();
                    Validate();
                }
                return _doc;
            }
        }

        void BuildIndex()
        {
            _index = new Dictionary<string, StoryNode>();
            foreach (var n in _doc.nodes) _index[n.id] = n;
        }

        void Validate()
        {
            foreach (var n in _doc.nodes)
            {
                if (n.type == NodeType.Line && !string.IsNullOrEmpty(n.next) && !_index.ContainsKey(n.next))
                    Debug.LogError($"[TLC] العقدة '{n.id}' بتشاور على '{n.next}' اللي مش موجود.");
                if (n.type == NodeType.Choice)
                    foreach (var o in n.options)
                        if (!string.IsNullOrEmpty(o.next) && !_index.ContainsKey(o.next))
                            Debug.LogError($"[TLC] الخيار في '{n.id}' بيشاور على '{o.next}' اللي مش موجود.");
            }
        }

        public StoryNode Get(string id)
        {
            var _ = Doc; // يضمن البناء
            return _index != null && _index.TryGetValue(id, out var n) ? n : null;
        }

        public StoryDocument Data => Doc;
    }

    [Serializable]
    public class StoryDocument
    {
        public int schemaVersion = 1;
        public string id;
        public string title;
        public string entry;
        public List<StoryNode> nodes = new();
    }
}
