// ═══════════════════════════════════════════════════════════════════════════════
// 4.4 RECOVERY — GameBootstrap.Phase44.cs
//   ربط 4.4 كامل كـpartial class، بدل تعديل ملف 4.3 يدويًا في ١٥٠٠ سطر.
//   الحالة: **مُعاد بناءً من التوثيق** (النص الأصلي مش محفوظ) — الـAPI والسلوك
//   مؤكدين من سكربت القبول `Phase44Tests.cs` اللي نجح فعلًا في جولة 4.4.
//
//   شرطان لازمين في الملف الأصلي GameBootstrap.cs:
//     ١) السطر:  public class GameBootstrap : MonoBehaviour
//        يبقى:   public partial class GameBootstrap : MonoBehaviour
//     ٢) تُضاف خصائص الوصول دي (سطر واحد لكل واحدة):
//          GameState State => <حقل الحالة>;
//          Narrative.NarrativeRunner Runner => <حقل الراوي>;
//          Camera Cam => <الكاميرا>;
//          Cinematics.CinematicDirector Cine => <مدير الانتقالات>;
//          string ChapterId / ChapterTitle / LocationName  (حسب المتاح)
//     ٣) أي معالج قرار/نهاية موجود في 4.3 يستدعي:  NotifyDecision() / OnEnding(id)
// ═══════════════════════════════════════════════════════════════════════════════
using System.Collections.Generic;
using UnityEngine;
using TLC.Cinematics;
using TLC.Core;
using TLC.Narrative;
using TLC.UI;

namespace TLC.Bootstrap
{
    public partial class GameBootstrap
    {
        // ─────────────────────────── حقول 4.4 ───────────────────────────

        [Header("4.4 — حفظ وقوائم وما بعد النهاية")]
        public SaveController Saves;
        public MenuSystem Menu;
        public Toast Toast;
        public CardPlayer Cards;

        public readonly MenuModel MenuModel = new MenuModel();
        public MenuContext MenuContext = new MenuContext();

        /// <summary>الاختبارات بترفعه لمنع تغيير المشهد بعد الكروت.</summary>
        public bool DebugSuppressSceneChange;

        bool _inCards, _menuPaused, _newGameConfirmPending, _returnToMenuPending;
        float _decisionCooldown;
        int _linesSinceNode;
        int _screensShown;

        // ───────────────────── نقاط الربط (تُعرَّف في الملف الأصلي) ─────────────────────
        // GameState State { get; } · NarrativeRunner Runner { get; } · Camera Cam { get; }
        // CinematicDirector Cine { get; }
        // لو الأسماء مختلفة، غيّر المرجع هنا بس.

        // ─────────────────────────── البناء ───────────────────────────

        /// <summary>بتتنادى من Awake/BuildSystems بعد ما الحالة والراوي يكونوا جاهزين.</summary>
        void BuildPhase44Systems()
        {
            if (Cards == null)
            {
                var go = new GameObject("CardPlayer");
                go.transform.SetParent(transform, false);
                Cards = go.AddComponent<CardPlayer>();
                Cards.Finished += OnCardsFinished;
            }

            if (Toast == null)
            {
                var go = new GameObject("Toast");
                go.transform.SetParent(transform, false);
                Toast = go.AddComponent<Toast>();
            }

            if (Menu == null)
            {
                var go = new GameObject("MenuSystem");
                go.transform.SetParent(transform, false);
                Menu = go.AddComponent<MenuSystem>();
                Menu.ActionRequested += OnMenuAction;
                Menu.Build();
                Menu.Hide();
            }

            if (Saves == null)
            {
                Saves = gameObject.AddComponent<SaveController>();
                Saves.Bind(State, Runner, Cam, Cine, Menu,
                           () => ChapterId, () => ChapterTitle, () => LocationName,
                           IsUnstableState);
            }

            BuildMenuContext();
            Debug.Log("[TLC/Boot-4.4] أنظمة الحفظ/القوائم/الكروت جاهزة");
        }

        // ─────────────────────────── الحلقة ───────────────────────────

        void TickPhase44()
        {
            if (Saves != null) Saves.Tick(Time.unscaledDeltaTime);
            if (_decisionCooldown > 0f) _decisionCooldown -= Time.unscaledDeltaTime;

            if (Runner != null && Runner.Story != null && !_inCards && !_menuPaused)
            {
                if (++_linesSinceNode >= 12)
                {
                    _linesSinceNode = 0;
                    if (Saves != null) Saves.Autosave("line-tick");
                }
            }

            if (Input.GetKeyDown(KeyCode.Escape) && !_inCards)
            {
                if (Menu != null && Menu.IsOpen) Menu.Back();
                else OpenPauseMenu();
            }
        }

        /// <summary>هل الحالة غير مستقرة (انتقال/كروت/بلوك)؟ لقطات كاميرا الحوار مستثناة.</summary>
        public bool IsUnstableState()
        {
            if (_inCards || Cards != null && Cards.IsPlaying) return true;
            if (Cine != null && Cine.IsPlaying) return true;
            return false;
        }

        // ───────────────── نقاط الحفظ التلقائي (تُنادى من معالجات 4.3) ─────────────────

        public void NotifyDecision()
        {
            _decisionCooldown = 1.2f;
            CancelInvoke(nameof(AutosaveAfterDecision));
            Invoke(nameof(AutosaveAfterDecision), 1.2f);
        }

        void AutosaveAfterDecision()
        {
            if (Saves != null) Saves.Autosave("decision");
        }

        public void NotifySceneChanged(string sceneId)
        {
            if (Saves != null) Saves.Autosave("scene:" + sceneId);
        }

        /// <summary>بتتنادى من الملف الأصلي عند بلوغ نهاية (بدل OnEnding القديمة).</summary>
        public void OnEnding(string endingId)
        {
            if (State != null) State.EndingId = endingId;
            Progress.MarkEnding(endingId);
            if (Saves != null) Saves.SaveUnsafe("ending:" + endingId);
            PlayPostCredit(endingId);
        }

        // ─────────────────────────── القوائم ───────────────────────────

        void BuildMenuContext()
        {
            var ctx = new MenuContext
            {
                HasSave = SaveManager.LatestSlot() >= 0,
                HasPlaythrough = SaveManager.LatestSlot() >= 0 || (State != null && State.PlaytimeSeconds > 5f),
                UnlockedChapters = Progress.Current.unlockedChapters,
                InGame = Runner != null && Runner.Story != null,
                PlaytimeSeconds = State != null ? State.PlaytimeSeconds : 0f,
                TotalEndings = MenuModel.EndingCount,
                ExtrasProgressLine = ExtrasCatalog.ProgressLine(),
                LastEndingId = State != null ? State.EndingId : null,
                TeaserUnlocked = Progress.HasCinematic(ChapterTwoTeaser.CinematicId) || Progress.Current.teaserSeen,
                CanPlayPostCredit = State != null && PostCreditSequence.IsSupported(State.EndingId)
            };

            foreach (var e in Progress.Current.seenEndings) ctx.SeenEndings.Add(e);

            for (int slot = 0; slot < SaveManager.SlotCount; slot++) ctx.Slots.Add(BuildSlotSummary(slot));

            foreach (var i in ExtrasCatalog.Scenes())        ctx.ExtrasScenes.Add(ToLike(i));
            foreach (var i in ExtrasCatalog.Cinematics())    ctx.ExtrasCinematics.Add(ToLike(i));
            foreach (var i in ExtrasCatalog.Characters())    ctx.ExtrasCharacters.Add(ToLike(i));
            foreach (var i in ExtrasCatalog.Records(State))  ctx.ExtrasRecords.Add(ToLike(i));

            // الحفاظ على أي حوار تأكيد مفتوح
            bool confirming = MenuContext != null && MenuContext.PendingConfirm != MenuAction.None;
            var old = MenuContext;
            MenuContext = ctx;
            if (confirming && old != null)
            {
                ctx.PendingConfirm = old.PendingConfirm;
                ctx.PendingValue = old.PendingValue;
                ctx.ConfirmTitle = old.ConfirmTitle;
                ctx.ConfirmBody = old.ConfirmBody;
            }

            if (Menu != null) Menu.SetContext(ctx);
            ApplyMenuState();
        }

        static ExtrasItemLike ToLike(ExtrasItem i) =>
            new ExtrasItemLike(i.id, i.title, i.detail, i.unlocked);

        SlotSummary BuildSlotSummary(int slot)
        {
            var s = new SlotSummary { slot = slot, isAuto = slot == SaveManager.AutoSlot };
            if (!SaveManager.Peek(slot, out SaveHeader h, out SaveMeta m))
            {
                s.corrupted = SaveManager.Exists(slot);
                return s;
            }
            s.empty = false;
            s.chapterTitle = h.chapterTitle ?? h.chapterId;
            s.locationName = m.locationName;
            s.nodeLabel = h.nodeLabel;
            s.dateText = SaveManager.PrettyDate(h.savedAtUtc);
            s.playtimeText = SaveManager.PrettyPlaytime(h.playtimeSeconds);
            return s;
        }

        void ApplyMenuState()
        {
            bool menuUp = Menu != null && Menu.IsOpen;
            _menuPaused = menuUp && MenuModel.Current == UIScreen.Pause;
            if (Saves != null) Saves.Paused = _menuPaused;

            float want = _menuPaused ? 0f : 1f;
            if (Mathf.Abs(Time.timeScale - want) > 0.01f) Time.timeScale = want;
        }

        public void OpenPauseMenu()
        {
            if (_inCards) return;
            BuildMenuContext();
            MenuModel.Reset(UIScreen.Pause);
            Menu.ShowPause();
            ApplyMenuState();
        }

        void OnMenuAction(MenuAction action, int value)
        {
            switch (action)
            {
                case MenuAction.NewGame:
                    if (MenuContext.HasPlaythrough && !_newGameConfirmPending)
                    {
                        _newGameConfirmPending = true;
                        MenuModel.RequestConfirm(MenuContext, MenuAction.NewGame, 0,
                            "لعبة جديدة؟", "التقدّم الحالي هيتصفّر، والخانات المحفوظة هتفضل زي ما هي.");
                    }
                    else { _newGameConfirmPending = false; Menu.Hide(); StartNewGame(); }
                    break;

                case MenuAction.Continue:       Menu.Hide(); LoadLatest(); break;
                case MenuAction.StartChapter:   Menu.Hide(); StartChapter(value); break;
                case MenuAction.Resume:         Menu.Hide(); ApplyMenuState(); break;

                case MenuAction.Quit:
                    Application.Quit();
                    break;

                case MenuAction.QuitToMain:
                    MenuModel.RequestConfirm(MenuContext, MenuAction.QuitToMain, 0,
                        "القائمة الرئيسية؟", "هيتم حفظ تلقائي قبل الرجوع.");
                    break;

                case MenuAction.RestartChapter:
                    MenuModel.RequestConfirm(MenuContext, MenuAction.RestartChapter, value,
                        "إعادة الفصل من أوله؟", "التقدّم من غير حفظ هيتفقد.");
                    break;

                case MenuAction.SaveToSlot:   DoManualSave(value); break;
                case MenuAction.LoadFromSlot: DoLoadSlot(value);   break;

                case MenuAction.DeleteSlot:
                    MenuModel.RequestConfirm(MenuContext, MenuAction.DeleteSlot, value,
                        "مسح الخانة؟", "مش هتقدر ترجّعها.");
                    break;

                case MenuAction.Confirm: ResolveConfirm(); break;
                case MenuAction.Cancel:
                    _newGameConfirmPending = false;
                    MenuContext.PendingConfirm = MenuAction.None;
                    Menu.Refresh();
                    break;

                // ── إعدادات: تطبيق فوري + ثبات بعد إعادة التشغيل ──
                case MenuAction.MasterDown: AudioLevels.Adjust("master", -AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.MasterUp:   AudioLevels.Adjust("master", +AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.MusicDown:  AudioLevels.Adjust("music",  -AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.MusicUp:    AudioLevels.Adjust("music",  +AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.SfxDown:    AudioLevels.Adjust("sfx",    -AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.SfxUp:      AudioLevels.Adjust("sfx",    +AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.VoiceDown:  AudioLevels.Adjust("voice",  -AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.VoiceUp:    AudioLevels.Adjust("voice",  +AudioLevels.Step); Menu.Refresh(); break;
                case MenuAction.CycleQuality:   QualityManager.Cycle();      Menu.Refresh(); break;
                case MenuAction.ToggleLanguage: Localization.Toggle();       Menu.Refresh(); break;
                case MenuAction.CycleLook:      ControlSettings.CycleLook(); Menu.Refresh(); break;
                case MenuAction.CycleStick:     ControlSettings.CycleStick();Menu.Refresh(); break;
                case MenuAction.ToggleStickSide:ControlSettings.ToggleSide();Menu.Refresh(); break;
                case MenuAction.ToggleInvertY:  ControlSettings.ToggleInvertY(); Menu.Refresh(); break;
                case MenuAction.ToggleAutoRun:  ControlSettings.ToggleAutoRun(); Menu.Refresh(); break;
                case MenuAction.ResetControls:  ControlSettings.ResetAll();  Menu.Refresh(); break;

                // ── كروت ──
                case MenuAction.PlayPostCredit:
                    Menu.Hide();
                    PlayPostCredit(State != null ? State.EndingId : null);
                    break;
                case MenuAction.PlayTeaser:
                    Menu.Hide();
                    PlayTeaser(State != null ? State.EndingId : null);
                    break;
            }
        }

        void ResolveConfirm()
        {
            var action = MenuModel.ClearConfirm(MenuContext, out int value);
            switch (action)
            {
                case MenuAction.NewGame:        _newGameConfirmPending = false; Menu.Hide(); StartNewGame(); break;
                case MenuAction.DeleteSlot:     SaveManager.Delete(SaveManager.ManualFirst + value); BuildMenuContext(); Menu.Refresh(); break;
                case MenuAction.RestartChapter: Menu.Hide(); StartChapter(string.IsNullOrEmpty(State?.ChapterId) ? 1 : 1); break;
                case MenuAction.QuitToMain:
                    if (Saves != null) Saves.SaveUnsafe("quit-to-menu");
                    Menu.Hide(); ReturnToMainMenu();
                    break;
                default: Menu.Refresh(); break;
            }
        }

        // ─────────────────────────── حفظ/تحميل ───────────────────────────

        void DoManualSave(int menuSlotValue)
        {
            int slot = SaveManager.ManualFirst + menuSlotValue;   // 0→A(1) · 1→B(2) · 2→C(3)
            string message;
            if (Saves.ManualSave(slot, out message)) Toast.Show(message);
            else Toast.Show(message, 3.2f, error: true);

            BuildMenuContext();
            if (Menu.IsOpen) Menu.Refresh();
        }

        void DoLoadSlot(int menuSlotValue)
        {
            int slot = SaveManager.ManualFirst + menuSlotValue;
            if (!SaveManager.Peek(slot, out _, out _))
            {
                Toast.Show("الخانة دي فاضية أو تالفة", 3f, error: true);
                return;
            }
            LoadFrom(slot);
        }

        /// <summary>Continue — أحدث حفظ صالح (تلقائي أو يدوي).</summary>
        public void DebugContinue() => LoadLatest();

        public void LoadLatest()
        {
            int slot = SaveManager.LatestSlot();
            if (slot < 0) { Toast.Show("مفيش حفظ لسه", 2.5f, error: true); return; }
            LoadFrom(slot);
        }

        void LoadFrom(int slot)
        {
            if (!SaveManager.Load(slot, State, out SaveHeader header))
            {
                Toast.Show("الحفظ ده تالف أو ناقص", 3f, error: true);
                return;
            }

            Menu.Hide();
            ApplyMenuState();
            Runner.LoadAt(State.CurrentNodeId);     // مش LoadScene — بيحافظ على الـbootstrap
            BuildMenuContext();
            Toast.Show("اتحمّل: " + (header.nodeLabel ?? header.nodeId), 2.6f);
            Debug.Log($"[TLC/Boot-4.4] ✔ استعادة من الخانة {slot} · {State.ChapterId}/{State.CurrentNodeId}" +
                      $" · أعلام={State.Flags.Count} · قرارات={State.Decisions.Count}");
        }

        public void StartNewGame()
        {
            State.Reset();
            State.ChapterId = "chapter01";
            State.CurrentNodeId = "c1_open";
            Progress.Current.lastChapter = 1;
            Progress.Save();

            Menu.Hide();
            ApplyMenuState();
            Runner.LoadAt("c1_open");
            if (Saves != null) Saves.SaveUnsafe("chapter-start");
            BuildMenuContext();
            Debug.Log("[TLC/Boot-4.4] لعبة جديدة من c1_open");
        }

        void StartChapter(int chapter)
        {
            Debug.Log($"[TLC/Boot-4.4] بدء الفصل {chapter}");
            // الفصل ٠٢/٠٣ مش متاحين للعب في النسخة دي — القوائم بتقفلهم
            if (chapter == 1) { StartNewGame(); return; }
            Toast.Show("الفصل ده لسه مش متاح", 3f, error: true);
            ReturnToMainMenu();
        }

        void ReturnToMainMenu()
        {
            MenuModel.Reset(UIScreen.Title);
            BuildMenuContext();
            Menu.Show(UIScreen.Title);
            ApplyMenuState();
        }

        // ─────────────────────────── الكروت ───────────────────────────

        public void PlayPostCredit(string endingId)
        {
            if (!PostCreditSequence.IsSupported(endingId))
            {
                Debug.LogWarning($"[TLC/Boot-4.4] ما-بعد-نهاية '{endingId}' مش موجود (متوقع: الفصل ٢ مش متاح)");
                return;
            }

            _inCards = true;
            Progress.MarkPostCredit(endingId);
            Cards.Play(PostCreditSequence.For(endingId), OnPostCreditFinished);
            Debug.Log($"[TLC/Boot-4.4] ▶ ما بعد النهاية {endingId}");
        }

        void OnPostCreditFinished(string seqId)
        {
            _inCards = false;
            PlayTeaser(State != null ? State.EndingId : null);
        }

        public void PlayTeaser(string endingId)
        {
            _inCards = true;
            Progress.MarkCinematic(ChapterTwoTeaser.CinematicId);
            Progress.Current.teaserSeen = true;
            Progress.Save();
            Cards.Play(ChapterTwoTeaser.For(endingId), OnCardsFinished);
        }

        void OnCardsFinished(string id)
        {
            _inCards = false;
            ApplyMenuState();

            if (DebugSuppressSceneChange) return;   // الاختبارات مش عايزة المشهد يتغيّر
            _returnToMenuPending = true;
            ReturnToMainMenu();
            _returnToMenuPending = false;
        }

        // ─────────────────────────── API الاختبارات (Phase44Tests) ───────────────────────────

        public bool DebugRunnerReady => Runner != null && Runner.Story != null && State != null;
        public string DebugChapterId => State != null ? State.ChapterId : null;
        public string DebugNodeId => State != null ? State.CurrentNodeId : null;
        public string DebugEndingId => State != null ? State.EndingId : null;
        public float DebugPlaytime => State != null ? State.PlaytimeSeconds : 0f;
        public GameState DebugState => State;
        public int DebugDecisionCount => State != null ? State.Decisions.Count : 0;
        public int DebugFlagCount => State != null ? State.Flags.Count : 0;
        public float DebugStat(string key) => State != null ? State.Get(key) : 0f;
        public bool DebugWaitingForInput => Runner != null && Runner.WaitingForChoice;
        public float DebugTimeScale => Time.timeScale;
        public string DebugLocationName => LocationName;
        public Toast DebugToast => Toast;
        public CardPlayer DebugCards => Cards;
        public MenuSystem DebugMenu => Menu;
        public MenuModel DebugMenuModel => MenuModel;
        public MenuContext DebugMenuContext => MenuContext;
        public bool DebugConfirmPending => MenuContext != null && MenuContext.PendingConfirm != MenuAction.None;
        public int DebugScreensShown => _screensShown;

        public void DebugChoose(int index) => Runner.Choose(index);

        public int DebugAdvanceUntilChoice()
        {
            int steps = 0;
            while (!Runner.WaitingForChoice && steps < 400) { Runner.Advance(); steps++; }
            return steps;
        }

        public int DebugAutoPlay(int decisions)
        {
            int made = 0;
            for (int i = 0; i < decisions && made < decisions; i++)
            {
                DebugAdvanceUntilChoice();
                if (!Runner.WaitingForChoice) break;
                Runner.Choose(0);
                made++;
                if (i < decisions - 1) DebugAdvanceLines(3);
            }
            return made;
        }

        public void DebugAdvanceLines(int lines)
        {
            for (int i = 0; i < lines && !Runner.WaitingForChoice; i++) Runner.Advance();
        }

        public bool DebugReachEnding(string endingId)
        {
            var node = Runner.Story != null ? Runner.Story.Get(endingId) : null;
            if (node == null) return false;
            OnEnding(endingId);
            return true;
        }

        public void DebugManualSave(int menuSlotValue) => DoManualSave(menuSlotValue);
        public void DebugLoadSlot(int menuSlotValue) => DoLoadSlot(menuSlotValue);
        public void DebugOpenPause() { BuildMenuContext(); MenuModel.Reset(UIScreen.Pause); Menu.ShowPause(); ApplyMenuState(); }
        public void DebugClosePause() { Menu.Hide(); ApplyMenuState(); }

        public void DebugShowScreen(UIScreen screen)
        {
            BuildMenuContext();
            MenuModel.Reset(screen);
            Menu.Show(screen);
            ApplyMenuState();
            _screensShown++;
        }

        public void DebugMenuRefresh()
        {
            if (Menu != null) { Menu.SetContext(MenuContext); Menu.Refresh(); }
        }
    }
}
