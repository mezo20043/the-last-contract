using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TLC.Core
{
    /// <summary>
    /// حفظ/تحميل على القرص. كتابة ذرّية (ملف مؤقت ثم استبدال) عشان مايتلفش الحفظ
    /// لو التطبيق اتقفل في نص الكتابة — مهم جداً على الموبايل.
    /// </summary>
    public static class SaveSystem
    {
        public const int AutoSlot = 0;
        public const int ManualSlotA = 1;
        public const int ManualSlotB = 2;
        public const int ManualSlotC = 3;

        const string FolderName = "saves";

        static string DirPath => Path.Combine(Application.persistentDataPath, FolderName);
        static string PathFor(int slot) => Path.Combine(DirPath, $"slot_{slot}.json");

        public static bool Exists(int slot) => File.Exists(PathFor(slot));

        public static bool Save(int slot, GameState state)
        {
            if (state == null) return false;
            try
            {
                Directory.CreateDirectory(DirPath);

                string json = JsonUtility.ToJson(state.ToDto(), true);
                string path = PathFor(slot);
                string tmp = path + ".tmp";

                File.WriteAllText(tmp, json);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);

                Debug.Log($"[TLC] اتحفظ في الخانة {slot} → {path}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC] فشل الحفظ (خانة {slot}): {e.Message}");
                return false;
            }
        }

        public static bool Load(int slot, GameState state)
        {
            if (state == null) return false;
            string path = PathFor(slot);
            if (!File.Exists(path)) return false;

            try
            {
                var dto = JsonUtility.FromJson<GameStateDto>(File.ReadAllText(path));
                if (dto == null) return false;
                state.FromDto(dto);
                Debug.Log($"[TLC] اتحمّل من الخانة {slot}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC] ملف الحفظ تالف (خانة {slot}): {e.Message}");
                return false;
            }
        }

        public static void Delete(int slot)
        {
            string path = PathFor(slot);
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) { Debug.LogError($"[TLC] فشل حذف الحفظ: {e.Message}"); }
        }

        public static List<int> ListSlots()
        {
            var found = new List<int>();
            for (int i = AutoSlot; i <= ManualSlotC; i++) if (Exists(i)) found.Add(i);
            return found;
        }

        public static string Describe(int slot)
        {
            if (!Exists(slot)) return "— فاضي —";
            try
            {
                var dto = JsonUtility.FromJson<GameStateDto>(File.ReadAllText(PathFor(slot)));
                if (dto == null) return "— تالف —";
                string when = DateTime.TryParse(dto.savedAtUtc, out var t)
                    ? t.ToLocalTime().ToString("dd/MM HH:mm")
                    : "—";
                return $"{when} · {dto.chapterId ?? "?"}";
            }
            catch { return "— تالف —"; }
        }
    }
}
