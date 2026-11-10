#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TLC.Bootstrap;
using TLC.CameraRig;
using TLC.UI;
using TLC.World;

namespace TLC.EditorTools
{
    /// <summary>
    /// بيولّد <c>Assets/_Project/Scenes/Play.unity</c> بالكود.
    ///
    /// المصدر الرسمي للمشهد هو <c>tools/make_scenes.py</c> (بيولّد نفس البنية
    /// وبيتحقق منها أوفلاين). الأداة دي بتعمل نفس الحاجة جوه المحرر
    /// عشان لو المشهد اتشال أو اتخرب، يترجع بضغطة من غير ما تحتاج بايثون.
    ///
    /// المشهد فيه كائنات حقيقية بمواضع محفوظة:
    ///   Main Camera        — Camera + AudioListener + ThirdPersonCameraRig (target = آدم)
    ///   Directional Light  — ضوء مشهد حقيقي
    ///   WORLD              — الشقة + الإضاءة العملية + الكاست
    ///   ADAM_MANSOUR       — نقطة آدم، والموديل بيتحمّل من Resources وقت التشغيل
    ///   MOBILE_CONTROLS    — العصا وزر التفاعل ومنطقة الكاميرا
    ///   OrientationWatcher — إدارة الاتجاه
    ///   GAME               — GameBootstrap بوضع useSceneObjects
    ///
    /// ملاحظة صريحة: موديل آدم نفسه **مش** كائن مسروق في المشهد.
    /// الـ FBX بيتحمّل من Resources/Characters وقت التشغيل لأن مفيش prefabs
    /// في المشروع، والمشهد بيتضمن نقطة الظهور والكاميرا والضوء والبيئة.
    /// </summary>
    public static class PlaySceneBuilder
    {
        public const string PlayPath = "Assets/_Project/Scenes/Play.unity";

        [MenuItem("TLC/أدوات/توليد مشهد اللعب فقط", priority = 50)]
        public static void BuildPlayScene()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── الكاميرا ──
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.06f;
            cam.farClipPlane = 70f;
            cam.fieldOfView = GameBootstrap.BaseFieldOfView;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.035f, 0.045f, 1f);
            camGo.AddComponent<AudioListener>();
            // مطابق لحساب الـrig (pitch 8 · distance 3.1 · shoulder 0.30 · height 1.35)
            camGo.transform.position = new Vector3(-0.9f, 1.781f, 1.67f);
            camGo.transform.rotation = Quaternion.Euler(8f, 180f, 0f);

            var rig = camGo.AddComponent<ThirdPersonCameraRig>();

            // ── آدم (نقطة الظهور) ──
            var adamGo = new GameObject("ADAM_MANSOUR");
            adamGo.transform.position = new Vector3(-0.6f, 0f, -1.4f);
            adamGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            adamGo.AddComponent<AdamBuilder>();

            // الكاميرا بتستهدف آدم بمرجع حقيقي في المشهد
            var rigSo = new SerializedObject(rig);
            var targetProp = rigSo.FindProperty("target");
            if (targetProp != null) targetProp.objectReferenceValue = adamGo.transform;
            rigSo.ApplyModifiedPropertiesWithoutUndo();

            // ── ضوء الشمس ──
            var sunGo = new GameObject("Directional Light");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(0.88f, 0.91f, 1f);
            sun.intensity = 0.55f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.position = new Vector3(0f, 3.2f, 0f);
            sunGo.transform.rotation = Quaternion.Euler(48f, 34f, 0f);

            // ── العالم ──
            var worldGo = new GameObject("WORLD");
            var apartmentGo = new GameObject("Apartment");
            apartmentGo.transform.SetParent(worldGo.transform, false);
            apartmentGo.AddComponent<ApartmentBuilder>();

            var lightingGo = new GameObject("Lighting");
            lightingGo.transform.SetParent(worldGo.transform, false);
            lightingGo.AddComponent<LightingRig>();

            var castGo = new GameObject("CAST");
            castGo.transform.SetParent(worldGo.transform, false);
            castGo.AddComponent<CastDirector>();

            // ── واجهة الموبايل ──
            var mobileGo = new GameObject("MOBILE_CONTROLS");
            mobileGo.AddComponent<MobileControls>();

            // ── الاتجاه ──
            var orientGo = new GameObject("OrientationWatcher");
            orientGo.AddComponent<OrientationWatcher>();

            // ── نقطة الدخول ──
            var gameGo = new GameObject("GAME");
            var bootstrap = gameGo.AddComponent<GameBootstrap>();
            var so = new SerializedObject(bootstrap);
            so.FindProperty("menuOnly").boolValue = false;
            so.FindProperty("useSceneObjects").boolValue = true;
            so.FindProperty("showTitleMenu").boolValue = false;
            so.FindProperty("openWithCinematic").boolValue = true;
            so.FindProperty("buildMobileControls").boolValue = true;
            so.FindProperty("playSceneName").stringValue = SceneFlow.PlayScene;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, PlayPath);
            AssetDatabase.Refresh();

            Debug.Log($"[TLC] {PlayPath} اتولّد: كاميرا + ضوء + عالم + آدم + تحكم موبايل + GameBootstrap(useSceneObjects).");
        }
    }
}
#endif
