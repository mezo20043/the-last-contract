# تشغيل Unity على GitHub — خطوة بخطوة (بدون ما تبعت باسوردك لحد)

**الهدف:** Unity 6 يستورد المشروع، يولّد الـAnimator والـAvatars، ويبني **APK** حقيقي —
وكل ده على سيرفرات GitHub. أنت بتكتب بياناتك في **ريبوك الخاص**، وأنا بقرأ النواتج بس.

**الوقت:** ~١٥ دقيقة إعداد + ٣٠–٦٠ دقيقة أول بناء.

---

## ٠) قبل ما تبدأ

| الحاجة | ليه |
|---|---|
| حساب GitHub (مجاني) | المجاني بيدّيك 2000 دقيقة شهريًا لريبوهات خاصة — البناء ده ~٤٠ دقيقة |
| ملف `THE_LAST_CONTRACT_v1.8.zip` | هو المشروع نفسه |
| متصفح Chrome/Edge | بيدعم رفع مجلدات بالسحب |

---

## ١) اعمل ريبو خاص

1. <https://github.com/new>
2. **Repository name**: `the-last-contract`
3. اختار **Private** ← مهم
4. **متعلمش** على Add README (خليه فاضي)
5. **Create repository**

## ٢) ارفع المشروع

1. فك ضغط `THE_LAST_CONTRACT_v1.8.zip` على جهازك.
2. هتلاقي فولدر جوّاه اسمه `THE_LAST_CONTRACT` — **ادخل جواه**.
3. في الريبو على GitHub: **Add file ▾ → Upload files**.
4. اسحب **كل اللي جوه الفولدر** (مش الفولدر نفسه): `Assets` · `ProjectSettings` · `Packages` · `.github` · `tools` · كل ملفات `.md` · `Screenshots` · `VERIFICATION`
5. استنى الرفع يخلص (الموديلات ~78 MB) → **Commit changes**.

> **مهم:** فولدر `.github` مخفي في ماك/لينكس. لو مش ظاهر أو مارضيش يترفع:
> **Add file ▾ → Create new file** → اكتب المسار: `.github/workflows/build-android.yml`
> → افتح نفس الملف من الـzip والصق محتواه كله → **Commit**.
> (من غيره مفيش Actions تشتغل.)

## ٣) حط بيانات Unity كـSecrets

1. في الريبو: **Settings** → (الشريط الجانبي) **Secrets and variables** → **Actions**
2. **New repository secret** →
   - Name: `UNITY_EMAIL` · Secret: بريد حساب Unity
3. **New repository secret** →
   - Name: `UNITY_PASSWORD` · Secret: الباسورد

> البيانات دي متشفّرة ومحدش بيشوفها — ولا أنا. GitHub هي اللي بيسجّل الدخول على السيرفر.
> لو عندك 2FA: اقفله مؤقتًا لحد ما البناء يخلص، أو اعمله Access Token.

## ٤) شغّل

1. تبويب **Actions**
2. من الشمال: **Unity 6 → استيراد + تقارير + APK**
3. **Run workflow ▾** → **Run workflow**
4. استنى — أول مرة بتحمّل محرر Unity على السيرفر (٤–٦ جيجا) فبتاخد وقت.

## ٥) اللي هطلع في الآخر

تحت الـrun هتلاقي **Artifacts**:

| Artifact | فيه إيه |
|---|---|
| `verification-report` | تقرير التحقق من **داخل المحرك** + `Assets/_Project/Resources/Animators/TLC_Character.controller` (الدليل إن الـAnimator اتولّد) + `Logs/` |
| `TheLastContract-APK` | ملف `TheLastContract.apk` — يتسطّب على أندرويد |
| `unity-logs` | سجل Unity الكامل |

**ابعتلي:** لينك الـrun (أو نزّل `verification-report` وارفعه هنا). أوعى تبعت الـSecrets.

---

## لو البناء فشل

| الرسالة | المعنى | الحل |
|---|---|---|
| `No valid Unity Editor license found` | الـSecrets غلط أو فيها 2FA | راجع `UNITY_EMAIL`/`UNITY_PASSWORD`، اقفل 2FA مؤقتًا |
| `Package [...] cannot be found` | نسخة حزمة مش متاحة لـ6000.0.28f1 | ابعتهالي وأنا أعدّل `Packages/manifest.json` |
| `error CS....` | خطأ ترجمة | ابعتهالي بالسطر وأنا أصلحه في المشروع |
| `Android SDK not found` | الـmodule مش متسطّب في الصورة | استخدم `game-ci/unity-builder@v4` زي ما هو مكتوب (بينزّل Android module لوحده) |

---

## بديل: نفس الحاجة من جهازك في ٥ دقايق

لو عندك جهاز فيه Unity 6 (أو تقدر تسطّبه): افتح المشروع → `TLC → 13 · تشغيل تلقائي + لقطات`
→ بيطلّع لقطات + سجل Console + تقرير في `VERIFICATION/`. أسرع من كل ده، وبيدي **صور** كمان
(اللي الـCI مش هيقدر يعملها لأن سيرفرات GitHub ملهاش كارت شاشة).
