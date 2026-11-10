# ٢) البحث عن نسخة `0a7490f` — النتيجة والتوثيق

## ١) الأماكن اللي اتفحصت (بدون أي حذف أو تعديل)

| المكان | إيه اللي اتفحص | النتيجة |
|---|---|---|
| `/home/user/` | الملفات والفولدرات (`.zip` / md / png) | فيه `THE_LAST_CONTRACT_v2.0.zip` + شجرة `THE_LAST_CONTRACT/` |
| `/home/user/THE_LAST_CONTRACT/` | `.git` · `Assets/_Project/**` · `Screenshots/**` | **مفيش `.git`** · الشجرة = 4.2 + تقارير 4.3 · مفيش `Screenshots/phase4_3` |
| `/home/user/THE_LAST_CONTRACT_v2.0.zip` | فهرسة كامل (452 ملف) | نفس شجرة 4.2 (`GameBootstrap.cs` 46KB · `SaveSystem.cs` 3.5KB · مفيش `SaveManager` ولا `Phase43Tests`) |
| `find / -xdev -name .git` | كل الأقراص | **صفر نتيجة** |
| `find / -xdev -iname "*.zip|*.tar|*.tar.gz|*.tgz|*.7z"` (>1MB) | | `/usr/lib/jvm/jdk-11/lib/src.zip` (JDK) + `THE_LAST_CONTRACT_v2.0.zip` بس |
| `find / -xdev -name SaveManager.cs|Phase44Tests.cs|Phase43Tests.cs|CardPlayer.cs|SaveController.cs|PostCreditSequence.cs` | | **صفر نتيجة** |
| `find / -xdev -type d -name "phase4_3"` · `-name "phase43_report.txt"` · `phase44_report.txt` | | **صفر نتيجة** |
| `find / -xdev -type d -name 6000.0.28f1` · `-type f -name Unity -size +10M` | محرّر Unity | **مفيش محرّر Unity** على الجهاز |
| `~/.config/unityhub/` | إعدادات Hub | موجودة (إعدادات بس) — مفيش مسار تثبيت لمحرّر |
| `~/.config/unity3d/DefaultCompany/THE_LAST_CONTRACT/` | حفظ اللعبة من جولات 4.4 | **مفيش `saves/`** — بس `prefs` |
| `~/.cache/` | محرّر Unity + مشروع العمل `work/tlc` | **فاضي بالكامل** (`.cache` مش بيتحفظ في اللقطات) |

## ٢) الخلاصة

**مش ممكن استعادة `0a7490f` من أي مصدر محلي على الجهاز ده.** اللي نُجا هو:

1. شجرة **4.2** في `/home/user/THE_LAST_CONTRACT/` (بدون git، بدون 4.3 r2 ولا 4.4).
2. نفس الشجرة كأرشيف: `THE_LAST_CONTRACT_v2.0.zip` (بتاريخ 2026-11-09، 452 ملف).
3. **٥ لقطات** من جولة 4.4 نجت في جذر `/home/user` واتنقلت لـ`EVIDENCE/`.
4. تقارير 4.3 (r1) النصية جوه الشجرة، و**`RECOVERY_STATUS.md`** اللي بيوثّق إن نفس الحادثة
   حصلت قبل كده (تصفير بيئة) — دليل إن النمط متكرر وإن `/home/user` بيفضل لكن بأقدم نسخة.

> ملاحظة مهمة: `RECOVERY_STATUS.md` مكتوب فيه إن تصفير سابق خلّى شجرة 4.2 بس، وإن 4.3 r2 اتبنى
> بعد كده على `0a7490f`. الشجرة الحالية = **قبل** إعادة البناء دي. يعني الحالة اتأخرت أكتر من 4.3 r2.

## ٣) اللي محتاجينه للاستعادة (مصادر خارجية)

واحد من دول يكفي:

1. **ريبو git فيه** commit `0a7490f` وتاج `phase4-3-final-r2` (مثلاً مرفوع على GitHub/أرشيف).
2. **أرشيف** للشجرة عند `0a7490f` (zip/tar) — أي نسخة.
3. **أرشيف لنسخة 4.4 نفسها** لو اتصدّرت قبل التصفير (الأفضل: يوفّر إعادة التطبيق كلها).

وبعد رجوع الشجرة، بيئة التشغيل المطلوبة (مش موجودة الآن):

```
Unity 6000.0.28f1  → ~/.cache/unity/Editor/Unity (أو /opt/unity/editor)
ترخيص Unity        → ~/.local/share/unity3d/Unity_lic.ulf
مشروع عمل          → ~/.cache/work/tlc (نسخة من Assets + ProjectSettings + Packages)
Xvfb               → :99  (ممنوع -batchmode مع اللقطات)
مساحة حرة          → ≥ 8G
```

أمر التحقق من نقطة البداية بعد الرجوع:

```bash
cd THE_LAST_CONTRACT
git log --oneline -1          # لازم يبان 0a7490f
git tag | grep phase4-3-final-r2
git status --short            # لازم يكون نضيف
wc -l Assets/_Project/Scripts/Core/SaveManager.cs   # ٥٢٢ سطر (لو 4.4 رجعت مع اللقطة)
```
