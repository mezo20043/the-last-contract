using System.Collections.Generic;
using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// مكتبة أصول البيئات (CC0 — Poly Haven) اللي اتستوردت كـFBX في
    /// `Resources/Props/<id>/`، مع خامات 1K (diffuse + normal + roughness).
    ///
    /// كل موديل بياخد مادة Standard بتتبني وقت التشغيل من خاماته، لأن الـFBX
    /// المستورد من Poly Haven مش بيربط الخامات تلقائيًا جوه Unity.
    /// وبيتضاف BoxCollider على حدود كل جزء عشان الشخصيات واللاعب ما يعدّوش
    /// جوه الأثاث (تصادم مناسب).
    /// </summary>
    public static class PropLibrary
    {
        public static readonly string[] All =
        {
            "WoodenChair_01", "WoodenTable_01", "WoodenTable_03", "CoffeeTable_01",
            "Sofa_01", "ArmChair_01", "GreenChair_01", "SchoolDesk_01",
            "Shelf_01", "GothicCabinet_01", "bar_chair_round_01", "desk_lamp_arm_01"
        };

        static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly HashSet<string> Warned = new HashSet<string>();

        public static GameObject Prefab(string id)
        {
            if (Prefabs.TryGetValue(id, out var p)) return p;

            p = Resources.Load<GameObject>($"Props/{id}/{id}");
            Prefabs[id] = p;

            if (p == null && Warned.Add(id))
                Debug.LogWarning($"[TLC/Props] الأصل '{id}' مش موجود في Resources/Props — هيتخطّى.");
            return p;
        }

        public static Material Material(string id)
        {
            if (Materials.TryGetValue(id, out var m) && m != null) return m;

            // المشروع URP — لازم URP/Lit الأول، وإلا الموديل بيطلع أسود/باهت
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            m = new Material(shader) { name = "MAT_" + id };

            var diff = Resources.Load<Texture2D>($"Props/{id}/{id}_diff");
            if (diff != null)
            {
                m.mainTexture = diff;
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", diff);
            }

            var nor = Resources.Load<Texture2D>($"Props/{id}/{id}_nor");
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_BumpScale", 0.8f);
            }

            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.22f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.05f);

            Materials[id] = m;
            return m;
        }

        /// <summary>يحطّ موديل في العالم بخامته وتصادمه.</summary>
        /// <summary>
        /// أكبر بُعد بالمتر لكل أصل — موديلات Poly Haven بتستورد بمقاس مش مترّي،
        /// فلازم نطبّعها قبل الاستخدام وإلا بتبقى نقطة صغيرة على الأرض.
        /// </summary>
        /// <summary>الارتفاع الحقيقي بالمتر لكل أصل (بتتطبّع عليه الموديلات).</summary>
        static float TargetHeight(string id)
        {
            switch (id)
            {
                case "WoodenTable_01":
                case "WoodenTable_03":    return 0.76f;
                case "CoffeeTable_01":    return 0.45f;
                case "Sofa_01":           return 0.85f;
                case "Shelf_01":          return 1.90f;
                case "GothicCabinet_01":  return 2.00f;
                case "SchoolDesk_01":     return 0.76f;
                case "ArmChair_01":       return 0.95f;
                case "GreenChair_01":     return 0.95f;
                case "WoodenChair_01":    return 1.00f;
                case "bar_chair_round_01":return 1.05f;
                case "desk_lamp_arm_01":  return 0.50f;
                default:                  return 1.00f;
            }
        }

        static Bounds Measure(GameObject go)
        {
            var rs = go.GetComponentsInChildren<MeshRenderer>(true);
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);

            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        public static GameObject Spawn(string id, Transform parent, Vector3 localPos, float yaw, float scale = 1f, Color? tint = null)
        {
            var prefab = Prefab(id);
            if (prefab == null) return null;

            var go = Object.Instantiate(prefab, parent, false);
            go.name = id;
            go.transform.localPosition = localPos;
            // موديلات Poly Haven مصدّرة Z-up من Blender؛ لازم تتدوّر 90° حول X
            // وإلا الطاولة/الكرسي بيبانوا نايمين على جنبهم.
            go.transform.localRotation = Quaternion.Euler(-90f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;

            // ── تطبيع المقاس: بنضبط الارتفاع على مقاس واقعي بالمتر ──
            ModelFit.FitHeight(go.transform, TargetHeight(id) * scale);

            // حماية: لو العرض طلع مبالغ فيه بعد التطبيع نقلّله
            var bb = ModelFit.PreciseBounds(go.transform);
            float widest = Mathf.Max(bb.size.x, bb.size.z);
            if (widest > 3.2f) go.transform.localScale *= (3.2f / widest);


            var mat = Material(id);
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }

            // ── تنزيل الموديل لحد ما قاعدته تلمس الأرض ──
            float floorY = parent != null ? parent.position.y + localPos.y : localPos.y;
            ModelFit.DropToGround(go.transform, floorY);


            AddCollision(go, 1f);
            return go;
        }

        /// <summary>يضيف BoxCollider على كل جزء مرئي (تصادم معقول بدون MeshCollider تقيل).</summary>
        /// <summary>
        /// تصادم الأثاث: صندوق واحد صلب من الأرض لأعلى نقطة في الموديل.
        ///
        /// مهم: الموديلات بتتدوّر ‎−90°‎ حول X، فلو حطينا الـBoxCollider على جسم
        /// الموديل نفسه بيتحوّل معاه ويطلع صندوق غلط. عشان كده الكولادر بيتحط على
        /// كائن مساعد **محاذي للعالم** (بدون دوران وبدون مقياس) تحت جذر الموقع.
        /// </summary>
        /// <summary>تفتيح/تغميق خامة الموديل (بيستخدم لأرفف الأرشيف عشان السطوع الزيادة).</summary>
        static void Tint(GameObject go, Color tint)
        {
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true))
            {
                var m = r.sharedMaterial;
                if (m == null) continue;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            }
        }

        static void AddCollision(GameObject go, float scale)
        {
            var parent = go.transform.parent;
            if (parent == null) return;

            var renderers = go.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) return;

            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            float groundY = go.transform.position.y;

            var colGo = new GameObject(go.name + "_COL");
            colGo.transform.SetParent(parent, false);      // جذر الموقع: بدون دوران/مقياس
            colGo.transform.position = new Vector3(b.center.x, (groundY + b.max.y) / 2f, b.center.z);
            colGo.transform.rotation = Quaternion.identity;
            colGo.transform.localScale = Vector3.one;

            var box = colGo.AddComponent<BoxCollider>();
            box.size = new Vector3(
                Mathf.Max(0.2f, b.size.x),
                Mathf.Max(0.2f, b.max.y - groundY),
                Mathf.Max(0.2f, b.size.z));
        }

        /// <summary>عدد الأصول المتاحة فعلًا على القرص (للتحقق في التقارير).</summary>
        public static int AvailableCount()
        {
            int n = 0;
            foreach (var id in All) if (Prefab(id) != null) n++;
            return n;
        }
    }
}
