# تشغيل اختبارات قبول 4.4 — الحفظ والقوائم وما بعد النهاية

اختبارات 4.4 بتشتغل على **عمليتين Unity منفصلتين** بالترتيب. ده مقصود مش عيب:
العملية التانية = إعادة تشغيل التطبيق فعلياً، فالحفظ لازم يكون على القرص بس.

## ١) متطلبات قبل التشغيل

```bash
# نسخة العمل (اللي بينفّذ فيها) — منفصلة عن الريبو
ls ~/.cache/work/tlc/Assets/_Project
ls ~/.cache/unity/Editor/Unity          # Unity 6000.0.28f1
```

- Xvfb شغّال على `:99` (المشروع بيرندر بلقطات شاشة، فما ينفعش `-batchmode`).
- مساحة حرة ≥ 3G في `~/.cache` (اللوجات كبيرة).
- **مفيش أي Unity شغّال على نفس المشروع** — امسح `Temp/UnityLockfile` لو موجود.

## ٢) الجلسة الأولى: لعب فعلي + حفظ

```bash
cp -a /home/user/THE_LAST_CONTRACT/Assets/. ~/.cache/work/tlc/Assets/
cp /home/user/THE_LAST_CONTRACT/Packages/manifest.json ~/.cache/work/tlc/Packages/manifest.json
cd ~/.cache/work/tlc && rm -f Temp/UnityLockfile

env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=/home/user/.cache/tmp \
 ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc \
 -executeMethod TLC.EditorTools.Phase44Tests.Run \
 -captureDir /home/user/.cache/work/tlc/Screenshots/phase4_4 -t44mode save \
 -logFile /home/user/.cache/tmp/t44_save.log \
 -force-glcore -screen-width 1600 -screen-height 1000
```

بتعمل: تصفير الخانات → لعبة جديدة → لعب لحد القرار الأول → لعب آلي ٤ قرارات
→ حفظ يدوي في A → حفظ تلقائي بعد القرار → حفظ يدوي في B من قائمة الإيقاف
→ لقطات القوائم → **كتابة `restore_expected.json`**.

## ٣) الجلسة الثانية: إعادة تشغيل + استعادة

```bash
env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=/home/user/.cache/tmp \
 ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc \
 -executeMethod TLC.EditorTools.Phase44Tests.RunRestore \
 -captureDir /home/user/.cache/work/tlc/Screenshots/phase4_4 -t44mode restore \
 -logFile /home/user/.cache/tmp/t44_restore.log \
 -force-glcore -screen-width 1600 -screen-height 1000
```

بتعمل: Continue من الحفظ الأحدث → مقارنة العقدة/الفصل/الأعلام/القرارات/العلاقات/وقت اللعب
→ خانة فاضية → ملف تالف → ترقية v1 → تأكيد لعبة جديدة → ثبات الإعدادات → منع الحفظ
أثناء الكروت → ما بعد النهاية A/B/C + التيزر بعد كل واحدة → نهاية غير مدعومة → لقطات الإضافات.

**الترتيب إلزامي**: الجلسة ١ بتكتب الحالة المتوقعة؛ تشغيل الجلسة ٢ لوحدها بعد جلسة ٢ سابقة
هيقارن مع حالة قديمة (الحفظ التلقائي بيتغيّر في جلسة ٢ بسبب النهايات).

## ٤) قراءة النتائج

```bash
tr -d '\000' < ~/.cache/tmp/t44_save.log    | grep -aE "TLC/T44|EXIT=" | tail -60
tr -d '\000' < ~/.cache/tmp/t44_restore.log | grep -aE "TLC/T44|EXIT=" | tail -80
```

- `EXIT=0` = كل الفحوص نجحت. `EXIT=1` = فيه FAIL (التفاصيل في الملخّص).
- `EXIT=4` = الجلسة وقعت في انتظار (شوف `state=` في اللوج).
- التقرير النصي الكامل: `Screenshots/phase4_4/phase44_report.txt`.
- اللقطات: `Screenshots/phase4_4/*.png` (قوائم/خانات/إيقاف/إضافات/ما-بعد-النهاية/تيزر).

## ٥) ملفات الحفظ على القرص

```
~/.config/unity3d/DefaultCompany/THE_LAST_CONTRACT/saves/
  slot_0.json        الحفظ التلقائي
  slot_1..3.json     الخانات اليدوية A/B/C
  slot_N.json.bak    النسخة الاحتياطية (بتُقرأ لو الأساسي تلف)
  slot_N.png         صورة مصغّرة للخانة
  slot_N.corrupt-*   ملفات اتعرّفت كتالفة واتعزلت بدل ما تتمسح
```

## ٦) تنظيف

```bash
rm -f ~/.cache/work/tlc/Temp/UnityLockfile
rm -rf ~/.config/unity3d/DefaultCompany/THE_LAST_CONTRACT/saves   # تصفير الخانات
```
