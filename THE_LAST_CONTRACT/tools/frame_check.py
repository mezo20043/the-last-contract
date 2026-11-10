#!/usr/bin/env python3
"""
فحص تأطير الكاميرا — حساب هندسي بحت على `Play.unity`.

السؤال اللي بيجاوب عليه: **لو اللعبة اشتغلت دلوقتي، آدم هيبان في الكادر ولا لأ؟**

بيقرأ من المشهد: موضع وزاوية كاميرا اللعب + الـ FOV + near/far + موضع آدم.
وبيقرأ من `ThirdPersonCameraRig.cs` القيم الافتراضية اللي الكاميرا بتستعملها وقت
التشغيل (heightOffset / distance / shoulderOffset / pitch الابتدائي) — لأن
الكاميرا بتحسب موضعها بنفسها في `Awake/Start` وبتتجاهل الموضع المكتوب في المشهد.

⚠️ ده **مش** اختبار بصري. ده حساب NDC/فرستُم من أرقام المشهد.
   لو طلع «آدم بره الكادر» يبقى فيه باج حقيقي، ولو طلع جوه يبقى ده شرط لازم
   ومش كافٍ — الشكل نفسه لسه محتاج تشغيل.
"""
import math
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCENE = os.path.join(ROOT, "Assets", "_Project", "Scenes", "Play.unity")
RIG = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Camera", "ThirdPersonCameraRig.cs")
OUT = os.path.join(ROOT, "VERIFICATION", "frame-check.txt")

ADAM_HEIGHT = 1.80     # من VERIFICATION/fbx-summary.txt
ADAM_HALF_WIDTH = 0.22


# ───────── قراءة المشهد ─────────
def parse_blocks(text):
    blocks = {}
    for m in re.finditer(r"^--- !u!(\d+) &(\d+)[^\n]*\n(.*?)(?=^--- |\Z)", text,
                         re.S | re.M):
        cls, fid, body = int(m.group(1)), int(m.group(2)), m.group(3)
        blocks[fid] = (cls, body)
    return blocks


def vec(body, key):
    m = re.search(key + r":\s*\{([^}]*)\}", body)
    if not m:
        return None
    d = {k.strip(): v for k, v in (kv.split(":") for kv in m.group(1).split(","))}
    return tuple(float(d[k]) for k in ("x", "y", "z"))


def quat(body, key):
    m = re.search(key + r":\s*\{([^}]*)\}", body)
    if not m:
        return None
    d = {k.strip(): v for k, v in (kv.split(":") for kv in m.group(1).split(","))}
    return tuple(float(d[k]) for k in ("x", "y", "z", "w"))


def quat_mul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
           aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def quat_rotate(q, v):
    x, y, z, w = q
    vx, vy, vz = v
    # v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + w*q.xyz)
    cx = y * vz - z * vy
    cY = z * vx - x * vz
    cz = x * vy - y * vx
    cx += w * x
    cY += w * y
    cz += w * z
    return (vx + 2 * (y * cz - z * cY),
            vy + 2 * (z * cx - x * cz),
            vz + 2 * (x * cY - y * cx))


def scene_graph():
    text = open(SCENE, encoding="utf-8").read()
    blocks = parse_blocks(text)

    names = {}
    go_components = {}
    for fid, (cls, body) in blocks.items():
        if cls == 1:  # GameObject
            n = re.search(r"m_Name:\s*(.*)", body)
            if n:
                names[n.group(1).strip()] = fid
            comps = re.findall(r"component: \{fileID: (\d+)\}", body)
            go_components[fid] = [int(c) for c in comps]

    transforms = {}
    pos = {}
    rot = {}
    father = {}
    for fid, (cls, body) in blocks.items():
        if cls == 4:
            g = re.search(r"m_GameObject: \{fileID: (\d+)\}", body)
            f = re.search(r"m_Father: \{fileID: (\d+)\}", body)
            transforms[int(g.group(1))] = fid
            pos[fid] = vec(body, "m_LocalPosition") or (0.0, 0.0, 0.0)
            rot[fid] = quat(body, "m_LocalRotation") or (0.0, 0.0, 0.0, 1.0)
            father[fid] = int(f.group(1)) if f else 0

    def world(go_id):
        tf = transforms[go_id]
        p = pos[tf]
        q = rot[tf]
        seen = set()
        while father[tf] and father[tf] in pos and tf not in seen:
            seen.add(tf)
            tf = father[tf]
            p = tuple(a + b for a, b in zip(quat_rotate(rot[tf], p), pos[tf]))
            q = quat_mul(rot[tf], q)
        return p, q

    cam_go = names.get("Main Camera")
    adam_go = names.get("ADAM_MANSOUR")
    if cam_go is None or adam_go is None:
        raise SystemExit("✗ مش لاقي Main Camera أو ADAM_MANSOUR في Play.unity")

    cam_pos, cam_rot = world(cam_go)
    adam_pos, adam_rot = world(adam_go)

    fov = near = far = None
    for c in go_components.get(cam_go, []):
        cls, body = blocks[c]
        if cls == 20:
            fov = float(re.search(r"field of view:\s*([\d.]+)", body).group(1))
            near = float(re.search(r"near clip plane:\s*([\d.]+)", body).group(1))
            far = float(re.search(r"far clip plane:\s*([\d.]+)", body).group(1))

    return dict(cam_pos=cam_pos, cam_rot=cam_rot, adam_pos=adam_pos,
                adam_yaw=math.degrees(2 * math.atan2(adam_rot[1], adam_rot[3])),
                fov=fov, near=near, far=far)


# ───────── إعدادات الـ rig من الكود ─────────
def rig_defaults():
    src = open(RIG, encoding="utf-8").read()
    def num(name, default):
        m = re.search(r"float " + name + r"\s*=\s*([-\d.]+)f", src)
        return float(m.group(1)) if m else default
    pitch = 8.0
    m = re.search(r"_pitch\s*=\s*([-\d.]+)f", src)
    if m:
        pitch = float(m.group(1))
    return dict(height=num("heightOffset", 1.55), distance=num("distance", 3.1),
                shoulder=num("shoulderOffset", 0.42), pitch=pitch)


def rig_pose(adam_pos, adam_yaw, rig):
    """نفس حساب ApplyTransform: pivot + shoulder + rot*back*distance"""
    yaw, pitch = math.radians(adam_yaw), math.radians(rig["pitch"])
    fwd = (math.cos(pitch) * math.sin(yaw), -math.sin(pitch), math.cos(pitch) * math.cos(yaw))
    right = (math.cos(yaw), 0.0, -math.sin(yaw))
    up = (math.sin(pitch) * math.sin(yaw), math.cos(pitch), math.sin(pitch) * math.cos(yaw))
    pivot = (adam_pos[0], adam_pos[1] + rig["height"], adam_pos[2])
    cam = tuple(pivot[i] + right[i] * rig["shoulder"] - fwd[i] * rig["distance"] for i in range(3))
    return cam, fwd, right, up


def project(p, cam, fwd, right, up, vfov_deg, aspect):
    d = tuple(p[i] - cam[i] for i in range(3))
    xc = sum(d[i] * right[i] for i in range(3))
    yc = sum(d[i] * up[i] for i in range(3))
    zc = sum(d[i] * fwd[i] for i in range(3))
    if zc <= 1e-4:
        return None
    t = math.tan(math.radians(vfov_deg) / 2)
    return xc / zc / (t * aspect), yc / zc / t, zc


def main():
    sc = scene_graph()
    rig = rig_defaults()
    cam, fwd, right, up = rig_pose(sc["adam_pos"], sc["adam_yaw"], rig)

    lines = []
    add = lines.append
    add("فحص تأطير الكاميرا — حساب هندسي على Play.unity (مش لقطة ومش تشغيل)")
    add("=" * 62)
    add(f"كاميرا المشهد (المكتوبة)   : {sc['cam_pos'][0]:.2f}, {sc['cam_pos'][1]:.2f}, {sc['cam_pos'][2]:.2f}")
    add(f"كاميرا الـrig (وقت التشغيل): {cam[0]:.2f}, {cam[1]:.2f}, {cam[2]:.2f}")
    add(f"FOV رأسي {sc['fov']:.0f}° · near {sc['near']} · far {sc['far']}")
    add(f"إعدادات الـrig: height {rig['height']} · distance {rig['distance']} · shoulder {rig['shoulder']} · pitch {rig['pitch']}")
    add(f"آدم عند {sc['adam_pos'][0]:.2f}, {sc['adam_pos'][1]:.2f}, {sc['adam_pos'][2]:.2f} · اتجاه {sc['adam_yaw']:.0f}°")
    add("")

    delta = math.dist(sc["cam_pos"], cam)
    add(f"الفرق بين كاميرا المشهد وكاميرا التشغيل: {delta:.2f} م "
        f"({'متوافق — المشهد بيمثّل الوضع الحقيقي' if delta < 0.6 else '⚠️ بعيد — المشهد مش بيمثّل التشغيل'})")
    add("")

    ok = True
    pts = [("قدمين", 0.00), ("ركبة", 0.45), ("حوض", 0.95), ("صدر", 1.35),
           ("راس", 1.72), ("قمة الراس", ADAM_HEIGHT)]
    for label, aspect_name in ((0.4615, "موبايل طولي 9:19.5"), (2.166, "موبايل عرضي 19.5:9")):
        add(f"── {aspect_name} (aspect {label:.2f}) ──")
        ys = []
        for nm, h in pts:
            p = (sc["adam_pos"][0], sc["adam_pos"][1] + h, sc["adam_pos"][2])
            r = project(p, cam, fwd, right, up, sc["fov"], label)
            if r is None:
                add(f"  {nm:<10} ورا الكاميرا ✗")
                ok = False
                continue
            nx, ny, zc = r
            inside = abs(nx) <= 1 and abs(ny) <= 1
            ys.append(ny)
            add(f"  {nm:<10} NDC x={nx:+.2f} y={ny:+.2f} · بعد {zc:.2f} م · {'داخل الكادر ✓' if inside else 'بره الكادر ✗'}")
            if not inside:
                ok = False
        if ys:
            cover = (max(ys) - min(ys)) / 2 * 100
            add(f"  طول آدم في الكادر: {min(cover, 100):.0f}% من ارتفاع الشاشة")
            add(f"  التغطية فوق بعضها: {'آدم مقطوع' if min(ys) < -1 or max(ys) > 1 else 'آدم كامل في الكادر'}")
        add("")

    # مسافة/حجب
    add("فحوصات إضافية:")
    add(f"  {'.' if sc['near'] < 0.2 else '✗'} near clip {sc['near']} < 0.2 (مفيش قص للأجزاء القريبة)")
    add(f"  {'.' if sc['fov'] in (48.0, 50.0) or 40 <= sc['fov'] <= 65 else '✗'} FOV رأسي {sc['fov']:.0f}° في المدى المريح 40–65")
    dot = sum((sc["adam_pos"][i] - cam[i]) * fwd[i] for i in range(3))
    add(f"  {'.' if dot > 0 else '✗'} آدم قدام الكاميرا (dot={dot:.2f} م)")

    txt = "\n".join(lines)
    print(txt)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    open(OUT, "w", encoding="utf-8").write(txt + "\n")
    print(f"\n✓ {OUT}")
    print("النتيجة: " + ("آدم في الكادر من كل النقاط — شرط لازم مش كافٍ." if ok else "⚠️ فيه نقط بره الكادر."))
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
