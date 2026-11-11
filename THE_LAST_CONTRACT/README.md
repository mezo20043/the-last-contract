# THE LAST CONTRACT

لعبة **Cinematic Story Adventure** ثلاثية الأبعاد للأندرويد — Unity 6 · C# · URP.

> **حالة المشروع:** المحرك والسرد والقوائم والصوت والخامات والشخصيات الواقعية
> **موجودة ومُختبَرة آلياً**. الحركة إجرائية على الهيكل العظمي (مش موكاب).
> **الحوار الصوتي البشري غير مسجّل**، و**APK لم يُبنَ**.
> اقرأ [TESTED.md](TESTED.md) قبل أي حكم على المشروع.

---

## البدء السريع

1. Unity Hub → **Add project** → اختر مجلد `THE_LAST_CONTRACT`
2. افتح بـ **Unity 6000.0.28f1** (أول استيراد: 5–15 دقيقة)
3. **`TLC → 0 · إعداد المشروع للأندرويد`** (مرة واحدة)
4. **`TLC → 5 · فحص إعدادات ومشاهد المشروع`** → لازم يطلع: `Missing Scripts: 0`
5. **`TLC → 12 · تقرير التحقق الشامل`** → `VerificationOut/editor-report.txt`
6. **`TLC → 13 · تشغيل تلقائي + لقطات + سجل`** → لقطات + Console حقيقيين
7. افتح `Assets/_Project/Scenes/Main.unity` → **Play** (قائمة) أو
   `Assets/_Project/Scenes/Play.unity` → **Play** (لعب مباشر)

### المشهدان

| المشهد | الدور |
|---|---|
| `Main.unity` | القائمة فقط — `GameBootstrap` بوضع `menuOnly` |
| `Play.unity` | اللعب الكامل — كاميرا + ضوء + شقة + آدم + تحكم موبايل |

«لعبة جديدة» في القائمة بتحمّل `Play.unity` فعلياً عبر `SceneManager.LoadScene`.
التفاصيل: **[PLAY_SCENE.md](PLAY_SCENE.md)**

> **خط عربي إجباري:** بدون Font Asset عربي، النص هيظهر مربعات.
> `TLC → 3 · توليد خط عربي` ثم عيّن الخط في `UIFactory`.
> كل التفاصيل في [ANDROID_BUILD.md](ANDROID_BUILD.md).

---

## الأرقام

| | |
|---|---|
| سكربتات C# | 48 |
| أسطر C# | 8,723 |
| مشاهد | 2 (`Main` + `Play`) |
| ملفات `.meta` | 82 (GUIDs ثابتة) |
| فصول | 3 |
| عقد السرد | 109 |
| نهايات | 12 |
| متغيّرات العلاقات | 6 |
| ملفات صوت | 25 (8.4 MB) |
| خامات PBR | 8 (16 ملف) |
| **شخصيات 3D واقعية** | **4 موديلات · 1.15 مليون رأس** |
| تكستشرات شخصيات | 31 ملف |
| خط عربي | Amiri (OFL) — تلقائي |
| فحوصات آلية | **237 — كلها ناجحة** |
| **فحص داخل محرك Unity** | **32/32 ناجح** (Unity 6000.0.28f1 — استيراد حقيقي) |
| **Play Mode حقيقي** | **8 لقطات 3D + 32 لقطة للفصل الأول + 5 قرارات** — [`VERIFICATION/IN_ENGINE_RUN.md`](VERIFICATION/IN_ENGINE_RUN.md) |
| Humanoid Avatars | **4/4 `isHuman = true`** |
| صور إثبات | `Screenshots/` (رسم خارجي + رسم بيانات — مفيش لقطة Unity) |
| تشغيل كود الحركة بدون Unity | ✅ 600 فريم · `VERIFICATION/movement-sim.txt` |
| تأطير الكاميرا | ✅ آدم كامل في الكادر طولي وعرضي · `VERIFICATION/frame-check.txt` |
| بناء APK بدون جهاز | `.github/workflows/build-android.yml` — دليل التشغيل: [GITHUB_CI.md](GITHUB_CI.md) (**جاهز، ما اتجرّبش**) |

---

## الشخصيات

| الشخصية | الموديل | رؤوس | الطول | الدور |
|---|---|---|---|---|
| **آدم منصور** | `man-base-character-rigged` | 143k | 1.80م | البطل — يتحرّك باللاعب |
| **مراد** | `elderly-man-rigged` | 427k | 1.75م | الرجل القديم — قاعد في الصالة |
| **ليلى** | `woman-rigged` | 309k | 1.70م | الصحافية — واقفة |
| **ياسين** | `man-rigged` | 268k | 1.80م | الأخ — عند الباب |

موديلات بشرية ماسحة بترخيص **CC0**، بهيكل 24 عظمة، بتُستورد تلقائياً كـ **Humanoid**
عبر `CharacterImportSettings` (مفيش خطوة يدوية).

### الحركة — `HumanoidPuppet`

ست حالات على الهيكل العظمي الحقيقي:

| الحالة | التفاصيل |
|---|---|
| **Idle** | تنفّس (صدر/كتفين) · ميل وزن · حركة راس |
| **Walk** | تأرجح فخذين وركب وأذرع متعاكس · دوران حوض · ارتداد رأسي |
| **Run** | نفس الدورة بضعف السعة وميل أمامي أكبر |
| **Sit** | فخذين أفقيين · ركبتين 90° · إنزال 45 سم لمستوى الكرسي |
| **Turn** | لف الجزء العلوي ناحية الهدف |
| **Talk** | إيماءات إيد متبدّلة + حركة راس مع الكلام |

> العظام بتتجاب من `Animator.GetBoneTransform` (الـ Avatar) — مستقل عن أسماء العظام،
> مع مطابقة أسماء كاحتياطي. **الحركة رياضية مش موكاب** — انظر [TESTED.md](TESTED.md).

## نظام اللعب

| العنصر | التنفيذ |
|---|---|
| حركة | `ThirdPersonController` + عصا افتراضية |
| كاميرا | `ThirdPersonCameraRig` — سحب بالإصبع، كاميرا ثالثة الشخص |
| تفاعل | `Interactable` + نقاط ساخنة في المشهد (صورة، هاتف، باب) |
| حوار | `DialogueUI` — كتابة تدريجية + خيارات |
| قرارات | `NarrativeRunner` — بيانات JSON، 6 متغيّرات مشروطة |
| سينمائيات | `CinematicDirector` + `DialogueCameraSystem` |
| صوت | `AudioDirector` (موسيقى متلاشية) + `FootstepSystem` + `VoiceOverSystem` |
| كاميرا سينمائية | لقطات على **عظمة الراس الحقيقية** لا ارتفاع تقديري |
| خط عربي | `ArabicFont` — Amiri ديناميكي + RTL |
| حفظ | `SaveSystem` — 4 خانات + `Progress` للتقدّم الدائم |

### التحكم

| الفعل | اللمس | لوحة المفاتيح |
|---|---|---|
| حركة | عصا يسار | WASD |
| كاميرا | سحب على الشاشة | حركة الماوس |
| تفاعل | زر التفاعل | E |
| متابعة الحوار | لمس الشاشة | مسافة |
| إيقاف | زر الإيقاف | Escape |
| جودة | — | F1 |

---

## الواجهة

`MenuSystem` + `MenuModel` — الشاشات: العنوان · القائمة · الفصول · الإعدادات ·
النهايات · الحفظ/التحميل · الإيقاف.

- منطق التنقّل (`MenuModel`) **منفصل عن الرسم** — عشان يتختبر بلا Unity.
- دعم **عربي RTL** وإنجليزي LTR بتبديل فوري.
- شاشة النهايات بتقفل النهايات اللي لسه ما وصلتهاش (12 نهاية).
- `Progress` بيحفظ الفصول المفتوحة والنهايات المكتشفة بين الجلسات.

---

## هيكل المشروع

```
Assets/_Project/
├── Scenes/Main.unity              المشهد الوحيد — GameObject واحد + GameBootstrap
├── Scripts/
│   ├── Core/          GameState · SaveSystem · Progress
│   ├── Narrative/     NarrativeTypes · NarrativeRunner
│   ├── Player/        InputReader · ThirdPersonController · ProceduralRig · Interactable
│   ├── Camera/        ThirdPersonCameraRig
│   ├── Cinematics/    CinematicDirector · DialogueCameraSystem · LipSyncTimeline
│   ├── Audio/         AudioDirector · FootstepSystem · VoiceOverSystem
│   ├── UI/            UIFactory · MenuModel · MenuSystem · DialogueUI · MobileControls
│   ├── Settings/      QualityManager · Localization · MobileOptimizations
│   ├── World/         ApartmentBuilder · AdamBuilder · LightingRig · MaterialLibrary
│   └── Bootstrap/     GameBootstrap
├── Editor/            ProjectBootstrapper · ProjectConfigurator · AndroidBuilder · …
└── Resources/
    ├── Story/         chapter01/02/03.json
    ├── Audio/         25 ملف مولّد
    └── Art/Textures/  8 خامات PBR
tools/
├── gen_audio.py       مولّد الصوت
├── gen_textures.py    مولّد الخامات
├── build_chapters.py  مولّد الفصول ٢ و٣
└── sim/               الاختبار الآلي (84 فحص)
```

---

## أوامر المحرر

| الأمر | الوظيفة |
|---|---|
| `TLC/0 · إعداد المشروع للأندرويد` | Bundle ID · minSdk 26 · IL2CPP · ARM64 · Portrait |
| `TLC/1 · إنشاء المشهد` | يعيد بناء `Main.unity` لو اتشوّه |
| `TLC/2 · فتح المشهد` | يفتح المشهد ويشغّل |
| `TLC/3 · توليد خط عربي` | خط TextMeshPro عربي |
| `TLC/5 · إعداد شخصية واقعية` | FBX → Humanoid + AnimatorController |
| `TLC/6 · بناء APK` | `Builds/Android/TheLastContract.apk` |
| `TLC/7 · فحص المشروع` | تقرير إعدادات ومشاكل |

---

## ما **لم** يُنجَز

| | السبب |
|---|---|
| تشغيل اللعبة في Unity | Unity غير متاح في بيئة التطوير |
| بناء APK | نفس السبب + لا Android SDK |
| ~~موديلات شخصيات واقعية~~ | ✅ **موجودة** |
| كليبات موكاب مسجّلة | الحركة إجرائية بدلاً منها |
| حوار صوتي بشري | لا تسجيلات |
| لقطات شاشة | لا `DISPLAY` ولا `GPU` |

**تقارير المرحلة:**
**[IMPORT_REPORT.md](IMPORT_REPORT.md)** — استيراد الموديلات والـAvatar (بيانات من فك الـFBX)
**[PLAY_SCENE.md](PLAY_SCENE.md)** — مشهد اللعب والتحكم والاتجاه
**[UNITY_ERRORS.md](UNITY_ERRORS.md)** — تحليل ساكن للمشاهد والأخطاء المتوقّعة
**[VERIFICATION.md](VERIFICATION.md)** — حالة كل بند تحقق + أدلة + خطوات خارجية

التفصيل الكامل والدليل: **[TESTED.md](TESTED.md)** ·
التراخيص: **[ASSETS_LICENSES.md](ASSETS_LICENSES.md)** ·
البناء: **[ANDROID_BUILD.md](ANDROID_BUILD.md)** ·
المحتويات: **[MANIFEST.md](MANIFEST.md)**

---

## التحكم على الموبايل

| العنصر | السلوك |
|---|---|
| عصا افتراضية | حركة نسبة لاتجاه الكاميرا |
| منطقة سحب | دوران الكاميرا (النص التاني من الشاشة) |
| زر تفاعل | بيظهر بس لما يكون فيه هدف قريب |
| زر جري | ضغط مستمر أو جري تلقائي |
| زر التالي | متابعة الحوار |

**الإعدادات** (القائمة → الإعدادات، محفوظة في `PlayerPrefs`):
حساسية الكاميرا 0.35–2.00 · حجم العصا 0.75–1.35 · جهة العصا (يسار/يمين) ·
عكس محور Y · جري تلقائي.

**الاتجاه:** اللعبة شغالة رأسي وأفقي. المرجع بيتبدّل مع الاتجاه، والكاميرا
بتوسّع الـ FOV رأسياً على الشاشة الرأسية عشان المجال الأفقي يفضل ثابت.

الربط: العصا → `PlayerInputReader` → `ThirdPersonController`.
مفيش مسار إدخال تاني.
