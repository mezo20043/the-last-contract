# R1.5 — تقرير جدوى اختبار Unity الفعلي

**التاريخ:** 2026-11-09 · **النطاق:** فحص الجدوى فقط — **مفيش تثبيت · مفيش تنزيل · مفيش أي تعديل على المشروع**
**النسخة المطلوبة:** Unity **6000.0.28f1** · revision **`f336aca0cab5`**

---

## ١) الخلاصة في سطرين

**بيئة Unity غير متاحة الآن** — المحرر كان **مثبَّت وشغّال فعلاً في نفس هذه الحاوية يوم 2026-11-09** واتمسح في تصفير الـworkspace. إعادة تثبيته = تنزيل **4.3 جيجا** (محتاج موافقتك)، ومع ذلك **مش هيدّي دليل موثوق** لأن ذاكرة الساندبوكس **1.9 جيجا** ومفيش كارت شاشة. **التوصية: التنفيذ على جهازك الـWindows** — الخطوات الكاملة في القسم ٦.

---

## ٢) البيئة الحالية — دليل فعلي

| البند | القيمة | الأثر |
|---|---|---|
| النظام | Debian GNU/Linux 13 (trixie) · kernel 6.1 x86_64 · حاوية | — |
| المعالج | **2 cores** | استيراد ٤ موديلات (٦٠٠ ألف+ رأس) هيبقى بطيء جدًا |
| الذاكرة | **1.9 GiB إجمالي · 1.0 GiB متاح** | 🔴 أقل من الحد الأدنى لـUnity (8 GB موصى به) |
| القرص | 25 GB · **متاح 19 GB** | 4.3 جيجا تنزيل + ~10 جيجا تثبيت ⇒ ممكن بالعافية |
| GPU / DISPLAY | **مفيش `/dev/dri`** · `DISPLAY` فاضي · مفيش Xvfb | 🔴 Play Mode مع رندر + سكرين شوت **مش موثوق** |
| `unity` binary | **غير موجود** | — |
| `/opt` | **فاضي** (كان فيه `/opt/unity/editor`) | المحرر اتمسح |
| حزم dpkg فيها unity | **0** | — |
| Xvfb / dotnet / mono / csc | **ولا واحد** | — |

### 🔎 اكتشاف مهم: Unity كان بيشتغل هنا قبل كده
| دليل | التفصيل |
|---|---|
| `~/.config/unity3d/upm.log` | `Command-line: /opt/unity/editor/Data/Resources/PackageManager/Server/UnityPackageManager` · `project:list-packages → 200` · 2026-11-09 10:30→10:32 |
| `~/.config/unity3d/Unity/licenses/UnityEntitlementLicense.xml` | **Unity Personal** · `IssueDate="2026-11-09T09:02:07Z"` |
| `~/.config/unity3d/Unity/Unity.Licensing.Client.log` | 208 KB نشاط ترخيص حتى 10:32 |
| `~/.config/unity3d/DefaultCompany/THE_LAST_CONTRACT/prefs` | `unity.player_session_count` + `tlc.quality` ⇒ المشروع اشتغل |
| `UNITY_ACTIVATION_TLC.alf` (815 B) | ملف طلب تفعيل قديم — **مش ترخيص ساري** |

⇒ القدرة التقنية موجودة، **اللي اتمسح هو التثبيت (الملفات الثنائية)**، مش الإمكانية.

### سيرفرات Unity متاحة من هنا (فحص رؤوس HTTP فقط — بدون تنزيل أي بايت)
| الرابط | HTTP | الحجم |
|---|---|---|
| `…/f336aca0cab5/Windows64EditorInstaller/UnitySetup64-6000.0.28f1.exe` | **200** | **3,849 MB** |
| `…/f336aca0cab5/LinuxEditorInstaller/Unity.tar.xz` | **200** | **4,302 MB** |
| `license.unity3d.com/manual` · `packages.unity.com` | **200** | — |

---

## ٣) ليه الساندبوكس مش حل كويس حتى لو وافقت على التنزيل

| المشكلة | التفصيل |
|---|---|
| الذاكرة | 1.9 GB · Unity 6 محتاج عمليًا 8 GB. الاستيراد هيضرب swap/OOM أو يعلّق |
| مفيش GPU | `-nographics` بيسمح بالاستيراد، لكن **Play Mode مع رندر مستحيل** ⇒ مفيش سكرين شوت للشخصيات (ده طلبك رقم ٧ الأصلي) |
| المعالج | 2 cores — استيراد ٤ موديلات ثقيلة ممكن ياخد ٢٠–٤٠ دقيقة+ |
| الترخيص | الـentitlement القديم ممكن يشتغل، لكن ترخيص Personal بيرتبط بالـMachineID وممكن يحتاج إعادة تفعيل من متصفحك |
| النتيجة | ممكن نطلّع **JSON تحقق** لكن **مش** دليل بصري ⇒ R1.5 ما تتحققش كاملة |

---

## ٤) اللي **اتحقق منه فعلاً** في هذه المرحلة (بدون Unity)

| فحص | النتيجة |
|---|---|
| وجود المحرر/الترخيص | ❌ مش موجود (دليل فوق) |
| سيرفرات التنزيل + الحجم الحقيقي | ✅ متحقَّق بـHTTP HEAD |
| نسخة المشروع المطلوبة | ✅ `ProjectVersion.txt` = `6000.0.28f1 (f336aca0cab5)` |
| حزم المشروع المطلوبة | ✅ URP 17.0.3 · Cinemachine 3.1.3 · ugui 2.0.0 |
| حجم المشروع للاستنساخ | ✅ `.git` = **121 MB** · أكبر ملف **19.88 MB** (< حد GitHub) |
| مكان جذر المشروع في الريبو | ✅ `the-last-contract/THE_LAST_CONTRACT/` (هو اللي فيه `Assets` و`ProjectSettings` و`Packages`) |
| سكربت تحقق جاهز | ✅ `tools/TLC_R1_5_Validator.cs` (مكتوب، **مش متنسخ للمشروع** — محتاج موافقتك) |

---

## ٥) اللي لسه **غير متحقق** (وما ينفعش يُعلن نجاحه)

1. صحة الـHumanoid Avatar لكل شخصية (`isValid` / `isHuman`).
2. أخطاء/تحذيرات الاستيراد في الـConsole.
3. ارتباط `Idle` / `Walk` / `Run` في `TLC_Character.controller` — الـfileIDs المصدر:
   - `Walk` ← `-3419257869308726280` (من FBX آدم)
   - `Run` ← `6200578666267062213` (من FBX آدم)
   - `Idle` ← `7400000` من `Animators/idle.anim`
4. ظهور المواد (متوقع **ماجنتا** لأنها Standard في مشروع URP — **R2 مؤجلة بأمرك**).
5. `CharacterLibrary.AvailableCount()` وقت التشغيل + ظهور الشخصيات في Play Mode.

---

## ٦) المطلوب بالظبط على جهاز Windows — خطوة بخطوة

### أ) المتطلبات
| البند | المطلوب |
|---|---|
| النظام | Windows 10/11 64-bit |
| الذاكرة | **16 GB** (8 GB حد أدنى) |
| القرص | **25 GB** فاضي (محرر ~10 GB + Library ~2 GB + المشروع) |
| كارت شاشة | أي GPU بـDX11 (للـPlay Mode والسكرين شوت) |
| إنترنت | مطلوب في أول فتح (تحميل حزم URP/Cinemachine + الترخيص) |
| Git for Windows | مطلوب للاستنساخ من GitHub |

### ب) تثبيت المحرر (نسخة واحدة بالظبط)
1. نزّل **Unity Hub** → https://unity.com/download
2. Hub ▸ Installs ▸ **Install Editor** ▸ **Install with a version** ▸ اكتب: **`6000.0.28f1`**
3. **أو** مباشرة من المتصفح (الرابط متحقَّق منه 200 / 3.85 GB):
   `https://download.unity3d.com/download_unity/f336aca0cab5/Windows64EditorInstaller/UnitySetup64-6000.0.28f1.exe`
4. **Modules:** مش محتاج أي حاجة لـR1.5. (Android Build Support لاحقًا لو هتبني APK.)

> ⚠️ متستخدمش نسخة أحدث (6000.0.3x أو 6.1) — ده بيولّد إعادة استيراد كاملة وتغيير صيغ المكتبة، ومش مطلوب.

### ج) الترخيص
Hub ▸ **Preferences ▸ Licenses ▸ Add ▸ Get a free personal license** ▸ سجّل دخول بحساب Unity.

### د) استنساخ المشروع
```powershell
git clone git@github.com:mezo20043/the-last-contract.git
cd the-last-contract\THE_LAST_CONTRACT
```
> افتح **`THE_LAST_CONTRACT`** في الـHub (المجلد اللي فيه `Assets` و`ProjectSettings` و`Packages`) — **مش** جذر الريبو.

### هـ) أول فتح
1. Package Manager هيحمّل URP 17.0.3 + Cinemachine 3.1.3 (٢–٥ دقايق).
2. استيراد الـ٤ موديلات + ١٦ تكستشرة (٥–١٥ دقيقة على جهاز متوسط).
3. **متفتحش مشهد قبل ما الاستيراد يخلص** (شريط التقدم تحت).

### و) سكربت التحقق (خطوة اختيارية لكن موصى بها — محتاجة موافقتك)
```
انسخ:  tools/TLC_R1_5_Validator.cs
إلى:   THE_LAST_CONTRACT/Assets/_Project/Editor/
```
> **أنا ما نسختوش** لأنه بيضيف ملف للمشروع، وقاعدة «أي تغيير في ملفات المشروع يحتاج موافقتك» سارية. قول «وافق» وأنا أضيفه في commit منفصل.

بعدها: منيو **TLC ▸ R1.5 Validation** ⇒ يكتب `R1_5_VALIDATION.json` في جذر المشروع.

### ز) الدليل اللي محتاجه منك (٤ حاجات)
| # | الدليل | إزاي |
|---|---|---|
| 1 | `R1_5_VALIDATION.json` | من السكربت، أو Console: كليك يمين ▸ Copy All |
| 2 | صورة Console مفلترة على `TLC` | Console ▸ بحث: `TLC` |
| 3 | صورة Avatar inspector لكل شخصية | اختار الـFBX ▸ Rig ▸ لاحظ `Avatar is valid` + عدد العظام |
| 4 | سكرين شوت Play Mode للشخصيات الأربعة | شغّل المشهد واضغط Play، صوّر |

### ح) معايير القبول لـR1.5 (اللي هحكم بيه)
| # | المعيار | المتوقع |
|---|---|---|
| 1 | `avatarIsValid` + `avatarIsHuman` للأربعة | `true` / `true` |
| 2 | عظام مريوطة | 23 · 52 · 22 · 52 |
| 3 | كليبس داخل كل FBX | `walk` 1.000ث · `run` 0.625ث · `isHumanMotion = true` |
| 4 | `missingMotions` في الـcontroller | **0** |
| 5 | أخطاء استيراد في الـConsole | **0** |
| 6 | `CharacterLibrary.AvailableCount()` | **4** |
| 7 | المواد | هنشوف `Standard` ⇒ تحذير متوقع (مش فشل لـR1.5) |

---

## ٧) قرارك المطلوب

| الخيار | التفصيل | الحكم |
|---|---|---|
| **أ) أنا أثبّت Unity هنا** | تنزيل 4.3 GB · استيراد batchmode · مخرجات: JSON تحقق فقط · **مفيش سكرين شوت** · خطر OOM | ممكن تنفيذها **بعد موافقتك الصريحة** — بس مش بتاعتك كاملة |
| **ب) تنفّذها على Windows** ⭐ | دليل كامل: JSON + Console + صور Avatar + سكرين شوت Play Mode | **الموصى به** — بيدي R1.5 كاملة |
| ج) نأجل R1.5 | نكمل حاجات تانية | متاح |

**أنا واقف هنا ومش هعمل حاجة من التلاتة غير لما تقول.**

---

*مفيش تعديل على أي ملف في المشروع · مفيش تنزيل · مفيش تثبيت · R2/R3 ما اتلمستش.*
