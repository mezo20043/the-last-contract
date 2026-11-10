// TLC — R1.5 Render Test (Editor only)
// بيحمّل الشخصيات الأربعة من نفس مسار اللعبة (Resources.Load) وبيرندر صورة واحدة.
// ما بيعدّلش أي أصل. بيتشغّل من:
//   xvfb-run -a Unity -projectPath ... -executeMethod TLC.EditorTools.R15RenderTest.Run -quit -logFile ...
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TLC.EditorTools
{
    public static class R15RenderTest
    {
        static readonly (string key, string file, string cast)[] Roster =
        {
            ("adam",   "man-base-character-rigged", "Adam"),
            ("murad",  "elderly-man-rigged",        "Murad"),
            ("laila",  "woman-rigged",              "Laila"),
            ("yassin", "man-rigged",                "Yassin"),
        };

        public static void Run()
        {
            var root = Path.GetDirectoryName(Application.dataPath);
            Debug.Log($"[TLC/R1.5R] Graphics device: {SystemInfo.graphicsDeviceType} · {SystemInfo.graphicsDeviceName} · shaderLevel={SystemInfo.graphicsShaderLevel}");

            // سين جديد نضيف — مش بنلمس أي مشهد موجود
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("AuditCam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 200f;
            camGo.transform.position = new Vector3(1.5f, 1.6f, 6.2f);
            camGo.transform.LookAt(new Vector3(1.5f, 0.95f, 0f));

            var lightGo = new GameObject("KeyLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            int loaded = 0;
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < Roster.Length; i++)
            {
                var (key, file, cast) = Roster[i];
                string path = $"Characters/{key}/{file}";
                var prefab = Resources.Load<GameObject>(path);          // نفس نداء اللعبة بالظبط
                if (prefab == null) { Debug.LogError($"[TLC/R1.5R] فشل تحميل {path}"); lines.AppendLine($"{cast}: FAIL"); continue; }

                var go = Object.Instantiate(prefab);
                go.name = $"AUDIT_{cast}";
                go.transform.position = new Vector3(i * 1.0f, 0f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);  // يبص للكاميرا

                var anim = go.GetComponent<Animator>();
                var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
                float height = smr != null ? smr.bounds.size.y : -1f;
                int bones = smr != null && smr.bones != null ? smr.bones.Length : 0;
                loaded++;
                lines.AppendLine($"{cast}: loaded ✔ · animator={(anim != null ? "✔" : "✗")} · avatarHuman={(anim != null && anim.avatar != null && anim.avatar.isHuman ? "✔" : "✗")} · bones={bones} · height={height:0.00}m · mats={(smr != null ? smr.sharedMaterials.Length : 0)}");
                Debug.Log($"[TLC/R1.5R] {cast} → loaded · animator={(anim != null)} · avatarHuman={(anim != null && anim.avatar != null && anim.avatar.isHuman)} · bones={bones} · height={height:0.00} m");
            }

            // رندر لصورة
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            var png = Path.Combine(root, "R1_5_CHARACTERS.png");
            File.WriteAllBytes(png, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();

            lines.AppendLine($"loaded={loaded}/{Roster.Length}");
            File.WriteAllText(Path.Combine(root, "R1_5_RENDER_TEST.txt"), lines.ToString());
            Debug.Log($"[TLC/R1.5R] صورة اترندرت: {png} · محمّل {loaded}/{Roster.Length}");
        }
    }
}
