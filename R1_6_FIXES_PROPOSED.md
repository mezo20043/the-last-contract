# R1.6 — إصلاحات مقترحة لسكربتات Auto-run (لم تُطبَّق)

> ⚠️ **مفيش سطر واحد من اللي تحت اتطبّق.** ده مقترح للمراجعة والموافقة بس.
> المبدأ: **صفر حذف تلقائي · صفر تعديل إعدادات تلقائي** — أي عمل هدّام يبقى من المنيو بضغطة صريحة منك.

---

## ١) `TMPEssentials.cs` — 🟠 **الأخطر: بيمسح أصل تلقائيًا**

### السلوك الحالي (من الكود)
```csharp
static TMPEssentials()  // [InitializeOnLoad]
{
    EditorApplication.delayCall += () => Ensure(false);
}

public static void Ensure(bool verbose)
{
    bool hasSettings = AssetDatabase.LoadAssetAtPath<Object>(SettingsPath) != null;
    bool hasShaders  = Directory.Exists("Assets/TextMesh Pro/Shaders");
    if (hasSettings && hasShaders) return;                 // تمام
    if (hasSettings && !hasShaders)
    {
        AssetDatabase.DeleteAsset(SettingsPath);           // ← حذف أصل
    }
    AssetDatabase.ImportPackage(pkg, false);               // ← استيراد
    ...
    AssetDatabase.CreateAsset(settings, SettingsPath);     // ← إنشاء أصل
}
```

### المشكلة في مشروعنا تحديدًا
| فحص | النتيجة |
|---|---|
| `Assets/TextMesh Pro/Resources/TMP Settings.asset` | **موجود** (GUID `e0014e32303ebb3a797d16ac45c720d2`) |
| `Assets/TextMesh Pro/Shaders/` | **مش موجود** |
⇒ عند أول فتح تفاعلي، السكربت **هيحذف TMP Settings** ويحاول يستورد الحزمة من جديد ⇒ **GUID جديد** ⇒ أي إشارة للأصل القديم تنكسر.

> ملاحظة: في تشغيلنا الـheadless (`-batchmode -quit`) الـ`delayCall` ما اتنفذش، فالأصل ما اتلمسش — يعني **المشكلة هتظهر على جهازك، مش هنا**.

### الإصلاح المقترح (مقترح فقط)
```csharp
static TMPEssentials()
{
    EditorApplication.delayCall += AutoCheck;      // بدل: () => Ensure(false)
}

/// <summary>فحص غير هدّام — بيتنفذ تلقائيًا. مابيحذفش ومابينشئش ومابيستوردش.</summary>
static void AutoCheck()
{
    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
    bool hasSettings = AssetDatabase.LoadAssetAtPath<Object>(SettingsPath) != null;
    bool hasShaders  = Directory.Exists("Assets/TextMesh Pro/Shaders");
    if (hasSettings && hasShaders) return;         // كل حاجة تمام

    Debug.LogWarning("[TLC] موارد TMP ناقصة (Settings=" + hasSettings + " · Shaders=" + hasShaders + ") — " +
                     "شغّل «TLC/استيراد موارد TMP» يدويًا. الفحص التلقائي مش بيعدّل أي أصل.");
}
```
`Ensure(true)` تفضل زي ما هي = المسار الهدّام **من المنيو بس**.

**الأثر:** صفر حذف تلقائي · صفر GUID churn · لسه بيبلّغك لو TMP ناقص.

---

## ٢) `ProjectBootstrapper.cs` — 🟢 خطر منخفض (بس يتحسّن)

### السلوك الحالي
```csharp
static ProjectBootstrapper() { EditorApplication.delayCall += EnsureScenes; }
```
`EnsureScenes()` بتعمل: `Directory.CreateDirectory` · إنشاء Main/Play **لو ناقصين** · `EnsureInBuildSettings()` · `AssetDatabase.Refresh()` — **كل فتح**.

### التحليل الفعلي
| الجزء | هدّام؟ |
|---|---|
| إنشاء المشاهد | ❌ بينشئ **بس لو ناقص** |
| `EnsureInBuildSettings()` | ❌ **idempotent** — بيقارن وبيخرج من غير كتابة لو مطابق (شفته في الكود، سطر 89-98) |
| `AssetDatabase.Refresh()` | ❌ مش بيغيّر محتوى — بس إعادة مسح |

⇒ **مفيش حذف ومفيش تعديل إعدادات فعلي.** المشكلة بس إنه بيشتغل كل فتح بدون داعي.

### الإصلاح المقترح (مقترح فقط)
```csharp
static ProjectBootstrapper()
{
    EditorApplication.delayCall += AutoEnsureOnce;
}

static void AutoEnsureOnce()
{
    if (EditorApplication.isPlayingOrWillChangePlaymode) return;
    if (SessionState.GetBool("TLC_BootstrapChecked", false)) return;
    SessionState.SetBool("TLC_BootstrapChecked", true);

    bool mainMissing = !File.Exists(MainPath);
    bool playMissing = !File.Exists(PlayPath);
    if (!mainMissing && !playMissing) return;   // مفيش مشاهد ناقصة ⇒ صفر كتابة وصفر Refresh

    EnsureScenes();                             // بينشئ الناقص بس
}
```

---

## ٣) الـ7 سكربتات التانية (مش محتاجة تعديل)
`PlayModeCapture` · `CollisionTest` · `ChapterPlaytestCapture` · `ChapterPlaythrough` · `EnvironmentCapture` · `MenuFlowCapture` · `FirstOpenSetup`
— كلهم بيتحققوا من `SessionState` الأول ومابيبدأوش لوحدهم، و`FirstOpenSetup` بينشئ الـcontroller **بس لو مش موجود**. ✅ سيبتهم زي ما هم.

---

## قرارك المطلوب
- [ ] أوافق على تعديل `TMPEssentials` (منع الحذف التلقائي)
- [ ] أوافق على تعديل `ProjectBootstrapper` (منع العمل لو مفيش حاجة ناقصة)
- [ ] سيبتهم زي ما هم (وأنا أسجّل الخطر في التقرير)
