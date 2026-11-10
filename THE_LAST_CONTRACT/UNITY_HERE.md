# Unity اتثبّت — الحالة الحقيقية

**التاريخ:** 2026-11-09 · **النسخة:** Unity 6000.0.28f1

## الملخص في ٣ سطور

1. **Unity 6 اتنزّل واتثبّت وبيشتغل** في بيئة التطوير دي (`/opt/unity/editor/Editor/Unity` · 7.9 GB).
2. المحرر **بيقلع فعلاً** وبيوصل لخدمة الترخيص، وبيوقف عند سطر واحد:
   ```
   No valid Unity Editor license found. Please activate your license.
   ```
3. يعني العائق الوحيد الباقي **ترخيص Unity** (مش التنزيل ولا الرام). ولّدت ملف الطلب
   جاهز: `UNITY_ACTIVATION_TLC.alf`.

## اللي محتاجه منك — ٣ خطوات، مفيش أي تثبيت ولا كود

| # | تعمل إيه | الناتج |
|---|---|---|
| ١ | نزّل `UNITY_ACTIVATION_TLC.alf` من الـ workspace | الملف |
| ٢ | افتح <https://license.unity3d.com/mws/media/activate> أو <https://license.unity3d.com/manual> → سجّل دخول بحساب Unity بتاعك → ارفع الملف → اختار **Personal** → نزّل ملف `.ulf` | `Unity_v6000.x.ulf` |
| ٣ | ابعت ملف `.ulf` هنا (ارفعه في المحادثة) | — |

بعدها أنا بنفّذ الباقي لوحدي:
```bash
./tools/unity_here.sh license <الملف.ulf>   # تفعيل
./tools/unity_here.sh report                # استيراد + تقرير تحقق كامل
```
وأطلّع: سجل Console حقيقي · `TLC_Character.controller` متولّد · نتيجة `isHuman` لكل Avatar
· تقرير المشهدين · أي أخطاء استيراد أصلحها في المشروع.

> **ليه مش هبعتلك كلمة سر؟** لأن التفعيل اليدوي بالملف هو الطريقة الوحيدة اللي مش
> محتاجة تسلّم حد بيانات حسابك. مفيش داعي تبعت بريد ولا باسورد خالص.

## اللي هيفضل مستحيل حتى بعد الترخيص

| البند | السبب |
|---|---|
| لقطات من واجهة المحرر / فيديو لعب | مفيش GPU ولا `DISPLAY` (`/dev/dri` غير موجود) — الرندرة مش موجودة في البيئة |
| بناء APK | مفيش Android SDK/NDK ولا Android module (تنزيل إضافي ~6 GB على 5.4 GB فراغ) |
| اختبار على موبايل | مفيش جهاز ولا `/dev/kvm` |

يبقى: الترخيص بيفتح **الاستيراد والتوليد والتقارير** (وهي أهم حاجة ليك دلوقتي: هل المشروع
بيفتح ويولّد الـ Animator والـ Avatars؟). أما «شكل اللعبة على الشاشة» فمحتاج جهازك.

---

## سجل التشغيل الفعلي

```
$ /opt/unity/editor/Editor/Unity -batchmode -nographics -quit -createProject /opt/tlc/testproj
Unity Editor version:    6000.0.28f1 (f336aca0cab5)
Branch:                  6000.0/staging
Batch mode:              YES
System name:             Linux
Available memory:        1982 MB
[Licensing::Module] Successfully launched the LicensingClient (PId: 19739)
[Licensing::Module] Successfully connected to LicensingClient on channel: "LicenseClient-user"
[Licensing::Client] Error: Code 500 … No ULF license found.,Token not found in cache
[Licensing::Client] Error: Code 404 … Found 0 entitlement groups and 0 free entitlements
Pro License: NO
No valid Unity Editor license found. Please activate your license.
exit=1
```

السبب اللي قبل ده (اتصلّح): `error while loading shared libraries: libgtk-3.so.0`
⇒ اتثبّتت 23 حزمة نظام (`libgtk-3-0` · `libnss3` · `libgbm1` …).

كمان فعّلت **6 GB swap** بنجاح (`swapon` اشتغل جوه الحاوية) عشان الرام تبقى 8 GB منطقية.
