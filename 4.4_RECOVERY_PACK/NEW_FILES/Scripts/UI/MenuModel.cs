// 4.4 RECOVERY — MenuModel.cs — **مُعاد حرفيًا** (النسخة النهائية اللي نجحت في 4.4)
using System;
using System.Collections.Generic;
using TLC.Settings;

namespace TLC.UI
{
    public enum UIScreen
    {
        None = 0, Title = 1, Main = 2, Chapters = 3, Settings = 4, Endings = 5,
        SaveLoad = 6, Pause = 7, Extras = 8, Characters = 9, Records = 10,
        Cinematics = 11, Credits = 12
    }

    public enum MenuAction
    {
        None, NewGame, Continue, OpenChapters, OpenSettings, OpenEndings,
        OpenSaveLoad, Resume, SaveToSlot, LoadFromSlot, Back, Quit, StartChapter,

        // إعدادات تحكم الموبايل — بتتطبّق فوراً وبتتحفظ
        CycleLook, CycleStick, ToggleStickSide, ToggleInvertY, ToggleAutoRun, ResetControls,

        // 4.4
        OpenExtras, OpenCharacters, OpenRecords, OpenCinematics, OpenCredits,
        DeleteSlot, RestartChapter, QuitToMain,
        MasterUp, MasterDown, MusicUp, MusicDown, SfxUp, SfxDown, VoiceUp, VoiceDown,
        CycleQuality, ToggleLanguage,
        Confirm, Cancel,
        PlayPostCredit, PlayTeaser
    }

    public class MenuEntry
    {
        public string Key;          // مفتاح ترجمة
        public MenuAction Action;
        public bool Enabled = true;
        public int Value;           // رقم الفصل/الخانة/العنصر
        public string Suffix;       // نص إضافي قصير
        public string Detail;       // سطر تاني (وصف الخانة/العنصر)
        public bool Locked;         // بيتعرض مقفول (للإضافات)
        public bool IsSlotRow;      // صف خانة حفظ (بيتعرض بصورة + تفاصيل)
        public bool IsHeader;       // سطر عنوان مجموعة

        public MenuEntry(string key, MenuAction action, bool enabled = true,
                         int value = 0, string suffix = null)
        {
            Key = key; Action = action; Enabled = enabled; Value = value; Suffix = suffix;
        }
    }

    /// <summary>ملخّص خانة حفظ للعرض (بيتبني من SaveManager بدون تحميل اللعبة).</summary>
    public class SlotSummary
    {
        public int slot;
        public bool empty = true;
        public bool corrupted;
        public bool isAuto;
        public string chapterTitle;
        public string locationName;
        public string nodeLabel;
        public string dateText;
        public string playtimeText;

        public string Describe()
        {
            if (empty) return corrupted ? "— تالف —" : "— فاضي —";
            return $"{chapterTitle} · {locationName} · {dateText} · {playtimeText}";
        }
    }

    /// <summary>حالة اللعبة اللي الواجهة بتقرأها — تُحقن عشان المنطق يبقى قابل للاختبار.</summary>
    public class MenuContext
    {
        public bool HasSave;
        public bool HasPlaythrough;          // فيه حفظ أو تقدّم => لعبة جديدة لازم تتأكد
        public int UnlockedChapters = 1;
        public HashSet<string> SeenEndings = new HashSet<string>();
        public int TotalEndings = 12;
        public bool InGame;
        public float PlaytimeSeconds;

        public List<SlotSummary> Slots = new List<SlotSummary>();
        public List<ExtrasItemLike> ExtrasScenes = new List<ExtrasItemLike>();
        public List<ExtrasItemLike> ExtrasCinematics = new List<ExtrasItemLike>();
        public List<ExtrasItemLike> ExtrasCharacters = new List<ExtrasItemLike>();
        public List<ExtrasItemLike> ExtrasRecords = new List<ExtrasItemLike>();
        public string ExtrasProgressLine;

        public bool CanPlayPostCredit;
        public string LastEndingId;
        public bool TeaserUnlocked;

        // تأكيد قبل أي فعل مدمّر
        public MenuAction PendingConfirm;
        public int PendingValue;
        public string ConfirmTitle;
        public string ConfirmBody;
    }

    /// <summary>نسخة خفيفة من عنصر الإضافات (بدون اعتماد على Core).</summary>
    public class ExtrasItemLike
    {
        public string id;
        public string title;
        public string detail;
        public bool unlocked;
        public ExtrasItemLike(string id, string title, string detail, bool unlocked)
        {
            this.id = id; this.title = title; this.detail = detail; this.unlocked = unlocked;
        }
    }

    /// <summary>
    /// منطق تنقّل القوائم كله — بلا Unity وبلا رسم.
    /// الـMonoBehaviour بيرسم، وده اللي بيقرر.
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
            if (Current == UIScreen.Title) return;
            Back();
        }

        public void Reset(UIScreen s = UIScreen.Title)
        {
            _history.Clear();
            Current = s;
            Changed?.Invoke();
        }

        public bool AtRoot => Current == UIScreen.Title;

        // ───────────────────── بناء عناصر كل شاشة ─────────────────────

        public List<MenuEntry> Build(MenuContext ctx)
        {
            ctx = ctx ?? new MenuContext();

            if (ctx.PendingConfirm != MenuAction.None) return BuildConfirm(ctx);

            switch (Current)
            {
                case UIScreen.Title:      return BuildTitle(ctx);
                case UIScreen.Main:       return BuildMain(ctx);
                case UIScreen.Chapters:   return BuildChapters(ctx);
                case UIScreen.Settings:   return BuildSettings(ctx);
                case UIScreen.Endings:    return BuildEndings(ctx);
                case UIScreen.SaveLoad:   return BuildSaveLoad(ctx);
                case UIScreen.Pause:      return BuildPause(ctx);
                case UIScreen.Extras:     return BuildExtras(ctx);
                case UIScreen.Characters: return BuildList(ctx, ctx.ExtrasCharacters, "extras.characters", MenuAction.None);
                case UIScreen.Records:    return BuildList(ctx, ctx.ExtrasRecords, "extras.records", MenuAction.None);
                case UIScreen.Cinematics: return BuildList(ctx, ctx.ExtrasCinematics, "extras.cinematics", MenuAction.None);
                case UIScreen.Credits:    return BuildCredits(ctx);
                default:                  return new List<MenuEntry>();
            }
        }

        List<MenuEntry> BuildConfirm(MenuContext ctx) => new List<MenuEntry>
        {
            new MenuEntry("confirm.yes", MenuAction.Confirm),
            new MenuEntry("confirm.no",  MenuAction.Cancel),
        };

        List<MenuEntry> BuildTitle(MenuContext ctx) => new List<MenuEntry>
        {
            new MenuEntry("menu.newgame",  MenuAction.NewGame),
            new MenuEntry("menu.continue", MenuAction.Continue, ctx.HasSave),
            new MenuEntry("menu.chapters", MenuAction.OpenChapters, ctx.UnlockedChapters > 1),
            new MenuEntry("menu.settings", MenuAction.OpenSettings),
            new MenuEntry("menu.extras",   MenuAction.OpenExtras),
            new MenuEntry("menu.credits",  MenuAction.OpenCredits),
            new MenuEntry("menu.quit",     MenuAction.Quit),
        };

        List<MenuEntry> BuildMain(MenuContext ctx) => BuildTitle(ctx);

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

        List<MenuEntry> BuildSettings(MenuContext ctx)
        {
            string vol(string k) => $"{Mathf_Round01(GetVolume(k)):0}%";

            var list = new List<MenuEntry>
            {
                new MenuEntry("settings.section.audio", MenuAction.None, false) { IsHeader = true },
                new MenuEntry("settings.master", MenuAction.MasterDown) { Suffix = "◄ " + vol("master") + " ►" },
                new MenuEntry("settings.music",  MenuAction.MusicDown)  { Suffix = "◄ " + vol("music") + " ►" },
                new MenuEntry("settings.sfx",    MenuAction.SfxDown)    { Suffix = "◄ " + vol("sfx") + " ►" },
                new MenuEntry("settings.voice",  MenuAction.VoiceDown)  { Suffix = "◄ " + vol("voice") + " ►" },

                new MenuEntry("settings.section.camera", MenuAction.None, false) { IsHeader = true },
                new MenuEntry("settings.look",     MenuAction.CycleLook),
                new MenuEntry("settings.invert",   MenuAction.ToggleInvertY),
                new MenuEntry("settings.stick",    MenuAction.CycleStick),
                new MenuEntry("settings.stickSide",MenuAction.ToggleStickSide),
                new MenuEntry("settings.autoRun",  MenuAction.ToggleAutoRun),
                new MenuEntry("settings.controls.reset", MenuAction.ResetControls),

                new MenuEntry("settings.section.video", MenuAction.None, false) { IsHeader = true },
                new MenuEntry("settings.graphics", MenuAction.CycleQuality),

                new MenuEntry("settings.section.lang", MenuAction.None, false) { IsHeader = true },
                new MenuEntry("settings.language", MenuAction.ToggleLanguage, true, 0,
                              Localization.Get(Localization.IsRtl ? "settings.language.arabic" : "settings.language.english")),

                new MenuEntry("menu.back", MenuAction.Back),
            };
            return list;
        }

        List<MenuEntry> BuildEndings(MenuContext ctx)
        {
            var list = new List<MenuEntry>();
            for (int i = 0; i < ctx.TotalEndings; i++)
            {
                string id = EndingIdAt(i);
                bool seen = ctx.SeenEndings.Contains(id);
                bool isCh1 = id != null && id.StartsWith("CH1_");
                string detail = seen ? "اتفتحت" : (isCh1 ? "الفصل الأول" : "لسه مش موجود في اللعب");
                list.Add(new MenuEntry(seen ? id : "endings.locked", MenuAction.None,
                                       seen, i, seen ? null : Localization.Get("endings.lockedhint"))
                { Detail = detail, Locked = !seen });
            }
            list.Add(new MenuEntry("menu.back", MenuAction.Back));
            return list;
        }

        List<MenuEntry> BuildSaveLoad(MenuContext ctx)
        {
            var list = new List<MenuEntry>
            {
                new MenuEntry("save.section.auto", MenuAction.None, false) { IsHeader = true },
            };

            // الخانة التلقائية: تحميل بس (اللعبة بتكتبها)
            var auto = FindSlot(ctx, 0);
            bool autoEmpty = auto == null || auto.empty;
            list.Add(new MenuEntry("save.auto", MenuAction.LoadFromSlot, !autoEmpty, 0)
            {
                IsSlotRow = true,
                Detail = auto != null ? auto.Describe() : "— فاضي —"
            });

            list.Add(new MenuEntry("save.section.manual", MenuAction.None, false) { IsHeader = true });

            for (int slot = 1; slot <= 3; slot++)
            {
                var s = FindSlot(ctx, slot);
                bool empty = s == null || s.empty;

                list.Add(new MenuEntry("save.write", MenuAction.SaveToSlot, true, slot - 1)
                {
                    IsSlotRow = true,
                    Suffix = SlotLabel(slot),
                    Detail = s != null ? s.Describe() : "— فاضي —"
                });

                if (!empty)
                {
                    list.Add(new MenuEntry("save.read", MenuAction.LoadFromSlot, true, slot - 1)
                    {
                        Suffix = SlotLabel(slot),
                        Detail = s != null ? s.Describe() : null
                    });
                }
            }

            list.Add(new MenuEntry("menu.back", MenuAction.Back));
            return list;
        }

        static SlotSummary FindSlot(MenuContext ctx, int slot)
        {
            if (ctx.Slots == null) return null;
            foreach (var s in ctx.Slots) if (s.slot == slot) return s;
            return null;
        }

        public static string SlotLabel(int slot) =>
            slot == 0 ? "تلقائي" : ((char)('A' + slot - 1)).ToString();

        List<MenuEntry> BuildPause(MenuContext ctx) => new List<MenuEntry>
        {
            new MenuEntry("pause.resume",    MenuAction.Resume),
            new MenuEntry("pause.save",      MenuAction.OpenSaveLoad),
            new MenuEntry("settings.title",  MenuAction.OpenSettings),
            new MenuEntry("pause.chapter",   MenuAction.RestartChapter),
            new MenuEntry("pause.mainmenu",  MenuAction.QuitToMain),
        };

        List<MenuEntry> BuildExtras(MenuContext ctx)
        {
            var list = new List<MenuEntry>
            {
                new MenuEntry("extras.scenes",     MenuAction.OpenChapters, ctx.UnlockedChapters > 1)
                    { Detail = ctx.ExtrasProgressLine },
                new MenuEntry("extras.cinematics", MenuAction.OpenCinematics) { Detail = "نهايات + افتتاحية + تيزر" },
                new MenuEntry("extras.endings",    MenuAction.OpenEndings)
                    { Detail = $"{ctx.SeenEndings.Count}/{ctx.TotalEndings}" },
                new MenuEntry("extras.characters", MenuAction.OpenCharacters),
                new MenuEntry("extras.records",    MenuAction.OpenRecords),
            };

            if (ctx.CanPlayPostCredit && !string.IsNullOrEmpty(ctx.LastEndingId))
                list.Add(new MenuEntry("extras.replaypost", MenuAction.PlayPostCredit, true, 0, ctx.LastEndingId));

            if (ctx.TeaserUnlocked)
                list.Add(new MenuEntry("extras.playteaser", MenuAction.PlayTeaser));

            list.Add(new MenuEntry("menu.back", MenuAction.Back));
            return list;
        }

        List<MenuEntry> BuildList(MenuContext ctx, List<ExtrasItemLike> items, string titleKey, MenuAction action)
        {
            var list = new List<MenuEntry>
            {
                new MenuEntry(titleKey, MenuAction.None, false) { IsHeader = true },
            };

            if (items == null || items.Count == 0)
            {
                list.Add(new MenuEntry("extras.empty", MenuAction.None, false));
            }
            else
            {
                foreach (var it in items)
                {
                    list.Add(new MenuEntry(it.unlocked ? it.title : "extras.locked",
                                           MenuAction.None, it.unlocked, 0, null)
                    {
                        Detail = it.detail,
                        Locked = !it.unlocked
                    });
                }
            }

            list.Add(new MenuEntry("menu.back", MenuAction.Back));
            return list;
        }

        List<MenuEntry> BuildCredits(MenuContext ctx)
        {
            return new List<MenuEntry>
            {
                new MenuEntry("credits.role.writing", MenuAction.None, false) { Detail = "THE LAST CONTRACT · Chapter 01" },
                new MenuEntry("credits.role.engine", MenuAction.None, false)  { Detail = "Unity 6 — Native Android" },
                new MenuEntry("credits.role.audio", MenuAction.None, false)   { Detail = "أصوات مُولّدة إجرائياً (مش تعليق مسجّل)" },
                new MenuEntry("credits.role.art", MenuAction.None, false)     { Detail = "موديلات مفتوحة المصدر · تفاصيل الترخيص في المستندات" },
                new MenuEntry("credits.thanks", MenuAction.None, false)       { Detail = "شكراً إنك وصلت لآخر الفصل الأول." },
                new MenuEntry("menu.back", MenuAction.Back),
            };
        }

        // ───────────────────── تأكيد ─────────────────────

        public void RequestConfirm(MenuContext ctx, MenuAction action, int value, string title, string body)
        {
            if (ctx == null) return;
            ctx.PendingConfirm = action;
            ctx.PendingValue = value;
            ctx.ConfirmTitle = title;
            ctx.ConfirmBody = body;
            Changed?.Invoke();
        }

        public MenuAction ClearConfirm(MenuContext ctx, out int value)
        {
            value = 0;
            if (ctx == null) return MenuAction.None;
            var a = ctx.PendingConfirm;
            value = ctx.PendingValue;
            ctx.PendingConfirm = MenuAction.None;
            ctx.ConfirmTitle = null;
            ctx.ConfirmBody = null;
            Changed?.Invoke();
            return a;
        }

        // ───────────────────── معرّفات النهايات ─────────────────────

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

        static float Mathf_Round01(float v) => (float)Math.Round(Math.Clamp(v, 0f, 1f) * 100f);

        static float GetVolume(string which) => Settings.AudioLevels.Get(which);
    }
}
