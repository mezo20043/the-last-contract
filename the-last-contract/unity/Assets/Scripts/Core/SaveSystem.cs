using System;
using System.IO;
using UnityEngine;

namespace TLC.Core
{
    /// <summary>
    /// حفظ/تحميل ٣ خانات + خانة أوتوماتيك. JSON على القرص في persistentDataPath.
    /// </summary>
    public static class SaveSystem
    {
        public const int AutoSlot  = 0;
        public const int ManualMin = 1;
        public const int ManualMax = 3;

        static string PathFor(int slot) =>
            Path.Combine(Application.persistentDataPath, $"tlc_save_{slot}.json");

        public static bool Exists(int slot) => File.Exists(PathFor(slot));

        public static void Save(int slot, GameState state)
        {
            try
            {
                var dto = state.ToDto();
                dto.version = 1;
                dto.savedAtUtc = DateTime.UtcNow.ToString("o");

                // كتابة ذرّية: ملف مؤقت ثم استبدال — يمنع تلف الحفظ لو التطبيق اتقفل
                string path = PathFor(slot);
                string tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(dto, true));

                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC] فشل الحفظ في الخانة {slot}: {e.Message}");
            }
        }

        public static bool Load(int slot, GameState state)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return false;

            try
            {
                var dto = JsonUtility.FromJson<SaveDto>(File.ReadAllText(path));
                if (dto == null) return false;
                state.FromDto(dto);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC] ملف الحفظ {slot} تالف: {e.Message}");
                return false;
            }
        }

        public static void Delete(int slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }

        public static string Describe(int slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return "— فاضي —";
            try
            {
                var dto = JsonUtility.FromJson<SaveDto>(File.ReadAllText(path));
                var when = DateTime.TryParse(dto.savedAtUtc, out var t) ? t.ToLocalTime().ToString("dd/MM HH:mm") : "؟";
                return $"{when} — {dto.storyId}";
            }
            catch { return "— تالف —"; }
        }
    }
}
