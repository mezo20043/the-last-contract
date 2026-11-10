#!/usr/bin/env python3
"""
يرسم مسار الحركة اللي طلع من المحاكاة (VERIFICATION/movement-trace.csv)
كصورة SVG: منحنى السرعة + مسقط أفقي فوق مخطط الشقة.

⚠️ الصورة دي **مش** لقطة من Unity. هي رسم بياني لبيانات رقميّة
طلعِت من تشغيل `ThirdPersonController` الحقيقي بدون محرك (tools/sim).
"""
import csv
import os
import sys

# matplotlib 3.11 بيتعامل مع تشكيل العربي واتجاهه بنفسه،
# فأي reshaper/bidi فوقه بيقلب النص. بنمرّر النص زي ما هو.
def ar(t):
    return t


ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CSV = os.path.join(ROOT, "VERIFICATION", "movement-trace.csv")
OUT = os.path.join(ROOT, "Screenshots", "03_movement_trace.svg")

# أبعاد الشقة من World/ApartmentBuilder.cs
ROOMS = [
    ("الصالة 8×6", -4, -3, 8, 6),
    ("أوضة النوم 5×4", 4, -3, 5, 4),
    ("المطبخ 4×3.5", -8, -2.95, 4, 3.5),
]
SPAWN = (-0.6, -1.4)
WALLS = [(-4, -3, 4, -3), (-4, 3, 4, 3), (-4, -3, -4, 3), (4, -3, 4, 3)]

W, H = 1180, 720
PAD = 40


def load():
    rows = []
    with open(CSV, encoding="utf-8-sig") as fh:
        for r in csv.DictReader(fh):
            rows.append({
                "t": float(r["t"]), "x": float(r["x"]), "z": float(r["z"]),
                "v": float(r["speed"]), "run": r["isRunning"] == "1",
                "phase": r["phase"],
            })
    return rows


def main():
    rows = load()
    if not rows:
        raise SystemExit("الملف فاضي")

    tmax = max(r["t"] for r in rows) or 1
    vmax = max(r["v"] for r in rows) or 1
    vscale = max(4.5, vmax * 1.15)

    # ── لوحة ١: السرعة ──
    cw, ch = 520, 300
    cx, cy = PAD, 90

    def sx(t):  return cx + t / tmax * cw
    def sy(v):  return cy + ch - v / vscale * ch

    speed_path = " ".join(
        f"{'M' if i == 0 else 'L'}{sx(r['t']):.1f},{sy(r['v']):.1f}"
        for i, r in enumerate(rows)
    )

    phase_bands = []
    prev, start = None, 0.0
    for r in rows:
        if r["phase"] != prev:
            if prev is not None:
                phase_bands.append((prev, start, r["t"]))
            prev, start = r["phase"], r["t"]
    if prev is not None:
        phase_bands.append((prev, start, tmax))

    band_colors = {"سكون": "#2b3240", "مشي": "#3b4a63", "جري": "#5c4a2a",
                   "وقف": "#2b3240", "حوار مفتوح": "#4a2b33"}
    bands = "".join(
        f'<rect x="{sx(a):.1f}" y="{cy}" width="{max(1, sx(b)-sx(a)):.1f}" height="{ch}" '
        f'fill="{band_colors.get(p, "#2b3240")}" opacity="0.55"/>'
        for p, a, b in phase_bands
    )
    bands += "".join(
        f'<text x="{(sx(a)+sx(b))/2:.0f}" y="{cy-8}" fill="#9aa4b2" font-size="13" '
        f'text-anchor="middle">{p}</text>'
        for p, a, b in phase_bands
    )

    grid = ""
    for v in (0, 1, 2, 3, 4):
        if v > vscale:
            continue
        y = sy(v)
        grid += (f'<line x1="{cx}" y1="{y:.1f}" x2="{cx+cw}" y2="{y:.1f}" '
                 f'stroke="#39414f" stroke-width="1" opacity="0.6"/>'
                 f'<text x="{cx-10}" y="{y+4:.1f}" fill="#7d8695" font-size="12" '
                 f'text-anchor="end">{v}</text>')

    # ── لوحة ٢: المسقط الأفقي ──
    px, py, pw, ph = 640, 90, 460, 420
    world_min_x, world_max_x = -8.6, 7.4
    world_min_z, world_max_z = -3.6, 3.6

    def wx(x): return px + (x - world_min_x) / (world_max_x - world_min_x) * pw
    def wz(z): return py + ph - (z - world_min_z) / (world_max_z - world_min_z) * ph

    rooms = "".join(
        f'<rect x="{wx(x):.1f}" y="{wz(z+h):.1f}" width="{w/(world_max_x-world_min_x)*pw:.1f}" '
        f'height="{h/(world_max_z-world_min_z)*ph:.1f}" fill="#1c2129" stroke="#39414f" stroke-width="1.5"/>'
        f'<text x="{wx(x+w/2):.0f}" y="{wz(z+h/2):.0f}" fill="#6d7684" font-size="13" '
        f'text-anchor="middle">{label}</text>'
        for label, x, z, w, h in ROOMS
    )
    walls = "".join(
        f'<line x1="{wx(a):.1f}" y1="{wz(b):.1f}" x2="{wx(c):.1f}" y2="{wz(d):.1f}" '
        f'stroke="#8a6a3a" stroke-width="3"/>'
        for a, b, c, d in WALLS
    )

    # المسار داخل حدود الشقة بس — اللي برّا (مفيش تصادم في المحاكاة) بيتقص ويتعلّم عليه
    inside = [r for r in rows if world_min_x <= r["x"] <= world_max_x and world_min_z <= r["z"] <= world_max_z]
    exit_row = next((r for r in rows if r not in inside), None)

    path_pts = " ".join(f"{wx(r['x']):.1f},{wz(r['z']):.1f}" for r in inside)
    run_pts = " ".join(f"{wx(r['x']):.1f},{wz(r['z']):.1f}" for r in inside if r["run"])
    exit_mark = ""
    if exit_row is not None:
        exit_mark = (
            f'<circle cx="{wx(min(exit_row["x"], world_max_x)):.1f}" cy="{wz(min(exit_row["z"], world_max_z)):.1f}" '
            f'r="5" fill="none" stroke="#b0806a" stroke-width="2"/>'
            f'<text x="{px + pw - 6:.0f}" y="{py + 18:.0f}" fill="#b0806a" font-size="12" text-anchor="end">'
            f'خرج من حدود الشقة عند t={exit_row["t"]:.1f} ث</text>'
        )

    start_pt = rows[0]
    end_pt = rows[-1]

    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">
<rect width="{W}" height="{H}" fill="#12161c"/>
<text x="{PAD}" y="40" fill="#e8e3da" font-size="24" font-family="sans-serif">حركة آدم — محاكاة بكود المشروع (مش لقطة Unity)</text>
<text x="{PAD}" y="64" fill="#8a94a3" font-size="14" font-family="sans-serif">ThirdPersonController · PlayerInputReader · 600 فريم @ 60fps · من نقطة ظهور Play.unity</text>

<text x="{cx}" y="{cy+ch+30}" fill="#c9d1dc" font-size="15" font-family="sans-serif">السرعة (م/ث) مقابل الزمن (ث)</text>
<text x="{px}" y="{cy+ch+30}" fill="#c9d1dc" font-size="15" font-family="sans-serif">المسقط الأفقي — مخطط الشقة بأبعادها الحقيقية</text>

<g font-family="sans-serif">
{bands}
{grid}
<path d="{speed_path}" fill="none" stroke="#d9a441" stroke-width="2.5"/>
<circle cx="{sx(start_pt['t']):.0f}" cy="{sy(start_pt['v']):.0f}" r="4" fill="#7fc8a9"/>
<circle cx="{sx(end_pt['t']):.0f}" cy="{sy(end_pt['v']):.0f}" r="4" fill="#d97a7a"/>

{rooms}
{walls}
<polyline points="{path_pts}" fill="none" stroke="#7fc8a9" stroke-width="2" opacity="0.8"/>
<polyline points="{run_pts}" fill="none" stroke="#d9a441" stroke-width="3"/>
<circle cx="{wx(SPAWN[0]):.1f}" cy="{wz(SPAWN[1]):.1f}" r="6" fill="#7fc8a9"/>
<text x="{wx(SPAWN[0])+10:.0f}" y="{wz(SPAWN[1])+4:.0f}" fill="#7fc8a9" font-size="13">نقطة ظهور آدم</text>

{exit_mark}

<text x="{px}" y="{py+ph+30}" fill="#8a94a3" font-size="13">الأخضر = مشي · الذهبي = جري · البني = حوائط الصالة</text>
<text x="{px}" y="{py+ph+52}" fill="#b0806a" font-size="13">⚠ مفيش تصادم في المحاكاة — المسار مقصوص عند حدود الشقة</text>
</g>
</svg>'''

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as fh:
        fh.write(svg)

    # ملخّص رقمي في الطرفية
    wall_cross = next((r for r in rows if r["z"] >= 3.0), None)
    print(f"✓ {OUT}")
    print(f"  فريمات: {len(rows)} · مدة {tmax:.1f} ث")
    print(f"  أعلى سرعة: {vmax:.2f} م/ث")
    print(f"  آخر موضع: x={end_pt['x']:.2f} z={end_pt['z']:.2f}")
    if wall_cross:
        print(f"  عدّى حد الصالة (z=3) عند t={wall_cross['t']:.2f} ث — متوقّع، مفيش تصادم في المحاكاة")


def render_png(rows):
    """نسخة PNG بنص عربي متشكّل صح (SVG بيتعامل مع العربي في المتصفح، PNG لأ)."""
    try:
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt
        from matplotlib import font_manager, patches
    except Exception as e:
        print(f"  (تخطّي PNG: {e})")
        return

    font_path = os.path.join(ROOT, "Assets", "_Project", "Resources", "Fonts", "Amiri-Regular.ttf")
    fp = font_manager.FontProperties(fname=font_path) if os.path.exists(font_path) else None

    bg = "#12161c"; fg = "#e8e3da"; muted = "#8a94a3"; gold = "#d9a441"; green = "#7fc8a9"

    fig = plt.figure(figsize=(13.5, 5.8), dpi=115, facecolor=bg)
    gs = fig.add_gridspec(1, 2, width_ratios=[1, 1.45], left=0.06, right=0.97,
                          top=0.80, bottom=0.13, wspace=0.18)

    fig.text(0.06, 0.93, ar("حركة آدم — تشغيل كود المشروع بدون Unity"),
             color=fg, fontsize=18, fontproperties=fp)
    fig.text(0.06, 0.885, "ThirdPersonController · PlayerInputReader · 600 frames @ 60fps",
             color=muted, fontsize=9)

    # ── لوحة السرعة ──
    ax = fig.add_subplot(gs[0])
    ax.set_facecolor(bg)
    bands = {"سكون": "#2b3240", "مشي": "#3b4a63", "جري": "#5c4a2a",
             "وقف": "#2b3240", "حوار مفتوح": "#4a2b33"}
    prev, start = None, 0.0
    for r in rows:
        if r["phase"] != prev:
            if prev is not None:
                ax.axvspan(start, r["t"], color=bands.get(prev, "#2b3240"), alpha=0.55, lw=0)
                ax.text((start + r["t"]) / 2, 4.75, ar(prev), color=muted,
                        fontsize=9, ha="center", fontproperties=fp)
            prev, start = r["phase"], r["t"]
    ax.axvspan(start, rows[-1]["t"], color=bands.get(prev, "#2b3240"), alpha=0.55, lw=0)
    ax.text((start + rows[-1]["t"]) / 2, 4.75, ar(prev), color=muted, fontsize=9,
            ha="center", fontproperties=fp)

    ax.plot([r["t"] for r in rows], [r["v"] for r in rows], color=gold, lw=2.4)
    ax.set_xlim(0, rows[-1]["t"])
    ax.set_ylim(-0.15, 5.0)
    ax.set_yticks([0, 1, 2, 3, 4])
    ax.tick_params(colors=muted, labelsize=9)
    for sp in ax.spines.values():
        sp.set_color("#39414f")
    ax.grid(color="#39414f", alpha=0.5, lw=0.8)
    ax.set_title(ar("السرعة (م/ث) مقابل الزمن (ث)"), color="#c9d1dc",
                 fontsize=12, fontproperties=fp)

    # ── لوحة المخطط ──
    ax2 = fig.add_subplot(gs[1])
    ax2.set_facecolor(bg)
    ax2.set_xlim(-8.9, 9.3)
    ax2.set_ylim(-3.7, 3.7)
    ax2.set_aspect("equal")
    ax2.tick_params(colors=muted, labelsize=8)
    for sp in ax2.spines.values():
        sp.set_color("#39414f")

    for label, x, z, w, h in ROOMS:
        ax2.add_patch(patches.Rectangle((x, z), w, h, facecolor="#1c2129",
                                        edgecolor="#39414f", lw=1.5))
        ax2.text(x + w / 2, z + h - 0.5, ar(label), color="#6d7684",
                 fontsize=8.5, ha="center", va="top", fontproperties=fp)

    for a, b, c, d in WALLS:
        ax2.plot([a, c], [b, d], color="#8a6a3a", lw=3)

    inside = [r for r in rows if -8.6 <= r["x"] <= 7.4 and -3.6 <= r["z"] <= 3.6]
    ax2.plot([r["x"] for r in inside], [r["z"] for r in inside], color=green, lw=2, alpha=0.85)
    ax2.plot([r["x"] for r in inside if r["run"]], [r["z"] for r in inside if r["run"]],
             color=gold, lw=3)
    ax2.scatter([SPAWN[0]], [SPAWN[1]], s=60, color=green, zorder=5)
    ax2.annotate(ar("نقطة ظهور آدم"), SPAWN, textcoords="offset points", xytext=(8, -14),
                 color=green, fontsize=9, fontproperties=fp)

    exit_row = next((r for r in rows if r not in inside), None)
    if exit_row:
        ax2.axhline(3.0, color="#b0806a", lw=1, ls="--", alpha=0.8)
    ax2.set_title(ar("المسقط الأفقي — مخطط الشقة بأبعادها الحقيقية"),
                  color="#c9d1dc", fontsize=12, fontproperties=fp)

    exit_txt = ""
    if exit_row:
        exit_txt = ar(f" · خرج من حدود الشقة عند t={exit_row['t']:.1f} ث (مفيش تصادم في المحاكاة)")
    fig.text(0.06, 0.035,
             ar("المحاكاة فيها حركة وحسابات بس — مفيش تصادم ولا رندرة") + exit_txt,
             color="#b0806a", fontsize=8.5, fontproperties=fp)

    out = os.path.join(ROOT, "Screenshots", "03_movement_trace.png")
    fig.savefig(out, facecolor=bg)
    plt.close(fig)
    print(f"✓ {out}")


if __name__ == "__main__":
    main()
    _rows = load()
    render_png(_rows)
