# أول تشغيل حقيقي للعبة داخل Unity Play Mode — تقرير

**التاريخ:** 2026-11-09 · **Unity:** 6000.0.28f1 · **الرندر:** llvmpipe (سوفتوير، مفيش GPU)
**الطريقة:** Unity editor + Xvfb (شاشة افتراضية) + Play Mode حقيقي + تصوير بالكاميرا نفسها عبر RenderTexture

---

## ١) اللي اتنفّذ فعلاً في الجولة دي

| # | الشغل | الحالة |
|---|---|---|
| ١ | تثبيت Unity 6 من جديد (الساندبوكس اتمسح) + تفعيل الرخصة | ✅ |
| ٢ | استيراد المشروع كامل (Library 1.5GB) | ✅ 0 أخطاء |
| ٣ | تشغيل Play Mode فعلي لأول مرة (Xvfb + llvmpipe) | ✅ |
| ٤ | **كشف وإصلاح ٣ باجات حقيقية** كانت مخليّة اللعبة مش شغالة | ✅ (تحت) |
| ٥ | تصوير ٨ لقطات من كاميرا اللعب | ✅ `Screenshots/unity/` |
| ٦ | تشغيل الفصل الأول من أوله: 32 سطر حوار + 5 قرارات | ✅ `Screenshots/chapter01/` |
| ٧ | قياس أداء حقيقي | ✅ 2.9–28.6 FPS على llvmpipe |
| ٨ | لقطات فيها الواجهة (نص الحوار) | ❌ الرندر مالوش UI (تحت) |
| ٩ | إكمال الفصل الأول لآخره | ❌ المحرر وقع بعد 32 سطر (تحت) |
| ١٠ | APK | ❌ مفيش Android module |

---

## ٢) الباجات اللي المحرك كشفها — وكانت مخفية تمامًا

### باج ١: كل مواد اللعبة كانت ماجنتا (اللعبة مش بتترندر أصلاً)
```
ProjectSettings/GraphicsSettings.asset → m_CustomRenderPipeline: {fileID: 0}
```
المشروع بيستخدم مواد `Universal Render Pipeline/Lit` بس مفيش URP مربوط ⇒ Built-in RP ⇒ **كل حاجة ماجنتا**.
أول ٨ لقطات طلعت ماجنتا بالكامل. **الإصلاح:** `TLC/إعداد URP` بينشئ `TLC_URP.asset` + Renderer ويربطهم في Graphics + Quality.

### باج ٢: آدم كان بيظهر كمكعبات — الموديل الحقيقي ماكانش بيتحمّل
```csharp
Resources.Load<GameObject>("Characters/" + key)   // ❌ بيدوّر على "Characters/adam"
// الملف الحقيقي: Resources/Characters/adam/man-base-character-rigged.fbx
```
⇒ `null` ⇒ البديل البدائي (مكعبات + كورة للراس). **الإصلاح:** مسار الموديل الصريح
(`modelFile` في `CharacterLibrary.Roster`) + fallback بـ`Resources.LoadAll`.
**النتيجة:** في اللقطات بقى فيه **موديلات بشرية حقيقية** (آدم + مراد قاعدين على الطرابيزة).

### باج ٣: الكاميرا بتبوّظ اللقطات — كانت جوه الأبواب والحيطان
اللقطة الأمامية كانت بتطلع من جوه `FrontDoor`. **الإصلاح:** `SafeCameraPos` (SphereCast) في المصوّر،
و`ThirdPersonCameraRig` الجديد بـ٥ عينات اصطدام + استثناء جسم آدم.
```
⚠ الكاميرا كانت جوه FrontDoor — اتقرّبت لـ1.18م
```

### مشكلة رابعة اتكشفت: الواجهة كانت بترمي NullReferenceException
```
NullReferenceException at TMPro.TMP_Settings.get_clearDynamicDataOnBuild
  ← TLC.Settings.ArabicFont.Ensure()
```
`TMP Settings` مش موجود في المشروع أصلاً ⇒ أول سطر واجهة يوقع.
**الإصلاح:** `TMPEssentials.cs` بيستورد موارد TMP أوتوماتيك عند فتح المشروع.

### مظهر: المطر بينزل **جوه** الشقة
ظاهر في `02_gameplay_front.png` — نظام المطر مش متقيّد بالمساحة الخارجية. **لسه مفتوح.**

---

## ٣) أدلة التشغيل الفعلي

### أ) لقطات كاميرا اللعب (8)
`Screenshots/unity/` — 1280×720 من `camera.Render()`:

| اللقطة | اللي فيها |
|---|---|
| `04_apartment_wide.png` | الغرفة كاملة: أرضية خشب، حيطة، لمبة بتنوّر، شباك، سرير |
| `02_gameplay_front.png` | ديتيل الخامات + المطر (باج) |
| `01/03/05/06/07/08` | زوايا من فوق الكتف، قريبة، منخفضة، ولقطة قدام الحيطة |

**الأداء المقيس:** 28.6 FPS (16.6 أسوأ إطار) قبل الموديلات الحقيقية · 10.1 FPS بعد URP · **2.9 FPS** بعد ما الشخصيات الحقيقية (143k–427k رأس) ظهرت. ده على **سوفتوير رندر بلا GPU** — مش رقم موبايل.

### ب) الفصل الأول شغّال فعلاً (32 لقطة + سجل)
`Screenshots/chapter01/line_01.png … line_32.png` + `playthrough-log.txt`

```
[TLC/Play] ببدأ الفصل الأول عبر GameBootstrap.StartChapter(1)
📷 line_03.png · آدم
📷 line_05.png · مراد
   ▸ اخترت القرار الأول
📷 line_13.png · متصل مجهول
📷 line_29.png · ياسين
```
سجل حقيقي: 32 سطر، تغيّر المتحدثين، **5 قرارات اتاخدت فعلاً**، والفصل ماشي على مساره.
**ما كمّلش لآخره:** المحرر وقع (`Exiting early due to double fault`) بعد 32 سطر — الذاكرة 2GB + سوفتوير رندر.

---

## ٤) اللي **مش** اتعمل — بصراحة كاملة

| المطلوب | الحالة | السبب الحقيقي |
|---|---|---|
| لقطات فيها صندوق الحوار العربي | ❌ | الكانفس كان Screen Space Overlay مش بيدخل RenderTexture. اتحوّل لـScreen Space Camera بس لسه محتاج جولة تأكيد |
| إكمال الفصل الأول لآخره | ❌ | المحرر وقع (RAM 2GB). محتاج جهاز ≥8GB |
| الفصلين الثاني والثالث في Play Mode | ❌ | نفس السبب |
| تعبيرات وجه / Lip Sync / إيماءات | ❌ | الموديلات CC0 ملهاش BlendShapes للوجه ولا Visemes — محتاج موديلات فيس-ريج + تسجيل صوتي |
| موسيقى وحوار صوتي بشري | ❌ | مفيش تسجيلات؛ اللي موجود تونات مولّدة، ومش هقول إنها تمثيل صوتي |
| APK | ❌ | مفيش Android module (≈6GB) ولا SDK/NDK |
| أثاث/بيئة AAA | ⚠️ جزئي | الغرفة ما زالت مكوّنات إجرائية (صناديق/سطوح بمقاسات). الأثاث الحقيقي محتاج أصول 3D مستوردة |
| حفظ/تحميل · Pause · إعدادات · شابتر سيليكت | ⚠️ الكود موجود | **لم يُختبر بصريًا** بعد — مش هقول إنه شغّال |

---

## ٥) المطلوب عشان الجولة الجاية تكمل

1. **جهاز/سيرفر فيه ≥8GB RAM + GPU** (حتى GTX 1050): Play Mode كامل بدون وقوع، وأداء حقيقي.
2. **تشغيل الـAPK**: Unity 6 Android module + SDK/NDK (≈6GB) — مساحة الساندبوكس 8GB فاضية مش كفاية مع المشروع.
3. **موديلات بوجه متحرك** (ARKit/FACS أو Ready Player Me) لو المطلوب Lip Sync وتعبيرات حقيقية.
4. **تسجيلات صوتية** لو المطلوب حوار صوتي — السكربت (`VO_SCRIPT.md`) جاهز للتسجيل.

---

## ٦) إعادة تشغيل كل اللي فوق

```bash
Xvfb :99 -screen 0 1280x720x24 &
export DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1

# ١) ربط URP (مرة واحدة)
Unity -batchmode -nographics -quit -projectPath . \
      -executeMethod TLC.EditorTools.URPSetup.Setup

# ٢) ٨ لقطات من كاميرا اللعب في Play Mode
Unity -projectPath . -executeMethod TLC.EditorTools.PlayModeCapture.Run \
      -captureDir Screenshots/unity -force-glcore

# ٣) تشغيل الفصل الأول وتصوير كل سطر
Unity -projectPath . -executeMethod TLC.EditorTools.ChapterPlaythrough.Run \
      -captureDir Screenshots/chapter01 -force-glcore
```
