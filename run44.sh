#!/bin/bash
# usage: ./run44.sh save|restore
set -u
MODE="${1:-save}"
cp -a /home/user/THE_LAST_CONTRACT/Assets/. ~/.cache/work/tlc/Assets/
cp /home/user/THE_LAST_CONTRACT/Packages/manifest.json ~/.cache/work/tlc/Packages/manifest.json
cd ~/.cache/work/tlc && rm -f Temp/UnityLockfile
mkdir -p /home/user/.cache/tmp
if [ "$MODE" = "save" ]; then METHOD=TLC.EditorTools.Phase44Tests.Run; else METHOD=TLC.EditorTools.Phase44Tests.RunRestore; fi
env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=/home/user/.cache/tmp \
 ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc -executeMethod $METHOD \
 -captureDir /home/user/.cache/work/tlc/Screenshots/phase4_4 -t44mode "$MODE" \
 -logFile /home/user/.cache/tmp/t44_$MODE.log -force-glcore -screen-width 1600 -screen-height 1000
echo "UNITY_DONE"
tr -d '\000' < /home/user/.cache/tmp/t44_$MODE.log | grep -aE "TLC/T44|EXIT=|error CS" | tail -80
