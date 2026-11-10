# تشغيل اختبارات قبول 4.4 — الحفظ والقوائم وما بعد النهاية

الاختبارات بتشتغل على **عمليتين Unity منفصلتين** بالترتيب. ده مقصود:
العملية التانية = إعادة تشغيل التطبيق فعليًا، فالحفظ لازم يكون على القرص بس.

## ١) تحضير

```bash
cp -a /home/user/THE_LAST_CONTRACT/Assets/. ~/.cache/work/tlc/Assets/
cp /home/user/THE_LAST_CONTRACT/Packages/manifest.json ~/.cache/work/tlc/Packages/manifest.json
cd ~/.cache/work/tlc && rm -f Temp/UnityLockfile
mkdir -p /home/user/.cache/tmp
```

مطلوب: Xvfb على `:99` (ممنوع `-batchmode` مع اللقطات) · مساحة ≥ 3G في `~/.cache`.

## ٢) الجلسة ١: لعب فعلي + حفظ

```bash
env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=/home/user/.cache/tmp \
 ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc \
 -executeMethod TLC.EditorTools.Phase44Tests.Run \
 -captureDir /home/user/.cache/work/tlc/Screenshots/phase4_4 -t44mode save \
 -logFile /home/user/.cache/tmp/t44_save.log \
 -force-glcore -screen-width 1600 -screen-height 1000
```

تعمل: تصفير الخانات → لعبة جديدة → لعب لحد أول قرار → لعب آلي ٤ قرارات → حفظ يدوي A
→ حفظ تلقائي بعد القرار → حفظ من قائمة الإيقاف (B) → لقطات القوائم → كتابة `restore_expected.json`.

## ٣) الجلسة ٢: إعادة تشغيل + استعادة

```bash
env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=/home/user/.cache/tmp \
 ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc \
 -executeMethod TLC.EditorTools.Phase44Tests.RunRestore \
 -captureDir /home/user/.cache/work/tlc/Screenshots/phase4_4 -t44mode restore \
 -logFile /home/user/.cache/tmp/t44_restore.log \
 -force-glcore -screen-width 1600 -screen-height 1000
```

**الترتيب إلزامي**: الجلسة ٢ بتقارن مع `restore_expected.json` اللي كتبته الجلسة ١.

## ٤) القراءة

```bash
tr -d '\000' < ~/.cache/tmp/t44_save.log    | grep -aE "TLC/T44|EXIT=" | tail -60
tr -d '\000' < ~/.cache/tmp/t44_restore.log | grep -aE "TLC/T44|EXIT=" | tail -80
```

- `EXIT=0` = نجاح · `EXIT=1` = فيه FAIL (قائمة الفشل في نهاية اللوج) · `EXIT=4` = الجلسة وقعت في انتظار.
- تقرير نصي كامل: `Screenshots/phase4_4/phase44_report.txt`.

## ٥) ملفات الحفظ على القرص

```
~/.config/unity3d/DefaultCompany/THE_LAST_CONTRACT/saves/
  slot_0.json            تلقائي
  slot_1..3.json         A/B/C
  slot_N.json.bak        احتياطي (بيُقرأ لو الأساسي تلف)
  slot_N.png             صورة مصغّرة
  slot_N.corrupt-<ts>.json   ملف اتعرّف كتالف واتعزل
```

## ٦) قبل إعادة الجولة: إصلاحان لازم يكونوا موجودين وإلا كل الحفظ هيتقفل

1. `CinematicDirector.IsPlaying` لازم يبقى `_isPlaying && _playing != null` + `CleanupAborted()`
   (وإلا بيفضل مرفوع للأبد ⇒ «انتقال سينمائي جاري» على كل حفظ).
2. موارد TMP موجودة (`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`)
   + `Resources/Fonts/Amiri SDF.asset` — من غيرهم كل نصوص الواجهة بتترسم فاضية.
