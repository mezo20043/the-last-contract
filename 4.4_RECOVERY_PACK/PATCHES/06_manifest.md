# PATCH — `Packages/manifest.json`

**النتيجة: مفيش حزمة جديدة مطلوبة.**

`com.unity.textmeshpro` (أو `com.unity.ugui` اللي جواه TMP في Unity 6) و`com.unity.ugui`
موجودين من 4.3، والموارد نفسها (خطوط/إعدادات TMP) اللي كانت ناقصة بتتضاف كأصول في
`Assets/TextMesh Pro/` بـ`Editor/ImportTmpEssentials.cs` — مش حزمة.

## تحقق

```bash
python3 - <<'PY'
import json
m = json.load(open("Packages/manifest.json"))
need = ["com.unity.ugui", "com.unity.inputsystem", "com.unity.render-pipelines.universal"]
for n in need:
    print(f"{n:45s} {'✔' if n in m['dependencies'] else '✖ ناقصة'}")
print("TMP:", "✔" if any("textmeshpro" in k for k in m["dependencies"]) else "جوه ugui (طبيعي في Unity 6)")
PY
```

## الأصل الجانبي اللي لازم يكون موجود

```
Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset
Assets/TextMesh Pro/Resources/TMP Settings.asset
Assets/_Project/Resources/Fonts/Amiri SDF.asset
Assets/_Project/Resources/Fonts/Amiri-Regular.ttf
```

من غيرهم: كل `TextMeshProUGUI` بيرسم فاضي (حروف = 0) واللقطات بتطلع فاضية —
ده كان سبب مشكلة 4.3 r1 اللي اتحلّت في r2.
