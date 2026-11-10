#!/usr/bin/env python3
"""
قارئ FBX ثنائي مبسّط — يستخرج:
  • هيكل العظام (الأب/الابن)
  • أسماء الأخذات (takes)
  • إحصاءات الميش
  • مسارات التكستشرات

الهدف: التحقق الفعلي من الهيكل قبل فتح Unity، بدل الافتراض.
"""
import struct
import zlib


class ArmArray(tuple):
    """بديل خفيف للمصفوفات الكبيرة: الطول + البيانات لو محتاجينها."""
    def __new__(cls, length, data):
        return super().__new__(cls, (length, data))
    @property
    def length(self):
        return self[0]


class Record:
    __slots__ = ("name", "props", "children", "offset")

    def __init__(self, name, props, offset):
        self.name = name
        self.props = props
        self.children = []
        self.offset = offset


def read_fbx(path):
    data = open(path, "rb").read()
    if not data.startswith(b"Kaydara FBX Binary"):
        raise ValueError("مش FBX ثنائي")

    version = struct.unpack("<I", data[23:27])[0]
    wide = version >= 7500
    pos = 27

    def read_props(p, count):
        props = []
        for _ in range(count):
            t = chr(data[p]); p += 1
            if t == "Y":
                props.append(struct.unpack("<h", data[p:p+2])[0]); p += 2
            elif t == "C":
                props.append(bool(data[p])); p += 1
            elif t == "I":
                props.append(struct.unpack("<i", data[p:p+4])[0]); p += 4
            elif t == "F":
                props.append(struct.unpack("<f", data[p:p+4])[0]); p += 4
            elif t == "D":
                props.append(struct.unpack("<d", data[p:p+8])[0]); p += 8
            elif t == "L":
                props.append(struct.unpack("<q", data[p:p+8])[0]); p += 8
            elif t in "fdlib":
                ln, enc, clen = struct.unpack("<III", data[p:p+12]); p += 12
                raw = data[p:p+clen]; p += clen
                if enc == 1:
                    raw = zlib.decompress(raw)
                if t == "b":
                    props.append(ArmArray(ln, raw))
                else:
                    props.append(ArmArray(ln, None))
            elif t in "SR":
                ln = struct.unpack("<I", data[p:p+4])[0]; p += 4
                val = data[p:p+ln]; p += ln
                props.append(val.decode("utf-8", "replace") if t == "S" else val)
            else:
                break
        return props, p

    def read_record(p):
        if wide:
            end, nprops, plen = struct.unpack("<QQQ", data[p:p+24])
            name_len = data[p+24]; p2 = p + 25
        else:
            end, nprops, plen = struct.unpack("<III", data[p:p+12])
            name_len = data[p+12]; p2 = p + 13

        if end == 0:
            return None, end

        name = data[p2:p2+name_len].decode("ascii", "replace")
        p2 += name_len
        props, p2 = read_props(p2, nprops)

        rec = Record(name, props, p)
        while p2 < end:
            child, nxt = read_record(p2)
            if child is None:
                break
            rec.children.append(child)
            p2 = nxt
        return rec, end

    # في FBX الثنائي مفيش "root" صريح — سلسلة سجلات لحد السجل الصفري
    fake_root = Record("", [], pos)
    p = pos
    while p < len(data) - 13:
        rec, nxt = read_record(p)
        if rec is None or nxt <= p:
            break
        fake_root.children.append(rec)
        p = nxt
    return fake_root, version


def walk(rec, name):
    if rec.name == name:
        yield rec
    for c in rec.children:
        yield from walk(c, name)


def analyze(path):
    root, version = read_fbx(path)

    models = {}
    for m in walk(root, "Model"):
        if len(m.props) < 3:
            continue
        uid, name, kind = m.props[0], m.props[1], m.props[2]
        models[uid] = {"name": name.split("\x00")[0], "type": kind}
        for c in m.children:
            if c.name == "Properties70":
                for p in c.children:
                    if len(p.props) >= 5 and p.props[0] == "Lcl Translation":
                        models[uid]["translation"] = p.props[4:7]

    # العلاقات: C: "OO", child, parent
    parents = {}
    for conn in walk(root, "Connections"):
        for c in conn.children:
            if c.name == "C" and len(c.props) >= 3 and c.props[0] == "OO":
                child, parent = c.props[1], c.props[2]
                if child in models:
                    parents[child] = parent

    # الأخذات
    # اسم الأخذة بيتخزّن في props[1] بصيغة "walk\x00\x01AnimStack"
    takes = []
    for stack in walk(root, "AnimationStack"):
        for p in stack.props:
            if isinstance(p, str) and "\x00" in p:
                takes.append(p.split("\x00")[0])
    names = [t for t in takes if t]

    # الميش
    geo_counts = []
    for g in walk(root, "Geometry"):
        verts = 0
        for c in g.children:
            if c.name == "Vertices" and c.props and isinstance(c.props[0], ArmArray):
                verts = c.props[0].length // 3
        geo_counts.append(verts)

    return {
        "version": version,
        "models": models,
        "parents": parents,
        "takes": names,
        "geo": geo_counts,
    }


if __name__ == "__main__":
    import sys
    r = analyze(sys.argv[1])
    print("FBX", r["version"], "| موديلات:", len(r["models"]),
          "| أخذات:", r["takes"], "| جيومتري:", r["geo"])
