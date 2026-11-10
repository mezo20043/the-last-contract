#!/usr/bin/env bash
# يشغّل Unity 6 على المشروع من غير واجهة — جوه البيئة دي أو على أي جهاز لينكس.
#
# الاستخدام:
#   ./tools/unity_here.sh license  <ملف.ulf>   # تفعيل الترخيص (مرة واحدة)
#   ./tools/unity_here.sh report               # استيراد + تقرير تحقق كامل
#   ./tools/unity_here.sh play                 # دخول Play + لقطات (محتاج شاشة/GPU)
#   ./tools/unity_here.sh apk                  # بناء APK (محتاج Android module)
set -uo pipefail

UNITY="${UNITY:-/opt/unity/editor/Editor/Unity}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/VERIFICATION"
mkdir -p "$OUT"

if [ ! -x "$UNITY" ]; then
  echo "✗ مفيش Unity على المسار $UNITY — حدّد المسار بمتغير UNITY."
  exit 1
fi

cmd="${1:-report}"

case "$cmd" in
  license)
    ulf="${2:-}"
    [ -f "$ulf" ] || { echo "✗ استخدم: $0 license <ملف.ulf>"; exit 1; }
    echo "▶ تفعيل الترخيص من $ulf"
    "$UNITY" -batchmode -nographics -quit -manualLicenseFile "$ulf" \
             -logFile "$OUT/unity-activation.log"
    echo "exit=$? · السجل: $OUT/unity-activation.log"
    ;;

  report)
    echo "▶ استيراد المشروع + تقرير تحقق (headless)"
    "$UNITY" -batchmode -nographics -quit \
             -projectPath "$ROOT" \
             -executeMethod TLC.EditorTools.VerificationRunner.RunEditorVerification \
             -logFile "$OUT/unity-editor.log"
    echo "exit=$? · السجل: $OUT/unity-editor.log"
    ;;

  play)
    echo "▶ دخول Play + لقطات (محتاج DISPLAY أو GPU)"
    "$UNITY" -batchmode -quit \
             -projectPath "$ROOT" \
             -executeMethod TLC.EditorTools.VerificationRunner.RunPlayTest \
             -logFile "$OUT/unity-play.log"
    echo "exit=$? · السجل: $OUT/unity-play.log"
    ;;

  apk)
    echo "▶ بناء APK"
    "$UNITY" -batchmode -nographics -quit \
             -projectPath "$ROOT" \
             -executeMethod TLC.EditorTools.AndroidBuilder.BuildApkCI \
             -logFile "$OUT/unity-apk.log"
    echo "exit=$? · السجل: $OUT/unity-apk.log"
    ;;

  *)
    echo "أوامر متاحة: license <ulf> | report | play | apk"
    exit 1
    ;;
esac
