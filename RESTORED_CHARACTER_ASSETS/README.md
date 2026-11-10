# موديلات الشخصيات المستخرجة من أرشيف v2.0

**تاريخ الاستخراج:** 2026-11-09 · **المصدر:** `THE_LAST_CONTRACT_v2.0.zip` (98.2 ميجا · سليم: `unzip -t` = No errors)
**المكان:** مجلد مستقل جديد — **مفيش أي ملف من `THE_LAST_CONTRACT/` أو `4.4_RECOVERY_PACK/` اتعدّل أو اتحذف.**

---

## ⚠️ تنبيه معنى الاستخراج

> **استخراج الموديلات ده مش استعادة لـ4.3 r2 ولا 4.4.**
> ده **ملفات أصول (Assets)** كانت مستبعدة من شجرة العمل لتوفير المساحة، ورجعت من الأرشيف.
> **صفر** سطر كود 4.3 r2 أو 4.4 في هذه العملية: لا `AnimationClipBuilder` · لا `FacialExpressionSystem` ·
> لا `Phase43Tests` · لا `SaveManager` · لا `.git`. الحالة الفعلية للكود تفضل زي ما هي: **4.2**.

---

## الموديلات الأربعة

| الشخصية | المجلد | الملف | الحجم (بايت) | SHA-256 |
|---|---|---|---|---|
| **Adam** | `adam/` | `man-base-character-rigged.fbx` | 17,030,620 | `824cb4f22cdf021eb35745240c3fc6c384ec8d2e4ee1322803eec57cad4ca3bd` |
| **Laila** | `laila/` | `woman-rigged.fbx` | 19,726,700 | `d2bf5ed31f51b8a86630e91f7b55519c9a69be1cc9f6a714cc98174a7762f20c` |
| **Murad** | `murad/` | `elderly-man-rigged.fbx` | 20,845,452 | `a7421e7f1771f12b859dd77899150f711b76e69ef79855b40a47aea24143b9bb` |
| **Yassin** | `yassin/` | `man-rigged.fbx` | 19,466,764 | `7d262069c36d4181809ccd96128b4be2a7e15bcf2d0755148e179257d8be3e26` |

**إجمالي الموديلات:** 77,069,536 بايت (73.50 ميجا) · **إجمالي المستخرج:** ٥٦ ملف · 76 ميجا (موديلات + تكستشرات + `.meta` + تراخيص).

## نتيجة التحقق

| الفحص | النتيجة |
|---|---|
| تطابق كل ملف مع محتواه في الأرشيف (`SHA-256` + الحجم) | **٥٦/٥٦ ✅** |
| ترويسة FBX | `Kaydara FBX Binary \x00` ✔ في الأربعة |
| إصدار FBX | **7400** (FBX 2014/2015) ✔ |
| ذيل الملف (footer mag) | `f85a8c6adef5d97e` — التوقيع القياسي ✔ (ملف مش مقطوع) |
| تطابق `guid` في `.fbx.meta` مع الشجرة الحالية | **٤/٤ MATCH** ⇒ النسخ للمشروع مستقبلاً هيربط بنفس الـGUID بدون كسر المراجع |

## بنية المجلد (مطابقة للأصل بالحرف)

```
RESTORED_CHARACTER_ASSETS/
└── THE_LAST_CONTRACT/Assets/_Project/Resources/Characters/
    ├── adam/   man-base-character-rigged.fbx  + .fbm/ (4 تكستشرات) + LICENSE.txt + .meta
    ├── laila/  woman-rigged.fbx               + .fbm/ (4 تكستشرات) + LICENSE.txt + .meta
    ├── murad/  elderly-man-rigged.fbx         + .fbm/ (4 تكستشرات) + LICENSE.txt + .meta
    └── yassin/ man-rigged.fbx                 + .fbm/ (4 تكستشرات) + LICENSE.txt + .meta
```

## لو حبيت ترجّعهم للمشروع (مش اتعمل ومش هيتعمل من غير أمرك)

```bash
# الأداة الأصلية في المشروع بتعمل نفس الحاجة من الأرشيف مباشرة:
cd /home/user/THE_LAST_CONTRACT && ./tools/restore_characters.sh

# أو يدويًا من المجلد ده (نسخ بس، من غير حذف):
cp -rn /home/user/RESTORED_CHARACTER_ASSETS/THE_LAST_CONTRACT/Assets/_Project/Resources/Characters/. \
       /home/user/THE_LAST_CONTRACT/Assets/_Project/Resources/Characters/
```

## ملاحظة عن حجم المساحة

المجلد ده **76 ميجا**، والمساحة الحالية بقت **~251 ميجا**. لقطات مساحة العمل بتُختصر عند حدود معينة،
فالمجلدات الكبيرة ممكن ماتدخلش في اللقطة. **الأضمن:** نزّل المجلد على جهازك من الـviewer.
