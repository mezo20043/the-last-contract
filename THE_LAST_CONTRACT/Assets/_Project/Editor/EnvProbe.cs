using System.Text;
using UnityEditor;
using UnityEngine;
using TLC.World;

namespace TLC.EditorTools
{
    /// <summary>
    /// فحص سريع (بدون Play Mode) لأصول البيئة: هل الموديلات والخامات بتتحمّل
    /// من Resources فعلاً؟ الشيدر المستخدم إيه؟ — بيوفّر جولة Unity كاملة.
    /// </summary>
    public static class EnvProbe
    {
        public static void Run()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══ فحص أصول البيئة ═══");
            sb.AppendLine("الشيدر: Standard=" + (Shader.Find("Standard") != null) +
                          " · URP/Lit=" + (Shader.Find("Universal Render Pipeline/Lit") != null) +
                          " · Diffuse=" + (Shader.Find("Diffuse") != null));
            sb.AppendLine("نوع الـPipeline: " + (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null
                ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.GetType().Name : "Built-in"));

            int ok = 0;
            foreach (var id in PropLibrary.All)
            {
                var p = PropLibrary.Prefab(id);
                var diff = Resources.Load<Texture2D>($"Props/{id}/{id}_diff");
                var nor = Resources.Load<Texture2D>($"Props/{id}/{id}_nor");
                sb.AppendLine($"  {id,-22} prefab={(p != null ? "✓" : "✗")} diff={(diff != null ? diff.width + "px ✓" : "✗")} " +
                              $"nor={(nor != null ? "✓" : "✗")}");
                if (p != null) ok++;
            }
            sb.AppendLine($"الموديلات الجاهزة: {ok}/{PropLibrary.All.Length}");

            string[] keys = { "wood_floor", "wood_dark", "plaster_wall", "tile_ceramic",
                              "fabric_dark", "metal_brushed", "concrete", "city_facade" };
            foreach (var k in keys)
            {
                var a = Resources.Load<Texture2D>($"Art/Textures/Interior/{k}_albedo")
                     ?? Resources.Load<Texture2D>($"Art/Textures/Exterior/{k}_albedo");
                var n = Resources.Load<Texture2D>($"Art/Textures/Interior/{k}_normal")
                     ?? Resources.Load<Texture2D>($"Art/Textures/Exterior/{k}_normal");
                sb.AppendLine($"  خامة {k,-16} albedo={(a != null ? a.width + "px ✓" : "✗")} normal={(n != null ? "✓" : "✗")}");
            }

            Debug.Log(sb.ToString());
            EditorApplication.Exit(0);
        }
    }
}
