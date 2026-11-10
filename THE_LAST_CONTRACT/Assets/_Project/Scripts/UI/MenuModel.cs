using System;
using System.Collections.Generic;
using TLC.Settings;

namespace TLC.UI
{
    public enum UIScreen
    {
        None = 0,
        Title = 1,
        Main = 2,
        Chapters = 3,
        Settings = 4,
        Endings = 5,
        SaveLoad = 6,
        Pause = 7
    }

    public enum MenuAction
    {
        None, NewGame, Continue, OpenChapters, OpenSettings, OpenEndings,
        OpenSaveLoad, Resume, SaveToSlot, LoadFromSlot, Back, Quit, StartChapter,

        // إعدادات تحكم الموبايل — بتتطبّق فوراً وبتتحفظ
        CycleLook, CycleStick, ToggleStickSide, ToggleInvertY, ToggleAutoRun, ResetControls
    }

    public class MenuEntry
    {
        public string Key;          // مفتاح ترجمة
        public MenuAction Action;
        public bool Enabled = true;
        public int Value;           // رقم الفصل أو الخانة
        public string Suffix;       // نص إضافي (زي "الفصل الثاني" أو "مقفول")

        public MenuEntry(string key, MenuAction action, bool enabled = true,
                         int value = 0, string suffix = null)
        {
            Key = key; Action = action; Enabled = enabled; Value = value; Suffix = suffix;
        }
    }

    /// <summary>حالة اللعبة اللي الواجهة بتقرأها — تُحقن عشان المنطق يبقى قابل للاختبار.</summary>
    public class MenuContext
    {
        public bool HasSave;
        public int UnlockedChapters = 1;
        public HashSet<string> SeenEndings = new HashSet<string>();
        public int TotalEndings = 12;
        public bool InGame;
    }

    /// <summary>
    /// منطق تنقّل القوائم كله — بلا Unity وبلا رسم.
    /// الـ MonoBehaviour بيرسم، وده اللي بيقرر. عشان كده ينفع يتختبر آلياً.
    /// </summary>
    public class MenuModel
    {
        readonly Stack<UIScreen> _history = new Stack<UIScreen>();

        public UIScreen Current { get; private set; } = UIScreen.Title;
        public int HistoryDepth => _history.Count;

        public event Action Changed;

        public void Open(UIScreen screen)
        {
            if (screen == Current) return;
            _history.Push(Current);
            Current = screen;
            Changed?.Invoke();
        }

        /// <summary>يرجع للشاشة السابقة. لو مفيش سابق يفضل زي ما هو ويرجّع false.</summary>
        public bool Back()
        {
            if (_history.Count == 0) return false;
            Current = _history.Pop();
            Changed?.Invoke();
            return true;
        }

        /// <summary>التصرف الصح لزرار الرجوع في أندرويد أو Escape.</summary>
        public void HardwareBack()
        {
            if (Current == UIScreen.Title) return;      // مفيش خروج من هنا بالرجوع
            Back();
        }

        public void Reset(UIScreen s = UIScreen.Title)
        {
            _history.Clear();
            Current = s;
            Changed?.Invoke();
        }

        // ───────────────────── بناء عناصر كل شاشة ─────────────────────

        public List<MenuEntry> Build(MenuContext ctx)
        {
            ctx = ctx ?? new MenuContext();
            switch (Current)
            {
                case UIScreen.Title:   return BuildTitle(ctx);
                case UIScreen.Main:    return BuildMain(ctx);
                case UIScreen.Chapters:return BuildChapters(ctx);
                case UIScreen.Settings:return BuildSettings(ctx);
                case UIScreen.Endings: return BuildEndings(ctx);
                case UIScreen.SaveLoad:return BuildSaveLoad(ctx);
                case UIScreen.Pause:   return BuildPause(ctx);
                default:             return new List<MenuEntry>();
            }
        }

        List<MenuEntry> BuildTitle(MenuContext ctx) => new List<MenuEntry>
        {
            new MenuEntry("menu.newgame",  MenuAction.NewGame),
            new MenuEntry("menu.continue", MenuAction.Continue, ctx.HasSave),
            new MenuEntry("menu.chapters", MenuAction.OpenChapters, ctx.UnlockedChapters > 1),
            new MenuEntry("menu.settings", MenuAction.OpenSettings),
            new MenuEntry("menu.endings",  MenuAction.OpenEndings),
        };

        List<MenuEntry> BuildMain(MenuContext ctx) => new List<MenuEntry>
        {
            new MenuEntry("menu.newgame",  MenuAction.NewGame),
            new MenuEntry("menu.continue", MenuAction.Continue, ctx.HasSave),
            new MenuEntry("menu.chapters", MenuAction.OpenChapters),
            new MenuEntry("menu.settings", MenuAction.OpenSettings),
            new MenuEntry("menu.endings",  MenuAction.OpenEndings),
            new MenuEntry("menu.quit",     MenuAction.Quit),
        };

        List<MenuEntry> BuildChapters(MenuContext ctx)
        {
            var list = new List<MenuEntry>();
            string[] keys = { "chapters.one", "chapters.two", "chapters.three" };
            for (int i = 0; i < 3; i++)
            {
                bool unlocked = ctx.UnlockedChapters >= i + 1;
                list.Add(new MenuEntry(keys[i], MenuAction.StartChapter, unlocked,
                                       i + 1, unlocked ? null : Localization.Get("chapters.locked")));
            }
            list.Add(new MenuEntry("menu.back", MenuAction.Back));
            return list;
        }

        List<MenuEntry> BuildSettings(MenuContext ctx) => new List<MenuEntry>
        {
            new MenuEntry("settings.controls", MenuAction.None, false),
            new MenuEntry("settings.look",     MenuAction.CycleLook),
            new MenuEntry("settings.stick",    MenuAction.CycleStick),
            new MenuEntry("settings.stickSide",MenuAction.ToggleStickSide),
            new MenuEntry("settings.invert",   MenuAction.ToggleInvertY),
            new MenuEntry("settings.autoRun",  MenuAction.ToggleAutoRun),
            new MenuEntry("settings.controls.reset", MenuAction.ResetControls),
            new MenuEntry("settings.graphics", MenuAction.None),
            new MenuEntry("settings.music",    MenuAction.None),
            new MenuEntry("settings.sfx",      MenuAction.None),
            new MenuEntry("settings.voice",    MenuAction.None),
            new MenuEntry("settings.language", MenuAction.None,
                          true, 0, Localization.Get(Localization.IsRtl
                                    ? "settings.language.arabic" : "settings.language.english")),
            new MenuEntry("menu.back", MenuAction.Back),
        };

        List<MenuEntry> BuildEndings(MenuContext ctx)
        {
            var list = new List<MenuEntry>();
            for (int i = 0; i < ctx.TotalEndings; i++)
            {
                string id = EndingIdAt(i);
                bool seen = ctx.SeenEndings.Contains(id);
                list.Add(new MenuEntry(seen ? id : "endings.locked", MenuAction.None,
                                       seen, i, seen ? null : Localization.Get("endings.lockedhint")));
            }
            list.Add(new MenuEntry("menu.back", MenuAction.Back));
            return list;
        }

        List<MenuEntry> BuildSaveLoad(MenuContext ctx)
        {
            var list = new List<MenuEntry>();
            for (int i = 0; i < 3; i++)
            {
                list.Add(new MenuEntry("save.slot", MenuAction.SaveToSlot, true, i));
                list.Add(new MenuEntry("save.slot", MenuAction.LoadFromSlot, ctx.HasSave, i));
            }
            list.Add(new MenuEntry("menu.back", MenuAction.Back));
            return list;
        }

        List<MenuEntry> BuildPause(MenuContext ctx) => new List<MenuEntry>
        {
            new MenuEntry("pause.resume",   MenuAction.Resume),
            new MenuEntry("pause.save",     MenuAction.OpenSaveLoad),
            new MenuEntry("settings.title", MenuAction.OpenSettings),
            new MenuEntry("pause.mainmenu", MenuAction.Quit),
        };

        /// <summary>معرّفات النهايات الاثني عشر بترتيب العرض.</summary>
        public static readonly string[] EndingIds =
        {
            "CH1_A", "CH1_B", "CH1_C",
            "CH2_A", "CH2_B", "CH2_C",
            "CH3_1", "CH3_2", "CH3_3", "CH3_4", "CH3_5", "CH3_6"
        };

        public static string EndingIdAt(int index) =>
            index >= 0 && index < EndingIds.Length ? EndingIds[index] : null;

        public static int EndingCount => EndingIds.Length;
    }
}
