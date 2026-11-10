using System;
using System.Collections.Generic;

namespace TLC.Settings
{
    /// <summary>
    /// نصوص الواجهة بلغتين. المنطق هنا نقي (بلا Unity) عشان ينختبر آلياً.
    /// الترجمة العربية هي الأساس، والإنجليزية بديل كامل — مفيش مفتاح ناقص في واحدة.
    /// </summary>
    public static class Localization
    {
        public enum Lang { Arabic = 0, English = 1 }

        public static Lang Current { get; private set; } = Lang.Arabic;
        public static bool IsRtl => Current == Lang.Arabic;
        public static event Action Changed;

        // المفتاح → [عربي, إنجليزي]
        static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // العنوان
            { "game.title",          new[]{ "العقد الأخير", "THE LAST CONTRACT" } },
            { "game.subtitle",       new[]{ "قصة عن الأمانة اللي بتتكسر", "A story about a promise that breaks" } },
            { "menu.newgame",        new[]{ "لعبة جديدة", "New Game" } },
            { "menu.continue",       new[]{ "متابعة", "Continue" } },
            { "menu.chapters",       new[]{ "الفصول", "Chapters" } },
            { "menu.settings",       new[]{ "الإعدادات", "Settings" } },
            { "menu.endings",        new[]{ "شاشة النهايات", "Endings" } },
            { "menu.quit",           new[]{ "خروج", "Quit" } },
            { "menu.back",           new[]{ "رجوع", "Back" } },

            // الإعدادات
            { "settings.title",      new[]{ "الإعدادات", "Settings" } },
            { "settings.graphics",   new[]{ "جودة الرسوميات", "Graphics Quality" } },
            { "settings.quality.low",    new[]{ "منخفضة", "Low" } },
            { "settings.quality.medium", new[]{ "متوسطة", "Medium" } },
            { "settings.quality.high",   new[]{ "عالية", "High" } },
            { "settings.music",      new[]{ "الموسيقى", "Music" } },
            { "settings.sfx",        new[]{ "المؤثرات", "Sound Effects" } },
            { "settings.voice",      new[]{ "الحوار", "Voice" } },
            { "settings.language",   new[]{ "اللغة", "Language" } },
            { "settings.language.arabic",  new[]{ "العربية", "Arabic" } },
            { "settings.language.english", new[]{ "English", "English" } },
            { "settings.framerate",  new[]{ "حد الإطارات", "Frame Rate" } },
            { "settings.controls",       new[]{ "— التحكم —", "— Controls —" } },
            { "settings.look",           new[]{ "حساسية الكاميرا", "Camera Sensitivity" } },
            { "settings.stick",          new[]{ "حجم العصا", "Joystick Size" } },
            { "settings.stickSide",      new[]{ "جهة العصا", "Joystick Side" } },
            { "settings.stickSide.left", new[]{ "يسار", "Left" } },
            { "settings.stickSide.right",new[]{ "يمين", "Right" } },
            { "settings.invert",         new[]{ "عكس محور الكاميرا", "Invert Camera Y" } },
            { "settings.autoRun",        new[]{ "جري تلقائي", "Auto Run" } },
            { "settings.on",             new[]{ "مفعّل", "On" } },
            { "settings.off",            new[]{ "مقفول", "Off" } },
            { "settings.controls.reset", new[]{ "استرجاع التحكم الافتراضي", "Reset Controls" } },
            { "settings.reset",      new[]{ "استرجاع الافتراضي", "Reset to Default" } },

            // الإيقاف
            { "pause.title",         new[]{ "متوقف", "Paused" } },
            { "pause.resume",        new[]{ "استكمال", "Resume" } },
            { "pause.save",          new[]{ "حفظ", "Save" } },
            { "pause.load",          new[]{ "تحميل", "Load" } },
            { "pause.mainmenu",      new[]{ "القائمة الرئيسية", "Main Menu" } },

            // الحفظ
            { "save.title",          new[]{ "الحفظ والتحميل", "Save & Load" } },
            { "save.slot",           new[]{ "خانة", "Slot" } },
            { "save.empty",          new[]{ "فاضية", "Empty" } },
            { "save.auto",           new[]{ "تلقائي", "Auto" } },
            { "save.overwrite",      new[]{ "هيتم استبدال الحفظ", "This will overwrite" } },
            { "save.done",           new[]{ "اتحفظ", "Saved" } },
            { "save.loaded",         new[]{ "اتحمّل", "Loaded" } },

            // الفصول
            { "chapters.title",      new[]{ "الفصول", "Chapters" } },
            { "chapters.locked",     new[]{ "مقفول", "Locked" } },
            { "chapters.one",        new[]{ "الفصل الأول · العقد الأخير", "Act I · The Last Contract" } },
            { "chapters.two",        new[]{ "الفصل الثاني · البيت القديم", "Act II · The Old House" } },
            { "chapters.three",      new[]{ "الفصل الثالث · الحساب", "Act III · The Reckoning" } },

            // النهايات
            { "endings.title",       new[]{ "النهايات", "Endings" } },
            { "endings.progress",    new[]{ "اتكشفت", "Discovered" } },
            { "endings.locked",      new[]{ "؟؟؟", "???" } },
            { "endings.lockedhint",  new[]{ "لسه ما وصلتهاش", "Not reached yet" } },
            { "endings.ch1",         new[]{ "نهايات الفصل الأول", "Act I Endings" } },
            { "endings.ch2",         new[]{ "نهايات الفصل الثاني", "Act II Endings" } },
            { "endings.ch3",         new[]{ "نهايات الفصل الثالث", "Act III Endings" } },

            // اللعب
            { "play.interact",       new[]{ "تفاعل", "Interact" } },
            { "play.sprint",         new[]{ "جري", "Sprint" } },
            { "play.pause",          new[]{ "إيقاف", "Pause" } },
            { "play.skip",           new[]{ "تخطّي", "Skip" } },
            { "play.continue",       new[]{ "استمرار", "Continue" } },

            // رسائل
            { "msg.autosaved",       new[]{ "اتحفظ تلقائياً", "Auto-saved" } },
            { "msg.nosave",          new[]{ "مفيش حفظ موجود", "No save found" } },
            { "msg.confirmquit",     new[]{ "هتخرج من غير حفظ التقدّم؟", "Quit without saving?" } },
        };

        public static IEnumerable<string> Keys => Table.Keys;

        public static string Get(string key)
        {
            if (Table.TryGetValue(key, out var v))
                return v[(int)Current] ?? key;
            return key;      // مفتاح ناقص يبان زي ما هو بدل ما يختفي
        }

        public static string Get(string key, params object[] args)
        {
            try { return string.Format(Get(key), args); }
            catch (FormatException) { return Get(key); }
        }

        public static void SetLanguage(Lang lang)
        {
            if (Current == lang) return;
            Current = lang;
            Changed?.Invoke();
        }

        public static void Toggle() =>
            SetLanguage(Current == Lang.Arabic ? Lang.English : Lang.Arabic);

        /// <summary>للاختبار: هل المفتاح موجود ومترجم في اللغتين؟</summary>
        public static bool IsComplete(out List<string> broken)
        {
            broken = new List<string>();
            foreach (var kv in Table)
            {
                var v = kv.Value;
                if (v == null || v.Length < 2 ||
                    string.IsNullOrWhiteSpace(v[0]) ||
                    string.IsNullOrWhiteSpace(v[1]))
                    broken.Add(kv.Key);
            }
            return broken.Count == 0;
        }
    }
}
