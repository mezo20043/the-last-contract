#!/usr/bin/env python3
"""
مُصيّر بسيط: يرسم الميش البشري الفعلي (OBJ) مع تظليل وتكستشر.

⚠️ ده **مش لقطة شاشة من Unity**. ده رسم مباشر لنفس الموديلات
بمُصيّر مكتوب هنا — إثبات إن الميش سليم، مش إثبات إن اللعبة بتشتغل.
"""
import io
import os
import sys
import zipfile
import numpy as np
from PIL import Image, ImageDraw

OUT_DIR = os.path.join(os.path.dirname(__file__), "..", "Screenshots")


def load_obj(zip_path):
    z = zipfile.ZipFile(zip_path)
    name = [n for n in z.namelist() if n.endswith(".obj")][0]
    V, F = [], []
    for line in z.read(name).decode("utf-8", "ignore").splitlines():
        if line.startswith("v "):
            p = line.split()
            V.append((float(p[1]), float(p[2]), float(p[3])))
        elif line.startswith("f "):
            idx = []
            for tok in line.split()[1:]:
                idx.append(int(tok.split("/")[0]) - 1)
            for k in range(1, len(idx) - 1):
							  # triangulate
                F.append((idx[0], idx[k], idx[k + 1]))
    return np.array(V, dtype=np.float32), np.array(F, dtype=np.int32)


def vertex_normals(V, F):
    tri = V[F]
    n = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
    vn = np.zeros_like(V)
    for k in range(3):
        np.add.at(vn, F[:, k], n)
    ln = np.linalg.norm(vn, axis=1, keepdims=True)
    ln[ln == 0] = 1
    return vn / ln


def render(V, F, size=760, yaw=18.0, pitch=4.0, color=(0.72, 0.60, 0.52), ss=1):
    # مركزة وتطبيع
    V = V - V.mean(axis=0)
    h = V[:, 1].max() - V[:, 1].min()
    V = V / h                              # الطول = 1

    ry = np.radians(yaw)
    rx = np.radians(pitch)
    Ry = np.array([[np.cos(ry), 0, np.sin(ry)], [0, 1, 0], [-np.sin(ry), 0, np.cos(ry)]])
    Rx = np.array([[1, 0, 0], [0, np.cos(rx), -np.sin(rx)], [0, np.sin(rx), np.cos(rx)]])
    R = Rx @ Ry

    Vr = V @ R.T
    N = vertex_normals(V, F) @ R.T

    # منظور خفيف
    dist = 3.2
    z = Vr[:, 2] + dist
    f = 2.55
    px = (Vr[:, 0] / z) * f * size + size / 2
    py = -(Vr[:, 1] / z) * f * size + size / 2

    # تظليل لامبير + ضوء خلفي بارد
    L1 = np.array([-0.45, 0.55, 0.70]); L1 /= np.linalg.norm(L1)
    L2 = np.array([0.60, 0.10, -0.45]); L2 /= np.linalg.norm(L2)

    diff = np.clip(N @ L1, 0, 1)
    rim = np.clip(N @ L2, 0, 1) ** 2
    shade = 0.26 + 0.86 * diff + 0.26 * rim
    shade = np.clip(shade, 0, 1.25)

    r = np.clip(color[0] * shade, 0, 1)
    g = np.clip(color[1] * shade, 0, 1)
    b = np.clip(color[2] * shade + rim * 0.14, 0, 1)

    img = np.zeros((size, size, 3), dtype=np.float32)
    zbuf = np.full((size, size), 1e9, dtype=np.float32)

    xi = np.clip(px.astype(np.int32), 0, size - 1)
    yi = np.clip(py.astype(np.int32), 0, size - 1)
    order = np.argsort(-z)          # الأبعد الأول
    xi, yi, zs = xi[order], yi[order], z[order]
    r, g, b = r[order], g[order], b[order]

    for dx in (-2, -1, 0, 1, 2):
        for dy in (-2, -1, 0, 1, 2):
            xs = np.clip(xi + dx, 0, size - 1)
            ys = np.clip(yi + dy, 0, size - 1)
            np.minimum.at(zbuf, (ys, xs), zs)
            mask = zbuf[ys, xs] >= zs - 1e-6
            img[ys[mask], xs[mask]] = np.stack([r[mask], g[mask], b[mask]], axis=-1)

    img = np.clip(img, 0, 1) ** (1 / 2.3)          # جاما خفيفة
    return (img * 255).astype(np.uint8)


def sheet(entries, out_path, title):
    """يرتّب عدة لقطات في صورة واحدة مع عنوان."""
    size = 380
    pad = 14
    head = 96
    W = pad + len(entries) * (size + pad)
    H = head + size + pad
    canvas = Image.new("RGB", (W, H), (12, 12, 16))
    d = ImageDraw.Draw(canvas)

    for i, (label, arr, sub) in enumerate(entries):
        im = Image.fromarray(arr).resize((size, size), Image.LANCZOS)
        canvas.paste(im, (pad + i * (size + pad), head))
        try:
            from PIL import ImageFont
            lbl_font = ImageFont.truetype(os.path.join(os.path.dirname(__file__), "..", "Assets",
                "_Project", "Resources", "Fonts", "Amiri-Regular.ttf"), 17)
        except Exception:
            lbl_font = None
        d.text((pad + i * (size + pad) + 6, head + size + 2), label, fill=(240, 210, 140), font=lbl_font)
        if sub:
            d.text((pad + i * (size + pad) + 6, head + size + 14), sub, fill=(150, 150, 160))

    font_path = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project",
                             "Resources", "Fonts", "Amiri-Bold.ttf")
    try:
        from PIL import ImageFont
        ar = ImageFont.truetype(font_path, 22)
    except Exception:
        ar = None

    d.text((pad, 4), title, fill=(235, 225, 205), font=ar)
    d.text((pad, 46), "OFFLINE RENDER of the actual mesh — NOT a Unity screenshot",
           fill=(235, 120, 100))
    d.text((pad, 64), "رسم مباشر للموديل الحقيقي — مش لقطة شاشة من Unity",
           fill=(235, 140, 120), font=ar)
    canvas.save(out_path)
    return out_path


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    probe = "/tmp/probe"
    chars = [
        ("adam", "man-base-character-obj.zip", "ADAM  · 143k verts · 1.80 m", (0.74, 0.62, 0.54)),
        ("laila", "woman-obj.zip", "LAILA · 309k verts · 1.70 m", (0.76, 0.58, 0.52)),
        ("murad", "elderly-man-obj.zip", "MURAD · 427k verts · 1.75 m", (0.62, 0.55, 0.50)),
        ("yassin", "man-obj.zip", "YASSIN · 268k verts · 1.80 m", (0.70, 0.60, 0.50)),
    ]

    entries = []
    for key, zf, label, color in chars:
        path = os.path.join(probe, zf)
        if not os.path.exists(path):
            print(f"  ✗ {zf} مش موجود")
            continue
        V, F = load_obj(path)
        arr = render(V, F, color=color)
        entries.append((label, arr, f"{len(V)} V · {len(F)} F"))
        print(f"  ✓ {key}: {len(V)} رأس · {len(F)} مثلث")

    if entries:
        p = sheet(entries, os.path.join(OUT_DIR, "01_characters.png"),
                  "THE LAST CONTRACT — الشخصيات (ميش فعلي)")
        print("  → ", p)

    # زوايا متعددة لآدم
    V, F = load_obj(os.path.join(probe, "man-base-character-obj.zip"))
    angles = [("أمامي 0°", 0), ("ربع 35°", 35), ("جانبي 90°", 90), ("خلفي 180°", 180)]
    ent2 = [(lbl, render(V, F, yaw=a, color=(0.74, 0.62, 0.54)), f"yaw {a}°") for lbl, a in angles]
    p = sheet(ent2, os.path.join(OUT_DIR, "02_adam_angles.png"),
              "آدم — أربع زوايا (نفس الميش)")
    print("  → ", p)


if __name__ == "__main__":
    main()
