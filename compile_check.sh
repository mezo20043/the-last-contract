#!/bin/bash
set -u
cp -a /home/user/THE_LAST_CONTRACT/Assets/. ~/.cache/work/tlc/Assets/
cp /home/user/THE_LAST_CONTRACT/Packages/manifest.json ~/.cache/work/tlc/Packages/manifest.json
cd ~/.cache/work/tlc && rm -f Temp/UnityLockfile
mkdir -p /home/user/.cache/tmp
env DISPLAY=:99 LIBGL_ALWAYS_SOFTWARE=1 GALLIUM_DRIVER=llvmpipe TMPDIR=/home/user/.cache/tmp \
 ~/.cache/unity/Editor/Unity -projectPath ~/.cache/work/tlc -batchmode -quit \
 -logFile /home/user/.cache/tmp/compile.log -force-glcore 2>&1 | tail -5
tr -d '\000' < /home/user/.cache/tmp/compile.log | grep -aE "error CS|Compilation failed|Scripts have compiler errors" | head -40
echo "---done---"
