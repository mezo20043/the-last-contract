# سجل الأصول والتراخيص — THE LAST CONTRACT

> آخر تحديث: 2026-11-10 (R1.8 — إضافة قسم Props + تصحيحات توثيقية)

## ١) أصول مولّدة داخل المشروع (الملكية الكاملة)

الأصول اللي في **هذا القسم** **مُصنّعة برمجياً من الصفر** بواسطة سكربتات موجودة في `tools/`.

> ⚠️ **تصحيح (R1.8):** كانت العبارة هنا «مفيش أي ملف محمّل من الإنترنت» — **وده مش صحيح**.
> المشروع بيحتوي على أصول طرف ثالث محمّلة من الإنترنت وموثّقة بالكامل:
> **٤ موديلات شخصيات** من Grab3D (قسم ١-ب) · **١١ موديل أثاث + ٣٣ خامة** من Poly Haven (قسم ١-هـ) ·
> **خط Amiri** من Google Fonts (قسم ١-ج). كلهم بتراخيص مفتوحة (CC0 / OFL) ومصادرهم مسجّلة تحت.
> مفيش أصل مجهول المصدر أو غير موثّق.

| النوع | المولّد | العدد | الحجم | الترخيص |
|---|---|---|---|---|
| موسيقى وأجواء ومؤثرات صوتية | `tools/gen_audio.py` | 25 ملف WAV | 8.4 MB | ملك المشروع — استخدام تجاري بلا قيود |
| خامات PBR (albedo + normal) | `tools/gen_textures.py` | 8 خامات · 16 ملف | 1.7 MB | ملك المشروع — استخدام تجاري بلا قيود |
| ملفات القصة | `Assets/_Project/Resources/Story/*.json` | 3 فصول | 46 KB | ملك المشروع |
| سكربتات اللعبة | `Assets/_Project/Scripts/` | 35 ملف C# | — | ملك المشروع |

**كيف تتأكد بنفسك؟** شغّل:
```bash
python3 tools/gen_audio.py      # يعيد توليد كل الصوت
python3 tools/gen_textures.py   # يعيد توليد كل الخامات
```
المخرجات متطابقة (البذور ثابتة) — يعني الأصول قابلة لإعادة الإنتاج، مش ملفات سحرية.

### تفاصيل تقنية

**الصوت** — 24 كيلوهرتز، أحادي، 16-بت. توليف جمعي وتضميني (additive/FM)،
مغلفات ADSR، صدى التفاف FFT، وتلاشي الحلقات عشان تتكرر بلا قطع.

| الملف | المدة | الاستخدام |
|---|---|---|
| `Music/title_theme` | 31.6s | شاشة العنوان والفصل الأول |
| `Music/act_tension` | 27.6s | مشاهد التوتر |
| `Music/act_resolve` | 31.6s | مشاهد الهدوء |
| `Music/chapter_sting` | 5.5s | انتقال الفصول |
| `Ambience/apartment_hum_loop` | 21.6s | الشقة |
| `Ambience/city_night_loop` | 25.6s | المدينة ليلاً |
| `Ambience/rain_loop` | 25.6s | المطر |
| `SFX/footstep_concrete_01..04` | 0.1s ×4 | خطوات على أسمنت |
| `SFX/footstep_wood_01..04` | 0.1s ×4 | خطوات على خشب |
| `SFX/door_open` · `door_close` | 1.4s · 0.7s | أبواب |
| `SFX/phone_vibrate` · `paper_rustle` · `heartbeat` | — | تفاعلات |
| `SFX/ui_click` · `ui_back` · `ui_hover` | 0.1s | واجهة |
| `SFX/whoosh_transition` · `ending_sting` | — | سينمائيات |
| `SFX` (إجمالي) | 25 ملف | — |

**التحقق:** فحص آلي على كل ملف — **صفر ملفات صامتة**، كلها بمحتوى صوتي حقيقي (RMS > 0.05).

**الخامات** — 512×512، ضجيج طيفي دوري (قابل للتكرار بلا خطوط)،
مع خريطة Normal مشتقة من خريطة الارتفاع بحساب التفاف دوري.

| الخامة | الاستخدام |
|---|---|
| `wood_floor` · `wood_dark` | أرضيات |
| `plaster_wall` | حوائط |
| `tile_ceramic` | حمام/مطبخ |
| `fabric_dark` | كنب/أقمشة |
| `metal_brushed` | معدن |
| `concrete` | خارجي |
| `city_facade` | واجهات مبانٍ ليلية |


---

## ١-ب) الشخصيات ثلاثية الأبعاد (CC0 — طرف ثالث)

أربعة موديلات بشرية **ماسحة عالية التفاصيل** من [Grab3D](https://grab3d.com).

| الشخصية | الملف | رؤوس | الطول | الترخيص |
|---|---|---|---|---|
| آدم منصور | `man-base-character-rigged.fbx` | 143,446 | 1.80 م | CC0 1.0 |
| مراد | `elderly-man-rigged.fbx` | 427,179 | 1.75 م | CC0 1.0 |
| ليلى | `woman-rigged.fbx` | 309,383 | 1.70 م | CC0 1.0 |
| ياسين | `man-rigged.fbx` | 268,089 | 1.80 م | CC0 1.0 |

- **الهيكل:** 24 عظمة بشرية · يأتي مع كليبي `walk` و `run`.
- **الترخيص:** CC0 1.0 Universal (Public Domain) — استخدام تجاري بلا إسناد.
- **نسخة الرخصة** محفوظة مع كل موديل في `LICENSE.txt` جوّه مجلده.

> ### ⚠️ إفصاح إلزامي
> الناشر الأصلي بيصرّح إن الموديلات **مُنتَجة بمساعدة الذكاء الاصطناعي**
> (Meshy image-to-3D ثم تنظيف وتكستشر في Blender). لو هتنشر على منصة
> بتطلب الإفصاح عن المحتوى المولّد بالـ AI (زي Steam)، لازم تعلن ده.
> النص الأصلي موجود في `Assets/_Project/Resources/Characters/*/LICENSE.txt`.

**التكستشرات:** basecolor + normal + metallic + roughness، مُعاد تحجيمها لـ 1024
لتوفير حجم البناء على الموبايل.

---

## ١-ج) الخط العربي (OFL)

| الملف | المصدر | الترخيص |
|---|---|---|
| `Amiri-Regular.ttf` | [Google Fonts — Amiri](https://github.com/google/fonts/tree/main/ofl/amiri) | SIL Open Font License 1.1 |
| `Amiri-Bold.ttf` | نفس المصدر | SIL OFL 1.1 |

- **الاستخدام التجاري:** مسموح. **التعديل والتضمين:** مسموح.
- **المطلوب:** عدم بيع الخط لوحده.
- نسخة الرخصة في `Assets/_Project/Resources/Fonts/OFL.txt`.

> **الخط بيتبني تلقائياً:** `ArabicFont` بيولّد `TMP_FontAsset` ديناميكي وقت التشغيل.
> مفيش خطوة يدوية في المحرر، والنص العربي بيتشكّل صح مع RTL.

---

## ١-هـ) موديلات الأثاث (Props) — CC0 · طرف ثالث

**١١ موديل أثاث + ٣٣ خامة** من [Poly Haven](https://polyhaven.com) — كلها **CC0 1.0 Universal**.

| # | المعرّف | حجم FBX | خامات | المصدر |
|---|---|---:|---|---|
| 1 | `WoodenChair_01` | 726.7 KB | diff + nor + rough | [polyhaven.com/a/WoodenChair_01](https://polyhaven.com/a/WoodenChair_01) |
| 2 | `bar_chair_round_01` | 649.7 KB | diff + nor + rough | [polyhaven.com/a/bar_chair_round_01](https://polyhaven.com/a/bar_chair_round_01) |
| 3 | `GothicCabinet_01` | 586.5 KB | diff + nor + rough | [polyhaven.com/a/GothicCabinet_01](https://polyhaven.com/a/GothicCabinet_01) |
| 4 | `CoffeeTable_01` | 332.5 KB | diff + nor + rough | [polyhaven.com/a/CoffeeTable_01](https://polyhaven.com/a/CoffeeTable_01) |
| 5 | `ArmChair_01` | 245.7 KB | diff + nor + rough | [polyhaven.com/a/ArmChair_01](https://polyhaven.com/a/ArmChair_01) |
| 6 | `GreenChair_01` | 221.1 KB | diff + nor + rough | [polyhaven.com/a/GreenChair_01](https://polyhaven.com/a/GreenChair_01) |
| 7 | `Sofa_01` | 186.8 KB | diff + nor + rough | [polyhaven.com/a/Sofa_01](https://polyhaven.com/a/Sofa_01) |
| 8 | `SchoolDesk_01` | 132.7 KB | diff + nor + rough | [polyhaven.com/a/SchoolDesk_01](https://polyhaven.com/a/SchoolDesk_01) |
| 9 | `WoodenTable_03` | 99.2 KB | diff + nor + rough | [polyhaven.com/a/WoodenTable_03](https://polyhaven.com/a/WoodenTable_03) |
| 10 | `WoodenTable_01` | 52.1 KB | diff + nor + rough | [polyhaven.com/a/WoodenTable_01](https://polyhaven.com/a/WoodenTable_01) |
| 11 | `Shelf_01` | 26.0 KB | diff + nor + rough | [polyhaven.com/a/Shelf_01](https://polyhaven.com/a/Shelf_01) |

- **الموقع:** `Assets/_Project/Resources/Props/<id>/`
- **الإجمالي:** ٣.١٨ MB موديلات + ٦.١٥ MB خامات (٣٣ صورة 1024×1024)
- **الترخيص:** CC0 1.0 Universal (مِلك عام) — **استخدام تجاري بلا إسناد**.
- **سجل الأصول التفصيلي:** `Assets/_Project/Resources/Props/ASSET_MANIFEST.json` (١٢ مدخل — يشمل `desk_lamp_arm_01` اللي ملفه مش موجود · انظر قسم ٣).
- **الاستخدام:** `PropLibrary` ← `EnvironmentLibrary` ← `GameBootstrap` (بناء الغرف وقت التشغيل).
- **أصل مسترجع (R1.8.1):** `desk_lamp_arm_01` — كان مطلوب في الكود (٣ نداءات + `PropLibrary.All` + المانيفست) وملفه مفقود.
  اتنزّل من Poly Haven (`https://polyhaven.com/a/desk_lamp_arm_01` · **CC0**) بنفس الاسم والمسار ونفس أسماء الخامات،
  والموديل اتلفّ حول محوره في Blender (+90° X) ليطابق قاعدة الأصول التانية (Z-up) — **بدون أي تعديل في الكود**.
  | البند | القيمة |
  |---|---|
  | FBX | 3.1 MB · 25 SkinnedMeshRenderer + 4 MeshFilter · 2,349 رأس · 3,344 مثلث |
  | خامات | `_diff` + `_nor` + `_rough` (1024×1024) |
  | المصدر | Poly Haven — Yann Kervran (Rigging) · Kuutti Siitonen (Modeling & texturing) |
- **ملاحظة تقنية:** خامات `_rough` (١١ ملف · ١.٨٦ MB) **مش مستخدمة في الكود** — `PropLibrary` بيبني المادة من `diff` + `nor` بس. الملفات محفوظة كما هي، واتعمل لها استبعاد من بناء أندرويد فقط (R1.8).

---

## ١-د) الحوار الصوتي — **غير موجود**

| البند | الحالة |
|---|---|
| تسجيلات صوتية بشرية | ❌ **غير موجودة** |
| عدد الأسطر المطلوبة | **80 سطراً** |
| سكربت التسجيل | ✅ `VO_SCRIPT.md` — كل سطر بمعرّفه ومتحدثه |

النظام جاهز: أي ملف يُوضع في `Resources/Audio/Voice/{nodeId}.wav` بيتشغّل تلقائياً
مع السطر. مفيش ملفات صوت بشرية مزيفة في المشروع، ومفيش أدّعاء بوجودها.

---

## ٢) حزم الطرف الثالث (من سجل Unity الرسمي)

دي **مش أصول فنية**، دي مكتبات برمجية بتتثبت تلقائياً عبر `Packages/manifest.json`:

| الحزمة | الإصدار | الترخيص | الاستخدام |
|---|---|---|---|
| Universal Render Pipeline | 17.0.3 | Unity Companion License | خط الرسم والإضاءة |
| Cinemachine | 3.1.3 | Unity Companion License | كاميرات سينمائية |
| TextMeshPro | (مدمجة في ugui 2.0.0) | Unity Companion License | واجهة ونصوص |
| Unity UI (ugui) | 2.0.0 | Unity Companion License | واجهة المستخدم |

> **تصحيح (R1.8):** الجدول ده كان بيذكر `Input System 1.11.2` و `Timeline 1.7.x`
> — **الحزمتين مش موجودتين في `Packages/manifest.json`** أصلاً. المصدر الوحيد للحقيقة
> هو `Packages/manifest.json` (١٣ مدخل) + `Packages/packages-lock.json` (٣٠ مدخل مع التبعيات).
> المشروع بيستخدم تحكم اللمس عبر `Input` القديم/UGUI، مفيش Input System Package.

كلها مجانية للاستخدام التجاري ضمن شروط Unity.

---

## ٣) أصول **غير موجودة** — ولن أدّعي غير ذلك

| الأصل | الحالة |
|---|---|
| ~~موديلات شخصيات 3D واقعية (FBX)~~ | ✅ **موجودة الآن** — 4 موديلات CC0 |
| تكستشرات الشخصيات | ✅ موجودة (31 ملف) |
| كليبات أنيميشن مسجّلة (موكاب) | ❌ غير موجودة — الحركة إجرائية بالعظام |
| تسجيلات صوتية للحوار (Voice Over) | ❌ غير موجودة — سكربت جاهز في `VO_SCRIPT.md` |
| Boom/موسيقى بشرية | ❌ غير موجودة |
| Prefabs للمشاهد | ❌ غير موجودة |

**الحالة الحقيقية:** الشخصيات **موديلات بشرية ماسحة حقيقية**، بتُحمَّل من
`Resources/Characters/` وبتتحرّك عن طريق الهيكل العظمي (`HumanoidPuppet`).
لو الموديل مش موجود بترجع لبديل بدائي تلقائياً — لكن البديل مش هو الافتراضي.

> ⚠️ **الفرق المهم:** الميش والجلد والتكستشر واقعي. الحركة **إجرائية**
> (رياضيات على العظام) مش كليبات موكاب مسجّلة.

**عند توفير موديلات مرخصة:** `TLC → 5 · إعداد شخصية واقعية` بيحوّل FBX إلى
Humanoid ويبني الـ AnimatorController تلقائياً — بدون كود إضافي.

---

## ٤) حقوق القصة

كل النصوص والشخصيات والأحداث في `Resources/Story/` **أصلية بالكامل**، مكتوبة
لهذا المشروع. مفيش اقتباس من أي عمل موجود. الإلهام في *أسلوب* السرد المتفرّع
فقط، وليس في المحتوى.
