# تفعيل Unity Personal — سجل المحاولة والسبب الحقيقي

**التاريخ:** 2026-11-09 · **البيئة:** لينكس headless داخل sandbox
**Unity:** 6000.0.28f1 (مثبّت في `/opt/unity/editor` · 7.9 GB · بيقلع فعلاً)

---

## ١) اللي اتجرّب بالترتيب

| الطريقة | النتيجة | السبب |
|---|---|---|
| Manual activation (`.alf` → `.ulf`) | ❌ مستحيل | Unity Personal **مش مؤهل** للتفعيل اليدوي (نص التوثيق الرسمي) |
| Command line `-serial` | ❌ مستحيل | محتاج سيريال، وPersonal بلا سيريال |
| Unity CLI — OAuth من المتصفح | ⚠️ **الدخول نجح، التبادل فشل** | شبكة الـsandbox بتقطع طلب تبادل الكود |
| Editor `-username -password` | لم يُجرَّب | محتاج بيانات الحساب (قرار المستخدم) |

## ٢) الدليل على إن المشكلة **مش** في الحساب

تسجيل الدخول من المتصفح كمل بنجاح فعلاً — القناة استلمت الكود:

```
GET https://services.api.unity.com/app-linking/v1/channels/ejLcKx0GNklWS3MWuNLn1nI3ldvjEBYuiifv
→ Response: https://implicit/callback/login/channel/ejLc...?code=GvQ0KdVFqrj16SJvq2
```

وبعدها الـCLI بيفشل في خطوة تبادل الكود بالتوكن:

```
Error: Sign-in failed: error sending request for url (https:[redacted]):
       client error (SendRequest): connection error: Connection reset by peer (os error 104)
```

**نفس اللحظة، نفس الجهاز، نفس الـendpoint — بـ curl يشتغل عادي:**

```
POST https://api.unity.com/v1/oauth2/token  → HTTP 400 (طلب ناقص) · مفيش reset
curl --http1.1 / --http2 / --http2-prior-knowledge → كلهم شغالين
```

يعني: **الـegress بتاع الـsandbox بيقفل اتصال الـCLI تحديداً** (عميل Rust/reqwest)،
مش خدمة Unity. ده عيب بيئة، مش عيب حساب ولا مشروع.

## ٣) اللي لسه ممكن

| المسار | محتاج إيه | مين بيعمله |
|---|---|---|
| **A. GitHub Actions (game-ci)** — الأفضل | ريبو GitHub + سيكرتين `UNITY_EMAIL` و`UNITY_PASSWORD` (انت تكتبهم في ريبوك، أنا مش بشوفهم) | اضغط Run — والباقي يطلع لوحده: APK + سجل Unity حقيقي |
| **B. تفعيل بمحرر Unity هنا** | بريد وباسورد الحساب تديهم لي | أنا أنفّذ `Unity -batchmode -serial -username -password` (مسار HTTP مختلف، ممكن يعدّي) |
| **C. على جهازك** | جهاز فيه Unity 6 | `TLC → 13` بيطلّع اللقطات والتقرير لوحده |

الملف اللي بيشتغل في كل الحالات هو نفس المشروع: `THE_LAST_CONTRACT_v1.7.zip`
(`.github/workflows/build-android.yml` جاهز للمسار A · `tools/unity_here.sh` جاهز للاتنين B وC).

## ٤) ملاحظة صريحة

**مفيش لقطة شاشة من Unity، ومفيش APK، ومفيش استيراد حقيقي — لسه.**
اللي اتحقق فعلاً: المشروع بيتبني (237 فحص) · كود الحركة بيشتغل بره المحرك (600 فريم) ·
آدم جوه كادر الكاميرا بالحساب · Unity نفسه بيقلع على الجهاز ومستني ترخيص.
