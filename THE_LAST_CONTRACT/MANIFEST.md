# بيان المحتويات — THE LAST CONTRACT v2.0

**402 ملف · 107.81 MB** (داخل الـ ZIP النهائي)

## الهيكل

| المجلد | ملفات | حجم |
|---|---|---|
| `Assets/_Project/Resources/Characters/murad` | 5 | 20371 KB |
| `Assets/_Project/Resources/Characters/laila` | 5 | 19292 KB |
| `Assets/_Project/Resources/Characters/yassin` | 5 | 19039 KB |
| `Assets/_Project/Resources/Characters/adam` | 5 | 16646 KB |
| `Screenshots/chapter01` | 28 | 15449 KB |
| `Screenshots/unity` | 9 | 5469 KB |
| `Assets/_Project/Resources/Audio/Music` | 8 | 4509 KB |
| `Assets/_Project/Resources/Audio/Ambience` | 6 | 3407 KB |
| `Assets/_Project/Resources/Art/Textures/Interior` | 24 | 1255 KB |
| `Assets/_Project/Resources/Fonts` | 6 | 830 KB |
| `Assets/_Project/Resources/Characters/murad/elderly-man-rigged.fbm` | 8 | 630 KB |
| `Assets/_Project/Resources/Audio/SFX` | 36 | 546 KB |
| `Assets/_Project/Resources/Characters/yassin/man-rigged.fbm` | 8 | 462 KB |
| `Assets/_Project/Resources/Art/Textures/Exterior` | 8 | 461 KB |
| `Screenshots` | 4 | 454 KB |
| `Assets/_Project/Resources/Characters/laila/woman-rigged.fbm` | 8 | 368 KB |
| `Assets/_Project/Resources/Characters/adam/man-base-character-rigged.fbm` | 8 | 257 KB |
| `tools` | 14 | 118 KB |
| `Assets/_Project/Editor` | 22 | 108 KB |
| `(الجذر)` | 12 | 101 KB |
| `Assets/_Project/Scripts/World` | 18 | 78 KB |
| `tools/sim` | 3 | 73 KB |
| `Assets/_Project/Scripts/UI` | 14 | 63 KB |
| `VERIFICATION` | 10 | 61 KB |
| `ProjectSettings` | 21 | 52 KB |
| `Assets/_Project/Scripts/Bootstrap` | 6 | 51 KB |
| `Assets/_Project/Resources/Story` | 6 | 45 KB |
| `Assets/_Project/Scripts/Settings` | 11 | 29 KB |
| `Assets/_Project/Scripts/Cinematics` | 7 | 25 KB |
| `Assets/_Project/Scripts/Audio` | 6 | 18 KB |
| `Assets/_Project/Scripts/Player` | 8 | 18 KB |
| `Assets/_Project/Scenes` | 4 | 17 KB |
| `Assets/_Project/Scripts/Narrative` | 4 | 15 KB |
| `Assets/_Project/Resources/Animators` | 4 | 14 KB |
| `Assets/_Project/Scripts/Core` | 6 | 13 KB |
| `Assets/_Project/Scripts/Camera` | 2 | 10 KB |
| `Assets/_Project/Resources/Audio` | 5 | 7 KB |
| `Assets/_Project/Settings` | 4 | 6 KB |
| `.github/workflows` | 1 | 3 KB |
| `Assets/_Project/Resources/Art/Textures` | 4 | 3 KB |
| `Assets/_Project/Scripts` | 10 | 1 KB |
| `Assets/_Project/Resources` | 6 | 1 KB |
| `Assets/_Project` | 4 | 0 KB |
| `Assets/_Project/Resources/Characters` | 4 | 0 KB |
| `Assets` | 3 | 0 KB |
| `Packages` | 1 | 0 KB |
| `Assets/_Project/Resources/Art` | 1 | 0 KB |




## الأصول الموجودة

| النوع | العدد | الحجم | الحالة |
|---|---|---|---|
| **شخصيات 3D واقعية (FBX)** | **4** | **77 MB** | ✅ 1.15M رأس · مرجّعة · CC0 |
| تكستشرات شخصيات | 31 | 3.3 MB | ✅ basecolor/normal/metallic/roughness |
| خط عربي Amiri | 2 + OFL | 826 KB | ✅ TMP ديناميكي + RTL |
| صوت مولّد (WAV) | 25 | 8.4 MB | ✅ صفر ملفات صامتة |
| خامات PBR | 16 | 1.7 MB | ✅ مطبّقة على البيئة |
| سكربتات C# | 48 | — | ✅ 8,723 سطر · 0 أخطاء |
| مشاهد Unity | 2 | 18 KB | ✅ `Main.unity` (قائمة) + `Play.unity` (لعب) |
| ملفات `.meta` | 82 | — | ✅ GUIDs ثابتة — مفيش Missing Script |
| ملفات القصة | 3 | 46 KB | ✅ 109 عقدة · 12 نهاية |
| سكربت تسجيل صوتي | 1 | — | ✅ 80 سطر · 4 أدوار |
| اختبار آلي | 237 فحص | — | ✅ كلها ناجحة |

## الأصول غير الموجودة

| الأصل | الأثر |
|---|---|
| كليبات موكاب مسجّلة | الحركة إجرائية على العظام (Idle/Walk/Run/Sit/Turn/Talk) |
| تسجيلات حوار بشري | 80 سطر محتاجة ممثلين — `VO_SCRIPT.md` جاهز |
| APK | **لم يُبنَ** — لا Unity ولا Android SDK |
| Prefabs | كل شيء يُبنى بالكود — والمشهد بيحمل الكائنات والوصلات |
| تشغيل فعلي | **لم يحدث** — المشهدين اتولّدوا واتفحصوا ساكن |

## الشخصيات

| الشخصية | الموديل | رؤوس | الطول |
|---|---|---|---|
| آدم منصور | man-base-character-rigged | 143k | 1.80م |
| مراد | elderly-man-rigged | 427k | 1.75م |
| ليلى | woman-rigged | 309k | 1.70م |
| ياسين | man-rigged | 268k | 1.80م |

## الفصول — الربط

`chapterResources = { chapter01, chapter02, chapter03 }` · CH1_* → ch2_from_ch1_* · CH2_* → ch3_from_ch2_* · CH3_* = نهاية اللعبة