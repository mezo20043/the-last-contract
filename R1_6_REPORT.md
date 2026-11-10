# R1.6 — مراجعة الأصول المولَّدة · سكربتات الاختبار · التحقيق في URP Global Settings

**التاريخ:** 2026-11-10 · **Unity:** 6000.0.28f1 · **النطاق:** مراجعة + شفاء محدود — **بدون R2 · بدون تحسينات رسومية · بدون لمس القصة/القرارات/النهايات**
**قاعدة:** مفيش حذف لأي ملف · مفيش اعتماد لأي أصل مولَّد من غير قرارك · مفيش Force Push

---

## ١) مراجعة الأصول والـ88 ملف Meta المولَّدة

### ١.١ الإحصاء الكامل
| البند | العدد |
|---|---|
| ملفات جديدة في نسخة العمل بعد أول فتح | **104** |
| منها `.meta` مولَّدة | **88** |
| منها ملفات أصول/نواتج | **16** |
| التصنيف: **ضروري** | **88** |
| التصنيف: **قرار مطلوب** | **2** (أصول URP) |
| التصنيف: **مؤقت — مش للريبو** | **14** (أدوات ونواتج R1.5 + مشهد تجريبي) |

📄 **القائمة الكاملة بالمسارات والـGUID والتصنيف والسبب:** `R1_6_GENERATED_FILES_INVENTORY.csv` (104 صف).

### ١.٢ من وين جت الـ88؟
سببها الجذري: **٦٧ أصل في المشروع الأصلي مالهمش `.meta`** + **١٥ مجلد** + ٦ ملفات من R1.5.

| نوع الأصل من غير meta | العدد |
|---|---|
| `.jpg` (تكستشرات الـProps) | 35 |
| `.cs` (سكربتات، منها 12 في `Editor/`) | 19 |
| `.fbx` (موديلات Props) | 11 |
| `.json` (مانيفستات) | 2 |
| **الإجمالي** | **67** |
| + metas مجلدات | 15 |
| + ملفات R1.5 (سكربتات/نواتج/مشهد) | 6 |
| = **88** ✔ مطابق تمامًا للي ولّده Unity | |

### ١.٣ ضروري ولا مؤقت؟ (الحكم مع السبب)
| الفئة | الحكم | السبب |
|---|---|---|
| 35 `.jpg` + 11 `.fbx` (Props) | **ضروري** | الأصول مستخدمة في اللعبة عبر `Resources`؛ بدون `.meta` كل نسخة من الريبو هتولّد GUIDs مختلفة ⇒ مواد/مراجع عشوائية |
| 19 `.cs` | **ضروري** | GUID السكربت لازم يثبت لأي MonoBehaviour في مشهد/بريفاب |
| 2 `.json` (مانيفستات) | **ضروري** | مراجع توثيقية داخلية للأصول |
| 15 meta مجلد | **ضروري** | تثبيت GUID المجلد (مراجع المجلدات في Unity) |
| `TMP Settings.asset` + meta | **ضروري** | المشروع بيستخدم TMP (`TMPEssentials.cs`, `FontAssetBuilder.cs`) |
| `packages-lock.json` | **ضروري** | تثبيت نسخ الباقات (URP 17.0.3 · Cinemachine 3.1.3) |
| `ProjectSettings/*` المولَّدة | **مقبول** | إعدادات Unity 6 القياسية |
| `UniversalRenderPipelineGlobalSettings.asset` · `DefaultVolumeProfile.asset` | **قرارك** | انظر القسم ٣ |
| `R1_5_*.cs` · `R1_5_*.png/txt/json` · `R1_5_PLAYMODE_TEMP.unity` | **مؤقت — يُستبعد** | أدوات/نواتج تجربة R1.5، مش جزء من اللعبة |

### ١.٤ مراجعة الـGUIDs والمراجع — أرقام حقيقية
| فحص | النتيجة |
|---|---|
| ملفات `.meta` في المشروع الأصلي | 165 |
| ملفات فيها مراجع GUID | **5** فقط: `Main.unity` · `Play.unity` · `TLC_Character.controller` · `TLC_URP.asset` · `TLC_UniversalRenderer.asset` |
| `Main.unity` | 1 مرجع سكربت — **0 غير محلول** ✔ |
| `Play.unity` | 8 مراجع سكربت — **0 غير محلول** ✔ |
| `TLC_URP.asset` / `TLC_UniversalRenderer.asset` | 7 مراجع لشيدرز/سكربتات **الباقة** (URP) — محلولة من `PackageCache` (7091 GUID) ⇒ **مش مكسورة** ✔ |
| **مرجع مكسور حقيقي** | **1**: `GraphicsSettings.asset` بيشاور على `b72d668f01260d279a991baffeb2a8be` — أصل غير موجود في أي مكان (القسم ٣) |
| metas يتيمة في الأصل | **3** |

**الخلاصة:** المشاهد والـController سليمة تمامًا — مشكلة المراجع محصورة في أصل URP واحد.

---

## ٢) سكربتات الاختبار — أُضيفت واتحققت فعليًا

**الموقع:** `Assets/_Project/Editor/R16_Validation/TLC_R16_Validation.cs` (+ `.meta` بـGUID `c0e67802a0ac511e6b366dd6c5aaed28`)
**المنيو:** `TLC ▸ R1.6` — خمسة عناصر:

| العنصر | بيفحص |
|---|---|
| فحص الشخصيات (Humanoid + Resources) | `Resources.Load` للأربعة · `animationType=Human` · `avatar.isValid/isHuman` · عدد العظام · الـ15 فقرة الإجبارية · الكليبس |
| فحص Animator Controller | الحالات · `motion` مفقود؟ · المعاملات الأربعة · الكليبس |
| فحص المواد والشيدر | الشيدر · BaseMap · Normal · MetallicGloss |
| فحص سلامة المشروع | أصول من غير meta · metas يتيمة · سكربتات `[InitializeOnLoad]` |
| كل الفحوصات + تصدير JSON | الكل + `R1_6_VALIDATION.json` في جذر المشروع |

**ضمانات الالتزام بقواعدك:**
- ✅ **Read-only**: مفيش `CreateAsset` / `SaveAssets` / `DeleteAsset` / تعديل GameSettings أو الحفظ أو بيانات اللعبة.
- ✅ **ما بتشتغلش تلقائيًا**: مفيش `[InitializeOnLoad]` ولا `[DidReloadScripts]` ولا `AssetPostprocessor` — من المنيو بس.
- ✅ بترفض العمل في Play Mode.
- ✅ الكتابة الوحيدة = تقرير JSON، وبرضه بأمر صريح منك.

**تم التحقق بتشغيلها فعليًا داخل Unity (batchmode):**
```
── ١) الشخصيات ──
  Adam    Resources.Load=✔ Humanoid=✔ isValid=✔ isHuman=✔ عظام=23 ناقص=0 كليبس=2
  Murad   …  عظام=22 ناقص=0
  Laila   …  عظام=52 ناقص=0
  Yassin  …  عظام=52 ناقص=0
── ٢) Animator ──  controller=✔ Resources=✔ states=3 مفقود=0 · parameters: Speed/IsRunning/Grounded/Talking ✔
── ٣) المواد ──  4 × URP/Lit · BaseMap=✔ Normal=✔ MetallicGloss=✘ (شغل R2)
── ٤) سلامة ──    أصول من غير meta=0 (في نسخة مُستوردة) · metas يتيمة=0 · [InitializeOnLoad]=9
════ النتيجة: fails=0 · warnings=5 ════
```

---

## ٣) التحقيق في `UniversalRenderPipelineGlobalSettings.asset`

### ٣.١ بحث شامل عن نسخة أصلية — **النتيجة: غير موجودة**
| مصدر البحث | النتيجة |
|---|---|
| المشروع `/home/user/THE_LAST_CONTRACT` | الأصل غير موجود · **الـmeta موجود** بـGUID `8ff020c81f294ec49a14ae4b03bd6f33` |
| `THE_LAST_CONTRACT_v2.0.zip` (الأرشيف الأصلي) | **الـmeta بس** (189 بايت) — الـ`.asset` مش موجود ⇒ **الفقد أقدم من الأرشيف** |
| `RESTORED_CHARACTER_ASSETS/` | مش موجود (شخصيات بس) |
| `4.4_RECOVERY_PACK/` | مش موجود |
| **تاريخ Git كله** (`git log --all`) | مفيش — الريبو اتعمل **بعد** الفقد |
| بحث نصي عن GUID `b72d668…` في كل الـworkspace | **صفر نتيجة** |
| بحث عن GUID `8ff020c…` في المشروع كله | **صفر مرجع** (المeta نفسه بس) |

### ٣.٢ اللي اكتشفته عن سلوك Unity عند أول فتح
| قبل | بعد أول فتح |
|---|---|
| `UniversalRenderPipelineGlobalSettings.asset.meta` = `8ff020c…` | اتكتب فوقه **`cbab407bc0e5222a485e9191d17363ab`** (Unity بيعمل GUID جديد للأصل اللي بيعيد إنشاءه) |
| `DefaultVolumeProfile.asset.meta` = `24aa18f…` | **`70eea7ac64eaadc33ae8dcf2a9c8f4f8`** |
| `GraphicsSettings.asset` → `b72d668…` (مكسور) | اتعدّل لـ **`cbab407…`** (بقى شغال) |
| `texture_manifest.json.meta` (يتيم) | **Unity حذفه** |

**بس كده:** أول فتح عدّل **٣ ملفات** وحذف **١** — كلهم بسبب أصل URP الناقص.

### ٣.٣ مقارنة المحتوى الافتراضي بإعدادات المشروع
| البند | الحالة |
|---|---|
| `TLC_URP.asset` + `TLC_UniversalRenderer.asset` | **موجودين وسليمين** — إعدادات الـPipeline نفسها مش متأثرة |
| اللي ضاع = **Global Settings**: shader stripping · RenderGraph toggle · Debug shaders · Volume Profile الافتراضي | اتولّد **بالإعدادات الافتراضية** |
| `DefaultVolumeProfile` المولَّد | فاضي تمامًا (`components: []`) — لو كان فيه Look/Color Adjustments مخصّص ⇒ ضاع |
| الأثر المتوقع | **مش وظيفي** — الأغلب تأثير على حجم/أداء البناء (shader variants) ودرجة ألوان افتراضية |

### ٣.٤ الخيارات (محتاجة قرارك — مفيش واحد اتنفذ)

| الخيار | اللي بيحصل | مميزات | عيوب |
|---|---|---|---|
| **A — نقبل مولَّدات Unity** | نعتمد الأصلين الجديدين + `GraphicsSettings.asset` المعدّل + نحذف meta اليتيم | أبسط · حالة نظيفة · صفر dangling | تعديل في `ProjectSettings` + تغيير GUIDs + قبول الافتراضي |
| **B — نعيد الإنشاء بـGUID الأصلي** ⭐ | نكتب `UniversalRenderPipelineGlobalSettings.asset` + meta بـGUID **`b72d668…`** اللي `GraphicsSettings` بيطلبه ⇒ `ProjectSettings` **ما يتغيّرش** | أقل churn · الشجرة تفضل زي ما هي · المرجع القديم يرجع حيّ | تعديل 2 metas + كتابة يدوية للأصل (المحتوى برضه افتراضي) |
| **C — نسيبها dangling** | مفيش اعتماد | صفر تعديل | Unity هيولّد كل مرة، وكل جهاز GUID مختلف ⇒ فوضى في الـrepo ❌ |

**توصيتي: B** — بتسيب `ProjectSettings` سليم ومابتغيّرش ملفات مش من نطاق الشغل. **محتاج موافقتك قبل التنفيذ.**

---

## ٤) تحليل الـ149 تحذير — مفصولة بالخطورة

| # | الفئة | العدد | الحكم | الأثر |
|---|---|---|---|---|
| 1 | **`CS0618` API قديمة** في `VerificationRunner.cs` · `ProjectConfigurator.cs` · `ProjectBootstrapper.cs` | **96** | 🟡 مهم لاحقًا | مابتمنعش البناء على 6000.0.28f1 — لكن هتتحول لأخطاء في Unity 6.x الأحدث |
| 2 | **شيدرز**: `URP/Terrain/Lit` «All subshaders removed» + `#pragma only_renderers` | **24** | 🟢 بيئي | بسبب **GL Core + llvmpipe** (مفيش Terrain في المشروع). على Windows/DX11 مش هيظهر |
| 3 | **metas يتيمة**: DefaultVolumeProfile (3) · URP GlobalSettings (2) | **5** | 🟠 مهم | القسم ٣ |
| 4 | **`[InitializeOnLoad]` في 9 سكربتات** | — | 🟠 مهم | تحليل سلوكي تحت 👇 |
| 5 | ضوضاء بيئية: ALSA · FMOD · Gtk · Licensing · `LogAssemblyErrors (0ms)` | ~14 | 🟢 غير مؤثر | مفيش كارت صوت/عرض في الساندبوكس |

### ٤.١ الـ9 سكربتات اللي بتشتغل لوحدها — تحليل فعلي للكود
| السكربت | بيعمل إيه عند الفتح | خطورة |
|---|---|---|
| `FirstOpenSetup` (جوه `AnimatorControllerBuilder.cs`) | `AnimatorControllerBuilder.EnsureExists()` — **بينشئ الـcontroller بس لو مش موجود** | 🟢 آمن (مابيدهسش الموجود) |
| `ProjectBootstrapper` | بينشئ Main/Play لو ناقصين + **بينادي `EnsureInBuildSettings()` و`AssetDatabase.Refresh()` كل فتح** | 🟡 بيكتب في Build Settings كل فتح |
| `TMPEssentials` | لو TMP Settings موجود والشيدرز ناقصة ⇒ **`DeleteAsset(TMP Settings)`** ثم استيراد الحزمة | 🟠 **بيحذف أصل** — سبب تغيّر GUID مفتاح TMP |
| `PlayModeCapture` · `CollisionTest` · `ChapterPlaytestCapture` · `ChapterPlaythrough` · `EnvironmentCapture` · `MenuFlowCapture` | بتستأنف بس لو `SessionState` مضبوط من تشغيل صريح — **مابتبدأش لوحدها** | 🟢 آمنة (بس بتدخل Play Mode لو اتنادت) |

> ⚠️ ملاحظة صدق: افترضت في R1.5 إن الـPlay Mode علّق بسبب السكربتات دي. **المراجعة بتقول غير كده** — دول ما بيبدأوش لوحدهم. تعليق R1.5 سببه البيئة (مفيش نافذة/GPU حقيقية) وده اللي هيفضل تفسيري.

---

## ٥) Play Mode — قائمة اختبارات لتنفيذها على Windows (مفيش ادعاء نجاح)

**الحالة:** ❌ **لم يُنفَّذ ولن أدّعي نجاحه.** 3 محاولات في الساندبوكس فشلت (موثّقة في R1.5).

### قائمة التنفيذ على جهازك (بالترتيب)
| # | الخطوة | النتيجة المطلوبة |
|---|---|---|
| 1 | فتح المشروع بـ **6000.0.28f1** واستنى الاستيراد يخلص | صفر أخطاء في Console |
| 2 | `TLC ▸ R1.6 ▸ كل الفحوصات + تصدير JSON` | `fails=0` |
| 3 | فتح `Assets/_Project/Scenes/Play.unity` | المشهد يفتح من غير أخطاء |
| 4 | ضغط **Play** | اللعبة تشتغل · الشخصيات تظهر (مش مكعبات) |
| 5 | راقب Console بحثًا عن `[TLC/Characters]` | **مفيش** تحذير «موديل مش موجود» |
| 6 | تأكد `CharacterLibrary.AvailableCount()` = 4 | من أي سكربت أو من الـConsole |
| 7 | شغّل Adam مشي (WASD) | الحركة تتحوّل Idle→Walk→Run بسلاسة |
| 8 | كرر للـ3 شخصيات في مشهد الحوار | كلهم بيشتغلوا بنفس الـAvatar |
| 9 | سكرين شوت للشخصيات الأربعة في Play | دليل بصري |
| 10 | سجّل `Editor.log` وقت التشغيل | مرفق مع التقرير |

**متوقع تسجّله:** `Player.log`/`Editor.log` + سكرين شوت + `R1_6_VALIDATION.json`.

---

## ٦) اللي اتعمل في الـcommit ده (المعتمد فقط)

| الملف | الحالة | ليه |
|---|---|---|
| `Assets/_Project/Editor/R16_Validation/TLC_R16_Validation.cs` | **جديد** | سكربتات الاختبار — طلبتها صريحًا |
| `.../TLC_R16_Validation.cs.meta` + `R16_Validation.meta` | **جديد** | GUIDs ثابتة للسكربت والمجلد (Unity محتاجهم) |
| `R1_6_REPORT.md` · `R1_6_GENERATED_FILES_INVENTORY.csv` | **جديد** | التقرير + قائمة الـ104 ملف |
| `tools/R16/TLC_R16_Validation.cs` | **جديد** | نسخة مصدرية في الـworkspace |

**مش معتمد (مستني قرارك):** ٨٨ ملف meta · `UniversalRenderPipelineGlobalSettings.asset` · `DefaultVolumeProfile.asset` · `packages-lock.json` · `ProjectSettings/*` الجديدة · تعديل `GraphicsSettings.asset` · حذف meta اليتيم.

---

## ٧) قرارات مطلوبة منك
1. **الـ٨٨ meta:** نعتمدهم كلهم (توصيتي: آه — بدونهم كل نسخة هتولّد GUIDs مختلفة)؟
2. **URP:** خيار **A** (مولَّدات Unity) ولا **B** (إعادة الإنشاء بـGUID الأصلي `b72d668`)؟
3. **`ProjectSettings`:** نعتمد `URPProjectSettings.asset` + `SceneTemplateSettings.json` + `packages-lock.json`؟
4. **السكربتات الـ9 auto-run:** نسيبها زي ما هي، ولا نضيف لها مفتاح تعطيل (مش تعديل سلوك) في مرحلة جاية؟
5. **meta اليتيم** `texture_manifest.json.meta` — Unity حذفه في نسخة العمل. نعتمده؟
