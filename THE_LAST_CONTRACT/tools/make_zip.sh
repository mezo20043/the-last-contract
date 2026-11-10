#!/usr/bin/env bash
# يبني ZIP التسليم. لازم الـ FBX تكون موجودة في الشجرة قبل التشغيل
# (شغّل tools/restore_characters.sh من داخل الـ ZIP القديم لو ناقصة).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${1:-$(dirname "$ROOT")/THE_LAST_CONTRACT_v2.0.zip}"

n=$(find "$ROOT/Assets/_Project/Resources/Characters" -name '*.fbx' 2>/dev/null | wc -l)
if [ "$n" -lt 4 ]; then
  echo "✗ الـ FBX ناقصة ($n/4) — الأرشيف هيطلع بدون موديلات. أوقفت."
  exit 1
fi

name="$(basename "$ROOT")"
cd "$(dirname "$ROOT")"
rm -f "$OUT"
zip -rq -9 "$OUT" "$name" \
  -x '*/bin/*' '*/obj/*' '*/.git/*' 'Library/*' 'Temp/*' 'Logs/*' 'Builds/*' \
     '*.DS_Store' '*.user' '*.pidb' '*.booproj' '*.svd' '*.userprefs'

echo "✓ $OUT"
echo "  ملفات: $(unzip -l "$OUT" | tail -1 | awk '{print $2}')"
echo "  حجم:   $(du -h "$OUT" | cut -f1)"
echo "  FBX:   $(unzip -l "$OUT" | grep -c '\.fbx$')"
