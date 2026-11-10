# PATCH — `GameState.cs` · `Progress.cs` · `SaveSystem.cs`

---

## أ) `Scripts/Core/GameState.cs`

### ١) `Stats` لازم تبقى قابلة للقراءة (الاختبارات بتقارنها)

```csharp
public Dictionary<string, float> Stats { get; private set; } = new Dictionary<string, float>();
public List<string> Flags { get; private set; } = new List<string>();
public List<string> Decisions { get; private set; } = new List<string>();
```

### ٢) DTO + تحويل

```csharp
[System.Serializable]
public class GameStateDto
{
    public int version = 1;                 // v1 = مسطّح (قديم) · v2 = جوه SaveFile
    public string chapterId;
    public string currentNodeId;
    public string endingId;
    public float playtimeSeconds;
    public string savedAtUtc;
    public List<string> flags = new List<string>();
    public List<string> decisions = new List<string>();
    public List<string> statKeys = new List<string>();
    public List<float> statValues = new List<float>();
}

public GameStateDto ToDto()
{
    var dto = new GameStateDto
    {
        chapterId = ChapterId,
        currentNodeId = CurrentNodeId,
        endingId = EndingId,
        playtimeSeconds = PlaytimeSeconds,
        savedAtUtc = System.DateTime.UtcNow.ToString("o")
    };
    dto.flags.AddRange(Flags);
    dto.decisions.AddRange(Decisions);
    foreach (var kv in Stats) { dto.statKeys.Add(kv.Key); dto.statValues.Add(kv.Value); }
    return dto;
}

public void FromDto(GameStateDto dto)
{
    if (dto == null) return;
    ChapterId = dto.chapterId;
    CurrentNodeId = dto.currentNodeId;
    EndingId = dto.endingId;
    PlaytimeSeconds = dto.playtimeSeconds;

    Flags.Clear();      if (dto.flags != null) Flags.AddRange(dto.flags);
    Decisions.Clear();  if (dto.decisions != null) Decisions.AddRange(dto.decisions);

    Stats.Clear();
    int n = dto.statKeys != null ? dto.statKeys.Count : 0;
    for (int i = 0; i < n && i < dto.statValues.Count; i++)
        Stats[dto.statKeys[i]] = dto.statValues[i];
}
```

> **مهم:** `JsonUtility` مش بيسلسل `Dictionary` — عشان كده بنستخدم قائمتين متوازيتين
> (`statKeys`/`statValues`). لو استخدمت `Dictionary` مباشرة الحفظ هيطلع فاضي والاستعادة
> هتنجح شكلياً وتفشل فعلياً.

### ٣) ثوابت أسماء الإحصائيات (بتتفصل عن الاستخدام المباشر للنص)

```csharp
public const string TrustMurad  = "Trust_Murad";
public const string TrustLaila  = "Trust_Laila";
public const string TrustYassin = "Trust_Yassin";
```

---

## ب) `Scripts/Core/Progress.cs`

```csharp
public bool HasScene(string sceneId)     => Current.visitedScenes.Contains(sceneId);
public bool HasCinematic(string id)      => Current.viewedCinematics.Contains(id);
public bool HasSeenPostCredit(string id) => Current.seenPostCredits.Contains(id);
public bool HasPostCreditAny()           => Current.seenPostCredits.Count > 0;
public int  SeenCount                     => Current.seenEndings.Count;

public void MarkPostCredit(string id)
{
    if (!Current.seenPostCredits.Contains(id)) Current.seenPostCredits.Add(id);
    Save();
}

public void MarkCinematic(string id)
{
    if (!Current.viewedCinematics.Contains(id)) Current.viewedCinematics.Add(id);
    Save();
}

/// <summary>حقن حالة مختبرة (استخدام الاختبارات/الاستعادة فقط).</summary>
public static void InjectForTests(Data d)
{
    _data = d ?? new Data();
    _loaded = true;
}
```

الحقول المطلوبة في `Data`: `unlockedChapters, lastChapter, seenEndings, seenPostCredits,
viewedCinematics, visitedScenes, teaserSeen`.

---

## ج) `Scripts/Core/SaveSystem.cs` (النسخة القديمة — ١٠١ سطر)

الملف القديم بيفضل موجود للتوافق فقط، عشان `Phase44Tests` بينادي `SaveSystem.ManualSlotA/B/C`.
**ممنوع الحفظ من خلاله** — كل الحفظ بيتكتب بـ`SaveManager`.

```csharp
public const int ManualSlotA = 1;
public const int ManualSlotB = 2;
public const int ManualSlotC = 3;
```

أي دالة قديمة في `SaveSystem` بتحفظ (زي `SaveSlot`/`LoadSlot` القديمة) لازم تتحوّل لتمرير
على `SaveManager`، وإلا هيبقى فيه مسارين حفظ بيكتبوا بصيغتين مختلفتين على نفس المجلد.

```csharp
[System.Obsolete("4.4 — استخدم SaveManager.Save/Load")]
public static bool SaveSlot(int slot) => false;   // ما بيتستخدمش
```
