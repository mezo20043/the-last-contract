#!/usr/bin/env python3
"""
خادم تفعيل Unity — بيخلي التفعيل يتم بمجهودك أنت بس (تسجيل دخول في متصفحك).

إيه اللي بيحصل:
  1) الخادم بيشغّل `unity auth login` (Unity CLI) باستمرار.
  2) كل محاولة بتطلّع **رابط تسجيل دخول مؤقت** (صالح ~5 دقايق).
  3) الصفحة بتعرض دايمًا **أحدث رابط** — تدوس عليه، تسجّل دخول بحسابك، وتدوس Allow.
  4) أول ما الجلسة تتسجّل، الخادم بيشغّل لوحده: unity license activate --personal --accept-eula
  5) الصفحة بتقولك «تم» وبعدها بنكمّل الاستيراد.

مفيش باسورد ولا سيريال بيوصل لأي حد — كله OAuth في متصفحك.
"""
import json
import os
import re
import subprocess
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

HOME = os.path.expanduser("~")
UNITY = os.path.join(HOME, ".local/bin/unity")
KRING = os.path.join(HOME, ".config/unity-headless/start-keyring.sh")
PORT = int(os.environ.get("PORT", "8000"))

STATE = {
    "url": None,
    "url_ts": 0,
    "phase": "starting",     # starting | waiting | signed_in | license_ok | error
    "message": "بجهّز رابط الدخول…",
    "attempt": 0,
    "url_age": 0,
    "log": [],
}
LOCK = threading.Lock()
SIGNED = threading.Event()


def log(msg):
    with LOCK:
        STATE["log"].append(time.strftime("%H:%M:%S") + " · " + msg)
        STATE["log"][:] = STATE["log"][-40:]
    print(msg, flush=True)


def run(cmd, timeout=None):
    return subprocess.run(cmd, shell=True, capture_output=True, text=True, timeout=timeout)


def auth_status():
    r = run(f'"{KRING}" bash -lc \'"{UNITY}" auth status --no-banner --no-color\'', timeout=120)
    out = ((r.stdout or "") + (r.stderr or "")).strip()
    low = out.lower()
    # "You are not signed in." بتحتوي على "signed in" — لازم نستثنيها الأول
    if "not signed in" in low or "sign in to" in low:
        return False, out
    return ("signed in" in low), out


def license_status():
    r = run(f'"{KRING}" bash -lc \'"{UNITY}" license status --no-banner --no-color\'', timeout=180)
    out = (r.stdout or "") + (r.stderr or "")
    return out.strip()


def watcher():
    """بيفحص حالة الدخول كل ٣ ثواني — أول ما تنجح بنقفل عملية الدخول فورًا."""
    while True:
        time.sleep(3)
        ok, _ = auth_status()
        if ok and not SIGNED.is_set():
            SIGNED.set()
            return


def login_loop():
    """يعيد تشغيل محاولة الدخول لحد ما تنجح."""
    while STATE["phase"] not in ("signed_in", "license_ok"):
        ok, _ = auth_status()
        if ok:
            on_signed_in()
            return

        with LOCK:
            STATE["attempt"] += 1
            STATE["phase"] = "waiting"
            STATE["message"] = "مستني تسجيل الدخول من المتصفح…"
        log(f"محاولة دخول #{STATE['attempt']}")

        # ملاحظة مهمة: من غير `script` (شبه-طرفية) الـCLI بيستنى 5 دقايق كاملة.
        # مع `script` كان بيقص بعد ~55 ثانية.
        p = subprocess.Popen(
            f'"{KRING}" bash -lc \'export BROWSER=/bin/true; "{UNITY}" auth login --no-color < /dev/null\'',
            shell=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1)

        RAW = "/home/user/unity_login_out.log"

        def pump():
            fh = open(RAW, "a", encoding="utf-8")
            fh.write(f"\n===== محاولة {STATE['attempt']} · {time.strftime('%H:%M:%S')} =====\n")
            for line in p.stdout:
                fh.write(line)
                fh.flush()
                if re.search(r"(rror|failed|Failed|cancel|Cancel)", line):
                    log("CLI: " + line.strip()[:160])
                m = re.search(r"(https://services\.api\.unity\.com/\S+)", line)
                if m:
                    with LOCK:
                        STATE["url"] = m.group(1)
                        STATE["url_ts"] = time.time()
                    log("رابط جديد جاهز")

        t = threading.Thread(target=pump, daemon=True)
        t.start()
        p.wait()

        if SIGNED.is_set():
            on_signed_in()
            return
        log("الرابط انتهت صلاحيته — بطّلّع واحد جديد")
        time.sleep(2)


def on_signed_in():
    with LOCK:
        STATE.update(phase="signed_in", message="✅ اتسجلت الدخول — بفعّل رخصة Personal…", url=None)
    log("تم تسجيل الدخول")

    r = run(f'"{KRING}" bash -lc \'"{UNITY}" license activate --personal --accept-eula --no-color\'', timeout=600)
    out = ((r.stdout or "") + (r.stderr or "")).strip()
    log("license activate ⇒ " + (out[-400:] or f"exit {r.returncode}"))

    st = license_status()
    log("license status ⇒ " + (st[-400:] or "?"))
    good = ("active" in st.lower()) or ("personal" in st.lower() and "no license" not in st.lower())
    with LOCK:
        STATE["phase"] = "license_ok" if good else "error"
        STATE["message"] = ("🎉 الرخصة اتفعّلت — ابعت رسالة «تمام» في المحادثة وأنا أكمّل الاستيراد"
                            if good else "الدخول تم لكن التفعيل ما نفعش — السجل تحت")


PAGE = """<!doctype html><html lang="ar" dir="rtl"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>تفعيل Unity — THE LAST CONTRACT</title>
<style>
 body{margin:0;background:#0f1218;color:#e8e3da;font-family:system-ui,"Segoe UI",Tahoma,sans-serif}
 .wrap{max-width:760px;margin:0 auto;padding:28px 20px 60px}
 h1{font-size:22px;font-weight:600;margin:0 0 6px}
 .sub{color:#8a94a3;font-size:14px;margin-bottom:22px;line-height:1.7}
 .card{background:#161b23;border:1px solid #262d38;border-radius:14px;padding:20px;margin-bottom:16px}
 .badge{display:inline-block;padding:5px 12px;border-radius:99px;font-size:13px;margin-bottom:12px}
 .b-wait{background:#2b2f18;color:#d9c441}.b-ok{background:#16301f;color:#7fc8a9}
 .b-err{background:#301616;color:#e08b8b}.b-start{background:#1c2129;color:#8a94a3}
 a.btn{display:block;text-align:center;background:#d9a441;color:#14181f;text-decoration:none;
   font-weight:700;padding:16px;border-radius:10px;font-size:17px}
 a.btn.off{background:#2a3140;color:#5d6674;pointer-events:none}
 ol{line-height:2;color:#c9d1dc;font-size:14px;padding-inline-start:20px;margin:0}
 pre{background:#0b0e13;border:1px solid #1e242e;border-radius:10px;padding:12px;
   font-size:12px;color:#93a1b5;white-space:pre-wrap;max-height:220px;overflow:auto;direction:ltr;text-align:left}
 .muted{color:#6d7684;font-size:12px}
</style></head><body><div class="wrap">
 <h1>تفعيل Unity 6 — رخصة Personal</h1>
 <div class="sub">التفعيل رسمي ومباشر: Unity CLI بيطلّع رابط OAuth، أنت تسجّل دخول
  في متصفحك وتدوس <b>Allow Login Request</b>. بياناتك ما بتوصلنيش خالص.</div>

 <div class="card">
   <div id="badge" class="badge b-start">بيجهّز…</div>
   <div id="msg" style="margin-bottom:14px;line-height:1.7"></div>
   <a id="link" class="btn" href="/go" target="_blank" rel="noopener">اضغط هنا للدخول والموافقة (Allow)</a>
   <div id="age" class="muted" style="margin-top:8px"></div>
   <div class="muted" style="margin-top:10px">الرابط بيتبدّل كل ~5 دقايق. الصفحة دي بتحدّث نفسها،
     فلو الرابط انتهى هتلاقي واحد جديد خلال ثواني — دوس عليه على طول.</div>
 </div>

 <div class="card">
   <div style="font-weight:600;margin-bottom:10px">الخطوات</div>
   <ol>
     <li>دوس الزر اللي فوق ⇒ هيفتح صفحة Unity.</li>
     <li>سجّل دخول بحساب Unity (لو مش مسجّل).</li>
     <li>دوس <b>Allow Login Request</b>.</li>
     <li>ارجع للصفحة دي — هتلاقيها بقت «الرخصة اتفعّلت».</li>
   </ol>
 </div>

 <div class="card">
   <div style="font-weight:600;margin-bottom:10px">السجل</div>
   <pre id="log">—</pre>
 </div>
</div>
<script>
async function tick(){
  try{
    const r = await fetch('/status',{cache:'no-store'});
    const s = await r.json();
    const cls = s.phase==='license_ok'?'b-ok':(s.phase==='error'?'b-err':(s.phase==='waiting'?'b-wait':'b-start'));
    const b = document.getElementById('badge');
    b.className = 'badge '+cls; b.textContent = s.phase;
    document.getElementById('msg').textContent = s.message||'';
    const a = document.getElementById('link');
    if(s.phase==='license_ok'){ a.className='btn off'; a.textContent='خلص ✔ الرخصة اتفعّلت'; }
    else { a.href='/go'; a.className='btn'; a.textContent='اضغط هنا للدخول والموافقة (Allow)'; }
    document.getElementById('age').textContent = s.url_age ? ('عمر الرابط الحالي: '+s.url_age+' ث') : '';
    document.getElementById('log').textContent = (s.log||[]).join('\\n') || '—';
  }catch(e){}
}
tick(); setInterval(tick, 3000);
</script></body></html>"""


def wait_fresh(max_age=100, timeout=25):
    """بيرجّع رابط عمره أقل من max_age، ومستعد ينتظر لحد ما الخادم يولّد واحد جديد."""
    end = time.time() + timeout
    while time.time() < end:
        with LOCK:
            url, ts = STATE.get("url"), STATE.get("url_ts") or 0
        if url and (time.time() - ts) <= max_age:
            return url
        time.sleep(0.4)
    with LOCK:
        return STATE.get("url")


class H(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith("/go"):
            url = wait_fresh()
            if not url:
                self.send_response(503)
                self.end_headers()
                self.wfile.write("لا يوجد رابط جاهز — أعد المحاولة بعد ثانيتين".encode())
                return
            self.send_response(302)
            self.send_header("Location", url)
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            return
        if self.path.startswith("/status"):
            with LOCK:
                age = int(time.time() - STATE["url_ts"]) if STATE.get("url_ts") else 0
                body = json.dumps({k: STATE[k] for k in ("url", "phase", "message", "attempt", "log")} |
                                  {"url_age": age},
                                  ensure_ascii=False).encode()
            self.send_response(200)
            self.send_header("Content-Type", "application/json; charset=utf-8")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        body = PAGE.encode()
        self.send_response(200)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def log_message(self, *a):
        pass


if __name__ == "__main__":
    # مهم: ممنوع أي أمر unity تاني (حتى auth status) أثناء انتظار الدخول —
    # أي نداء موازي بيقطع الجلسة المعلّقة. بنعرف النجاح من خروج العملية نفسها.
    threading.Thread(target=login_loop, daemon=True).start()
    print(f"serving on 0.0.0.0:{PORT}", flush=True)
    ThreadingHTTPServer(("0.0.0.0", PORT), H).serve_forever()
