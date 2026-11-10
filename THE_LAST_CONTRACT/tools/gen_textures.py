#!/usr/bin/env python3
"""
مولّد خامات PBR الأصلي لمشروع THE LAST CONTRACT.

كل تكستشر بيتولّد رياضياً (ضجيج طيفي دوري) — قابل للتكرار بلا خطوط،
ومعاه خريطة Normal مشتقة من خريطة الارتفاع.
الترخيص: ملك المشروع.

الاستخدام:  python3 tools/gen_textures.py
"""
import os
import json
import numpy as np
from PIL import Image

SIZE = 512
OUT = os.path.join(os.path.dirname(__file__), "..",
                   "Assets", "_Project", "Resources", "Art", "Textures")
SEED = 20261109
_registry = []


# ───────────────────── ضجيج طيفي دوري (قابل للتكرار) ─────────────────────
def spectral_noise(size, beta, seed, aniso=1.0):
    """ضجيج دوري تماماً — الحواف بتتلاقى بلا خط.
    aniso > 1 يمدّ الملمس أفقياً (ألياف الخشب، خطوط المعدن)."""
    rng = np.random.default_rng(seed)
    white = rng.normal(0, 1, (size, size))
    F = np.fft.fft2(white)
    fy = np.fft.fftfreq(size)[:, None] * aniso
    fx = np.fft.fftfreq(size)[None, :]
    f = np.sqrt(fx ** 2 + fy ** 2)
    f[0, 0] = 1e-6
    F *= 1.0 / (f ** beta)
    out = np.real(np.fft.ifft2(F))
    out -= out.min()
    m = out.max()
    return out / m if m > 1e-9 else out


def fbm(size, seed, octaves=5, base_beta=1.6, aniso=1.0):
    acc = np.zeros((size, size))
    amp = 1.0
    total = 0.0
    for o in range(octaves):
        acc += spectral_noise(size, base_beta - o * 0.22, seed + o * 977,
                              aniso=aniso) * amp
        total += amp
        amp *= 0.55
    acc /= total
    acc -= acc.min()
    m = acc.max()
    return acc / m if m > 1e-9 else acc


def normal_from_height(h, strength=2.0):
    """خريطة Normal من الارتفاع، بحساب التفاف دوري."""
    dx = (np.roll(h, -1, axis=1) - np.roll(h, 1, axis=1)) * strength
    dy = (np.roll(h, -1, axis=0) - np.roll(h, 1, axis=0)) * strength
    nz = np.ones_like(h)
    ln = np.sqrt(dx * dx + dy * dy + nz * nz)
    nx, ny, nz = -dx / ln, -dy / ln, nz / ln
    rgb = np.stack([(nx * 0.5 + 0.5), (ny * 0.5 + 0.5), (nz * 0.5 + 0.5)], axis=-1)
    return (np.clip(rgb, 0, 1) * 255).astype(np.uint8)


def tint(gray, color, contrast=1.0, brightness=1.0):
    g = np.clip((gray - 0.5) * contrast + 0.5, 0, 1) * brightness
    rgb = np.stack([g * color[0], g * color[1], g * color[2]], axis=-1)
    return (np.clip(rgb, 0, 1) * 255).astype(np.uint8)


def save(name, albedo, normal=None, folder=""):
    os.makedirs(os.path.join(OUT, folder), exist_ok=True)
    pa = os.path.join(OUT, folder, name + "_albedo.png")
    Image.fromarray(albedo, "RGB").save(pa, optimize=True)

    entry = {
        "material": name,
        "albedo": f"Art/Textures/{folder + '/' if folder else ''}{name}_albedo",
        "normal": None,
        "origin": "procedural — spectral noise synthesis, tools/gen_textures.py",
        "license": "Project-owned (CC0-equivalent, commercial use permitted)",
        "tileable": True,
        "size": SIZE,
    }

    if normal is not None:
        pn = os.path.join(OUT, folder, name + "_normal.jpg")
        Image.fromarray(normal, "RGB").save(pn, quality=92, optimize=True)
        entry["normal"] = f"Art/Textures/{folder + '/' if folder else ''}{name}_normal"

    _registry.append(entry)
    print(f"  ✓ {name:<16} albedo {os.path.getsize(pa)//1024:4d}KB"
          + (f" · normal {os.path.getsize(pn)//1024:3d}KB" if normal is not None else ""))


# ═══════════════════════════ الخامات ═══════════════════════════
def mat_wood_floor(seed, color, name):
    # ألياف أفقية: ضجيج ممدود + خطوط رفيعة قليلة التمويج
    grain = fbm(SIZE, seed, 5, 1.7, aniso=9.0)
    fibre = fbm(SIZE, seed + 5, 4, 2.4, aniso=24.0)
    lines = np.sin(np.linspace(0, 1, SIZE)[None, :] * 46 * np.pi
                   + grain * 2.2) * 0.5 + 0.5
    h = grain * 0.42 + lines * 0.22 + fibre * 0.36
    # فواصل الألواح كل 128 بكسل
    plank = (np.arange(SIZE) % 128) / 128.0
    seam = np.clip(np.minimum(plank, 1 - plank) / 0.02, 0, 1)
    h = h * seam
    tones = 0.86 + 0.14 * fbm(SIZE, seed + 31, 3, 2.4, aniso=6.0)
    alb = tint(h * 0.55 + 0.22, color, contrast=0.95) * tones[..., None]
    save(name, np.clip(alb, 0, 255).astype(np.uint8),
         normal_from_height(h, 3.0), "Interior")


def mat_plaster(seed, color):
    base = fbm(SIZE, seed, 6, 1.5)
    fine = fbm(SIZE, seed + 7, 4, 2.6)
    h = base * 0.7 + fine * 0.3
    stains = np.clip((fbm(SIZE, seed + 55, 3, 2.8) - 0.62) * 3.2, 0, 1)
    alb = tint(h, color, contrast=0.55, brightness=0.97)
    alb = (alb.astype(np.float32) * (1 - stains[..., None] * 0.22)).astype(np.uint8)
    save("plaster_wall", alb, normal_from_height(h, 1.1), "Interior")


def mat_concrete(seed, color):
    base = fbm(SIZE, seed, 6, 1.7)
    speck = fbm(SIZE, seed + 3, 3, 3.2)
    pores = (speck > 0.86).astype(np.float32)
    h = base * 0.8 + pores * 0.2
    alb = tint(h, color, contrast=1.25, brightness=0.98)
    alb = (alb.astype(np.float32) * (1 - pores[..., None] * 0.18)).astype(np.uint8)
    save("concrete", alb, normal_from_height(h, 2.2), "Exterior")


def mat_tile(seed, color):
    g = fbm(SIZE, seed, 5, 1.4)
    cell = 128
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    gx = (xx % cell) / cell
    gy = (yy % cell) / cell
    edge = np.minimum(np.minimum(gx, 1 - gx), np.minimum(gy, 1 - gy))
    grout = np.clip(edge / 0.035, 0, 1)
    h = grout * 0.85 + g * 0.15
    tile_tone = 0.94 + 0.06 * g
    alb = tint(h * 0.5 + 0.5, color, contrast=0.7) * tile_tone[..., None]
    alb = np.clip(alb, 0, 255).astype(np.uint8)
    save("tile_ceramic", alb, normal_from_height(h, 3.4), "Interior")


def mat_fabric(seed, color):
    g = fbm(SIZE, seed, 5, 2.0)
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    weave = (np.sin(xx * np.pi / 3.0) * 0.5 + 0.5) * (np.sin(yy * np.pi / 3.0) * 0.5 + 0.5)
    h = g * 0.45 + weave * 0.55
    alb = tint(h, color, contrast=1.5, brightness=1.05)
    save("fabric_dark", alb, normal_from_height(h, 1.6), "Interior")


def mat_metal(seed, color):
    rng = np.random.default_rng(seed)
    streak = rng.normal(0, 1, (SIZE, 1))
    streak = np.repeat(streak, SIZE, axis=1)
    streak = (streak - streak.min()) / (streak.max() - streak.min())
    g = fbm(SIZE, seed + 11, 4, 2.2, aniso=12.0)
    h = streak * 0.35 + g * 0.65
    alb = tint(h, color, contrast=0.55, brightness=1.0)
    save("metal_brushed", alb, normal_from_height(h, 1.4), "Interior")


def mat_city_facade(seed):
    """واجهة مبنى ليلي: شبابيك مضيئة/مطفية — بسيطة ومقروءة من بعيد."""
    yy, xx = np.mgrid[0:SIZE, 0:SIZE]
    cell_w, cell_h = 64, 96
    in_win = ((xx % cell_w) > 14) & ((xx % cell_w) < 50) & \
             ((yy % cell_h) > 20) & ((yy % cell_h) < 78)
    rng = np.random.default_rng(seed)
    lit_grid = rng.random((SIZE // cell_h + 1, SIZE // cell_w + 1)) > 0.62
    lit = np.zeros((SIZE, SIZE), dtype=bool)
    for cy in range(lit_grid.shape[0]):
        for cx in range(lit_grid.shape[1]):
            if lit_grid[cy, cx]:
                lit |= ((yy // cell_h) == cy) & ((xx // cell_w) == cx)
    window_on = in_win & lit
    g = fbm(SIZE, seed, 5, 1.8)

    alb = np.zeros((SIZE, SIZE, 3), np.float32)
    alb[..., 0] = 0.05 + g * 0.06
    alb[..., 1] = 0.055 + g * 0.065
    alb[..., 2] = 0.07 + g * 0.08
    warm = np.array([1.0, 0.82, 0.55])
    cool = np.array([0.72, 0.82, 1.0])
    alb[window_on] = np.where(rng.random() > 0.55, warm, cool) * 0.95
    alb = (np.clip(alb, 0, 1) * 255).astype(np.uint8)
    h = g * 0.4 + in_win.astype(np.float32) * 0.6
    save("city_facade", alb, normal_from_height(h, 2.0), "Exterior")


def main():
    print("═══ توليد الخامات ═══")
    print("── داخلية ──")
    mat_wood_floor(SEED + 1, (0.62, 0.44, 0.30), "wood_floor")
    mat_wood_floor(SEED + 2, (0.30, 0.20, 0.14), "wood_dark")
    mat_plaster(SEED + 10, (0.88, 0.85, 0.79))
    mat_tile(SEED + 20, (0.90, 0.89, 0.86))
    mat_fabric(SEED + 30, (0.30, 0.32, 0.38))
    mat_metal(SEED + 40, (0.72, 0.73, 0.76))
    print("── خارجية ──")
    mat_concrete(SEED + 50, (0.70, 0.70, 0.69))
    mat_city_facade(SEED + 60)

    with open(os.path.join(OUT, "texture_manifest.json"), "w", encoding="utf-8") as f:
        json.dump({"materials": _registry}, f, ensure_ascii=False, indent=2)

    total = sum(os.path.getsize(os.path.join(dp, fn))
                for dp, _, fns in os.walk(OUT) for fn in fns)
    print()
    print(f"✓ {len(_registry)} خامة · {total/1024/1024:.2f} ميجابايت")


if __name__ == "__main__":
    main()
