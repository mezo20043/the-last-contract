#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
تنزيل خامات البيئة (CC0 — Poly Haven) بصيغة JPG بدقة 1K ووضعها في المسار
اللي MaterialLibrary في المشروع بيتوقعه أصلًا:

    Assets/_Project/Resources/Art/Textures/Interior/<key>_albedo.jpg
    Assets/_Project/Resources/Art/Textures/Interior/<key>_normal.jpg
    Assets/_Project/Resources/Art/Textures/Exterior/<key>_albedo.jpg
    Assets/_Project/Resources/Art/Textures/Exterior/<key>_normal.jpg

المشروع كان بيدوّر على الخامات دي من الأول وبيفشل ويرجع لألوان مسطّحة —
السكربت ده بيحط الأصول الحقيقية مكانها فيبقى الشقة والمواقع الأربعة
بخامات CC0 حقيقية.

التشغيل: python3 tools/fetch_env_textures.py
"""

import json
import os
import sys
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, "Assets/_Project/Resources/Art/Textures")

# key في MaterialLibrary  →  (معرّف الأصل على Poly Haven, مجلد)
MAP = {
    "wood_floor":    ("dark_wooden_planks", "Interior"),
    "wood_dark":     ("dark_paneled_wood",  "Interior"),
    "plaster_wall":  ("grey_plaster",       "Interior"),
    "tile_ceramic":  ("brown_floor_tiles",  "Interior"),
    "fabric_dark":   ("denim_fabric",       "Interior"),
    "metal_brushed": ("metal_plate",        "Interior"),
    "concrete":      ("brushed_concrete",   "Exterior"),
    "city_facade":   ("concrete_tile_facade", "Exterior"),
}

UA = {"User-Agent": "Mozilla/5.0 (TLC asset fetch)"}


def get_json(url):
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.load(r)


def pick_jpg(files, cat):
    node = files.get(cat)
    if not isinstance(node, dict):
        return None
    for res in ("1k", "2k"):
        one = node.get(res)
        if isinstance(one, dict):
            jpg = one.get("jpg")
            if isinstance(jpg, dict) and jpg.get("url"):
                return jpg["url"]
    return None


def download(url, path):
    if os.path.exists(path) and os.path.getsize(path) > 1024:
        return os.path.getsize(path), True
    req = urllib.request.Request(url, headers=UA)
    with urllib.request.urlopen(req, timeout=180) as r, open(path, "wb") as f:
        f.write(r.read())
    return os.path.getsize(path), False


def main():
    manifest, total, ok = [], 0, 0

    for key, (asset, folder) in MAP.items():
        out_dir = os.path.join(RES, folder)
        os.makedirs(out_dir, exist_ok=True)

        try:
            files = get_json(f"https://api.polyhaven.com/files/{asset}")
        except Exception as e:
            print(f"✗ {key}: {e}")
            continue

        # أسماء الخامات في Poly Haven: Diffuse / nor_gl (أو Nor)
        albedo = pick_jpg(files, "Diffuse")
        normal = pick_jpg(files, "nor_gl") or pick_jpg(files, "Nor")

        entry = {"key": key, "asset": asset, "license": "CC0",
                 "source": f"https://polyhaven.com/a/{asset}"}

        if albedo:
            s, _ = download(albedo, os.path.join(out_dir, f"{key}_albedo.jpg"))
            total += s
            entry["albedo"] = s
            ok += 1
        if normal:
            s, _ = download(normal, os.path.join(out_dir, f"{key}_normal.jpg"))
            total += s
            entry["normal"] = s

        manifest.append(entry)
        print(f"+ {key:14s} ← {asset:22s} albedo={'✓' if albedo else '✗'} normal={'✓' if normal else '✗'}")

    with open(os.path.join(RES, "TEXTURE_MANIFEST.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)

    print(f"\n✓ {ok} خامة · إجمالي {total/1024/1024:.1f} MB")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
