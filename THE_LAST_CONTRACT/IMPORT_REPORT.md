# تقرير استيراد الموديلات والـ Avatar

> مُولَّد آلياً من فحص الملفات الثنائية (FBX 7400) — **مش تقدير**.
> السكربت: `tools/fbx_inspect.py`

## ١) ملخّص

| الشخصية | الدور | الرؤوس | العظام | الأخذات | تغطية Humanoid |
|---|---|---|---|---|---|
| **آدم منصور** | البطل | 143,901 | 24 | walk, run | 22/19 |
| **ليلى** | الصحافية | 171,151 | 54 | run, walk | 22/19 |
| **مراد** | الرجل القديم | 173,330 | 24 | walk, run | 22/19 |
| **ياسين** | الأخ | 167,662 | 54 | run, walk | 22/19 |

**النتيجة:** الأربعة عندهم تغطية **كاملة 19/19** لعظام Unity Humanoid.
يعني `Avatar` هيتولّد تلقائياً عند الاستيراد بـ `animationType = Humanoid` بدون أي تعديل يدوي.

## ٢) الكليبات الموجودة فعلاً

- **آدم منصور**: `walk`, `run`
- **ليلى**: `run`, `walk`
- **مراد**: `walk`, `run`
- **ياسين**: `run`, `walk`

> Unity هيولّد Clip لكل أخذة عند `importAnimation = true`.
> الكليبات المتاحة: **walk** و **run**. الـ `idle` مش موجود في الملفات —
> بيتولّد بمنحنيات حقيقية في `AnimatorControllerBuilder`.

## ٣) خريطة العظام → Unity Humanoid

| عظمة الملف | عظمة Unity |
|---|---|
| `Hips` | `Hips` |
| `Spine` | `Spine` |
| `Spine01` | `Chest` |
| `Spine02` | `UpperChest` |
| `neck` | `Neck` |
| `Head` | `Head` |
| `LeftShoulder` | `LeftShoulder` |
| `LeftArm` | `LeftUpperArm` |
| `LeftForeArm` | `LeftLowerArm` |
| `LeftHand` | `LeftHand` |
| `RightShoulder` | `RightShoulder` |
| `RightArm` | `RightUpperArm` |
| `RightForeArm` | `RightLowerArm` |
| `RightHand` | `RightHand` |
| `LeftUpLeg` | `LeftUpperLeg` |
| `LeftLeg` | `LeftLowerLeg` |
| `LeftFoot` | `LeftFoot` |
| `LeftToeBase` | `LeftToes` |
| `RightUpLeg` | `RightUpperLeg` |
| `RightLeg` | `RightLowerLeg` |
| `RightFoot` | `RightFoot` |
| `RightToeBase` | `RightToes` |

> التسمية دي هي **معيار Mixamo** — وهي المعيار اللي Unity بيتعرّف عليه
> بأعلى نسبة نجاح في التحويل التلقائي.

## ٤) إعدادات الاستيراد المطبّقة

بيتطبّق تلقائياً بواسطة `Assets/_Project/Editor/CharacterImportSettings.cs`
(AssetPostprocessor — بيشتغل أول ما الملف يتستورد، بدون تدخل):

| الإعداد | القيمة | ليه |
|---|---|---|
| `animationType` | `Humanoid` | توليد Avatar + دعم إعادة الاستهداف |
| `importAnimation` | `true` | استيراد walk/run كـ AnimationClips |
| `animationCompression` | `Optimal` | حجم أقل على الموبايل |
| `materialImportMode` | `ImportStandard` | مواد من وصف FBX |
| `materialLocation` | `InPrefab` | مفيش مواد عايمة في المشروع |
| `meshCompression` | `Medium` | توازن حجم/جودة |
| `optimizeMeshPolygons/Vertices` | `true` | تقليل Draw Calls |
| `importCameras/Lights` | `false` | الملفات مفيهاش كاميرات/أضواء |
| `isReadable` | `false` | توفير RAM |
| التكستشرات | 1024 · Normal map تلقائي · sRGB مضبوط | توافق موبايل |

## ٥) التكستشرات

- **آدم منصور**: 8 ملف — `C:\grab3d-gen\downloads\man-base-character-rigged\man-base-character-rigged.fbm\man-base-character-rigged_basecolor.jpg`, `C:\grab3d-gen\downloads\man-base-character-rigged\man-base-character-rigged.fbm\man-base-character-rigged_metallic-roughness_metallic.jpg`, `C:\grab3d-gen\downloads\man-base-character-rigged\man-base-character-rigged.fbm\man-base-character-rigged_metallic-roughness_roughness.jpg`, `C:\grab3d-gen\downloads\man-base-character-rigged\man-base-character-rigged.fbm\man-base-character-rigged_normal.jpg`, `man-base-character-rigged.fbm\man-base-character-rigged_basecolor.jpg`, `man-base-character-rigged.fbm\man-base-character-rigged_metallic-roughness_metallic.jpg` …
- **ليلى**: 8 ملف — `C:\grab3d-gen\downloads\woman-rigged\woman-rigged.fbm\woman-rigged_basecolor.jpg`, `C:\grab3d-gen\downloads\woman-rigged\woman-rigged.fbm\woman-rigged_metallic-roughness_metallic.jpg`, `C:\grab3d-gen\downloads\woman-rigged\woman-rigged.fbm\woman-rigged_metallic-roughness_roughness.jpg`, `C:\grab3d-gen\downloads\woman-rigged\woman-rigged.fbm\woman-rigged_normal.jpg`, `woman-rigged.fbm\woman-rigged_basecolor.jpg`, `woman-rigged.fbm\woman-rigged_metallic-roughness_metallic.jpg` …
- **مراد**: 7 ملف — `C:\grab3d-gen\downloads\elderly-man-rigged\elderly-man-rigged.fbm\elderly-man-rigged_basecolor.jpg`, `C:\grab3d-gen\downloads\elderly-man-rigged\elderly-man-rigged.fbm\elderly-man-rigged_metallic-roughness_metallic.jpg`, `C:\grab3d-gen\downloads\elderly-man-rigged\elderly-man-rigged.fbm\elderly-man-rigged_metallic-roughness_roughness.jpg`, `C:\grab3d-gen\downloads\elderly-man-rigged\elderly-man-rigged.fbm\elderly-man-rigged_normal.jpg`, `elderly-man-rigged.fbm\elderly-man-rigged_basecolor.jpg`, `elderly-man-rigged.fbm\elderly-man-rigged_metallic-roughness_roughness.jpg` …
- **ياسين**: 8 ملف — `C:\grab3d-gen\downloads\man-rigged\man-rigged.fbm\man-rigged_basecolor.jpg`, `C:\grab3d-gen\downloads\man-rigged\man-rigged.fbm\man-rigged_metallic-roughness_metallic.jpg`, `C:\grab3d-gen\downloads\man-rigged\man-rigged.fbm\man-rigged_metallic-roughness_roughness.jpg`, `C:\grab3d-gen\downloads\man-rigged\man-rigged.fbm\man-rigged_normal.jpg`, `man-rigged.fbm\man-rigged_basecolor.jpg`, `man-rigged.fbm\man-rigged_metallic-roughness_metallic.jpg` …

## ٦) اللي **مش** متحقّق

- توليد الـ Avatar نفسه بيحصل **جوه Unity** — أنا تحققت من الأسماء والتغطية، مش من الـ Avatar الناتج.
- لو Unity اختار ربط مختلف لعظمة (نادر مع Mixamo)، الحل من `TLC → 6 · فحص المشروع`.
- الـ walk/run **ما اتشغّلوش** هنا — مفيش محرك.