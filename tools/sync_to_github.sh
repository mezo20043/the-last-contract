#!/usr/bin/env bash
# مزامنة مساحة العمل → GitHub (بدون force · بدون أسرار)
# الاستخدام: ./tools/sync_to_github.sh "رسالة الـcommit"
set -euo pipefail

MSG="${1:-}"
[ -z "$MSG" ] && { echo "✗ لازم رسالة commit: $0 \"رسالة\""; exit 1; }

WS=/home/user
REPO=/var/tmp/tlc_sync
URL=git@github.com:mezo20043/the-last-contract.git
export GIT_SSH_COMMAND="ssh -o BatchMode=yes"

# ١) نسخة شغّالة من GitHub (بتحافظ على التاريخ كله)
if [ ! -d "$REPO/.git" ]; then
  rm -rf "$REPO"; git clone -q "$URL" "$REPO"
else
  git -C "$REPO" fetch -q origin master && git -C "$REPO" reset -q --hard origin/master
fi

# ٢) مزامنة (إضافة/تحديث فقط — بدون --delete عشان مفيش حذف تلقائي)
rsync -a --checksum \
  --exclude '.cache/' --exclude '.local/' --exclude '.config/' --exclude '.git/' \
  --exclude '.ssh/' --exclude '.nuget/' --exclude '.bashrc' --exclude '.profile' \
  --exclude '.sudo_as_admin_successful' --exclude '.python_history' --exclude '.*_history' \
  --exclude '*.zip' --exclude '*.bundle' --exclude '*.alf' --exclude 'lic/' \
  --exclude 'login_*.log' --exclude 'unity_login_out.log' \
  --exclude 'last_url.txt' --exclude 'url_now.txt' \
  --exclude 'bin/' --exclude 'obj/' --exclude 'Library/' --exclude 'Temp/' \
  "$WS"/ "$REPO"/

cd "$REPO"

# هوية الـcommit (محلية للنُسخ الآلية فقط)
git config user.name  >/dev/null 2>&1 || git config user.name  "TLC Recovery"
git config user.email >/dev/null 2>&1 || git config user.email "recovery@tlc.local"

# ٣) فحص أسرار قبل أي commit
BAD=$(git grep -In -E "ghp_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA|OPENSSH|EC) PRIVATE KEY-----" -- . 2>/dev/null | head -5 || true)
[ -n "$BAD" ] && { echo "✗ اتلغى: لقيت نمط سر:"; echo "$BAD"; exit 2; }
if git ls-files -o --exclude-standard | grep -Ei '\.alf$|^lic/|login_|_credentials|\.netrc' ; then
  echo "✗ اتلغى: فيه ملف حساس هيترفع"; exit 3
fi

# ٤) commit
if git diff --quiet && [ -z "$(git ls-files -o --exclude-standard)" ]; then
  echo "ℹ️ مفيش تغييرات — مفيش commit"
else
  git add -A
  git commit -q -m "$MSG"
fi

# ٥) push بدون force
git push origin master 2>&1 | tail -3

# ٦) تحقق
LOCAL=$(git rev-parse HEAD)
REMOTE=$(git ls-remote origin refs/heads/master | cut -f1)
echo "الـcommit المحلي: $LOCAL"
echo "على GitHub       : $REMOTE"
[ "$LOCAL" = "$REMOTE" ] && echo "✅ الرفع متحقق" || { echo "✗ الرفع مش متحقق"; exit 4; }
