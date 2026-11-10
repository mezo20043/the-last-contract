#!/usr/bin/env python3
"""
يولّد "بيبات حوار" (مش تعليق صوتي مسجّل) لأسطر chapter01.
الهدف: إثبات إن حدث الحوار بيربط مقطع ويتشغّل — من غير أي ادعاء بوجود أداء ممثل.
الناتج: Assets/_Project/Resources/Audio/Voice/<node_id>.wav (24kHz mono)
"""
import json, math, os, struct, wave

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STORY = os.path.join(ROOT, "Assets/_Project/Resources/Story/chapter01.json")
OUT = os.path.join(ROOT, "Assets/_Project/Resources/Audio/Voice")
SR = 12000
# طبقة مختلفة لكل متحدث (مش صوت بشري — نغمات)
PITCH = {"adam": 196.0, "murad": 147.0, "laila": 262.0, "yassin": 175.0, "yusuf": 220.0}


def collect(node, acc):
    if isinstance(node, dict):
        if node.get("speaker") and node.get("id"):
            acc.append((node["id"], node["speaker"]))
        for v in node.values():
            collect(v, acc)
    elif isinstance(node, list):
        for v in node:
            collect(v, acc)


def beep(freq, dur=0.55):
    n = int(SR * dur)
    out = [0.0] * n
    for i in range(n):
        t = i / SR
        env = min(1.0, t / 0.02) * max(0.0, 1.0 - (t / dur) ** 2)
        mod = 1.0 + 0.02 * math.sin(2 * math.pi * 5.5 * t)     # اهتزاز خفيف = إحساس كلام
        out[i] = 0.28 * env * math.sin(2 * math.pi * freq * mod * t)
        out[i] += 0.10 * env * math.sin(2 * math.pi * freq * 2.01 * t)
    return out


def write(path, samples):
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in samples))


def main():
    os.makedirs(OUT, exist_ok=True)
    data = json.load(open(STORY, encoding="utf-8"))
    lines = []
    collect(data, lines)
    seen, made = set(), 0
    for node_id, speaker in lines:
        if node_id in seen:
            continue
        seen.add(node_id)
        freq = PITCH.get(speaker, 200.0)
        dur = 0.45 + 0.05 * (len(node_id) % 4)
        write(os.path.join(OUT, node_id + ".wav"), beep(freq, dur))
        made += 1
    print(f"اتولّد {made} بيب حوار في {OUT}")


if __name__ == "__main__":
    main()
