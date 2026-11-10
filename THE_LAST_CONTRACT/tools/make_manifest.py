#!/usr/bin/env python3
"""
يحدّث الجزء المحسوب من MANIFEST.md (العدد الإجمالي + جدول المجلدات)
من الشجرة الفعلية للمشروع. باقي الملف (الأصول/الشخصيات/الفصول) مكتوب يدوي.
"""
import os
import sys
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SKIP_DIRS = {"bin", "obj", ".git", "Library", "Temp", "Logs", "Builds", ".github-workflows-cache"}
SKIP_FILES = {"MANIFEST.md"}


def walk_zip(zip_path):
    """يحسب من داخل الـ ZIP نفسه — أدق، لأن الـ FBX الكبيرة مش دايمًا في شجرة العمل."""
    per_dir = {}
    total_files = 0
    total_bytes = 0
    with zipfile.ZipFile(zip_path) as z:
        for info in z.infolist():
            if info.is_dir():
                continue
            name = info.filename
            if name.split("/", 1)[0] == "THE_LAST_CONTRACT":
                name = name.split("/", 1)[1]
            if name == "MANIFEST.md":
                continue
            parts = name.split("/")
            rel = "/".join(parts[:-1]) or "(الجذر)"
            c, sz = per_dir.get(rel, (0, 0))
            per_dir[rel] = (c + 1, sz + info.file_size)
            total_files += 1
            total_bytes += info.file_size
    return per_dir, total_files, total_bytes


def walk():
    per_dir = {}
    total_files = 0
    total_bytes = 0
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        rel = os.path.relpath(dirpath, ROOT)
        rel = "(الجذر)" if rel == "." else rel
        for fn in filenames:
            if fn in SKIP_FILES:
                continue
            size = os.path.getsize(os.path.join(dirpath, fn))
            c, s = per_dir.get(rel, (0, 0))
            per_dir[rel] = (c + 1, s + size)
            total_files += 1
            total_bytes += size
    return per_dir, total_files, total_bytes


def main():
    zip_path = None
    if "--from-zip" in sys.argv:
        zip_path = sys.argv[sys.argv.index("--from-zip") + 1]
    if zip_path:
        per_dir, total_files, total_bytes = walk_zip(zip_path)
    else:
        per_dir, total_files, total_bytes = walk()
    rows = sorted(per_dir.items(), key=lambda kv: -kv[1][1])

    table = ["| المجلد | ملفات | حجم |", "|---|---|---|"]
    for name, (count, size) in rows:
        if count == 0:
            continue
        table.append(f"| `{name}` | {count} | {size // 1024} KB |")

    path = os.path.join(ROOT, "MANIFEST.md")
    lines = open(path, encoding="utf-8").read().split("\n")

    out = []
    i = 0
    while i < len(lines):
        line = lines[i]
        if line.startswith("**") and "ملف" in line and "MB" in line:
            out.append(f"**{total_files:,} ملف · {total_bytes / 1048576:.2f} MB** (داخل الـ ZIP النهائي)")
            i += 1
            continue
        if line.startswith("| المجلد |"):
            out.extend(table)
            i += 1
            while i < len(lines) and lines[i].startswith("|"):
                i += 1
            continue
        out.append(line)
        i += 1

    open(path, "w", encoding="utf-8").write("\n".join(out))
    print(f"✓ MANIFEST.md — {total_files:,} ملف · {total_bytes / 1048576:.2f} MB · {len(rows)} مجلد")


if __name__ == "__main__":
    main()
