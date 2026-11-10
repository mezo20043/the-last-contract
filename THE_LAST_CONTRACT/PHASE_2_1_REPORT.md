# PHASE 2.1 — إظهار المنيو الرئيسية وتدفّق اللعب

**التاريخ:** 2026-11-09 · **Unity:** 6000.0.28f1 · **الرندر:** llvmpipe (سوفتوير، بلا GPU) · **Xvfb 1280×720**
**نقطة الاسترجاع:** `d00a8bf` (قبل أي تعديل) · `5b3d1be` (بعد تقرير المرحلة ١)

---

## ١) الخلاصة في سطرين

المنيو الرئيسية **بقت ظاهرة فعلاً** في Play Mode بأزرار عربية، ودوسة «لعبة جديدة» **بتقفل المنيو وتحمّل مشهد `Play` وتبدأ الفصل الأول** مع حوار عربي ظاهر على الشاشة.
**اللي لسه ناقص:** تخطيط النصوص مش مثالي (العنوان كبير بيتداخل مع الأزرار، ومحاذاة عمودية للنصوص جوه الأزرار مش مضبوطة). التفاصيل في §٥.

---

## ٢) معايير القبول

| المعيار | النتيجة | الدليل |
|---|---|---|
| المنيو ظاهرة في Game View | **✅ ناجح** | `Screenshots/menu/01_main_menu.png` |
| الأزرار بتستجيب لتفاعل حقيقي | **✅ ناجح** | `menu-flow-log.txt` — دوسة عبر `EventSystem` |
| «لعبة جديدة» تبدأ الفصل الأول | **✅ ناجح** | المشهد بقى `Play` والسرد `IsWaitingForInput=True` |
| حوار عربي ظاهر | **✅ ناجح** | `03_chapter1_dialogue.png` |
| الأدلة محفوظة ومراجعة | **✅ ناجح** | ٣ لقطات + سجل نصي |

**الحكم: المرحلة ٢.١ ناجحة مع ملاحظات تخطيطية (P1) مفتوحة.**

---

## ٣) الحلقة الكاملة المسجّلة (من `menu-flow-log.txt`)

```
MenuCanvas: mode=ScreenSpaceOverlay · cam=NULL …        ← قبل الإصلاح
حقول MenuSystem: _buttonArea=Buttons · _title=Title · _subtitle=Subtitle · _hint=Hint
── شجرة MenuCanvas ──
  • MenuSafeArea → Backdrop · GoldRule · Title · Subtitle · Buttons(Entry0..Entry4) · Hint
  زر: Entry0 · active=True · interactable=True      ← لعبة جديدة
  زر: Entry1 · active=True · interactable=True      ← متابعة (فيه حفظ فعلاً)
  زر: Entry2 · active=True · interactable=False     ← الفصول (مقفول: فصل واحد مفتوح)
  MenuSystem.IsOpen = True
  📷 01_main_menu.png · كانفسات مربوطة=1 · تباين الشاشة=0.081
  إحداثيات الدوسة: (345.50, 153.68) · كاميرا الواجهة: Menu Camera
  ✔ اتبعت pointerClick عبر EventSystem
  دوسة زر «لعبة جديدة»: ✅ الحدث اتبعت
  المنيو قفلت: ✅
  📷 02_after_new_game.png · كانفسات مربوطة=3 · تباين الشاشة=0.495
  اسم المشهد الحالي: Play
  الفصل الأول بدأ (السرد مستني إدخال): ✅
  📷 03_chapter1_dialogue.png · تباين الشاشة=0.975
  واجهة الحوار ظاهرة: ✅
  نصوص ظاهرة: «التالي» · «تفاعل» · «٢:١٧ صباحاً. شقة في مدينة نصر، الدور التاسع…» · «جري»
```

---

## ٤) الأعطال اللي اتكشفت واتصلحت في المرحلة دي

| # | العطل | الجذر الحقيقي | الإصلاح | قبل → بعد |
|---|---|---|---|---|
| ١ | **المنيو كانت بتتبني بزراير صفر** | `TMP Settings` مش موجود ⇒ `TMP_FontAsset.CreateFontAsset` بيرمي `NullReferenceException` جوه `Build()` فيقف عند العنوان | استخراج **TMP Essential Resources** من الحزمة مباشرة (37 ملف: Shaders + LiberationSans + Settings) + `TMPEssentials` بيستوردها تلقائيًا | 0 زر → **5 أزرار** |
| ٢ | **مفيش أي نص ظاهر** | نفس السبب + مفيش Font Asset ⇒ TMP بيرمي تحذير *"There is no Font Asset assigned to Title"* | `FontAssetBuilder` بيبني `Amiri SDF` (2048² dynamic) في المحرر، و`ArabicFont` بيحمّله من Resources | نص غير مرئي → **نص عربي مرسوم** |
| ٣ | **الواجهة مش داخلة في اللقطة** | كانفس Overlay + `Camera.Render()` ⇒ الـUI مش في الـRenderTexture | `UIFactory.BindAllCanvasesToCamera()` + نداء أوتوماتيكي بعد إطار من البناء | تباين 0.000 → **0.081 / 0.975** |
| ٤ | **نصوص رأسية أو مقصوصة** | `MenuSystem.Place()` كان بيدي `sizeDelta` **سالب** للعنوان/الفرعي/التلميح ⇒ صندوق نص بعرض سالب | مقاسات موجبة + تمريرة سلامة `FixBrokenTextLayouts()` بتصلّح أي مقاس صفر/سالب وقت التشغيل | **11 عنصر اتصلّح** |
| ٥ | مربّع بدل السهم `▾` | الجليف مش موجود في خط Amiri | استبدال بـ`»` | اختفى الـtofu |
| ٦ | الدوسة الصناعية مش بتنفّذ | إحداثيات بمستوى Overlay بينما الكانفس Screen Space Camera | `WorldToScreenPoint(canvas.worldCamera, …)` + رايكاست صريح | الدوسة بتوصل (مع `onClick.Invoke()` كخطة بديلة عند اللزوم) |

---

## ٥) مشاكل مفتوحة (بصراحة)

| # | المشكلة | الشدة | السبب | الاتجاه المقترح |
|---|---|---|---|---|
| ١ | **العنوان بيتداخل مع الأزرار** — «العقد الأخير» بحجم 108 بيقع فوق `Entry1/Entry2` | 🟠 P1 | التخطيط يدوي بإحداثيات مطلقة بدون `VerticalLayoutGroup` | استخدام `VerticalLayoutGroup` + `ContentSizeFitter` بدل `Place()` اليدوي |
| ٢ | **محاذاة نص الأزرار لأعلى** — الكلمة بتقع على الحد العلوي للزر | 🟠 P1 | خط Amiri له خط أساس (baseline) عالي؛ TMP بيفترض مقاييس LiberationSans | `text.margin`/`alignment` مضبوطة أو توليد Font Asset مع `Ascender/Descender` معدّلين |
| ٣ | **الفراغات والوصل العربي مش مثالي** (بعض الحروف متباعدة) | 🟡 P2 | وضع `Dynamic` في أطلس + عدم تفعيل `Kerning`/`Ligatures` | تفعيل `TMP_Settings.kerning` + فحص `isRightToLeftText` لكل نص |
| ٤ | **لوحة الحوار كبيرة/مزحلقة** (بوكس رمادي على اليسار) | 🟠 P1 | `DialogueUI` بيبني بمقاسات ثابتة على مرجع 1080×1920 | تمريرة تمركز على 16:9 + Safe Area |
| ٥ | زر «متابعة» ظاهر **مفعّل** لأن فيه حفظ قديم من جولة سابقة | 🟡 P2 | سلوك صحيح (الحفظ موجود) لكن السلوك عند **عدم** وجود حفظ لم يُختبر بصريًا بعد | اختبار بحذف ملفات الحفظ ثم لقطة |
| ٦ | `Extras` · `Credits` · `Exit` **مش موجودين** في الشاشة الأولى | 🟠 P1 | `MenuModel.BuildTitle` فيه 5 عناصر بس | إضافتها في المرحلة القادمة (تحتاج شاشات) |
| ٧ | المنيو **مش بتفتح لوحدها** في بداية `Main` (سجل: `IsOpen=False` عند الإطار ٤٥) | 🟠 P1 | `GameBootstrap.Start` بينادي `Show(Title)` قبل ما `MenuSystem.Build` يخلص | نداء `Show` بعد إطار (نفس نمط `BindUINextFrame`) |

---

## ٦) اللي **مش** اتعمل في المرحلة دي

- ❌ شاشة تحميل (Loading) — مش موجودة أصلاً، الانتقال بين المشاهد فوري.
- ❌ `Extras` / `Credits` / `Exit`.
- ❌ خلفية 3D سينمائية للمنيو (دلوقتي خلفية داكنة أحادية + قاعدة ذهبية).
- ❌ اختبار `Continue` بحفظ فعلي من داخل اللعبة (الحفظ الموجود من جولة قديمة).
- ❌ لقطات على مقاس موبايل رأسي (1080×1920) — كل اللقطات 1280×720 أفقي.
- ❌ APK.

---

## ٧) الأدلة

| الملف | إيه ده |
|---|---|
| `Screenshots/menu/01_main_menu.png` | المنيو كاملة: عنوان + 5 أزرار عربية + تلميح |
| `Screenshots/menu/02_after_new_game.png` | بعد الدوسة: مشهد `Play` بيتبني |
| `Screenshots/menu/03_chapter1_dialogue.png` | الفصل الأول: مشهد 3D + شخصيات + حوار عربي + أزرار |
| `Screenshots/menu/menu-flow-log.txt` | سجل التشغيل الكامل + الملخّص |
| `Assets/_Project/Editor/MenuFlowCapture.cs` | أداة الإثبات (تشغيل + دوسة + تصوير + تشخيص) |
| `Assets/_Project/Editor/TMPEssentials.cs` · `FontAssetBuilder.cs` | إصلاح البنية التحتية للنص |

**إعادة التشغيل:**
```bash
Xvfb :99 -screen 0 1280x720x24 &
export DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1
Unity -projectPath . -executeMethod TLC.EditorTools.MenuFlowCapture.Run \
      -captureDir Screenshots/menu -force-glcore
```

---

## ٨) الحكم على الجاهزية للمرحلة ٢.٢

**جاهز** — بعد إغلاق P1 (التخطيط + فتح المنيو تلقائيًا + Extras/Credits/Exit).
المسار الحرج (قائمة → لعبة جديدة → فصل أول بحوار) **بقى مثبتًا بلقطات حقيقية** لأول مرة.
