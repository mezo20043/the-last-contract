#!/usr/bin/env bash
# يستعيد موديلات الشخصيات (FBX) من أرشيف التسليم إلى شجرة المشروع.
# شجرة العمل بتستبعد الـ FBX لتوفير المساحة — النسخة الكاملة جوه الـ ZIP.
set -e
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(dirname "$HERE")"
ZIP="$(ls -1 "$ROOT"/../THE_LAST_CONTRACT_v*.zip 2>/dev/null | tail -1)"
[ -z "$ZIP" ] && { echo "مفيش أرشيف في $ROOT/.."; exit 1; }

TMP=$(mktemp -d)
unzip -q -o "$ZIP" 'THE_LAST_CONTRACT/Assets/_Project/Resources/Characters/*' -d "$TMP"
cp -r "$TMP"/THE_LAST_CONTRACT/Assets/_Project/Resources/Characters/. "$ROOT/Assets/_Project/Resources/Characters/"
rm -rf "$TMP"
echo "✓ اتعادت موديلات الشخصيات من: $(basename "$ZIP")"
ls -1 "$ROOT"/Assets/_Project/Resources/Characters/*/*.fbx | wc -l | xargs echo "موديلات:"
