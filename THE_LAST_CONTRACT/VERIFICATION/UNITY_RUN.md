# تشغيل Unity الحقيقي — أول مرة

**التاريخ:** 2026-11-09 · **Unity:** 6000.0.28f1 (f336aca0cab5) · **نظام:** Linux headless
**الأمر المستخدم:**
```
Unity -batchmode -nographics -quit \
  -projectPath ./THE_LAST_CONTRACT \
  -executeMethod TLC.EditorTools.VerificationRunner.RunEditorVerification \
  -logFile editor3.log
```

## النتيجة

```
════════ THE LAST CONTRACT — تقرير التحقق (محرر) ════════
Unity   : 6000.0.28f1
── النتيجة: 32 نجح · 0 فشل ──
```

**٣٢ فحص من ٣٢ نجحوا — جوه المحرر، مش على الورق.**

## أهم ٥ حقائق اتحققت (اللي كانت «تنبّؤ» وبقت نتيجة)

| البند | النتيجة الحقيقية |
|---|---|
| المشروع بيفتح ويتستورد | ✅ 42 دقيقة · `Library` 1.5 GB · صفر أخطاء استيراد |
| `TLC_Character.controller` | ✅ **اتولّد فعلاً** — `Resources/Animators/TLC_Character.controller` بـidle/walk/run |
| **Humanoid Avatars** | ✅ **الأربعة `isHuman = true`** — Unity نفسه هو اللي قال، مش أنا |
| `Missing Scripts` في `Play.unity` | ✅ **0** |
| Color Space | ❌ كان Gamma ⇒ اتصلّح إلى **Linear** ⇒ ✅ في التشغيل التالي |

## باج حقيقي لقطعه المحرك (مش الاختبار الآلي)

```
Assets/_Project/Editor/CharacterImportSettings.cs(25,59):
  error CS0117: 'ModelImporterAnimationType' does not contain a definition for 'Humanoid'
Assets/_Project/Editor/CharacterSetupWizard.cs(70,70) · (72,69) · (227,106)
Assets/_Project/Editor/VerificationRunner.cs(127,86)
```

**الحكاية:** في Unity القيمة الصح هي `ModelImporterAnimationType.Human` — مفيش حاجة اسمها
`Humanoid` في الـenum. الاختبار الآلي كان بيعدّيها لأن الطبقة البديلة (`compile-check/Stubs`)
كانت أنا اللي كاتبها بالقيمة الغلط. **أول ما المحرك الحقيقي شاف الملف، رفضه.**
اتصلّحت في الـ5 مواضع + في الـstubs، وبعدها الاستيراد كمل.

> الدرس ده لوحده بيثبت ليه «`dotnet build` عدّى» ما كانش دليل على حاجة.

## اللي لسه **مش** متحقق (بصراحة)

| البند | ليه |
|---|---|
| لقطة شاشة / فيديو من المحرر | مفيش GPU ولا `DISPLAY` — `-nographics` بس |
| تشغيل `Play.unity` فعليًا (Play Mode) | محتاج رندرة/شاشة، والحلقة الرسومية مش موجودة |
| بناء APK | مفيش Android module ولا SDK/NDK (~6 GB) والفراغ 4.4 GB |
| اختبار اللمس والـJoystick | محتاج جهاز |

## إزاي الترخيص اتحل (للمرجع)

| المحاولة | النتيجة |
|---|---|
| Manual `.alf/.ulf` | ❌ Unity Personal مش مؤهل |
| Unity CLI OAuth | ❌ الدخول نجح والقناة استلمت الكود، لكن تبادل التوكن اتقطع (`Connection reset by peer`) |
| `Unity -serial -username -password` | ✅ **الدخول نجح** (`logged in successfully`) لكن: `Found 0 entitlement groups` — الحساب ماكانش شايل رخصة Personal |
| **`Unity.Licensing.Client --activate-all --include-personal`** | ✅ **`Seat ID: 11270587052615-UnityPersonal` · `[200] ASSIGN_SEAT`** |

الطريقة الأخيرة هي المفتاح: أداة الترخيص اللي جوه المحرر نفسها، بـ`--include-personal`
بتحجز رخصة Personal للحساب وبتنزّل `UnityEntitlementLicense.xml`.

## المخرجات في المشروع

| الملف | إيه ده |
|---|---|
| `VERIFICATION/editor-report.txt` | تقرير الـ32 فحص كما كتبه المحرك |
| `VERIFICATION/unity-editor-console.txt` | سجل Console مختصر من التشغيل الحقيقي |
| `Assets/_Project/Resources/Animators/TLC_Character.controller` | الـAnimator المولّد + `idle.anim` |
| `ProjectSettings/*.asset` | إعدادات المشروع كاملة كما ولّدها Unity (Linear · Portrait · IL2CPP…) |
| `Assets/**/*.meta` | 163 ملف GUID ثابت (كانوا 82) — مفيش إعادة استيراد عشوائية عندك |
