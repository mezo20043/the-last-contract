using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TLC.Core
{
    /// <summary>
    /// تقدّم اللاعب الدائم: الفصول المفتوحة والنهايات المكتشفة.
    /// منفصل عن GameState (اللي بيتصفّر مع كل لعبة جديدة).
    /// </summary>
    public static class Progress
    {
        [Serializable]
        public class Data
        {
            public int unlockedChapters = 1;          // 1..3
            public List<string> seenEndings = new List<string>();
            public string lastChapter = "chapter01";
            public int gamesCompleted;
        }

        static Data _data;
        static string PathFile =>
            Path.Combine(Application.persistentDataPath, "tlc", "progress.json");

        public static Data Current
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static void Reset()
        {
            _data = new Data();
            Save();
        }

        public static bool IsChapterUnlocked(int chapterNumber) =>
            chapterNumber >= 1 && chapterNumber <= Current.unlockedChapters;

        public static void UnlockChapter(int chapterNumber)
        {
            var d = Current;
            if (chapterNumber <= d.unlockedChapters) return;
            d.unlockedChapters = Mathf.Clamp(chapterNumber, 1, 3);
            Save();
        }

        public static bool HasSeenEnding(string endingId) =>
            !string.IsNullOrEmpty(endingId) && Current.seenEndings.Contains(endingId);

        public static bool MarkEnding(string endingId)
        {
            if (string.IsNullOrEmpty(endingId)) return false;
            var d = Current;
            if (d.seenEndings.Contains(endingId)) return false;
            d.seenEndings.Add(endingId);
            Save();
            return true;      // نهاية جديدة اتكشفت
        }

        public static int SeenCount => Current.seenEndings.Count;

        public static void SetLastChapter(string chapterResource)
        {
            if (string.IsNullOrEmpty(chapterResource)) return;
            Current.lastChapter = chapterResource;
            Save();
        }

        public static string LastChapter => Current.lastChapter;

        public static bool Save()
        {
            try
            {
                var p = PathFile;
                Directory.CreateDirectory(Path.GetDirectoryName(p));
                File.WriteAllText(p, JsonUtility.ToJson(Current, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TLC] فشل حفظ التقدّم: {e.Message}");
                return false;
            }
        }

        public static bool Load()
        {
            try
            {
                var p = PathFile;
                if (File.Exists(p))
                {
                    var json = File.ReadAllText(p);
                    var d = JsonUtility.FromJson<Data>(json);
                    if (d != null && d.unlockedChapters > 0)
                    {
                        _data = d;
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TLC] فشل تحميل التقدّم: {e.Message}");
            }
            _data = new Data();
            return false;
        }

        /// <summary>للاختبار: حقن حالة مباشرة بدون قرص.</summary>
        public static void InjectForTests(Data d) => _data = d ?? new Data();
    }
}
