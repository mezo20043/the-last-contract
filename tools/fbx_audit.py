#!/usr/bin/env python3
"""قارئ FBX ثنائي (7400) لقراءة فقط — لاستخراج بيانات الـrig والخامات والأبعاد.
لا بيعدّل أي ملف."""
import struct, sys, zlib, json, math

class Node:
    __slots__ = ("name", "props", "children")
    def __init__(self, name, props):
        self.name = name; self.props = props; self.children = []
    def find(self, name):
        return [c for c in self.children if c.name == name]
    def first(self, name):
        r = self.find(name)
        return r[0] if r else None
    def __repr__(self):
        return f"<{self.name} props={len(self.props)} children={len(self.children)}>"

def read_props(buf, off, n):
    props = []
    for _ in range(n):
        t = buf[off:off+1]; off += 1
        if t == b'Y': v, = struct.unpack_from('<h', buf, off); off += 2
        elif t == b'C': v = buf[off] != 0; off += 1
        elif t == b'I': v, = struct.unpack_from('<i', buf, off); off += 4
        elif t == b'F': v, = struct.unpack_from('<f', buf, off); off += 4
        elif t == b'D': v, = struct.unpack_from('<d', buf, off); off += 8
        elif t == b'L': v, = struct.unpack_from('<q', buf, off); off += 8
        elif t in b'S':
            n2, = struct.unpack_from('<I', buf, off); off += 4
            v = buf[off:off+n2].decode('utf-8', 'replace'); off += n2
        elif t == b'R':
            n2, = struct.unpack_from('<I', buf, off); off += 4
            v = f"<raw {n2}B>"; off += n2
        elif t in b'fdlib':
            alen, enc, clen = struct.unpack_from('<III', buf, off); off += 12
            data = buf[off:off+clen]; off += clen
            fmt = {'f': 'f', 'd': 'd', 'l': 'q', 'i': 'i', 'b': 'b'}[t.decode()]
            if enc == 1:
                try: data = zlib.decompress(data)
                except Exception: pass
            cnt = alen
            try: v = struct.unpack('<' + fmt*cnt, data[:cnt*struct.calcsize(fmt)])
            except Exception: v = ()
        else:
            raise ValueError(f"نوع خاصية غير معروف {t!r} عند {off}")
        props.append(v)
    return props, off

def parse(buf, off, wide):
    nodes = []
    while True:
        if wide:
            end, nprops, plen = struct.unpack_from('<QQQ', buf, off); off += 24
            nl = buf[off]; off += 1
        else:
            end, nprops, plen = struct.unpack_from('<III', buf, off); off += 12
            nl = buf[off]; off += 1
        if end == 0:
            return nodes, off
        name = buf[off:off+nl].decode('ascii', 'replace'); off += nl
        props, off = read_props(buf, off, nprops)
        nd = Node(name, props)
        if off < end:
            nd.children, off = parse(buf, off, wide)
        nodes.append(nd)
        if off >= end:
            continue

def load(path):
    with open(path, 'rb') as f:
        buf = f.read()
    assert buf[:21] == b'Kaydara FBX Binary  \x00', "مش FBX ثنائي"
    ver, = struct.unpack_from('<I', buf, 23)
    wide = ver >= 7500
    nodes, _ = parse(buf, 27, wide)
    return ver, nodes, buf

def props70(node):
    """يرجّع dict من Properties70"""
    out = {}
    p70 = node.first("Properties70") if node else None
    if not p70: return out
    for p in p70.children:
        if p.name == "P" and p.props:
            name = p.props[0]
            vals = p.props[4:] if len(p.props) > 4 else ()
            out[name] = vals
    return out

def flat(nodes, name):
    res = []
    for n in nodes:
        if n.name == name: res.append(n)
        res.extend(flat(n.children, name))
    return res

def analyze(path):
    ver, nodes, buf = load(path)
    r = {"file": path, "version": ver, "size": len(buf)}
    gs = None
    for n in nodes:
        if n.name == "GlobalSettings": gs = n
    g = props70(gs) if gs else {}
    r["up_axis"] = g.get("UpAxis", ("?",))[0]
    r["up_sign"] = g.get("UpAxisSign", ("?",))[0]
    r["front_axis"] = g.get("FrontAxis", ("?",))[0]
    r["coord_axis"] = g.get("CoordAxis", ("?",))[0]
    r["unit_scale"] = g.get("UnitScaleFactor", ("?",))[0]
    r["orig_unit"] = g.get("OriginalUnitScaleFactor", ("?",))[0]

    objects = None
    for n in nodes:
        if n.name == "Objects": objects = n
    models, geoms, deformers, mats, texs, vids, stacks = [], [], [], [], [], [], []
    if objects:
        for o in objects.children:
            if o.name != "Model": continue
            if len(o.props) < 3: continue
            uid, name, typ = o.props[0], str(o.props[1]).split("\x00")[0], o.props[2]
            p = props70(o)
            models.append({"id": uid, "name": name, "type": typ,
                           "lcl_t": p.get("Lcl Translation"), "lcl_r": p.get("Lcl Rotation"),
                           "lcl_s": p.get("Lcl Scaling"), "pre_rot": p.get("PreRotation")})
        geoms = [o for o in objects.children if o.name == "Geometry"]
        deformers = [o for o in objects.children if o.name == "Deformer"]
        mats = [o for o in objects.children if o.name == "Material"]
        texs = [o for o in objects.children if o.name == "Texture"]
        vids = [o for o in objects.children if o.name == "Video"]
        stacks = [o for o in objects.children if o.name == "AnimationStack"]

    # الوصلات
    conns = []
    for n in nodes:
        if n.name == "Connections":
            for c in n.children:
                if c.name == "C" and len(c.props) >= 3:
                    conns.append((c.props[0], c.props[1], c.props[2]))
    child2parent, parent2children = {}, {}
    for kind, a, b in conns:
        child2parent.setdefault(a, []).append(b)
        parent2children.setdefault(b, []).append(a)

    bones = [m for m in models if m["type"] == "LimbNode"]
    meshes = [m for m in models if m["type"] == "Mesh"]
    roots = [b for b in bones if not any(p in [x["id"] for x in bones] for p in child2parent.get(b["id"], []))]
    r["models"], r["bones"], r["meshes"] = len(models), len(bones), len(meshes)
    r["bone_names"] = [b["name"] for b in bones]
    r["root_bones"] = [b["name"] for b in roots]
    r["root_bone_transform"] = [{"name": b["name"], "t": b["lcl_t"], "r": b["lcl_r"], "s": b["lcl_s"]} for b in roots]
    r["mesh_names"] = [m["name"] for m in meshes]
    r["mesh_transforms"] = [{"name": m["name"], "t": m["lcl_t"], "r": m["lcl_r"], "s": m["lcl_s"], "pre_rot": m["pre_rot"]} for m in meshes[:6]]

    # الجلد
    skins = [d for d in deformers if len(d.props) > 2 and d.props[2] == "Skin"]
    clusters = [d for d in deformers if len(d.props) > 2 and d.props[2] == "Cluster"]
    r["skins"], r["clusters"] = len(skins), len(clusters)

    # أبعاد الميش (Vertices)
    lo = [1e30]*3; hi = [-1e30]*3; vtot = 0; poly = 0
    for g_ in geoms:
        v = g_.first("Vertices")
        if v and v.props and isinstance(v.props[0], tuple):
            arr = v.props[0]; vtot += len(arr)//3
            for i in range(0, len(arr), 3):
                for k in range(3):
                    if arr[i+k] < lo[k]: lo[k] = arr[i+k]
                    if arr[i+k] > hi[k]: hi[k] = arr[i+k]
        pvi = g_.first("PolygonVertexIndex")
        if pvi and pvi.props and isinstance(pvi.props[0], tuple):
            arr = pvi.props[0]
            poly += sum(1 for x in arr if x < 0)
        uv = g_.first("LayerElementUV")
        if uv: r["has_uv"] = True
    r["vertices"] = vtot; r["polygons"] = poly
    r["bbox_min"] = [round(x, 2) for x in lo] if vtot else None
    r["bbox_max"] = [round(x, 2) for x in hi] if vtot else None
    if vtot:
        r["dims_units"] = [round(hi[k]-lo[k], 2) for k in range(3)]

    # خامات/صور
    def s(props, idx):
        return str(props[idx]).split("\x00")[0] if len(props) > idx else None
    r["materials"] = [s(m.props, 1) for m in mats]
    r["textures"] = [{"name": s(t.props, 1),
                      "rel": (t.first("RelativeFilename").props[0] if t.first("RelativeFilename") else None),
                      "abs": (t.first("FileName").props[0] if t.first("FileName") else None)}
                     for t in texs]
    r["videos"] = [{"name": s(v.props, 1),
                    "rel": (v.first("RelativeFilename").props[0] if v.first("RelativeFilename") else None)}
                   for v in vids]
    # محتوى مضمّن؟
    r["embedded_videos"] = sum(1 for v in vids if v.first("Content") is not None)
    r["animation_stacks"] = len(stacks)
    r["stack_names"] = [ (x.props[1] if len(x.props)>1 else None) for x in stacks]
    tks = flat(nodes, "Take")
    r["takes"] = []
    for t in tks:
        nm = None; dur = None
        for ch in t.children:
            if ch.name == "Current" and ch.props: nm = ch.props[0]
            if ch.name == "LocalStop" and ch.props: dur = ch.props[0]
        r["takes"].append({"name": nm, "stop": dur})
    r["creators"] = []
    for c in flat(nodes, "Creator"):
        if c.props: r["creators"].append(str(c.props[0])[:60])
    return r

if __name__ == "__main__":
    for p in sys.argv[1:]:
        print(json.dumps(analyze(p), ensure_ascii=False, indent=1, default=str))
