#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
تنزيل أصول البيئات (CC0) من Poly Haven وتجهيزها لمشروع Unity.

كل موديل بناخده بصيغة **FBX** (Unity بيستوردها أصلًا) + خامات **1K** بس
(ممنوع 4K حسب قيود المساحة) ونحطها في:
    Assets/_Project/Resources/Props/<id>/<id>.fbx
    Assets/_Project/Resources/Props/<id>/<id>_diff.jpg
    Assets/_Project/Resources/Props/<id>/<id>_nor.jpg
    Assets/_Project/Resources/Props/<id>/<id>_rough.jpg

المصدر: polyhaven.com — كل الأصول CC0 (Public Domain).
التشغيل: python3 tools/fetch_env_assets.py
"""

import json
import os
import sys
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DEST = os.path.join(ROOT, "Assets/_Project/Resources/Props")

MODELS = [
    "WoodenChair_01",
    "WoodenTable_01",
    "WoodenTable_03",
    "CoffeeTable_01",
    "Sofa_01",
    "ArmChair_01",
    "GreenChair_01",
    "SchoolDesk_01",
    "Shelf_01",
    "GothicCabinet_01",
    "bar_chair_round_01",
    "desk_lamp_arm_01",
]

UA = {"User-Agent": "Mozilla/5.0 (TLC asset fetch)"}


def get_json(url):
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.load(r)


def download(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 1024:
        return os.path.getsize(path), True
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=180) as r, open(path, "wb") as f:
        f.write(r.read())
    return os.path.getsize(path), False


def tex_url(files, cat):
    """يرجّع رابط خامة 1k بصيغة jpg لو موجودة."""
    node = files.get(cat)
    if not isinstance(node, dict):
        return None
    one = node.get("1k")
    if not isinstance(one, dict):
        return None
    jpg = one.get("jpg")
    if isinstance(jpg, dict):
        return jpg.get("url")
    return None


def main():
    os.makedirs(DEST, exist_ok=True)
    manifest, total = [], 0

    for mid in MODELS:
        folder = os.path.join(DEST, mid)
        os.makedirs(folder, exist_ok=True)

        try:
            files = get_json(f"https://api.polyhaven.com/files/{mid}")
        except Exception as e:
            print(f"✗ {mid}: فشل قراءة الملفات — {e}")
            continue

        fbx = (files.get("fbx", {}).get("1k", {}) or {}).get("fbx", {})
        fbx_url = fbx.get("url")
        if not fbx_url:
            print(f"✗ {mid}: مفيش FBX 1K")
            continue

        size, cached = download(fbx_url, os.path.join(folder, f"{mid}.fbx"))
        total += size
        print(f"{'=' if cached else '+'} {mid}.fbx  {size/1024:.0f} KB")

        got = {}
        for cat, suffix in (("Diffuse", "diff"), ("nor_gl", "nor"), ("Rough", "rough")):
            url = tex_url(files, cat)
            if not url:
                continue
            s, _ = download(url, os.path.join(folder, f"{mid}_{suffix}.jpg"))
            total += s
            got[suffix] = s

        manifest.append({
            "id": mid,
            "fbx_bytes": size,
            "textures": got,
            "license": "CC0",
            "source": f"https://polyhaven.com/a/{mid}",
        })

    with open(os.path.join(DEST, "ASSET_MANIFEST.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)

    print(f"\n✓ {len(manifest)} موديل · إجمالي {total/1024/1024:.1f} MB")
    return 0 if manifest else 1


if __name__ == "__main__":
    sys.exit(main())
