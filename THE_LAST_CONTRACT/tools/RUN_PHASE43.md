# تشغيل اختبارات PHASE 4.3 (وصفة الاستعادة على بيئة نظيفة)

## المتطلبات
- Unity **6000.0.28f1** (Linux Editor) + ترخيص مفعّل على الجهاز نفسه.
  **ملفات الترخيص ممنوعة في Git/Tقارير** — مكانها `~/.config/unity3d/Unity/licenses/`.
- `Xvfb :99` شغّال (مفيش `-batchmode` مع اللقطات).
- `TMPDIR` لازم يكون على ديسك (مش `/tmp` tmpfs): `export TMPDIR=~/.cache/tmp`.

## ١) حزمة الأنيميشن + الوضعيات + اللقطات + الكاميرا (≈١٤ دقيقة)
```bash
cp -a Assets/. ~/.cache/work/tlc/Assets/      # مزامنة نسخة العمل
cd ~/.cache/work/tlc && rm -rf Screenshots/phase4_3 && rm -f Temp/UnityLockfile
env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=$HOME/.cache/tmp \
  ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc \
  -executeMethod TLC.EditorTools.Phase43Tests.Run \
  -captureDir $HOME/.cache/work/tlc/Screenshots/phase4_3 \
  -logFile $HOME/.cache/tmp/t.log -force-glcore -screen-width 1600 -screen-height 1000
tr -d '\000' < $HOME/.cache/tmp/t.log | grep -aE "الملخّص|EXIT=|\[FAIL\]"
```
المتوقع: `ناجح=144 · فاشل=0` و`EXIT=0` + **٣٢ لقطة** + `phase43_report.txt`.

## ٢) حزمة الصوت (≈٧ دقايق)
```bash
cd ~/.cache/work/tlc && rm -f Temp/UnityLockfile
env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=$HOME/.cache/tmp \
  ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc \
  -executeMethod TLC.EditorTools.Phase43AudioTests.Run \
  -captureDir $HOME/.cache/work/tlc/Screenshots/phase4_3 \
  -logFile $HOME/.cache/tmp/a.log -force-glcore -screen-width 1600 -screen-height 1000
tr -d '\000' < $HOME/.cache/tmp/a.log | grep -aE "ملخّص الصوت|EXIT=|\[FAIL\]"
```
المتوقع: `ناجح=44 · فاشل=0` و`EXIT=0` + `phase43_audio_report.txt`.

## ملاحظات تشغيل حقيقية اتعلّمناها بالعافية
- شغّل **الأنيميشن قبل الصوت** (الاتنين بيكتبوا في نفس مجلد اللقطات).
- قبل كل تشغيل: `rm -f Temp/UnityLockfile` واقتل أي Unity قديم على نفس المشروع،
  وإلا هتلاقي عمليات متداخلة ونتايج غريبة.
- سجلات Unity ضخمة (`~80MB`) — نظّفها بعد كل جولة.

## توليد بيبات الحوار (اختياري)
```bash
python3 tools/gen_voice_beeps.py    # 194 ملف في Assets/_Project/Resources/Audio/Voice
```
**تنبيه:** دي نغمات مولّدة آليًا (beep)، **مش تعليق صوتي مسجّل ولا أداء ممثل**.
