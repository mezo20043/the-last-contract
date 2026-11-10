# R1 — استرجاع ملفات FBX إلى المشروع (تنفيذ)

**التاريخ:** 2026-11-09 · **النطاق:** R1 فقط · **مشروع:** Unity 6000.0.28f1

---

## ١) الملفات المضافة (٤ ملفات — مفيش غيره)

| # | المسار داخل المشروع | الحجم (بايت) | SHA-256 |
|---|---|---|---|
| 1 | `Assets/_Project/Resources/Characters/adam/man-base-character-rigged.fbx` | 17,030,620 | `824cb4f22cdf021e…` |
| 2 | `Assets/_Project/Resources/Characters/laila/woman-rigged.fbx` | 19,726,700 | `d2bf5ed31f51b8a8…` |
| 3 | `Assets/_Project/Resources/Characters/murad/elderly-man-rigged.fbx` | 20,845,452 | `a7421e7f1771f12b…` |
| 4 | `Assets/_Project/Resources/Characters/yassin/man-rigged.fbx` | 19,466,764 | `7d262069c36d4181…` |

**المصدر:** `RESTORED_CHARACTER_ASSETS/THE_LAST_CONTRACT/Assets/_Project/Resources/Characters/<نفس المسار>`
**الأوامر:** `cp -p` (بيحافظ على الـtimestamps) — ٤ أوامر فقط.

### إثبات عدم المساس بأي ملف تاني
| فحص | النتيجة |
|---|---|
| ملفات مضافة | **٤** (المذكورة فوق) |
| ملفات محذوفة | **٠** |
| ملفات تغيّر محتواها | **٠** |
| ملفات تغيّر `mtime` بتاعها | **٠** (من أصل ٥٢ ملف مسجّل قبل التنفيذ) |
| صور `.fbm` · Materials · Animator Controller | **ما اتلمستش** |
| Animations جديدة | **٠** |

---

## ٢) تطابق البصمات

| مقارنة | النتيجة |
|---|---|
| المشروع ↔ `RESTORED_CHARACTER_ASSETS` | **٤/٤ مطابق** SHA-256 |
| المشروع ↔ `THE_LAST_CONTRACT_v2.0.zip` (الأصل) | **٤/٤ مطابق** SHA-256 |
| الحجم على القرص = الحجم في الـcommit السابق (git) | مطابق |

---

## ٣) GUIDs — قبل / بعد

ملفات `.meta` **ما اتلمستش خالص** (٠ تعديل، ٠ تغيير `mtime`)، فالقيم هي هي:

| الملف | GUID قبل | GUID بعد | الحالة |
|---|---|---|---|
| `adam/man-base-character-rigged.fbx.meta` | `c868b684beda268a2a60b2fb94afc406` | `c868b684beda268a2a60b2fb94afc406` | ✅ مطابق |
| `laila/woman-rigged.fbx.meta` | `54b969dec9d11c51e89fc2946fa08c98` | `54b969dec9d11c51e89fc2946fa08c98` | ✅ مطابق |
| `murad/elderly-man-rigged.fbx.meta` | `65975604b5059155c99e00b6262839b5` | `65975604b5059155c99e00b6262839b5` | ✅ مطابق |
| `yassin/man-rigged.fbx.meta` | `bbecba5eed6c76e31a5fc0dcbd33fa56` | `bbecba5eed6c76e31a5fc0dcbd33fa56` | ✅ مطابق |

خريطة الـGUID لمجلد الشخصيات كله: **٣٢ ملف meta** · بصمة الخريطة `c5ee3c123eb9d68a69006436872daeaf` — **قبل = بعد**.

> ملاحظة: بما إن الـGUID هو نفسه، ملف `TLC_Character.controller` اللي بيشاور على آدم (`c868b68…`) **مش محتاج أي تعديل**.

---

## ٤) هل `CharacterLibrary.LoadModel()` بقى بيلاقي الموديلات؟ — **نعم (تحقق ثابت)**

محاكاة `Resources.Load<GameObject>("Characters/{key}/{modelFile}")` بالقواعد الحقيقية لـUnity (مسار نسبي تحت أي مجلد اسمه `Resources`، بدون الامتداد):

| الـRoster (من الكود) | المسار اللي بيتحسب | النتيجة |
|---|---|---|
| `Adam` → `adam/man-base-character-rigged` | `Assets/_Project/Resources/Characters/adam/man-base-character-rigged.fbx` | ✅ **موجود** (16 MB) |
| `Murad` → `murad/elderly-man-rigged` | `…/murad/elderly-man-rigged.fbx` | ✅ **موجود** (19 MB) |
| `Laila` → `laila/woman-rigged` | `…/laila/woman-rigged.fbx` | ✅ **موجود** (18 MB) |
| `Yassin` → `yassin/man-rigged` | `…/yassin/man-rigged.fbx` | ✅ **موجود** (18 MB) |
| `Yusuf` → `murad/elderly-man-rigged` (موثّق في الكود) | `…/murad/elderly-man-rigged.fbx` | ✅ **موجود** |

⇒ `AvailableCount()` المتوقع = **٤ / ٤** (كان ٠ / ٤) · المسار البديل (المكعبات) مش هيتفعّل.

**سبب كفاية الفحص الثابت:** `Resources.Load` بتفشل في حالتين بس — (أ) الأصل مش موجود في مجلد `Resources`، (ب) الاسم/المسار غلط. الحالتين اتحققوا مقابل الملفات الفعلية على القرص ومقابل نصوص `Entry.key` / `Entry.modelFile` المستخرجة من `CharacterLibrary.cs`.

---

## ٥) فحص "Missing References" — تحقق ثابت

| فحص | النتيجة |
|---|---|
| كل عظمة مكتوبة في `.meta` موجودة فعليًا في الـFBX؟ | **adam 23/23 · laila 52/52 · murad 22/22 · yassin 52/52 — ٠ مفقود ✔** |
| الـTakes اللي الـcontroller بيعتمد عليها موجودة؟ | `walk` ✔ · `run` ✔ في الأربعة |
| ملفات `.fbm` والصور (اللي الموديلات بتشاور عليها) | موجودة ✔ ١٦/١٦ |
| `.meta` ناقصة للـFBX؟ | لا — الأربعة عندهم meta ✔ |
| مراجع لـGUIDs الشخصيات في المشروع | `TLC_Character.controller` → آدم فقط (GUID مطابق ✔) · التلاتة التانية ٠ مراجع |

---

## ٦) Unity Import Logs + Play Mode — **لم يُنفَّذ** (البيئة غير متاحة)

فحص بيئة التشغيل (دليل فعلي، مش افتراض):

```
unity binary      : غير موجود
Unity Hub / /opt  : غير موجود
~/.local/share/unity3d : غير موجود
license .ulf      : 0
Xvfb              : غير موجود
dotnet / mono     : غير موجود
```

⇒ **مستحيل** يشتغل Unity Editor أو Play Mode أو يتولّد Import Log أو سكرين شوت من الساندبوكس.
**مفيش أي دليل بصري أو لوج استيراد — ومش هدّعي بوجوده.**

### اللي **اتحقق منه فعلًا** (دليل قابل للتكرار)
- الملفات موجودة بمقاسها وبصمتها الصح في المكان الصح.
- الـGUIDs مطابقة ⇒ أي مرجع حالي هيحل لنفس الأصل.
- عظام الـHumanoid كاملة ومطابقة للـFBX ⇒ توليد الـAvatar هيلاقي كل bone.
- الـTakes (`walk`/`run`) موجودة داخل الملف.

### اللي **لسه محتاج Unity** (غير متحقق — لا يُحسب نجاحًا)
1. توليد الـAvatar فعليًا وصحة الـHumanoid Valid/Invalid.
2. ارتباط كليبسي `Walk`/`Run` في `TLC_Character.controller` (fileID داخلي).
3. ظهور المواد (متوقع ماجنتا — R2 لم تُنفّذ بأمرك).
4. أي أخطاء استيراد أو تحذيرات في الـConsole.

---

## ٧) الحكم

| البند | الحالة |
|---|---|
| R1 (نسخ ٤ FBX لمساراتهم) | ✅ **منفَّذ ومتحقَّق** |
| الحفاظ على `.meta` والـGUID | ✅ **منفَّذ ومتحقَّق** |
| `CharacterLibrary.LoadModel()` تلاقي الموديلات | ✅ **متحقَّق بطريقة ثابتة** (تنفيذ فعلي مستحيل بلا Unity) |
| Import Logs | ❌ **غير منفَّذ** (مفيش Unity) |
| Play Mode + سكرين شوت | ❌ **غير منفَّذ** (مفيش Unity ولا ترخيص ولا Xvfb) |
| R2 / R3 أو أي توصية تانية | ⛔ **لم تُنفّذ** (ممنوعة بأمرك) |

---

*ملاحظة عن حجم الريبو: ملفات الـFBX بقت موجودة مرتين في الـgit (تحت `THE_LAST_CONTRACT/` وتحتف `RESTORED_CHARACTER_ASSETS/`) ⇒ ~+77 ميجا. نسخة الاستعادة مش هتتشال من غير موافقة صريحة منك.*
