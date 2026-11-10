# بناء APK — دليل كامل من الصفر

> **الهدف:** من ملف مضغوط لملف APK على موبايلك، بأقل عدد خطوات ممكن.
> الوقت المتوقع: ٤٥–٩٠ دقيقة (أغلبها تنزيل Unity).

---

## المتطلبات

| البند | التفصيل |
|---|---|
| **Unity Hub** | [unity.com/download](https://unity.com/download) |
| **Unity Editor** | **6000.0.28f1** بالظبط (أو أي 6000.0.x LTS — هيتحدّث تلقائياً) |
| **Android Build Support** | وحدة إجبارية في Unity Hub |
| **Android SDK & NDK Tools** | وحدة إجبارية (مع OpenJDK) |
| **مساحة قرص** | ~٢٥ جيجا |
| الموبايل | أندرويد ٨.٠ (API 26) أو أحدث، ومعه **٦ جيجا رام** على الأقل |

### تنزيل الوحدات
Unity Hub → **Installs** → جنب نسخة 6000.0.28f1 → **⚙ → Add modules** → علّم على:

```
☑ Android Build Support
    ☑ Android SDK & NDK Tools
    ☑ OpenJDK
```

> لو نسيت دي، البناء هيفشل برسالة `Android SDK not found` أو `No Android module loaded`.

---

## الخطوات (بالترتيب — متتخطاش حاجة)

### ١) افتح المشروع
```
Unity Hub → Add → Add project from disk → اختار فولدر THE_LAST_CONTRACT
```
افتحه. أول فتح بياخد ٥–١٥ دقيقة (استيراد الحزم وبناء الـ Library).

### ٢) ظبّط المشروع — **نقرة واحدة**
من المنيو العلوي:
```
TLC → 0 · إعداد المشروع للأندرويد (افعلها الأول)
```

ده بيظبّط أوتوماتيك:
- Bundle ID: `com.thelastcontract.game`
- IL2CPP + ARM64
- minSdk 26
- Color Space: **Linear**
- Vulkan + GLES3
- الاتجاه: Portrait
- **Active Input Handling = Both** ← أهم إعداد في المشروع كله
- Asset Serialization: Force Text (عشان Git)
- Managed Stripping: Low

هتلاقي في الـ Console: `✅ [TLC] المشروع اتظبّط للأندرويد.`

### ٣) جهّز المشهد
```
TLC → 1 · إنشاء مشهد الفصل الأول
TLC → 2 · فتح مشهد الفصل الأول
```
> `1` بيشتغل تلقائي أول مرة، بس نفّذها يدوياً عشان تكون متأكد.

### ٤) جرّب في المحرر قبل البناء
اضغط **▶ Play**.

المفروض تشوف: كارت «الفصل الأول» → الشقة → آدم → الحوار → الخيارات.

**قبل أي حاجة تانية، اتأكد من دي:**

| اختبر | المفروض يحصل |
|---|---|
| WASD | آدم يمشي |
| Shift + WASD | يجري |
| زر ماوس يمين + حركة | الكاميرا تلف |
| زر ماوس شمال على «التالي» | الحوار يتقدّم |
| المشي لحد الرف | يظهر «افحص الصورة» |
| زر E | تتفتح العقدة اللي بعدها |

### ٥) الخط العربي — **متظبّط بالفعل**
الخط **Amiri** (رخصة OFL 1.1) موجود جوه المشروع في
`Assets/_Project/Resources/Fonts/Amiri-Regular.ttf`، و`TLC.Settings.ArabicFont`
بيولّد منه Font Asset ديناميكي لـTextMeshPro **وقت التشغيل** أول ما أي واجهة تتنادى.
يعني مفيش أي خطوة يدوية ولا Font Asset Creator ولا ربط في الكود.

> لو الـConsole قال `[TLC/Font] 'Fonts/Amiri-Regular' مش موجود` — يبقى ملف الخط
> اتشال من المشروع، ارجعه مكانه بالظبط وخلاص.

### ٦) ابنِ الـ APK
```
TLC → 6 · بناء APK (Android)
```

المخرج: **`Builds/Android/TheLastContract.apk`**
(بيفتحلك الفولدر أوتوماتيك لما يخلص)

> أول بناء بياخد **٨–٢٥ دقيقة** لأن IL2CPP بيترجم C# لـ C++.
> البناءات اللي بعده أسرع بكتير.

### ٧) نزّل على الموبايل
- انقل `TheLastContract.apk` للموبايل (كابل / Google Drive / Telegram).
- افتح الملف → **تثبيت** (لازم تسمح بـ *Install unknown apps*).

---

## المشاكل المتوقعة وحلولها

| الرسالة | السبب | الحل |
|---|---|---|
| `Android SDK not found` | الوحدة مش متسطّبة | Unity Hub → Add modules → Android SDK & NDK |
| `Gradle build failed` | مشكلة في JDK | اتأكد إن OpenJDK متسطّب مع الوحدة |
| `Building for ARM64 requires IL2CPP` | إعداد غلط | أعد تنفيذ `TLC → 0` |
| الأزرار مش بتشتغل | Active Input Handling ≠ Both | `TLC → 0` ثم أقفل Unity وافتحه |
| النص مربعات | مفيش Font Asset عربي | خطوة ٥ |
| `Missing scene in build` | المشهد مش مسجّل | `TLC → 1` |
| اللعبة بتعمل crash عند الفتح | Stripping عالي | أعد `TLC → 0` (بيحط Low) |
| أسود تماماً | الإضاءة بالكود + URP | اتأكد إن SRP = URP في Graphics Settings |
| ١٥ إطار | الجهاز ضعيف | F1 في اللعبة بيغيّر الجودة |

### فحص سريع لو حاجة غلط
```
TLC → 7 · فحص جاهزية البناء
```
بيبعت في الـ Console: نسخة Unity، Color Space، Bundle ID، الـ Backend، الـ Scenes، وجود ملف القصة، ووجود URP.

---

## تحسينات أندرويد المطبّقة في الكود

| التحسين | المكان |
|---|---|
| ٣ مستويات جودة (LOW/MEDIUM/HIGH) | `QualityManager.cs` |
| تخفيف تلقائي عند الحمل | `MobileOptimizations.cs` |
| إيقاف مؤقت عند الخروج للتطبيق | `MobileOptimizations.cs` |
| معدل إطارات متكيّف | `MobileOptimizations.cs` |
| ضغط حركة optimal للموديلات | `CharacterSetupWizard.cs` |
| شيدو ديسانتس وبيكسل لايتس حسب الطبقة | `QualityManager.cs` |
| منطقة آمنة (نوتش) | `SafeAreaFitter.cs` |
| عناصر واجهة خفيفة (بدون بلوم في LOW) | `QualityManager.cs` |

---

## اللي **مش** متحقّق منه في بيئة التطوير

| البند | السبب |
|---|---|
| تشغيل اللعبة داخل Unity | Unity غير مثبّت في بيئة البناء |
| بناء APK فعلي | نفس السبب + مفيش Android SDK |
| اختبار الأداء على جهاز حقيقي | مفيش جهاز |
| تجربة الكاميرا والحركة بصرياً | مفيش GPU |

**اللي متحقّق فعلاً:**
- ✅ كل سكربتات اللعبة والمحرر اتترجمت — **٠ أخطاء، ٠ تحذيرات**
- ✅ منطق القصة: ١٠٬٠٠٠ مسار — صفر طرق مسدودة، ٣/٣ نهايات قابلة للوصول
- ✅ سلامة روابط شجرة الحوار (٤٥ عقدة)

---

## التالي بعد أول APK شغّال

1. **آدم واقعي:** Mixamo → شخصية مجانية → `TLC → 5` → اسحب الـ Prefab في `CharacterModelSlot`.
2. **الصوت:** حطّ ملفات في `Resources/Audio/Voice/c1_murad_1.mp3` بنفس اسم العقدة — بيشتغل لوحده.
3. **الفصل الثاني:** انسخ `chapter01.json` وغيّر الـ `id` — المحرك مش محتاج تعديل.

---

## بديل: البناء من سطر الأوامر (CI)

```bash
Unity -quit -batchmode -nographics \
  -projectPath "/path/to/THE_LAST_CONTRACT" \
  -executeMethod TLC.EditorTools.AndroidBuilder.BuildApkCI \
  -logFile build.log
```

كود الخروج: `0` = نجاح · `1` = مفيش APK · `2` = استثناء.

---

## بناء بدون جهاز عليه Unity — GitHub Actions (game-ci)

لو معندكش جهاز يقدر يشغّل Unity 6، الـAPK ممكن يتبني على سيرفر GitHub مجاناً
من غير ما تنزّل Unity خالص. الملف جاهز في المشروع:

```
.github/workflows/build-android.yml
```

**اللي محتاجه (مرة واحدة):**
1. ريبو GitHub **خاص** فيه المشروع.
2. Secret باسم `UNITY_LICENSE` — بتجيبه من
   [game-ci/unity-license-activation](https://game.ci/docs/github/activation)
   (workflow بيطلّع ملف `.ulf` من حسابك، من غير ما تسلّمه لأي حد).
   بديل لمشروع Plus/Pro: `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL`.
3. Actions → **Build APK (Android)** → **Run workflow**.

**النتيجة:** `TheLastContract-APK` في الـArtifacts — ملف APK موقّع للاختبار
(debug keystore؛ للنشر على Google Play محتاج keystore خاص، والشغل ده في
`AndroidBuilder` جاهز لاستقباله).

**ملاحظات صريحة:**
- المشروع **ما اتبناش** من بيئة التطوير دي، والـworkflow ده **ما اتجرّبش** —
  مفيش Unity ولا رخصة هنا (تفاصيل القياس في `VERIFICATION/ENVIRONMENT.txt`).
- أول تشغيل ممكن يطلع أخطاء ترجمة أو استيراد. لو حصل، ابعتلي نص الخطأ
  وأنا أصلحه في المشروع نفسه.
