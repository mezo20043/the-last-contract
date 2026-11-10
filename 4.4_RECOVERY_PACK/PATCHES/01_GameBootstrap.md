# PATCH — `Assets/_Project/Scripts/Bootstrap/GameBootstrap.cs`

الملف الأساسي اللي بيربط كل حاجة. التعديلات بتتضاف في الأماكن المذكورة **من غير** تغيير
منطق 4.3 الموجود (الانتقالات، الحوار، الكاميرا).

---

## ١) الحقول (بعد حقول 4.3 مباشرة)

```csharp
[Header("4.4 — حفظ وقوائم")]
public SaveController Saves;
public UI.MenuSystem Menu;
public UI.Toast Toast;
public UI.CardPlayer Cards;
public UI.MenuModel MenuModel = new UI.MenuModel();
public UI.MenuContext MenuContext = new UI.MenuContext();

readonly System.Collections.Generic.List<string> _seenCinematics = new();
float _decisionCooldown;
int _linesSinceNode;
bool _paused;
bool _inCards;
bool _newGameConfirmPending;

public bool DebugSuppressSceneChange;   // الاختبارات بتمنع تغيير المشهد
public bool DebugRunnerReady => _runner != null && _state != null;
public bool DebugConfirmPending => MenuContext != null && MenuContext.PendingConfirm != MenuAction.None;
```

---

## ٢) البناء (في `Awake` أو `BuildSystems` حسب الملف الحالي)

```csharp
// الكروت والرسائل والقوائم بتتبني مرة واحدة
Cards = new GameObject("CardPlayer").AddComponent<UI.CardPlayer>();
Cards.transform.SetParent(transform, false);
Cards.Finished += OnCardsFinished;

Toast = new GameObject("Toast").AddComponent<UI.Toast>();
Toast.transform.SetParent(transform, false);

Menu = new GameObject("MenuSystem").AddComponent<UI.MenuSystem>();
Menu.transform.SetParent(transform, false);
Menu.ActionRequested += OnMenuAction;
Menu.Build();
Menu.Hide();

Saves = gameObject.AddComponent<SaveController>();
Saves.Bind(_state, _runner, _camera, _cinematics, Menu,
           () => ChapterId, () => ChapterTitle, () => LocationName,
           () => IsUnstableState());
```

> `IsUnstableState()` = الانتقال/الكروت/البلوك السردي. **مش** لقطة كاميرا الحوار
> (دي بتشتغل كل سطر وبتخلي الحفظ مرفوض دايماً).

---

## ٣) `Update` — الإضافات

```csharp
void Update()
{
    // 4.3 موجود
    ...

    // 4.4 — وقت اللعب + الحفظ التلقائي
    Saves.Tick(Time.unscaledDeltaTime);

    if (_decisionCooldown > 0f) _decisionCooldown -= Time.unscaledDeltaTime;

    if (_runner != null && _runner.Story != null && !_inCards && !_paused)
    {
        _linesSinceNode++;
        if (_linesSinceNode >= 12)   // نقطة آمنة: كل ١٢ سطر
        {
            _linesSinceNode = 0;
            Saves.Autosave("line-tick");
        }
    }

    // أندرويد back
    if (Input.GetKeyDown(KeyCode.Escape) && !_inCards)
    {
        if (Menu != null && Menu.IsOpen) Menu.Back();
        else OpenPause();
    }
}
```

---

## ٤) نقاط الحفظ التلقائي (بتتضاف جوه المعالجات الموجودة)

```csharp
// بعد كل قرار (في المعالج اللي بيتنادى لما اللاعب يختار)
void OnDecision(string decisionId)
{
    ... منطق 4.3 ...
    _decisionCooldown = 1.2f;      // فاصل قبل الحفظ التلقائي
    Invoke(nameof(AutosaveAfterDecision), 1.2f);
}
void AutosaveAfterDecision() => Saves.Autosave("decision");

// بعد أي انتقال مشهد/فصل
void OnSceneChanged(string sceneId)
{
    ... منطق 4.3 ...
    Saves.Autosave("scene:" + sceneId, force: false);
}

// عند الوصول لنهاية
void OnEnding(string endingId)
{
    ... منطق 4.3 ...
    _state.EndingId = endingId;
    Saves.SaveUnsafe("ending:" + endingId);
    Progress.MarkEnding(endingId);
    PlayPostCredit(endingId);      // 4.4 — كروت ما بعد النهاية
}
```

---

## ٥) القوائم: القراءة من اللعبة

```csharp
void BuildMenuContext()
{
    var ctx = new UI.MenuContext
    {
        HasSave = SaveManager.LatestSlot() >= 0,
        HasPlaythrough = SaveManager.LatestSlot() >= 0 || _state.PlaytimeSeconds > 5f,
        UnlockedChapters = Progress.Current.unlockedChapters,
        InGame = _runner != null && _runner.Story != null,
        PlaytimeSeconds = _state.PlaytimeSeconds,
        TotalEndings = UI.MenuModel.EndingCount,
        ExtrasProgressLine = ExtrasCatalog.ProgressLine(),
        LastEndingId = _state.EndingId,
        TeaserUnlocked = Progress.HasCinematic(ChapterTwoTeaser.CinematicId) || Progress.Current.teaserSeen,
        CanPlayPostCredit = PostCreditSequence.IsSupported(_state.EndingId)
    };

    foreach (var e in Progress.Current.seenEndings) ctx.SeenEndings.Add(e);

    for (int slot = 0; slot < SaveManager.SlotCount; slot++)
        ctx.Slots.Add(BuildSlotSummary(slot));

    foreach (var i in ExtrasCatalog.Scenes())       ctx.ExtrasScenes.Add(ToLike(i));
    foreach (var i in ExtrasCatalog.Cinematics())   ctx.ExtrasCinematics.Add(ToLike(i));
    foreach (var i in ExtrasCatalog.Characters())   ctx.ExtrasCharacters.Add(ToLike(i));
    foreach (var i in ExtrasCatalog.Records(_state))ctx.ExtrasRecords.Add(ToLike(i));

    bool confirm = MenuContext != null && MenuContext.PendingConfirm != MenuAction.None;
    var pending = MenuContext;
    MenuContext = ctx;
    if (confirm && pending != null) { ctx.PendingConfirm = pending.PendingConfirm; ctx.PendingValue = pending.PendingValue; ctx.ConfirmTitle = pending.ConfirmTitle; ctx.ConfirmBody = pending.ConfirmBody; }

    if (Menu != null) Menu.SetContext(ctx);
    ApplyMenuToGame();
}

static UI.ExtrasItemLike ToLike(ExtrasItem i) => new UI.ExtrasItemLike(i.id, i.title, i.detail, i.unlocked);

UI.SlotSummary BuildSlotSummary(int slot)
{
    var s = new UI.SlotSummary { slot = slot, isAuto = slot == SaveManager.AutoSlot };
    if (!SaveManager.Peek(slot, out SaveHeader h, out SaveMeta m)) { s.corrupted = SaveManager.Exists(slot); return s; }
    s.empty = false;
    s.chapterTitle = h.chapterTitle ?? h.chapterId;
    s.locationName = m.locationName;
    s.nodeLabel = h.nodeLabel;
    s.dateText = SaveManager.PrettyDate(h.savedAtUtc);
    s.playtimeText = SaveManager.PrettyPlaytime(h.playtimeSeconds);
    return s;
}

void ApplyMenuToGame()
{
    bool menuUp = Menu != null && Menu.IsOpen;
    bool blocking = menuUp || _inCards;

    _paused = menuUp && MenuModel.Current == UIScreen.Pause;
    if (Saves != null) Saves.Paused = _paused;

    if (Mathf.Abs(Time.timeScale - (_paused ? 0f : 1f)) > 0.01f)
        Time.timeScale = _paused ? 0f : 1f;
}
```

---

## ٦) معالج أزرار القوائم

```csharp
void OnMenuAction(MenuAction action, int value)
{
    switch (action)
    {
        case MenuAction.NewGame:
            if (MenuContext.HasPlaythrough && MenuContext.PendingConfirm == MenuAction.None)
                MenuModel.RequestConfirm(MenuContext, MenuAction.NewGame, 0,
                    "لعبة جديدة؟", "التقدّم الحالي مش هيتحفظ فوق الخانات الموجودة.");
            else { Menu.Hide(); StartNewGame(); }
            break;

        case MenuAction.Continue:      LoadLatest();                 break;
        case MenuAction.StartChapter:  Menu.Hide(); StartChapter(value); break;

        case MenuAction.Resume:        Menu.Hide(); ApplyMenuToGame(); break;
        case MenuAction.QuitToMain:    MenuModel.RequestConfirm(MenuContext, MenuAction.QuitToMain, 0,
                                          "القائمة الرئيسية؟", "آخر حفظ اتوماتيك هيتحفظ الأول."); break;
        case MenuAction.RestartChapter:MenuModel.RequestConfirm(MenuContext, MenuAction.RestartChapter, 0,
                                          "إعادة الفصل من أوله؟", "التقدّم من غير حفظ هيتفقد."); break;

        case MenuAction.SaveToSlot:    DoManualSave(value);          break;
        case MenuAction.LoadFromSlot:  DoLoadSlot(value);            break;
        case MenuAction.DeleteSlot:    MenuModel.RequestConfirm(MenuContext, MenuAction.DeleteSlot, value,
                                          "مسح الخانة؟", "مش هتقدر ترجّعها."); break;

        case MenuAction.Confirm:       ResolveConfirm();             break;
        case MenuAction.Cancel:        MenuContext.PendingConfirm = MenuAction.None; Menu.Refresh(); break;

        case MenuAction.Quit:          Application.Quit();           break;

        // إعدادات — بتتطبّق فوراً وبتتحفظ في PlayerPrefs
        case MenuAction.MasterDown: AudioLevels.Adjust("master", -AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.MasterUp:   AudioLevels.Adjust("master", +AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.MusicDown:  AudioLevels.Adjust("music",  -AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.MusicUp:    AudioLevels.Adjust("music",  +AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.SfxDown:    AudioLevels.Adjust("sfx",    -AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.SfxUp:      AudioLevels.Adjust("sfx",    +AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.VoiceDown:  AudioLevels.Adjust("voice",  -AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.VoiceUp:    AudioLevels.Adjust("voice",  +AudioLevels.Step); Menu.Refresh(); break;
        case MenuAction.CycleQuality:  QualityManager.Cycle();     Menu.Refresh(); break;
        case MenuAction.ToggleLanguage:Localization.Toggle();      Menu.Refresh(); break;
        case MenuAction.CycleLook:     ControlSettings.CycleLook();Menu.Refresh(); break;
        case MenuAction.CycleStick:    ControlSettings.CycleStick();Menu.Refresh(); break;
        case MenuAction.ToggleStickSide:ControlSettings.ToggleSide();Menu.Refresh(); break;
        case MenuAction.ToggleInvertY: ControlSettings.ToggleInvertY();Menu.Refresh(); break;
        case MenuAction.ToggleAutoRun: ControlSettings.ToggleAutoRun();Menu.Refresh(); break;
        case MenuAction.ResetControls: ControlSettings.ResetAll(); Menu.Refresh(); break;

        // كروت
        case MenuAction.PlayPostCredit: Menu.Hide(); PlayPostCredit(value == 0 ? _state.EndingId : null); break;
        case MenuAction.PlayTeaser:     Menu.Hide(); PlayTeaser(_state.EndingId); break;
    }
}
```

---

## ٧) الحفظ والتحميل

```csharp
void DoManualSave(int menuSlotValue)
{
    int slot = SaveManager.ManualFirst + menuSlotValue;   // 0→1(A) · 1→2(B) · 2→3(C)
    if (Saves.ManualSave(slot, out string message)) Toast.Show(message);
    else Toast.Show(message, 3.2f, error: true);
    BuildMenuContext();
}

void DoLoadSlot(int menuSlotValue)
{
    int slot = SaveManager.ManualFirst + menuSlotValue;   // 0 = خانة A
    LoadFrom(slot);
}

void LoadFrom(int slot)
{
    if (!SaveManager.Load(slot, _state, out SaveHeader header))
    {
        Toast.Show("الخانة دي تالفة أو فاضية", 3f, error: true);
        return;
    }

    Menu.Hide();
    ApplyMenuToGame();
    _runner.LoadAt(_state.CurrentNodeId);      // مش LoadScene — الحالة راجعة أصلاً
    BuildMenuContext();                        // الصورة والتفاصيل تتحدّث
    Toast.Show("اتحمّل: " + (header.nodeLabel ?? header.nodeId), 2.6f);
    Debug.Log($"[TLC/Boot] ✔ استعادة من الخانة {slot} · {_state.ChapterId}/{_state.CurrentNodeId}");
}

void LoadLatest()
{
    int slot = SaveManager.LatestSlot();
    if (slot < 0) { Toast.Show("مفيش حفظ لسه", 2.5f, error: true); return; }
    LoadFrom(slot);
}
```

---

## ٨) الكروت: ما بعد النهاية ثم التيزر

```csharp
public void PlayPostCredit(string endingId)
{
    if (!PostCreditSequence.IsSupported(endingId)) { Debug.LogWarning($"[TLC/Boot] ما-بعد-نهاية لـ'{endingId}' مش موجود (متوقع، الفصل ٢ مش متاح)"); return; }

    var seq = PostCreditSequence.For(endingId);
    _inCards = true;
    ApplyMenuToGame();
    Progress.MarkPostCredit(endingId);
    Cards.Play(seq, OnPostCreditFinished);
}

void OnPostCreditFinished(string seqId)
{
    _inCards = false;
    PlayTeaser(_state.EndingId);
}

public void PlayTeaser(string endingId)
{
    var teaser = ChapterTwoTeaser.For(endingId);
    _inCards = true;
    ApplyMenuToGame();
    Progress.MarkCinematic(ChapterTwoTeaser.CinematicId);
    Progress.Current.teaserSeen = true;
    Progress.Save();
    Cards.Play(teaser, id => OnCardsFinished(id));
}

void OnCardsFinished(string id)
{
    _inCards = false;
    ApplyMenuToGame();
    if (!DebugSuppressSceneChange) _menuReturnPending = true;
    if (_menuReturnPending) { _menuReturnPending = false; ReturnToMainMenu(); }
}

void ReturnToMainMenu()
{
    MenuModel.Reset(UIScreen.Title);
    BuildMenuContext();
    Menu.Show(UIScreen.Title);
}
```

---

## ٩) `StartNewGame` — لازم يصفّر كل حاجة

```csharp
public void StartNewGame()
{
    _state.Reset();                   // أعلام/قرارات/إحصائيات/وقت
    _state.ChapterId = "chapter01";
    _state.CurrentNodeId = "c1_open";
    Progress.Current.lastChapter = 1;
    Progress.Save();

    Menu.Hide();
    ApplyMenuToGame();
    _runner.LoadAt("c1_open");
    Saves.SaveUnsafe("chapter-start");
    Debug.Log("[TLC/Boot] لعبة جديدة من c1_open");
}
```

---

## ١٠) الـAPI اللي `Phase44Tests` بينادي عليه (لازم كله موجود)

```csharp
public bool  DebugRunnerReady => ...;
public string DebugChapterId, DebugNodeId, DebugEndingId;
public float  DebugPlaytime;
public GameState DebugState => _state;
public int DebugDecisionCount, DebugFlagCount;
public float DebugStat(string key);
public bool DebugWaitingForInput { get; }
public void DebugChoose(int index);
public int  DebugAdvanceUntilChoice();
public int  DebugAutoPlay(int decisions);
public void DebugAdvanceLines(int lines);
public bool DebugReachEnding(string endingId);
public void DebugContinue();
public void DebugManualSave(int menuSlotValue);
public void DebugLoadSlot(int menuSlotValue);
public void DebugOpenPause();
public void DebugClosePause();
public void DebugShowScreen(UIScreen screen);
public void DebugMenuRefresh();
public float DebugTimeScale => Time.timeScale;
public string DebugLocationName;
public UI.Toast DebugToast => Toast;
public UI.CardPlayer DebugCards => Cards;
public UI.MenuSystem DebugMenu => Menu;
public UI.MenuModel DebugMenuModel => MenuModel;
public UI.MenuContext DebugMenuContext => MenuContext;
```

`DebugContinue()` = `LoadLatest()` بس مع لوج صريح، عشان الاختبار يثبت إن Continue عملت شغل.
