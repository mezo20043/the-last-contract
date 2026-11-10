# R1.6 (٢) — تنفيذ قراراتك: الـmetas · URP خيار B · مراجعة Auto-run · تحقيق المانيفست

**التاريخ:** 2026-11-10 · **Unity للتحقق:** 6000.0.28f1 (Linux · headless)
**القواعد الملتزم بها:** نسخة احتياطية قبل أي تعديل ✅ · مفيش حذف لأي أصل ✅ · مفيش لمس للقصة/المشاهد/الأنيميشن ✅ · مفيش R2 ✅ · مفيش Force Push ✅

---

## ١) الـ82 ملف Meta — **معتمدة ومنفَّذة**

من الـ88 ملف:
| البند | العدد |
|---|---|
| إجمالي الـmetas المولَّدة | 88 |
| − المؤقتة (R1.5: MatCheck · PlayModeTest · RenderTest · Validator + مشهد PLAYMODE_TEMP) | **−5** |
| − `TEXTURE_MANIFEST.json.meta` (تعارض حالة أحرف — القسم ٥) | **−1** |
| **= المعتمد والمنفَّذ** | **82** |

**+ ملف إضافي مبرَّر:** `Assets/_Project/Settings.meta` — المجلد الوحيد في المشروع اللي كان من غير meta (كان هيولّد GUID عشوائي على كل جهاز).
**+ أصل واحد مبرَّر:** `Assets/TextMesh Pro/Resources/TMP Settings.asset` (1.5KB) — الـmeta المعتمد بتاعه كان هيبقى **يتيم** من غيره.

### فحص التعارضات قبل النسخ
| فحص | النتيجة |
|---|---|
| GUID مكرر مع ملف تاني | **1** — `TEXTURE_MANIFEST.json.meta` (GUID نفسه الموجود في `texture_manifest.json.meta`) ⇒ **اتخطّى** |
| GUID موجود بالفعل لنفس الأصل | 0 |
| ملفات مؤقتة اتدخلت | **0** ✔ |

⚠️ **ملاحظة شفافية:** `.gitignore` فيه `*.fbx.meta` ⇒ **11 meta للـProps (موديلات)** اتجاهلت في أول commit واتضافوا بـ`git add -f`. الـ11 ملف `.fbx` نفسهم **مش معتمدين** (خارج طلبك) وسايبينهم على حالهم.

---

## ٢) خيار B — `UniversalRenderPipelineGlobalSettings` بالـGUID الأصلي ✅

### اللي اتعمل بالظبط
| خطوة | التفصيل |
|---|---|
| 1 | نسخة احتياطية: `BACKUPS/R1_6_20261110_175552/` فيها الـmetas الأصلية + `GraphicsSettings.asset` (مرفوعة في الـcommit) |
| 2 | كتابة `Assets/UniversalRenderPipelineGlobalSettings.asset` (14,032 بايت · المحتوى الافتراضي اللي ولّده Unity) |
| 3 | تعديل الـmeta: `8ff020c…` → **`b72d668f01260d279a991baffeb2a8be`** (الـGUID اللي `GraphicsSettings` بيطلبه) |
| 4 | كتابة `Assets/DefaultVolumeProfile.asset` (426 بايت · فاضي) — **meta الأصلي `24aa18f…` ما اتلمسش** |
| 5 | تعديل إشارة الـVolume Profile جوه الـasset لـ`24aa18f` (بدل GUID Unity العابر) |
| 6 | `ProjectSettings/GraphicsSettings.asset` — **ما اتلمسش** (اتأكد بـdiff) |

### 📌 تصريح صريح عن طبيعة المحتوى
> **المحتوى المُعاد إنشاؤه = الإعدادات الافتراضية لـ Unity 6.** ده **مش استعادة** للمحتوى الأصلي المفقود. اللي ضاع (لو كان فيه تخصيص) هو: shader stripping · RenderGraph toggle · Debug shaders · أي Look في Volume Profile. **الاستعادة الحقيقية مستحيلة** — الأصل مش موجود في المشروع ولا الأرشيف ولا Git ولا أي باك أب (تقرير R1.6 السابق · القسم ٣).

---

## ٣) `packages-lock.json` وملفات `ProjectSettings` — **مؤجَّلة (diffs جاهزة)**

| الملف | موجود في الأصل؟ | يتولّد على فتح نظيف؟ | المحتوى | التوصية |
|---|---|---|---|---|
| `Packages/packages-lock.json` (6,671 بايت) | ❌ | **✔ أيوه** | تثبيت نسخ كل الباقات (URP 17.0.3 · Cinemachine 3.1.3 · Burst 1.8.18 …) | 🟡 **أنصح باعتماده** — يضمن إن كل جهاز يبني بنفس النسخ. القرار ليك |
| `ProjectSettings/PackageManagerSettings.asset` (985 بايت) | ❌ | ❌ **لأ** (اتولّد في جلسة الواجهة بس) | `m_EnablePreReleasePackages: 0` + حالة UI للـPackage Manager | 🔴 **مش محتاج** — حالة واجهة بحتة |
| `ProjectSettings/SceneTemplateSettings.json` (3,535 بايت) | ❌ | ❌ لأ | قوالب مشاهد افتراضية (`templatePinStates: []`) | 🔴 **مش محتاج** — افتراضي |
| `ProjectSettings/URPProjectSettings.asset` (410 بايت) | ❌ | ❌ لأ | `m_LastMaterialVersion: 9` | 🔴 **مش محتاج** — رقم إصدار مواد داخلي |

📄 النسخ الكاملة في `diffs_R1_6/` للمراجعة.
**مفيش أي ملف `ProjectSettings` موجود اتعرض للتعديل غير `GraphicsSettings`** — وده كمان اتجنبناه بخيار B.

---

## ٤) سكربتات Auto-run — التقييم الدقيق + المقترح (لم يُطبَّق)

📄 **المقترح الكامل بالكود:** `R1_6_FIXES_PROPOSED.md`

| السكربت | السلوك الفعلي (من الكود) | التقييم |
|---|---|---|
| **`TMPEssentials`** | لو `TMP Settings` موجود و`Shaders/` ناقصة ⇒ **`AssetDatabase.DeleteAsset(TMP Settings)`** ثم استيراد/إنشاء | 🟠 **خطير** — بيمسح أصل تلقائيًا. والمشروع **بالظبط** في الحالة دي (`Shaders/` مش موجودة) ⇒ هيمسح عند أول فتح تفاعلي عندك |
| **`ProjectBootstrapper`** | بينشئ المشاهد لو ناقصة · `EnsureInBuildSettings()` **idempotent** (بيقارن ويخرج من غير كتابة) · `AssetDatabase.Refresh()` | 🟢 **آمن فعليًا** — بيشتغل زيادة عن اللزوم بس |
| `FirstOpenSetup` | ينشئ الـcontroller **بس لو مش موجود** | 🟢 آمن |
| 6 سكربتات Capture/Test | بتستأنف بس لو `SessionState` مضبوط من تشغيل صريح | 🟢 آمنة |

**المقترح باختصار:** `TMPEssentials` التلقائي يبقى **فحص + تحذير بس** (بدون حذف/إنشاء/استيراد)، والمسارات الهدّامة تفضل من المنيو. و`ProjectBootstrapper` يشتغل بس لو فيه مشهد ناقص. **محتاج موافقتك — مفيش سطر اتعدّل.**

---

## ٥) `texture_manifest.json.meta` — **ما اتحذفش** · التحقيق الكامل

### الحقيقة (من الأدلة)
| دليل | النتيجة |
|---|---|
| **داخل الأرشيف الأصلي `v2.0.zip`** | الملف اسمه **`texture_manifest.json`** (حروف صغيرة) **+** الـmeta بتاعه |
| **في المشروع الحالي** | الملف اسمه **`TEXTURE_MANIFEST.json`** (حروف كابيتال) والـmeta فضل **حروف صغيرة** |
| `ENVIRONMENT_PRODUCTION_REPORT.md` سطر 97-98 | **موثّق**: «الحذف الوحيد: `texture_manifest.json` المكرّر بحروف صغيرة (نسخة تانية من `TEXTURE_MANIFEST.json` كانت بتعمل تحذير تعارض حالة الأحرف)» |
| تاريخه في Git | commit واحد بس: `a031342` |
| GUID `bea0173b0e0ba163a8311e8755c55412` | **صفر مرجع** في المشروع كله |
| سلوك Unity | على Linux (حسّاس للحالة) بيمسحه ويعيد إنشاء `TEXTURE_MANIFEST.json.meta` **بنفس الـGUID** · على Windows (غير حسّاس) بيتعامل معاه كأنه صح |

### الحكم
الـmeta **مش أصل ضايع** — هو **بقايا تنظيف مقصود وموثّق** (حذف الملف المكرر بالحروف الصغيرة) لكن **الـmeta ما اتحذفش معاه**.

### التوصية (محتاجة موافقتك)
**إعادة تسميته** من `texture_manifest.json.meta` → `TEXTURE_MANIFEST.json.meta`
- ✅ **مش حذف** · الـGUID محفوظ · التحذير بيختفي · الملف بيطابق الأصل بتاعه
- 🔁 البديل: نسيبه زي ما هو — بيتصلح لوحده على Windows، ومابيضرّش

**ما عملتش أي حاجة — الملف موجود زي ما هو** (اتأكدت إنه موجود زي ما هو، وفيه نسخة في `BACKUPS/`).

---

## ٦) نتائج الاختبارات (Unity 6000.0.28f1 · نسخة نظيفة بلا Library)

| فحص | قبل R1.6(٢) | بعد | الحكم |
|---|---|---|---|
| أصول من غير `.meta` | **67** | **0** | ✅ |
| metas يتيمة | **3** | **0** | ✅ |
| تحذير «meta exists but asset can't be found» | 2 | **0** | ✅ |
| GUID الـGlobal Settings بعد فتح Unity | — | **`b72d668…`** (ثابت، ما اتغيّرش) | ✅ خيار B نجح |
| GUID الـVolume Profile | — | **`24aa18f…`** | ✅ |
| `GraphicsSettings.asset` تعرّض للتعديل؟ | — | **لأ** | ✅ |
| ملفات المشروع اتغيّرت بعد فتح Unity | 3 + حذف 1 | **0** | ✅ |
| سكربتات `[InitializeOnLoad]` | 9 | 9 (زي ما هي) | ℹ️ القسم ٤ |
| **R1.6 Validation** | — | **fails=0 · warnings=5** | ✅ |
| الشخصيات | — | 4/4 `Resources.Load` ✔ · Humanoid ✔ · 0 فقرات ناقصة | ✅ |
| Controller | — | 3 states · **0 مفقود** · 4 معاملات ✔ | ✅ |
| المواد | — | 4 × URP/Lit · MetallicGloss ✘ (شغل R2) | ℹ️ متوقع |

**الفرق الوحيد بعد فتح Unity على النسخة المتحقَّقة:** إضافة `packages-lock.json` + إعادة تسمية meta المانيفست (القسم ٥). **مفيش أي ملف اتعدّل محتواه.**

---

## ٧) المعتمد vs المؤجَّل

### ✅ معتمد ومنفَّذ ومرفوع
82 meta · `Settings.meta` · `TMP Settings.asset` · `UniversalRenderPipelineGlobalSettings.asset` (GUID `b72d668`) + meta · `DefaultVolumeProfile.asset` · نسخة احتياطية · التقرير + المقترحات.

### ⏸️ مؤجَّل بقرارك
`packages-lock.json` (توصيتي: نعتمده) · `PackageManagerSettings.asset` · `SceneTemplateSettings.json` · `URPProjectSettings.asset` (توصيتي: لأ) · تعديلات Auto-run · إعادة تسمية meta المانيفست.

### ⛔ لم يُنفَّذ بأمرك
R2 (المواد/الخرايط) · R3 · أي تعديل على القصة أو المشاهد أو الأنيميشن.

### 🖥️ Play Mode
خطة الاختبار الحالية محفوظة كما هي (R1.6 القسم ٥) للتنفيذ على Windows. **مفيش ادعاء نجاح.**

---

## ٨) قرارات مطلوبة
1. `packages-lock.json` — نعتمده؟
2. تعديل `TMPEssentials` (منع الحذف التلقائي)؟ — **التوصية: آه، بأولوية**
3. تعديل `ProjectBootstrapper`؟
4. إعادة تسمية `texture_manifest.json.meta` → `TEXTURE_MANIFEST.json.meta`؟
5. الـ11 ملف `.fbx` للـProps — نرفعهم للريبو (حالهم زي ملفات الشخصيات) ولا نسيبهُم؟
