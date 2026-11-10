using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLC.Core
{
    /// <summary>
    /// كل حالة اللعبة القابلة للحفظ: أعلام + متغيّرات + موضع السرد.
    /// دي الحقيقة الوحيدة — الواجهة والكاميرا والسرد بيقروا منها بس.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public const float StatMin = 0f;
        public const float StatMax = 100f;

        readonly Dictionary<string, float> _stats = new();
        readonly HashSet<string> _flags = new();

        public string storyId;
        public string nodeId;

        public event Action<string, float> OnStatChanged;
        public event Action<string, bool> OnFlagChanged;

        public IReadOnlyDictionary<string, float> Stats => _stats;
        public IReadOnlyCollection<string> Flags => _flags;

        // ---------- متغيّرات ----------
        public float Get(string key) => _stats.TryGetValue(key, out var v) ? v : 0f;

        public void SetStat(string key, float value)
        {
            float clamped = Mathf.Clamp(value, StatMin, StatMax);
            _stats[key] = clamped;
            OnStatChanged?.Invoke(key, clamped);
        }

        public void AddStat(string key, float delta) => SetStat(key, Get(key) + delta);

        // ---------- أعلام ----------
        public bool HasFlag(string flag) => _flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (_flags.Add(flag)) OnFlagChanged?.Invoke(flag, true);
        }

        public void ClearFlag(string flag)
        {
            if (_flags.Remove(flag)) OnFlagChanged?.Invoke(flag, false);
        }

        public bool HasAllFlags(params string[] flags)
        {
            foreach (var f in flags) if (!_flags.Contains(f)) return false;
            return true;
        }

        // ---------- مشاهدات ----------
        public readonly List<string> visitedNodes = new();
        public void MarkVisited(string id) { if (!string.IsNullOrEmpty(id)) visitedNodes.Add(id); }
        public bool HasVisited(string id) => visitedNodes.Contains(id);

        public void Reset()
        {
            _stats.Clear();
            _flags.Clear();
            visitedNodes.Clear();
            storyId = nodeId = null;
        }

        // ---------- تحويل للحفظ ----------
        public SaveDto ToDto()
        {
            var keys = new List<string>(_stats.Keys);
            var vals = new List<float>(_stats.Count);
            foreach (var k in keys) vals.Add(_stats[k]);

            return new SaveDto
            {
                storyId = storyId,
                nodeId = nodeId,
                flagList = new List<string>(_flags),
                statKeys = keys,
                statValues = vals,
                visited = new List<string>(visitedNodes)
            };
        }

        public void FromDto(SaveDto d)
        {
            Reset();
            if (d == null) return;

            storyId = d.storyId;
            nodeId  = d.nodeId;

            if (d.flagList != null) foreach (var f in d.flagList) _flags.Add(f);

            if (d.statKeys != null && d.statValues != null)
                for (int i = 0; i < d.statKeys.Count && i < d.statValues.Count; i++)
                    _stats[d.statKeys[i]] = d.statValues[i];

            if (d.visited != null) visitedNodes.AddRange(d.visited);
        }
    }

    [Serializable]
    public class SaveDto
    {
        public int version = 1;
        public string storyId;
        public string nodeId;
        public List<string> flagList = new();
        public List<string> statKeys = new();
        public List<float> statValues = new();
        public List<string> visited = new();
        public string savedAtUtc;
    }
}
