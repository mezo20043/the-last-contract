using System.Collections.Generic;
using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// مكتبة الخامات: بتحمّل التكستشرات المولّدة من Resources وتبني منها مواد PBR
    /// مرة واحدة وتخزّنها. لو التكستشر مش موجود بترجع مادة بلون احتياطي
    /// بدل ما اللعبة تقع أو تبان وردي (missing material).
    /// </summary>
    public static class MaterialLibrary
    {
        public class Spec
        {
            public string key;
            public string albedo;
            public string normal;
            public Color fallbackColor;
            public float smoothness;
            public float metallic;
            public Vector2 tiling = Vector2.one;
        }

        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static bool _shaderWarned;

        public static readonly Spec[] All =
        {
            new Spec { key = "wood_floor",    albedo = "Art/Textures/Interior/wood_floor_albedo",
                       normal = "Art/Textures/Interior/wood_floor_normal",
                       fallbackColor = new Color(0.30f,0.22f,0.15f), smoothness = 0.42f,
                       tiling = new Vector2(4f,4f) },
            new Spec { key = "wood_dark",     albedo = "Art/Textures/Interior/wood_dark_albedo",
                       normal = "Art/Textures/Interior/wood_dark_normal",
                       fallbackColor = new Color(0.16f,0.11f,0.08f), smoothness = 0.38f,
                       tiling = new Vector2(2f,2f) },
            new Spec { key = "plaster_wall",  albedo = "Art/Textures/Interior/plaster_wall_albedo",
                       normal = "Art/Textures/Interior/plaster_wall_normal",
                       fallbackColor = new Color(0.46f,0.44f,0.41f), smoothness = 0.12f,
                       tiling = new Vector2(4f,3f) },
            new Spec { key = "tile_ceramic",  albedo = "Art/Textures/Interior/tile_ceramic_albedo",
                       normal = "Art/Textures/Interior/tile_ceramic_normal",
                       fallbackColor = new Color(0.82f,0.82f,0.80f), smoothness = 0.68f,
                       tiling = new Vector2(3f,3f) },
            new Spec { key = "fabric_dark",   albedo = "Art/Textures/Interior/fabric_dark_albedo",
                       normal = "Art/Textures/Interior/fabric_dark_normal",
                       fallbackColor = new Color(0.13f,0.15f,0.19f), smoothness = 0.08f,
                       tiling = new Vector2(2f,2f) },
            new Spec { key = "metal_brushed", albedo = "Art/Textures/Interior/metal_brushed_albedo",
                       normal = "Art/Textures/Interior/metal_brushed_normal",
                       fallbackColor = new Color(0.35f,0.36f,0.38f), smoothness = 0.72f,
                       metallic = 0.9f },
            new Spec { key = "concrete",      albedo = "Art/Textures/Exterior/concrete_albedo",
                       normal = "Art/Textures/Exterior/concrete_normal",
                       fallbackColor = new Color(0.28f,0.28f,0.28f), smoothness = 0.18f,
                       tiling = new Vector2(8f,8f) },
            new Spec { key = "city_facade",   albedo = "Art/Textures/Exterior/city_facade_albedo",
                       normal = "Art/Textures/Exterior/city_facade_normal",
                       fallbackColor = new Color(0.04f,0.045f,0.06f), smoothness = 0.35f,
                       tiling = new Vector2(3f,6f) },
        };

        public static Material Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;

            Spec spec = null;
            foreach (var s in All) if (s.key == key) { spec = s; break; }
            if (spec == null)
            {
                Debug.LogWarning($"[TLC] خامة غير معروفة: {key}");
                return null;
            }

            var mat = Build(spec);
            Cache[key] = mat;
            return mat;
        }

        static Shader PickShader()
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            if (sh == null && !_shaderWarned)
            {
                _shaderWarned = true;
                Debug.LogWarning("[TLC] مفيش Shader متاح — هتستخدم خامات افتراضية.");
            }
            return sh;
        }

        static Material Build(Spec spec)
        {
            var shader = PickShader();
            var mat = shader != null ? new Material(shader) : null;
            if (mat == null) return null;

            mat.name = "M_" + spec.key;

            var albedo = Resources.Load<Texture2D>(spec.albedo);
            var normal = Resources.Load<Texture2D>(spec.normal);

            // URP
            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            else mat.SetColor("_BaseColor", spec.fallbackColor);
            // Standard (احتياطي لو المشروع اتحوّل لـ Built-in)
            if (albedo != null) mat.SetTexture("_MainTex", albedo);
            else mat.SetColor("_Color", spec.fallbackColor);

            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetFloat("_Smoothness", spec.smoothness);
            mat.SetFloat("_Glossiness", spec.smoothness);
            mat.SetFloat("_Metallic", spec.metallic);

            if (albedo != null && shader != null)
            {
                albedo.wrapMode = TextureWrapMode.Repeat;
                mat.SetTextureScale("_BaseMap", spec.tiling);
                mat.SetTextureScale("_MainTex", spec.tiling);
            }

            return mat;
        }

        /// <summary>اختبار: كام خامة اتحمّلت فعلاً من القرص.</summary>
        public static int LoadedTextureCount()
        {
            int n = 0;
            foreach (var s in All)
            {
                if (Resources.Load<Texture2D>(s.albedo) != null) n++;
                if (Resources.Load<Texture2D>(s.normal) != null) n++;
            }
            return n;
        }

        public static void Clear() => Cache.Clear();
    }
}
