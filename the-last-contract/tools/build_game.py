#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
يبني لعبة THE LAST CONTRACT كملف HTML واحد قائم بذاته (كل الصور مدمجة base64).
المخرجات: ../game/index.html
"""
import base64, io, json, os
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.abspath(__file__))
SRC  = os.path.join(ROOT, "..", "game-src")
OUT  = os.path.join(ROOT, "..", "game")

def img_uri(path, target, quality=72):
    im = Image.open(path).convert("RGB")
    tw, th = target
    tr = tw / th
    sr = im.width / im.height
    if sr > tr:
        nw = int(im.height * tr)
        im = im.crop(((im.width - nw) // 2, 0, (im.width + nw) // 2, im.height))
    else:
        nh = int(im.width / tr)
        top = int((im.height - nh) * 0.38)
        im = im.crop((0, top, im.width, top + nh))
    im = im.resize((tw, th), Image.LANCZOS)
    buf = io.BytesIO()
    im.save(buf, "JPEG", quality=quality, optimize=True, progressive=True)
    return "data:image/jpeg;base64," + base64.b64encode(buf.getvalue()).decode(), len(buf.getvalue())

images, total = {}, 0
BG = {"office", "photo", "street", "flat", "oldroom", "rooftop", "precinct", "alex"}
for f in sorted(os.listdir(os.path.join(SRC, "bg"))):
    key = f.rsplit(".", 1)[0]
    uri, n = img_uri(os.path.join(SRC, "bg", f), (720, 1280), 70); images[key] = uri; total += n
for f in sorted(os.listdir(os.path.join(SRC, "portraits"))):
    key = f.rsplit(".", 1)[0].replace("char-", "")
    uri, n = img_uri(os.path.join(SRC, "portraits", f), (460, 600), 74); images["p_" + key] = uri; total += n

story = json.load(open(os.path.join(SRC, "story.json"), encoding="utf-8"))
print(f"الصور: {len(images)} | {total/1024:.0f} KB خام")

HTML = r"""<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,viewport-fit=cover">
<meta name="theme-color" content="#07080a">
<meta name="apple-mobile-web-app-capable" content="yes">
<meta name="mobile-web-app-capable" content="yes">
<title>العقد الأخير</title>
<style>
*{margin:0;padding:0;box-sizing:border-box;-webkit-tap-highlight-color:transparent}
html,body{height:100%;overflow:hidden;background:#07080a;color:#e8e6e2;
  font-family:"Noto Naskh Arabic","Segoe UI",Tahoma,"Geeza Pro",serif;
  overscroll-behavior:none;user-select:none;-webkit-user-select:none}
#app{position:fixed;inset:0;display:flex;flex-direction:column;overflow:hidden}
.sans{font-family:"Segoe UI",Tahoma,system-ui,sans-serif}

/* ── الخلفية ── */
.bg{position:absolute;inset:0;background-size:cover;background-position:center;
  opacity:0;transition:opacity .9s ease;transform:scale(1.06);
  animation:drift 26s ease-in-out infinite alternate;filter:saturate(.92) contrast(1.05)}
.bg.on{opacity:1}
@keyframes drift{from{transform:scale(1.06) translateY(0)}to{transform:scale(1.12) translateY(-1.5%)}}
.scrim{position:absolute;inset:0;pointer-events:none;
  background:linear-gradient(to top,rgba(4,5,7,.97) 0%,rgba(4,5,7,.86) 26%,rgba(4,5,7,.30) 58%,rgba(4,5,7,.55) 100%)}
.grain{position:absolute;inset:-50%;pointer-events:none;opacity:.055;mix-blend-mode:overlay;
  background-image:url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='140' height='140'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='.85' numOctaves='3'/></filter><rect width='140' height='140' filter='url(%23n)'/></svg>");
  animation:grain 1.1s steps(2) infinite}
@keyframes grain{0%{transform:translate(0,0)}50%{transform:translate(-2%,1%)}100%{transform:translate(1%,-1%)}}

/* ── الهيدر ── */
#hud{position:relative;z-index:6;padding:calc(env(safe-area-inset-top) + 14px) 18px 0;
  display:flex;flex-direction:column;gap:9px;opacity:0;transition:opacity .5s}
#hud.on{opacity:1}
.hudrow{display:flex;align-items:center;justify-content:space-between;gap:12px}
#chapter{font-size:11.5px;letter-spacing:.16em;color:#9a948c;text-transform:uppercase;
  font-family:"Segoe UI",Tahoma,sans-serif}
.meters{display:flex;gap:9px;flex:1;justify-content:flex-end}
.meter{width:64px}
.meter b{display:block;font-size:8.5px;color:#7d7770;letter-spacing:.03em;margin-bottom:3px;font-weight:400;
  font-family:"Segoe UI",Tahoma,sans-serif;text-align:center}
.track{height:3px;border-radius:2px;background:rgba(255,255,255,.13);overflow:hidden}
.fill{height:100%;width:0;border-radius:2px;transition:width .8s cubic-bezier(.2,.8,.2,1)}
.f-h{background:linear-gradient(ltr,#d8b46a,#f0d79b)}
.f-t{background:linear-gradient(90deg,#5f9db3,#8fd0e0)}
.f-v{background:linear-gradient(90deg,#a8a2c8,#d6d0f0)}

/* ── منطقة الحوار ── */
#stage{position:relative;z-index:5;flex:1;display:flex;flex-direction:column;justify-content:flex-end}
#portrait{position:absolute;left:0;top:calc(env(safe-area-inset-top) + 62px);
  width:min(42vw,190px);opacity:0;
  transition:opacity .55s ease, transform .55s ease;transform:translateY(-12px);
  filter:drop-shadow(0 14px 38px rgba(0,0,0,.9));pointer-events:none}
#portrait.on{opacity:1;transform:translateY(0)}
#portrait img{width:100%;display:block;
  -webkit-mask-image:linear-gradient(to bottom,#000 55%,transparent 100%);
  mask-image:linear-gradient(to bottom,#000 55%,transparent 100%)}

#textbox{position:relative;padding:0 22px calc(env(safe-area-inset-bottom) + 20px);
  min-height:44vh;display:flex;flex-direction:column;justify-content:flex-end}
#speaker{font-size:13.5px;letter-spacing:.06em;color:#e0bd7d;margin-bottom:9px;
  font-family:"Segoe UI",Tahoma,sans-serif;opacity:0;transition:opacity .3s;font-weight:600}
#speaker.on{opacity:1}
#body{font-size:clamp(17px,4.9vw,21px);line-height:1.92;color:#eeebe6;
  text-shadow:0 2px 18px rgba(0,0,0,.95);min-height:3em}
#body.narr{color:#cfcac2;font-style:italic}
#tap{position:absolute;left:50%;bottom:calc(env(safe-area-inset-bottom) + 64px);transform:translateX(-50%);
  font-size:16px;color:#c9a35f;opacity:0;transition:opacity .35s;animation:bob 1.9s ease-in-out infinite}
#tap.on{opacity:.85}
@keyframes bob{0%,100%{transform:translate(-50%,0)}50%{transform:translate(-50%,7px)}}

/* ── الخيارات ── */
#choices{position:relative;z-index:7;padding:0 18px calc(env(safe-area-inset-bottom) + 18px);
  display:none;flex-direction:column;gap:9px;justify-content:flex-end}
#choices.on{display:flex}
#cprompt{font-size:14.5px;line-height:1.75;color:#b9b3aa;font-style:italic;margin-bottom:6px;
  text-shadow:0 2px 14px #000}
.opt{position:relative;background:rgba(17,19,23,.9);border:1px solid rgba(255,255,255,.11);
  border-radius:3px;padding:14px 16px;font-size:15.5px;line-height:1.6;color:#e9e5de;
  text-align:right;cursor:pointer;transition:.16s;backdrop-filter:blur(9px);
  font-family:"Segoe UI",Tahoma,sans-serif;animation:rise .3s backwards}
.opt:active{background:rgba(216,180,106,.16);border-color:rgba(216,180,106,.55);transform:scale(.985)}
@keyframes rise{from{opacity:0;transform:translateY(9px)}to{opacity:1;transform:translateY(0)}}

/* ── الشاشات ── */
.screen{position:absolute;inset:0;z-index:20;display:flex;flex-direction:column;
  align-items:center;justify-content:center;padding:34px 26px;text-align:center;
  background:#07080a;opacity:0;pointer-events:none;transition:opacity .7s}
.screen.on{opacity:1;pointer-events:auto}
.screen.bgimg{background-size:cover;background-position:center}
.screen .shade{position:absolute;inset:0;background:rgba(5,6,8,.80)}
.screen > *{position:relative;z-index:2}

h1{font-size:clamp(30px,9vw,46px);letter-spacing:.03em;line-height:1.3;
  background:linear-gradient(180deg,#f5e9cf,#c9a35f);-webkit-background-clip:text;
  background-clip:text;color:transparent;font-weight:400}
.sub{font-size:11.5px;letter-spacing:.34em;color:#8d867d;margin-top:12px;
  font-family:"Segoe UI",Tahoma,sans-serif}
.rule{width:52px;height:1px;background:#c9a35f;margin:22px auto;opacity:.55}
.tag{font-size:14px;color:#a29b92;line-height:2;max-width:330px;font-style:italic}
.btn{margin-top:11px;min-width:230px;padding:15px 26px;border:1px solid rgba(216,180,106,.42);
  background:rgba(216,180,106,.07);color:#f0e2c5;font-size:16px;border-radius:2px;
  font-family:"Segoe UI",Tahoma,sans-serif;letter-spacing:.05em;cursor:pointer;
  transition:.18s;backdrop-filter:blur(6px)}
.btn:active{background:rgba(216,180,106,.22);transform:scale(.98)}
.btn.ghost{border-color:rgba(255,255,255,.14);background:rgba(255,255,255,.03);color:#b4aea6;font-size:14px;padding:12px 22px}

/* الكارت السينمائي */
#card{position:absolute;inset:0;z-index:15;background:#050607;display:flex;
  align-items:center;justify-content:center;opacity:0;pointer-events:none;
  transition:opacity .6s}
#card.on{opacity:1}
#card span{font-size:clamp(20px,6vw,28px);color:#d8b46a;letter-spacing:.15em;
  font-family:"Segoe UI",Tahoma,sans-serif;opacity:0;animation:cardin 3.1s forwards}
@keyframes cardin{0%{opacity:0;letter-spacing:.5em}22%{opacity:1;letter-spacing:.15em}
  76%{opacity:1;letter-spacing:.15em}100%{opacity:0;letter-spacing:.1em}}

/* النهاية */
#endTitle{font-size:clamp(25px,7.5vw,36px);color:#f0d79b;line-height:1.4}
#endBody{font-size:15px;line-height:2.05;color:#c9c3ba;max-width:430px;margin-top:20px;
  max-height:44vh;overflow:auto}
#endStats{display:flex;gap:20px;margin-top:22px;justify-content:center}
#endStats div{font-size:11px;color:#8d867d;font-family:"Segoe UI",Tahoma,sans-serif}
#endStats b{display:block;font-size:19px;color:#d8b46a;font-weight:400;margin-bottom:2px}

/* المعرض */
#gallery{max-height:60vh;overflow:auto;width:100%;max-width:430px}
.gitem{display:flex;gap:12px;align-items:center;padding:13px 4px;
  border-bottom:1px solid rgba(255,255,255,.08);text-align:right}
.gitem span{font-size:15.5px;color:#e4dfd6}
.gitem.locked span{color:#4d4a46}
.gnum{width:26px;height:26px;flex:0 0 26px;border:1px solid rgba(216,180,106,.4);
  border-radius:50%;display:flex;align-items:center;justify-content:center;
  font-size:11px;color:#c9a35f;font-family:"Segoe UI",sans-serif}
.gitem.locked .gnum{border-color:rgba(255,255,255,.12);color:#3d3a36}

#snd{position:absolute;top:calc(env(safe-area-inset-top) + 12px);left:14px;z-index:30;
  width:34px;height:34px;border-radius:50%;border:1px solid rgba(255,255,255,.12);
  background:rgba(10,11,14,.6);color:#8d867d;font-size:14px;display:flex;
  align-items:center;justify-content:center;backdrop-filter:blur(6px)}
.hidden{display:none!important}
</style>
</head>
<body>
<div id="app">

  <div class="bg" id="bgA"></div>
  <div class="bg" id="bgB"></div>
  <div class="scrim"></div>
  <div class="grain"></div>

  <button id="snd" title="الصوت">♪</button>

  <div id="hud">
    <div class="hudrow">
      <div id="chapter">الفصل الأول</div>
      <div class="meters">
        <div class="meter"><b>الإنسانية</b><div class="track"><div class="fill f-h" id="mH"></div></div></div>
        <div class="meter"><b>نادين</b><div class="track"><div class="fill f-t" id="mT"></div></div></div>
        <div class="meter"><b>الحقيقة</b><div class="track"><div class="fill f-v" id="mV"></div></div></div>
      </div>
    </div>
  </div>

  <div id="stage">
    <div id="portrait"><img id="pimg" alt=""></div>
    <div id="textbox">
      <div id="speaker"></div>
      <div id="body"></div>
      <div id="tap">⌄</div>
    </div>
  </div>

  <div id="choices"><div id="cprompt"></div><div id="opts"></div></div>

  <!-- شاشة البداية -->
  <div class="screen on bgimg" id="s-title">
    <div class="shade"></div>
    <h1>العقد الأخير</h1>
    <div class="sub">THE LAST CONTRACT</div>
    <div class="rule"></div>
    <div class="tag">قاتل مأجور. عقد أخير. صورة واحدة تفكّ كل حاجة كان دفنها.</div>
    <button class="btn" id="b-new">ابدأ من الأول</button>
    <button class="btn ghost hidden" id="b-cont">استمر</button>
    <button class="btn ghost" id="b-gal">النهايات</button>
  </div>

  <!-- شاشة النهاية -->
  <div class="screen" id="s-end">
    <div id="endTitle"></div>
    <div class="rule"></div>
    <div id="endBody"></div>
    <div id="endStats"></div>
    <button class="btn" id="b-again" style="margin-top:26px">من الأول</button>
    <button class="btn ghost" id="b-gal2">النهايات</button>
  </div>

  <!-- المعرض -->
  <div class="screen" id="s-gal">
    <h1 style="font-size:26px">النهايات</h1>
    <div class="sub">ENDINGS</div>
    <div class="rule"></div>
    <div id="gallery"></div>
    <button class="btn ghost" id="b-galclose" style="margin-top:22px">رجوع</button>
  </div>

  <div id="card"><span></span></div>
</div>

<script>
const STORY = __STORY__;
const IMG = __IMAGES__;

const NODES = {}; STORY.nodes.forEach(n => NODES[n.id] = n);
const NAMES = {adam:"آدم",lotfy:"الحاج لطفي",nadine:"نادين",yassin:"ياسين",mariam:"مريم",ziad:"زياد",sherif:"العميد شريف"};
const ACTS = {1:"الفصل الأول · الظرف",2:"الفصل الثاني · البيت القديم",3:"الفصل الثالث · السطح"};
const SAVE_KEY = "tlc_save_v1", END_KEY = "tlc_endings_v1";

let S = {stats:{humanity:40,"trust.nadine":10,truth:0,"fear.lotfy":30},flags:{},node:null,seen:[]};
let typed = null, typing = false, waiting = false, currentOpts = null, bgFront = true, audioOn = true;

const $ = s => document.querySelector(s);
const clamp = v => Math.max(0, Math.min(100, v));

/* ── الصوت ── */
let AC = null;
function ac(){ if(!AC) AC = new (window.AudioContext||window.webkitAudioContext)(); return AC; }
function blip(f=520, d=.035, v=.026){
  if(!audioOn) return;
  try{ const c=ac(), o=c.createOscillator(), g=c.createGain();
    o.type="triangle"; o.frequency.value=f;
    g.gain.setValueAtTime(v,c.currentTime);
    g.gain.exponentialRampToValueAtTime(.0001,c.currentTime+d);
    o.connect(g); g.connect(c.destination); o.start(); o.stop(c.currentTime+d);
  }catch(e){}
}
function thud(){
  if(!audioOn) return;
  try{ const c=ac(), o=c.createOscillator(), g=c.createGain();
    o.type="sine"; o.frequency.setValueAtTime(120,c.currentTime);
    o.frequency.exponentialRampToValueAtTime(48,c.currentTime+.22);
    g.gain.setValueAtTime(.09,c.currentTime);
    g.gain.exponentialRampToValueAtTime(.0001,c.currentTime+.3);
    o.connect(g); g.connect(c.destination); o.start(); o.stop(c.currentTime+.32);
  }catch(e){}
}

/* ── الحالة ── */
function save(){
  try{ localStorage.setItem(SAVE_KEY, JSON.stringify({s:S.stats,f:S.flags,n:S.node,t:Date.now()})); }catch(e){}
}
function load(){
  try{ const d = JSON.parse(localStorage.getItem(SAVE_KEY)||"null");
    if(!d || !NODES[d.n]) return false;
    S.stats = Object.assign({humanity:40,"trust.nadine":10,truth:0,"fear.lotfy":30}, d.s||{});
    S.flags = d.f||{}; S.node = d.n; return true;
  }catch(e){ return false; }
}
function seenEndings(){
  try{ return JSON.parse(localStorage.getItem(END_KEY)||"[]"); }catch(e){ return []; }
}
function addEnding(id){
  const e = seenEndings(); if(!e.includes(id)){ e.push(id); }
  try{ localStorage.setItem(END_KEY, JSON.stringify(e)); }catch(e){}
}

/* ── الخلفية ── */
let curBg = null;
function setBg(key){
  if(!key || key === curBg || !IMG[key]) return;
  curBg = key;
  const a = $("#bgA"), b = $("#bgB");
  const showEl = bgFront ? b : a, hideEl = bgFront ? a : b;
  showEl.style.backgroundImage = `url(${IMG[key]})`;
  showEl.classList.add("on"); hideEl.classList.remove("on");
  bgFront = !bgFront;
}

/* ── العدّادات ── */
function meters(){
  const h = clamp(S.stats.humanity||0), t = clamp(S.stats["trust.nadine"]||0), v = clamp(S.stats.truth||0);
  $("#mH").style.width = h+"%"; $("#mT").style.width = t+"%"; $("#mV").style.width = v+"%";
}

/* ── الرسم ── */
function goTo(id){
  const n = NODES[id];
  if(!n) return;
  S.node = id; save();
  if(n.bg) setBg(n.bg);
  if(n.act) $("#chapter").textContent = ACTS[n.act] || "";
  if(n.onEnter && !S.flags["_applied_" + id]){
    S.flags["_applied_" + id] = true;
    if(n.onEnter.stats) n.onEnter.stats.forEach(m => {
      S.stats[m.key] = clamp((S.stats[m.key]||0) + (m.add||0));
    });
    if(n.onEnter.setFlags) n.onEnter.setFlags.forEach(f => S.flags[f] = true);
    meters();
  }
  if(n.require && !condMet(n.require)){ if(n.elseNext) return goTo(n.elseNext); return; }
  if(n.type === 1) return renderChoices(n);
  if(n.type === 4) return renderEnding(n);
  if(n.type === 3) return cinematic(n);
  // كارت الفصل
  if((n.text||"").trim().startsWith("الفصل") && (n.text||"").length < 46) return chapterCard(n);
  renderLine(n);
}

function chapterCard(n){
  hideChoices();
  $("#portrait").classList.remove("on");
  $("#textbox").classList.add("hidden");
  $("#hud").classList.remove("on");
  const card = $("#card");
  card.querySelector("span").textContent = n.text.trim();
  card.classList.add("on");
  S.node = n.id; save();
  setTimeout(() => {
    card.classList.remove("on");
    setTimeout(() => goTo(n.next), 450);
  }, 2700);
}

function condMet(c){
  if(!c) return true;
  for(const f of (c.allFlags||[])) if(!S.flags[f]) return false;
  for(const f of (c.noneFlags||[])) if(S.flags[f]) return false;
  if(c.anyFlags && c.anyFlags.length){ if(!c.anyFlags.some(f=>S.flags[f])) return false; }
  for(const r of (c.stats||[])){
    const v = S.stats[r.key]||0, t = r.value;
    const ok = r.op === ">=" ? v>=t : r.op === "<=" ? v<=t : r.op === ">" ? v>t : r.op === "<" ? v<t : false;
    if(!ok) return false;
  }
  return true;
}

function hideChoices(){ $("#choices").classList.remove("on"); currentOpts = null; }

function renderLine(n){
  hideChoices();
  $("#hud").classList.add("on");
  $("#textbox").classList.remove("hidden");

  const isNarr = !n.speaker;
  $("#speaker").classList.toggle("on", !isNarr);
  $("#speaker").textContent = isNarr ? "" : (NAMES[n.speaker] || n.speaker);
  const body = $("#body");
  body.className = isNarr ? "narr" : "";
  typeText(body, n.text || "");

  const pdiv = $("#portrait");
  if(!isNarr && IMG["p_" + n.speaker]){
    $("#pimg").src = IMG["p_" + n.speaker];
    pdiv.classList.add("on");
  } else {
    pdiv.classList.remove("on");
  }
  waiting = true;
}

function typeText(el, text){
  if(typed) clearInterval(typed);
  typing = true;
  $("#tap").classList.remove("on");
  const html = text.replace(/&/g,"&amp;").replace(/</g,"&lt;").split("\n").map(l=>`<div>${l||"&nbsp;"}</div>`).join("");
  el.innerHTML = html;
  const total = text.length;
  el.style.opacity = "1";
  // إخفاء تدريجي ثم إظهار حرف حرف عبر قصّ عبر clip بسيط
  let i = 0;
  el.setAttribute("data-full", html);
  const chars = [...text];
  el.textContent = "";
  const frag = document.createDocumentFragment();
  const spans = [];
  let line = document.createElement("div"); frag.appendChild(line);
  for(const ch of chars){
    if(ch === "\n"){ line = document.createElement("div"); frag.appendChild(line); continue; }
    const s = document.createElement("span");
    s.textContent = ch; s.style.opacity = "0";
    line.appendChild(s); spans.push(s);
  }
  el.innerHTML = ""; el.appendChild(frag);

  let k = 0;
  typed = setInterval(() => {
    const burst = 2;
    for(let b=0; b<burst && k<spans.length; b++, k++){
      spans[k].style.opacity = "1";
      if(k % 9 === 0) blip(600 + (k%5)*40, .02, .012);
    }
    if(k >= spans.length){
      clearInterval(typed); typed = null; typing = false;
      $("#tap").classList.add("on");
    }
  }, 22);
}

function finishTyping(){
  if(!typing) return false;
  clearInterval(typed); typed = null; typing = false;
  document.querySelectorAll("#body span").forEach(s => s.style.opacity = "1");
  $("#tap").classList.add("on");
  return true;
}

function renderChoices(n){
  finishTyping();
  waiting = false;
  $("#tap").classList.remove("on");
  $("#portrait").classList.remove("on");
  $("#textbox").classList.add("hidden");
  $("#hud").classList.add("on");

  const avail = (n.options||[]).filter(o => !(o.require && !condMet(o.require)));
  if(!avail.length){ return goTo(n.next); }
  currentOpts = avail;

  $("#cprompt").textContent = n.text || "";
  $("#cprompt").style.display = n.text ? "block" : "none";

  const box = $("#opts"); box.innerHTML = "";
  avail.forEach((o, i) => {
    const b = document.createElement("div");
    b.className = "opt"; b.style.animationDelay = (i*70)+"ms";
    b.textContent = o.text;
    b.onclick = (ev) => { ev.stopPropagation(); choose(o); };
    box.appendChild(b);
  });
  $("#choices").classList.add("on");
}

function choose(o){
  thud(); if(navigator.vibrate) navigator.vibrate(12);
  hideChoices();
  if(o.effect){
    if(o.effect.stats) o.effect.stats.forEach(m => {
      S.stats[m.key] = clamp((S.stats[m.key]||0) + (m.add||0));
      if(m.useSet) S.stats[m.key] = m.set;
    });
    if(o.effect.setFlags) o.effect.setFlags.forEach(f => S.flags[f] = true);
    meters();
  }
  if(o.setFlags) o.setFlags.forEach(f => S.flags["once_"+f] = true);
  S.node = o.next; save();
  setTimeout(() => goTo(o.next), 120);
}

function cinematic(n){
  const card = $("#card"); card.querySelector("span").textContent = "";
  card.classList.add("on");
  setTimeout(() => {
    card.classList.remove("on");
    setTimeout(() => goTo(n.next), 500);
  }, 500);
}

function renderEnding(n){
  hideChoices();
  $("#hud").classList.remove("on");
  $("#portrait").classList.remove("on");
  $("#textbox").classList.add("hidden");
  $("#endTitle").textContent = n.endingTitle || "النهاية";
  const lines = (n.endingBody || n.text || "").split("\n\n");
  $("#endBody").innerHTML = lines.map(p => {
    const ital = p.trim().startsWith("*") && p.trim().endsWith("*");
    const t = ital ? p.trim().slice(1,-1) : p;
    return `<div style="margin-bottom:14px;${ital?'font-style:italic;color:#a49d93;font-size:13.5px;':''}">${t.replace(/\n/g,"<br>")}</div>`;
  }).join("");
  $("#endStats").innerHTML =
    `<div><b>${Math.round(clamp(S.stats.humanity||0))}</b>الإنسانية</div>
     <div><b>${Math.round(clamp(S.stats["trust.nadine"]||0))}</b>نادين</div>
     <div><b>${Math.round(clamp(S.stats.truth||0))}</b>الحقيقة</div>`;
  addEnding(n.endingId);
  try{ localStorage.removeItem(SAVE_KEY); }catch(e){}
  $("#s-end").classList.add("on");
  thud();
}

function renderGallery(){
  const got = seenEndings();
  const all = STORY.nodes.filter(n => n.type === 4);
  const box = $("#gallery"); box.innerHTML = "";
  const order = ["E1","E2","E3","E4","E5","E6"];
  order.forEach((eid, i) => {
    const n = all.find(x => x.endingId === eid); if(!n) return;
    const has = got.includes(eid);
    const d = document.createElement("div");
    d.className = "gitem" + (has ? "" : " locked");
    d.innerHTML = `<div class="gnum">${i+1}</div><span>${has ? n.endingTitle : "؟؟؟"}</span>`;
    box.appendChild(d);
  });
  // إحصاء
  const c = document.createElement("div");
  c.style.cssText = "margin-top:18px;font-size:12.5px;color:#8d867d;font-family:Segoe UI,sans-serif";
  c.textContent = `${got.length} من 6 نهايات مكتشفة`;
  box.appendChild(c);
}

/* ── البداية ── */
function newGame(){
  S = {stats:{humanity:40,"trust.nadine":10,truth:0,"fear.lotfy":30},flags:{},node:null,seen:[]};
  meters(); curBg = null;
  document.querySelectorAll(".screen").forEach(s => s.classList.remove("on"));
  setTimeout(() => goTo(STORY.entry), 400);
}

function resumeGame(){
  document.querySelectorAll(".screen").forEach(s => s.classList.remove("on"));
  meters(); curBg = null;
  setTimeout(() => goTo(S.node), 400);
}

/* ── ربط ── */
$("#b-new").onclick = () => { blip(440,.09,.05); newGame(); };
$("#b-cont").onclick = () => { blip(440,.09,.05); resumeGame(); };
$("#b-again").onclick = () => { $("#s-end").classList.remove("on"); newGame(); };
let galFrom = "title";
$("#b-gal").onclick = () => { galFrom = "title"; renderGallery(); $("#s-gal").classList.add("on"); };
$("#b-gal2").onclick = () => { galFrom = "end"; renderGallery(); $("#s-gal").classList.add("on"); };
$("#b-galclose").onclick = () => {
  $("#s-gal").classList.remove("on");
  if(galFrom === "end") $("#s-end").classList.add("on");
  else $("#s-title").classList.add("on");
};
$("#snd").onclick = (e) => {
  e.stopPropagation();
  audioOn = !audioOn;
  $("#snd").textContent = audioOn ? "♪" : "×";
  $("#snd").style.color = audioOn ? "#8d867d" : "#43403c";
  if(audioOn) blip(520,.06,.04);
};

/* التقدّم: ضغطة على الشاشة */
function advanceTap(e){
  if(!waiting || currentOpts) return;
  if($("#s-title").classList.contains("on") || $("#s-end").classList.contains("on") ||
     $("#s-gal").classList.contains("on")) return;
  if(finishTyping()) return;
  const n = NODES[S.node];
  if(n && n.next) { blip(380,.02,.008); goTo(n.next); }
  else if(n && n.type === 3) { /* مدير السينمائية */ }
}
document.getElementById("app").addEventListener("click", advanceTap);

document.addEventListener("touchmove", e => { if(e.touches.length > 1) e.preventDefault(); }, {passive:false});

/* التهيئة */
(function init(){
  $("#bgA").style.backgroundImage = `url(${IMG["street"]})`;
  $("#bgA").classList.add("on");
  $("#s-title").style.backgroundImage = `url(${IMG["rooftop"]})`;
  if(load()){ $("#b-cont").classList.remove("hidden"); }
  meters();
})();
</script>
</body>
</html>
"""

HTML = HTML.replace("__STORY__", json.dumps(story, ensure_ascii=False))
HTML = HTML.replace("__IMAGES__", json.dumps(images))

os.makedirs(OUT, exist_ok=True)
path = os.path.join(OUT, "index.html")
with open(path, "w", encoding="utf-8") as f:
    f.write(HTML)

size = os.path.getsize(path)
print(f"✓ {path}")
print(f"✓ الحجم: {size/1024/1024:.2f} MB")
print(f"✓ عقد: {len(story['nodes'])}")
