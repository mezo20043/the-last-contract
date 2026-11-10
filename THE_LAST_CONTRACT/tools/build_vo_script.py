#!/usr/bin/env python3
"""يولّد سكربت التسجيل الصوتي من ملفات القصة — كل سطر بمتحدثه وموضعه."""
import json, os, glob, collections

STORY = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Resources", "Story")
OUT = os.path.join(os.path.dirname(__file__), "..", "VO_SCRIPT.md")

SPEAKERS = {
    "adam":   ("آدم منصور", "ذكر · 30–35 · صوت هادي ومتحكّم، طبقة متوسطة"),
    "murad":  ("مراد",      "ذكر · 55–65 · صوت خشن ثقيل، نَفَس قصير"),
    "laila":  ("ليلى",      "أنثى · 28–34 · حادّة وواضحة، إيقاع سريع"),
    "yassin": ("ياسين",     "ذكر · 25–30 · متوتر، بيرتفع صوته لما ينفعل"),
}

per_speaker = collections.defaultdict(list)
chapters = []

for path in sorted(glob.glob(os.path.join(STORY, "chapter0*.json"))):
    doc = json.load(open(path, encoding="utf-8"))
    chap = os.path.basename(path).replace(".json", "")
    lines = []
    for n in doc["nodes"]:
        if n.get("type") != 0:      # 0 = Line
            continue
        text = (n.get("text") or "").strip()
        if not text:
            continue
        spk = (n.get("speaker") or "").strip().lower()
        entry = (n["id"], spk, text, n.get("act"))
        lines.append(entry)
        per_speaker[spk or "narrator"].append((chap, n["id"], text))
    chapters.append((chap, lines))

total = sum(len(l) for _, l in chapters)

md = ["# سكربت التسجيل الصوتي — THE LAST CONTRACT", ""]
md.append(f"**{total} سطر** موزّعة على {len(chapters)} فصول.")
md.append("")
md.append("> الملفات المطلوبة **مش موجودة** في المشروع — لازم تُسجَّل بصوت بشري.")
md.append("> النظام جاهز: حط الملف باسم معرّف العقدة في `Resources/Audio/Voice/{nodeId}.wav`")
md.append("> وبيتشغّل تلقائياً مع السطر (انظر `VoiceOverSystem`).")
md.append("")
md.append("## المواصفات الفنية للتسجيل")
md.append("")
md.append("| البند | القيمة |")
md.append("|---|---|")
md.append("| الصيغة | WAV · 24 kHz · 16-bit · أحادي (أو 48 kHz وأنا أحوّلها) |")
md.append("| القص | بلا موسيقى خلفية · بلا ضجيج غرفة |")
md.append("| المستوى | −18 LUFS تقريباً · القمة −3 dB |")
md.append("| الصمت | 150 ms قبل وبعد كل سطر |")
md.append("| تسمية الملف | `{nodeId}.wav` بالحرف |")
md.append("")
md.append("## الأدوار")
md.append("")
md.append("| الشخصية | الوصف الصوتي | عدد الأسطر |")
md.append("|---|---|---|")
for key, (name, desc) in SPEAKERS.items():
    md.append(f"| {name} | {desc} | {len(per_speaker.get(key, []))} |")
md.append("")

for chap, lines in chapters:
    md.append(f"## {chap}  ·  {len(lines)} سطر")
    md.append("")
    md.append("| # | المعرّف (اسم الملف) | المتحدث | النص |")
    md.append("|---|---|---|---|")
    for i, (nid, spk, text, act) in enumerate(lines, 1):
        name = SPEAKERS.get(spk, (spk or "راوي", ""))[0]
        t = text.replace("|", "\\|")
        md.append(f"| {i} | `{nid}` | {name} | {t} |")
    md.append("")

open(OUT, "w", encoding="utf-8").write("\n".join(md))
print(f"✓ VO_SCRIPT.md · {total} سطر")
for k in SPEAKERS:
    print(f"   {SPEAKERS[k][0]}: {len(per_speaker.get(k, []))} سطر")
