// إضافات على طبقة المحاكاة: ما استخدمه الكود الجديد + واجهات UnityEditor
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
    }

    public static class Screen
    {
        public static int width => 1080;
        public static int height => 1920;
        public static Rect safeArea => new Rect(0, 0, 1080, 1920);
        public static int sleepTimeout { get; set; }
        public static void SetResolution(int w, int h, bool fullscreen) { }
        public static ScreenOrientation orientation { get; set; } = ScreenOrientation.AutoRotation;
        public static bool autorotateToPortrait { get; set; } = true;
        public static bool autorotateToPortraitUpsideDown { get; set; }
        public static bool autorotateToLandscapeLeft { get; set; } = true;
        public static bool autorotateToLandscapeRight { get; set; } = true;
    }

    public enum ScreenOrientation
    {
        Portrait = 1, PortraitUpsideDown = 2, LandscapeLeft = 3, LandscapeRight = 4, AutoRotation = 5
    }

    public static class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    public class AnimationClip : Motion
    {
        public float length => 1f;
        public float frameRate { get; set; } = 30f;
        public bool isLooping { get; set; }
    }

    public class Avatar : Object { public bool isHuman => true; }

    public enum AnimatorControllerParameterType { Float = 1, Int = 3, Bool = 4, Trigger = 9 }

    public class RuntimeAnimatorController : Object { }

    public class AnimatorControllerParameter
    {
        public string name { get; set; }
        public AnimatorControllerParameterType type { get; set; }
        public float defaultFloat { get; set; }
    }
}

namespace UnityEngine.Rendering
{
    public enum GraphicsDeviceType
    {
        OpenGLES2 = 8, OpenGLES3 = 11, Vulkan = 21, Metal = 16, Direct3D11 = 2, Direct3D12 = 18
    }

    public class RenderPipelineAsset : ScriptableObject { }

    public static class GraphicsSettings
    {
        public static RenderPipelineAsset defaultRenderPipeline { get; set; }
        public static RenderPipelineAsset currentRenderPipeline => null;
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public GameObject[] GetRootGameObjects() => Array.Empty<GameObject>();
        public string name { get; set; }
        public string path { get; set; }
        public bool IsValid() => true;
        public int buildIndex { get; set; }
    }

    public enum LoadSceneMode { Single = 0, Additive = 1 }

    public static class SceneManager
    {
        public static int sceneCountInBuildSettings => 2;
        public static Scene GetActiveScene() => new Scene { name = "Play" };
        public static void LoadScene(string name) { }
        public static void LoadScene(string name, LoadSceneMode mode) { }
        public static void LoadScene(int index, LoadSceneMode mode) { }
        public static AsyncOperation LoadSceneAsync(string name, LoadSceneMode mode) => null;
        public static event System.Action<Scene, LoadSceneMode> sceneLoaded;
        public static void invoke() => sceneLoaded?.Invoke(new Scene(), LoadSceneMode.Single);
    }
}

namespace UnityEditor
{
    using UnityEngine.SceneManagement;
    using UnityEngine.Rendering;

    // ── خصائص ──
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string path) { }
        public MenuItem(string path, bool validate) { }
        public MenuItem(string path, bool validate, int priority) { }
        public int priority;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoadAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeOnLoadMethodAttribute : Attribute { }

    // ── تطبيق ──
    public static class EditorApplication
    {
        public delegate void CallbackFunction();
        public static CallbackFunction delayCall;
        public static CallbackFunction update;
        public static void Exit(int code) { }
        public static bool isPlaying { get; set; }
        public static bool isPlayingOrWillChangePlaymode => false;
        public static void ExecuteMenuItem(string path) { }
    }

    public static class Selection
    {
        public static UnityEngine.Object activeObject { get; set; }
        public static UnityEngine.Object[] objects { get; set; } = Array.Empty<UnityEngine.Object>();
        public static GameObject activeGameObject { get; set; }
    }

    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => null;
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string path) => Array.Empty<UnityEngine.Object>();
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static void CreateFolder(string parent, string name) { }
        public static bool IsValidFolder(string path) => true;
        public static string[] FindAssets(string filter) => Array.Empty<string>();
        public static string[] FindAssets(string filter, string[] folders) => Array.Empty<string>();
        public static string GUIDToAssetPath(string guid) => "";
        public static bool CreateAsset(UnityEngine.Object o, string p) => true;
        public static bool DeleteAsset(string p) => true;
        public static string GetAssetPath(UnityEngine.Object o) => "";
        public static string AssetPathToGUID(string path) => "";
        public static void ImportAsset(string path) { }
    }

    public class AssetImporter : UnityEngine.Object
    {
        public string assetPath { get; set; }
        public static AssetImporter GetAtPath(string path) => null;
        public void SaveAndReimport() { }
    }

    public enum ModelImporterAnimationType { None = 0, Legacy = 1, Generic = 2, Human = 3 }
    public enum ModelImporterAnimationCompression { Off, KeyframeReduction, KeyframeReductionAndCompression, Optimal }
    public enum ModelImporterMeshCompression { Off, Low, Medium, High }

    public enum ModelImporterMaterialImportMode { None = 0, ImportStandard = 1, ImportViaMaterialDescription = 2 }
    public enum ModelImporterMaterialLocation { External = 0, InPrefab = 1 }
    public enum ModelImporterMaterialSearch { Local = 0, RecursiveUp = 1, Everywhere = 2 }
    public enum ModelImporterNormals { Import = 0, Calculate = 1, None = 2 }
    public enum ModelImporterTangents { None = 0, Import = 1, CalculateLegacy = 2, CalculateMikk = 3 }
    public enum TextureImporterType { Default = 0, NormalMap = 1, Sprite = 8 }
    public enum TextureImporterCompression { Uncompressed = 0, Compressed = 1, CompressedHQ = 2, CompressedLQ = 3 }

    public class AssetPostprocessor
    {
        public string assetPath { get; set; }
        public AssetImporter assetImporter { get; set; }
    }

    public class ModelImporter : AssetImporter
    {
        public ModelImporterAnimationType animationType { get; set; }
        public ModelImporterAnimationCompression animationCompression { get; set; }
        public ModelImporterMeshCompression meshCompression { get; set; }
        public bool importAnimation { get; set; }
        public bool importBlendShapes { get; set; }
        public bool isReadable { get; set; }
        public float globalScale { get; set; }
        public ModelImporterMaterialImportMode materialImportMode { get; set; }
        public ModelImporterMaterialLocation materialLocation { get; set; }
        public ModelImporterMaterialSearch materialSearch { get; set; }
        public bool importVisibility { get; set; }
        public bool importCameras { get; set; }
        public bool importLights { get; set; }
        public ModelImporterNormals importNormals { get; set; }
        public ModelImporterTangents importTangents { get; set; }
        public bool optimizeMeshPolygons { get; set; }
        public bool optimizeMeshVertices { get; set; }
        public bool addCollider { get; set; }
    }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType { get; set; }
        public bool sRGBTexture { get; set; }
        public int maxTextureSize { get; set; }
        public bool mipmapEnabled { get; set; }
        public TextureImporterCompression textureCompression { get; set; }
        public UnityEngine.TextureWrapMode wrapMode { get; set; }
        public int anisoLevel { get; set; }
    }

    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object o) { }
        public static void RevealInFinder(string path) { }
        public static bool DisplayDialog(string title, string message, string ok) => true;
        public static bool DisplayDialog(string t, string m, string ok, string cancel) => true;
        public static void DisplayProgressBar(string t, string i, float p) { }
        public static void ClearProgressBar() { }
    }

    public enum SerializationMode { Mixed, ForceBinary, ForceText }
    public enum EditorBehaviorMode { Mode3D, Mode2D }

    public static class EditorSettings
    {
        public static SerializationMode serializationMode { get; set; }
        public static EditorBehaviorMode defaultBehaviorMode { get; set; }
    }

    // ── إعدادات اللاعب ──
    public enum BuildTargetGroup { Unknown, Standalone, iOS, Android, WebGL }
    public enum BuildTarget { StandaloneWindows64, StandaloneLinux64, Android, iOS, WebGL }
    public enum ScriptingImplementation { Mono2x, IL2CPP }
    public enum ApiCompatibilityLevel { NET_Standard_2_0, NET_Unity_4_8, NET_Standard }
    public enum ManagedStrippingLevel { Disabled, Low, Medium, High }
    public enum ColorSpace { Gamma, Linear }
    public enum UIOrientation { Portrait, PortraitUpsideDown, LandscapeLeft, LandscapeRight, AutoRotation }

    [Flags]
    public enum AndroidArchitecture { None = 0, ARMv7 = 1, ARM64 = 2, X86 = 4, X86_64 = 8, All = 15 }

    public enum AndroidSdkVersions
    {
        AndroidApiLevelAuto = 0,
        AndroidApiLevel22 = 22, AndroidApiLevel23 = 23, AndroidApiLevel24 = 24,
        AndroidApiLevel26 = 26, AndroidApiLevel29 = 29, AndroidApiLevel30 = 30,
        AndroidApiLevel31 = 31, AndroidApiLevel33 = 33, AndroidApiLevel34 = 34
    }

    public static class PlayerSettings
    {
        public static string companyName { get; set; }
        public static string productName { get; set; }
        public static string bundleVersion { get; set; }
        public static ColorSpace colorSpace { get; set; }
        public static UIOrientation defaultInterfaceOrientation { get; set; }
        public static bool allowedAutorotateToPortrait { get; set; }
        public static bool allowedAutorotateToPortraitUpsideDown { get; set; }
        public static bool allowedAutorotateToLandscapeLeft { get; set; }
        public static bool allowedAutorotateToLandscapeRight { get; set; }

        public static void SetApplicationIdentifier(BuildTargetGroup g, string id) { }
        public static string GetApplicationIdentifier(BuildTargetGroup g) => "com.example";
        public static void SetScriptingBackend(BuildTargetGroup g, ScriptingImplementation i) { }
        public static ScriptingImplementation GetScriptingBackend(BuildTargetGroup g) => ScriptingImplementation.IL2CPP;
        public static void SetApiCompatibilityLevel(BuildTargetGroup g, ApiCompatibilityLevel l) { }
        public static void SetManagedStrippingLevel(BuildTargetGroup g, ManagedStrippingLevel l) { }
        public static void SetMobileMTRendering(BuildTargetGroup g, bool v) { }
        public static void SetGraphicsAPIs(BuildTarget b, GraphicsDeviceType[] apis) { }
        public static void SetUseDefaultGraphicsAPIs(BuildTarget b, bool v) { }
        public static int GetPropertyInt(string name) => 2;

        public static class Android
        {
            public static int bundleVersionCode { get; set; }
            public static AndroidArchitecture targetArchitectures { get; set; }
            public static AndroidSdkVersions minSdkVersion { get; set; }
            public static AndroidSdkVersions targetSdkVersion { get; set; }
            public static bool useCustomKeystore { get; set; }
            public static string keystoreName { get; set; }
            public static string keystorePass { get; set; }
            public static string keyaliasName { get; set; }
            public static string keyaliasPass { get; set; }
        }
    }

    public static class EditorUserBuildSettings
    {
        public static BuildTarget activeBuildTarget => BuildTarget.Android;
        public static bool buildAppBundle { get; set; }
        public static bool development { get; set; }
        public static bool switchActiveBuildTarget(BuildTargetGroup g, BuildTarget t) => true;
    }

    public class EditorBuildSettingsScene
    {
        public string path { get; set; }
        public bool enabled { get; set; }
        public EditorBuildSettingsScene() { }
        public EditorBuildSettingsScene(string path, bool enabled) { this.path = path; this.enabled = enabled; }
    }

    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes { get; set; } = Array.Empty<EditorBuildSettingsScene>();
    }

    public enum BuildOptions { None = 0, Development = 1, AutoRunPlayer = 4 }

    public struct BuildPlayerOptions
    {
        public string[] scenes;
        public string locationPathName;
        public BuildTarget target;
        public BuildTargetGroup targetGroup;
        public BuildOptions options;
    }

    public static class BuildPipeline
    {
        public static UnityEditor.Build.Reporting.BuildReport BuildPlayer(BuildPlayerOptions o)
            => new UnityEditor.Build.Reporting.BuildReport();
    }

    // ── تسلسل ──
    public class SerializedProperty
    {
        public int intValue { get; set; }
        public bool boolValue { get; set; }
        public string stringValue { get; set; }
        public float floatValue { get; set; }
        public UnityEngine.Object objectReferenceValue { get; set; }
    }

    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object target) { }
        public SerializedProperty FindProperty(string path) => new SerializedProperty();
        public bool ApplyModifiedProperties() => true;
        public bool ApplyModifiedPropertiesWithoutUndo() => true;
        public void Update() { }
    }
}

namespace UnityEditor.Build.Reporting
{
    public enum BuildResult { Unknown, Succeeded, Failed, Cancelled }

    public struct BuildSummary
    {
        public BuildResult result;
        public ulong totalSize;
        public TimeSpan totalTime;
        public int totalErrors;
        public int totalWarnings;
    }

    public class BuildReport
    {
        public BuildSummary summary => new BuildSummary { result = BuildResult.Succeeded };
    }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;

    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }

    public enum OpenSceneMode { Single = 0, Additive = 1, AdditiveWithoutLoading = 2 }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) => new Scene();
        public static Scene OpenScene(string path) => new Scene();
        public static Scene OpenScene(string path, OpenSceneMode mode) => new Scene();
        public static bool SaveScene(Scene s, string path) => true;
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => true;
    }
}

namespace UnityEditor.Animations
{
    using UnityEngine;

    public enum AnimatorConditionMode { If = 1, IfNot = 2, Greater = 3, Less = 4, Equals = 6, NotEqual = 7 }

    public class AnimatorStateTransition
    {
        public bool hasExitTime { get; set; }
        public float duration { get; set; }
        public bool hasFixedDuration { get; set; }
        public void AddCondition(AnimatorConditionMode mode, float threshold, string parameter) { }
    }

    public class AnimatorState : UnityEngine.Object
    {
        public Motion motion { get; set; }
        public float speed { get; set; }
        public AnimatorStateTransition AddTransition(AnimatorState destination) => new AnimatorStateTransition();
        public AnimatorStateTransition AddTransition(AnimatorStateMachine destination) => new AnimatorStateTransition();
    }

    public class AnimatorStateMachine : UnityEngine.Object
    {
        public AnimatorState defaultState { get; set; }
        public AnimatorState AddState(string name) => new AnimatorState();
        public AnimatorState AddState(string name, Vector3 position) => new AnimatorState();
    }

    public class AnimatorControllerLayer
    {
        public string name { get; set; }
        public AnimatorStateMachine stateMachine { get; set; } = new AnimatorStateMachine();
        public float defaultWeight { get; set; }
    }

    public class AnimatorController : RuntimeAnimatorController
    {
        public AnimatorControllerLayer[] layers { get; set; } = { new AnimatorControllerLayer() };
        public void AddParameter(string name, AnimatorControllerParameterType type) { }
        public void AddParameter(AnimatorControllerParameter p) { }
        public static AnimatorController CreateAnimatorControllerAtPath(string path) => new AnimatorController();
        public static AnimatorController CreateAnimatorControllerAtPathWithClip(string path, AnimationClip clip)
            => new AnimatorController();
    }
}

namespace UnityEngine
{
    public class Motion : Object { }
}

namespace UnityEngine
{
    public enum TextureWrapMode { Repeat = 0, Clamp = 1, Mirror = 2 }

    public class Texture : Object { public TextureWrapMode wrapMode { get; set; } }

    public class Texture2D : Texture
    {
        public Texture2D() { }
        public Texture2D(int w, int h) { width = w; height = h; }
        public int width { get; private set; }
        public int height { get; private set; }
    }

    public static class PlayerPrefs
    {
        static readonly System.Collections.Generic.Dictionary<string, int> Ints =
            new System.Collections.Generic.Dictionary<string, int>();
        static readonly System.Collections.Generic.Dictionary<string, float> Floats =
            new System.Collections.Generic.Dictionary<string, float>();
        static readonly System.Collections.Generic.Dictionary<string, string> Strings =
            new System.Collections.Generic.Dictionary<string, string>();

        public static void SetInt(string k, int v) => Ints[k] = v;
        public static int GetInt(string k, int d = 0) => Ints.TryGetValue(k, out var v) ? v : d;
        public static void SetFloat(string k, float v) => Floats[k] = v;
        public static float GetFloat(string k, float d = 0f) => Floats.TryGetValue(k, out var v) ? v : d;
        public static void SetString(string k, string v) => Strings[k] = v;
        public static string GetString(string k, string d = "") => Strings.TryGetValue(k, out var v) ? v : d;
        public static bool HasKey(string k) => Ints.ContainsKey(k) || Floats.ContainsKey(k) || Strings.ContainsKey(k);
        public static void DeleteKey(string k) { Ints.Remove(k); Floats.Remove(k); Strings.Remove(k); }
        public static void Save() { }
    }

    public class CanvasGroup : Behaviour
    {
        public float alpha { get; set; } = 1f;
        public bool interactable { get; set; } = true;
        public bool blocksRaycasts { get; set; } = true;
    }

    public static class Random
    {
        static readonly System.Random Rng = new System.Random(12345);
        public static float value => (float)Rng.NextDouble();
        public static float Range(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
        public static int Range(int a, int b) => Rng.Next(a, b);
    }
}

namespace UnityEngine
{
    public enum HumanBodyBones
    {
        Hips = 0, LeftUpperLeg = 1, RightUpperLeg = 2, LeftLowerLeg = 3, RightLowerLeg = 4,
        LeftFoot = 5, RightFoot = 6, Spine = 7, Chest = 8, Neck = 9, Head = 10,
        LeftShoulder = 11, RightShoulder = 12, LeftUpperArm = 13, RightUpperArm = 14,
        LeftLowerArm = 15, RightLowerArm = 16, LeftHand = 17, RightHand = 18,
        LeftToes = 19, RightToes = 20, LastBone = 21
    }

    public enum QuaternionIdentity { }
}


namespace UnityEngine
{
    public class AnimationCurve
    {
        readonly System.Collections.Generic.List<Keyframe> _keys = new System.Collections.Generic.List<Keyframe>();
        public int length => _keys.Count;
        public Keyframe this[int i] => _keys[i];
        public void AddKey(float t, float v) => _keys.Add(new Keyframe(t, v));
        public void AddKey(Keyframe k) => _keys.Add(k);
        public static AnimationCurve EaseInOut(float t0, float v0, float t1, float v1)
        {
            var c = new AnimationCurve();
            c.AddKey(t0, v0); c.AddKey(t1, v1);
            return c;
        }
    }

    public struct Keyframe
    {
        public float time, value;
        public Keyframe(float t, float v) { time = t; value = v; }
    }

}

namespace UnityEditor
{
    public struct EditorCurveBinding
    {
        public string path;
        public System.Type type;
        public string propertyName;
        public static EditorCurveBinding FloatCurve(string path, System.Type type, string property) =>
            new EditorCurveBinding { path = path, type = type, propertyName = property };
    }

    public static class AnimationUtility
    {
        public static void SetEditorCurve(UnityEngine.AnimationClip clip, EditorCurveBinding binding,
            UnityEngine.AnimationCurve curve) { }
        public static UnityEngine.AnimationCurve GetEditorCurve(UnityEngine.AnimationClip clip,
            EditorCurveBinding binding) => null;
    }

    public static class SessionState
    {
        static readonly System.Collections.Generic.Dictionary<string, bool> B =
            new System.Collections.Generic.Dictionary<string, bool>();
        public static bool GetBool(string k, bool d = false) => B.TryGetValue(k, out var v) ? v : d;
        public static void SetBool(string k, bool v) => B[k] = v;
    }
}
