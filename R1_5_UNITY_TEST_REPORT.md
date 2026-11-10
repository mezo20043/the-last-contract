# R1.5 — تثبيت Unity واختبار الاستيراد الفعلي (تقرير التنفيذ)

**التاريخ:** 2026-11-09/10 · **Unity:** 6000.0.28f1 (Linux Editor) · **المشروع:** THE_LAST_CONTRACT
**ملاحظة نطاق:** Unity اشتغل على **نسخة عمل** في `/var/tmp/tlc_unity/` — **المشروع الأصلي `~/THE_LAST_CONTRACT` ما اتلمسش** (`git status` نضيف) · **R2/R3 لم تُنفَّذا** · مفيش أي تعديل على خامات أو مواد أو أنيميشن.

---

## ١) التثبيت — اللي حصل بالظبط

| الخطوة | التفصيل |
|---|---|
| التنزيل | `Unity.tar.xz` من `download.unity3d.com/…/f336aca0cab5/LinuxEditorInstaller/` — **4,302 MB** في ~12 دقيقة |
| فك الضغط | `/opt/unity/Editor` — **7.9 GB** · تم حذف الملف المضغوط بعدها |
| مكتبات ناقصة | `libgtk-3.so.0` كانت مفقودة ⇒ تثبيت GTK3/NSS/ALSA/GBM/X11/Xvfb + **Mesa (llvmpipe)** للرندر البرمجي |
| الذاكرة | الساندبوكس **1.9 GB** ⇒ أضفت **swap 5 GB** على القرص |
| الترخيص | الـentitlement القديم (Unity Personal · 2026-11-09) اشتغل: `[Licensing::Client] Successfully resolved entitlement details` ✔ |
| النتيجة | `/opt/unity/Editor/Unity --version` → **`6000.0.28f1`** · `ldd` = 0 مكتبة ناقصة |

---

## ٢) الاستيراد — نجح

```
Asset Pipeline Refresh: Total 102.164 s (InitialRefreshV2 · ForceSynchronousImport)
ImportManagerImport: 47.92 s · CompileScripts: 45.57 s
Exiting batchmode successfully now!
Library/ = 1.5 GB
```
| فحص | النتيجة |
|---|---|
| أخطاء ترجمة C# (`error CS`) | **0** |
| الـFBX الأربعة اتستوردوا؟ | ✔ **الأربعة** — والـAssetPostprocessor بتاع المشروع اشتغل: `[TLC/Import] إعداد Humanoid: <file>` لكل واحد |
| أخطاء استيراد حقيقية | **0** (الباقي ضوضاء: ALSA/Gtk/FMOD مفيش كارت صوت) |
| تحذيرات | 149 — أهمها **تحذير أصل ناقص** (انظر القسم ٧) |

---

## ٣) الـHumanoid Avatar — ✅ ٤/٤

من `R1_5_VALIDATION.json` (مولَّد من داخل المحرر):

| الشخصية | animationType | avatarSetup | avatarIsValid | avatarIsHuman | عظام مريوطة | SkeletonBones |
|---|---|---|---|---|---|---|
| Adam | Human | CreateFromThisModel | **true** | **true** | **23** | 27 |
| Laila | Human | CreateFromThisModel | **true** | **true** | **52** | 57 |
| Murad | Human | CreateFromThisModel | **true** | **true** | **22** | 27 |
| Yassin | Human | CreateFromThisModel | **true** | **true** | **52** | 57 |

**`fails: 0` · `warnings: 0`**

---

## ٤) الكليبس والـAnimator Controller — ✅

| الكليب | الطول | `isHumanMotion` |
|---|---|---|
| `walk` | **1.000 ث** | **true** |
| `run` | **0.625 ث** | **true** |

`TLC_Character.controller`:

| فحص | النتيجة |
|---|---|
| موجود | ✔ · **3 states** |
| `missingMotions` | **0** |
| الحالات | `Idle → idle` · `Walk → walk` · `Run → run` — كلها `AnimationClip` حقيقية |
| Parameters | `Speed:Float` · `IsRunning:Bool` · `Grounded:Bool` · `Talking:Bool` |

> ⚠️ **تصحيح لفحصي السابق:** كنت قدّرت إن `internalIDToNameTable` الفاضي ممكن يكسر روابط `Walk`/`Run`. **الاختبار الفعلي أثبت إن الربط سليم 0/0 مفقود.** تقديري كان خاطئ.

---

## ٥) المواد والشيدر — ✅ أحسن من المتوقع · مع تحذير واحد مؤكَّد

| الشخصية | الشيدر | BaseMap | Normal | MetallicGloss | smoothness | metallic |
|---|---|---|---|---|---|---|
| Adam | **Universal Render Pipeline/Lit** | ✔ basecolor | ✔ normal | **NULL** | 0.50 | 0.00 |
| Laila | URP/Lit | ✔ | ✔ | **NULL** | 0.50 | 0.00 |
| Murad | URP/Lit | ✔ | ✔ | **NULL** | 0.50 | 0.00 |
| Yassin | URP/Lit | ✔ | ✔ | **NULL** | 0.50 | 0.00 |

- ❌ **تصحيح تاني لفحصي:** قلت إن المواد هتظهر **ماجنتا** لأنها Standard في مشروع URP. **غلط** — Unity 6 حوّل المواد تلقائيًا لـ**URP/Lit** وربط الـBaseColor والـNormal بنجاح. الصور ظهرت على الموديلات فعليًا في الرندر.
- ✅ **لكن** تحذيري عن خرايط `metallic/roughness` **اتأكد**: متصلة بـ`MetallicGlossMap = NULL` ⇒ الخرايط شبه السوداء **مش مستخدمة** أصلًا والمادة شغالة بـ`smoothness=0.5` افتراضي. ده شغل R2 — **ما اتنفذش**.

---

## ٦) تحميل الشخصيات + دليل بصري

**تحميل وقت تشغيل بنفس كود اللعبة** (`Resources.Load<GameObject>("Characters/{key}/{modelFile}")`):

```
Adam   → loaded ✔ · animator=✔ · avatarHuman=✔ · bones=24 · mats=1
Murad  → loaded ✔ · animator=✔ · avatarHuman=✔ · bones=24 · mats=1
Laila  → loaded ✔ · animator=✔ · avatarHuman=✔ · bones=54 · mats=1
Yassin → loaded ✔ · animator=✔ · avatarHuman=✔ · bones=54 · mats=1
loaded = 4/4
```
**الدليل البصري:** `evidence_R1_5/R1_5_CHARACTERS.png` — رندر حقيقي 1600×900 من **URP** على **llvmpipe** (OpenGLCore · shaderLevel 50) للشخصيات الأربعة واقفين جنب بعض بالخامات متركّبة.
> الإضاءة في الصورة بسيطة (ضوء اتجاهي واحد، بدون Environment/Ambient) فالشخصيات طالعة غامقة — **ده مش عيب في الأصول** ولم ألمس شيئًا لتحسينه (R2 ممنوعة).

---

## ٧) Missing References والمشاكل الحقيقية في المشروع

| # | الخطورة | المشكلة | الدليل | الأثر |
|---|---|---|---|---|
| 1 | 🟠 | **`Assets/UniversalRenderPipelineGlobalSettings.asset` ناقص والـ`.meta` بتاعه موجود** (meta يتيم) | لوج Unity: *"A meta data file (.meta) exists but its asset … can't be found"* ×2 · الملف غير موجود في المشروع | Unity أعاد توليد الأصل بإعدادات **افتراضية** ⇒ أي تخصيص URP قديم اتحوّل لفقد |
| 2 | 🟡 | **٢١ أصل من غير `.meta`** (من أول ٢٠٠ ملف اتفحصوا) — معظمهم سكربتات Editor | Unity ولّد **٨٨ ملف `.meta`** جديدة في نسخة العمل | الـGUIDs الجديدة ممكن تكسر أي مرجع قديم للأصول دي |
| 3 | 🟡 | Unity ولّد أصول تلقائيًا | `DefaultVolumeProfile.asset` · `TextMesh Pro/Resources/TMP Settings.asset` · `URPProjectSettings.asset` · `packages-lock.json` · `ProjectSettings/*` | **تغييرات في المشروع** لازم تعتمدها قبل ما تدخل الـrepo |
| 4 | 🟡 | `Assets/_Project/Resources/Art/Textures/texture_manifest.json.meta` **أصل مفقود** — Unity شال الـmeta | مقارنة شجرة الأصل بالنسخة | أصل ضاع في التصفير (مش جزء من R1.5) |

---

## ٨) Play Mode — ❌ **لم ينجح** (3 محاولات موثَّقة)

| # | المحاولة | النتيجة |
|---|---|---|
| 1 | Editor بواجهة تحت Xvfb + `EditorApplication.isPlaying` | علّق لحد **900 ثانية** (timeout) — `EditorApplication.update` ما اتنفذش |
| 2 | `-batchmode -executeMethod` بدون display | فشل فورًا: `Error opening default X display` |
| 3 | `-batchmode -force-glcore` تحت Xvfb | علّق لحد **300 ثانية** (exit 124) |

⇒ **Play Mode الفعلي مش متاح في هذه البيئة** (الساندبوكس بلا GPU/نافذة حقيقية، والذاكرة 1.9 GB). اللي **اتحقق** هو تحميل وقت تشغيل عبر **نفس مسار الكود** + رندر حقيقي — وده أقرب دليل ممكن من غير تشغيل لعبة كامل. **مش هدّعي إن Play Mode اشتغل.**

---

## ٩) معايير قبول R1.5

| # | المعيار | المطلوب | النتيجة |
|---|---|---|---|
| 1 | `avatarIsValid` + `avatarIsHuman` للأربعة | true/true | ✅ **4/4** |
| 2 | عظام مريوطة | 23 / 52 / 22 / 52 | ✅ مطابق تمامًا |
| 3 | كليبس `walk` 1.0ث · `run` 0.625ث · humanMotion | ✔ | ✅ |
| 4 | `missingMotions` | 0 | ✅ 0 |
| 5 | أخطاء استيراد Console | 0 | ✅ 0 أخطاء (149 تحذير، أهمهم أصل URP ناقص) |
| 6 | تحميل ٤ شخصيات من مسار اللعبة | 4/4 | ✅ 4/4 (الرندر) — `AvailableCount()` نفسه لم يُنادَ runtime |
| 7 | المواد | — | ✅ URP/Lit (مش ماجنتا) · ⚠ metallic/roughness غير مربوطة (R2) |
| 8 | Play Mode | إن أمكن | ❌ غير متاح في البيئة (3 محاولات) |

**الحكم: R1.5 محققة في كل بند ممكن في البيئة دي، ما عدا Play Mode.**

---

## ١٠) الملفات والدليل

| الملف | المحتوى |
|---|---|
| `evidence_R1_5/R1_5_VALIDATION.json` | تقرير المحرر الكامل (Avatar/كليبس/controller/شيدر) |
| `evidence_R1_5/R1_5_CHARACTERS.png` | رندر 1600×900 للشخصيات الأربعة |
| `evidence_R1_5/R1_5_MATERIALS.txt` | الشيدر + ربط الخرائط لكل مادة |
| `evidence_R1_5/R1_5_RENDER_TEST.txt` | نتيجة التحميل (4/4) + العظام |
| `evidence_R1_5/unity_import.log.gz` | لوج الاستيراد الكامل (4512 سطر) |
| `evidence_R1_5/unity_validate.log.gz` | لوج التحقق |
| `evidence_R1_5/unity_render.log.gz` | لوج الرندر (يثبت llvmpipe/OpenGLCore) |
| `evidence_R1_5/unity_logs_extract.txt` | مقتطف مركّز |
| `tools/TLC_R1_5_*.cs` | سكربتات التحقق (Editor) — **ما اتنسختش للمشروع الأصلي** |

---

## ١١) اللي محتاج قرارك

1. **الأصول/الملفات اللي Unity ولّدها** (URP Global Settings + TMP Settings + ٨٨ `.meta` + `packages-lock.json`) — أراجعها وأعمل commit؟ **محتاجة موافقتك.**
2. **إضافة سكربتات التحقق للمشروع** (`Assets/_Project/Editor/TLC_R1_5_*.cs`) عشان تقدر تشغّلها من المنيو على جهازك؟
3. **جذر المشكلة #1**: `UniversalRenderPipelineGlobalSettings.asset` — نجيب نسخة أصلية ولا نقبل الإعدادات الافتراضية؟
4. **Play Mode على Windows** — لو محتاجه فعلًا، ده هيحتاج جهازك (الخطوات في تقرير R1.5 السابق).
