// ═══════════════════════════════════════════════════════════════════
//  طبقة محاكاة (stubs) لواجهات Unity API — الغرض الوحيد هو التحقق من
//  أن كود اللعبة بيترجم. مش بديل عن Unity ومش بيشغّل أي حاجة.
//  للتحقق وقت التشغيل: لازم Unity Editor.
// ═══════════════════════════════════════════════════════════════════
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Font : Object { }
    // ── أساسيات ──
    public class Object
    {
        public string name { get; set; }
        public int GetInstanceID() => GetHashCode();
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DontDestroyOnLoad(Object o) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static T FindObjectOfType<T>() where T : Object => null;
        public static T[] FindObjectsOfType<T>() where T : Object => Array.Empty<T>();
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
    }

    public class Component : Object
    {
        public GameObject gameObject { get; set; }
        public Transform transform { get; set; }
        public T GetComponent<T>() where T : class => null;
        public Component GetComponent(Type t) => null;
        public T GetComponentInChildren<T>() where T : class => null;
        public T GetComponentInParent<T>() where T : class => null;
        public T[] GetComponentsInChildren<T>() where T : class => Array.Empty<T>();
        public T AddComponent<T>() where T : Component =>
            (T)System.Activator.CreateInstance(typeof(T));
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class => Array.Empty<T>();
        public string tag { get; set; }
    }

    public class Behaviour : Component { public bool enabled { get; set; } = true; }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => new Coroutine();
        public void StopCoroutine(Coroutine c) { }
        public void StopCoroutine(IEnumerator c) { }
        public void Invoke(string m, float t) { }
        public void InvokeRepeating(string m, float t, float r) { }
        public void CancelInvoke() { }
        public void CancelInvoke(string m) { }
        public bool isActiveAndEnabled => true;
    }

    public class Coroutine { }
    public class AsyncOperation { public bool isDone => true; public float progress => 1f; public bool allowSceneActivation { get; set; } }
    public class WaitForSeconds { public WaitForSeconds(float s) { } }
    public class WaitForSecondsRealtime { public WaitForSecondsRealtime(float s) { } }
    public class WaitUntil { public WaitUntil(Func<bool> p) { } }
    public class WaitWhile { public WaitWhile(Func<bool> p) { } }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; } = Vector3.one;
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 forward => Vector3.forward;
        public Vector3 right => Vector3.right;
        public Vector3 up => Vector3.up;
        public Vector3 back => Vector3.back;
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void SetSiblingIndex(int i) { }
        public int GetSiblingIndex() => 0;
        public Transform parent { get; set; }
        public int childCount => 0;
        public void SetParent(Transform p, bool worldPositionStays = true) { }
        public Transform GetChild(int i) => null;
        public Transform Find(string n) => null;
        public void LookAt(Vector3 worldPosition) { }
        public void LookAt(Transform t) { }
        public IEnumerator GetEnumerator() => Array.Empty<Transform>().GetEnumerator();
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Rect rect { get; set; } = new Rect(0, 0, 100, 100);
    }

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public Vector2 size => new Vector2(width, height);
        public Vector2 position => new Vector2(x, y);
        public Vector2 center => new Vector2(x + width / 2, y + height / 2);
        public static bool operator ==(Rect a, Rect b) => a.x == b.x && a.y == b.y && a.width == b.width && a.height == b.height;
        public static bool operator !=(Rect a, Rect b) => !(a == b);
        public override bool Equals(object o) => o is Rect r && this == r;
        public override int GetHashCode() => x.GetHashCode();
    }

    public class GameObject : Object
    {
        public Transform transform { get; set; } = new Transform();
        public string tag { get; set; }
        public bool activeSelf => true;
        public GameObject() { }
        public GameObject(string n) { name = n; }
        public GameObject(string n, params Type[] comps) { name = n; }
        public void SetActive(bool v) { }
        public T AddComponent<T>() where T : Component =>
            (T)System.Activator.CreateInstance(typeof(T));
        public T GetComponent<T>() where T : class => null;
        public T[] GetComponentsInChildren<T>() where T : class => new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class => new T[0];
        public T GetComponentInChildren<T>() where T : class => null;
        public static GameObject CreatePrimitive(PrimitiveType t) => new GameObject();
        public static GameObject Find(string n) => null;
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }

    // ── رياضيات ──
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2(0, 0);
        public static Vector2 one => new Vector2(1, 1);
        public float magnitude => Mathf.Sqrt(x * x + y * y);
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized => magnitude > 0 ? this / magnitude : zero;
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static Vector2 ClampMagnitude(Vector2 v, float max)
        {
            float m = v.magnitude;
            if (m <= max || m < 1e-6f) return v;
            return v / m * max;
        }
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a;
        public void Normalize() { float m = magnitude; if (m > 0) { x /= m; y /= m; } }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public float magnitude => Mathf.Sqrt(x * x + y * y + z * z);
        public float sqrMagnitude => x * x + y * y + z * z;
        public Vector3 normalized => magnitude > 0 ? this / magnitude : zero;
        public void Normalize()
        {
            float m = magnitude;
            if (m > 1e-6f) { x /= m; y /= m; z /= m; }
        }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d)
        {
            var delta = b - a;
            float m = delta.magnitude;
            if (m <= d || m < 1e-6f) return b;
            return a + delta / m * d;
        }
        public static Vector3 Normalize(Vector3 v) => v.normalized;
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity => new Quaternion();
        public static Quaternion Euler(float x, float y, float z) => new Quaternion();
        public static Quaternion Euler(Vector3 e) => new Quaternion();
        public static Quaternion LookRotation(Vector3 f, Vector3 u) => new Quaternion();
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d)
        {
            // تنفيذ مبسّط كفاية للمحاكاة: نرجّع الهدف لو الفرق صغير
            return Quaternion.Slerp(a, b, d > 0f ? 1f : 0f);
        }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => t >= 0.5f ? b : a;
        public static Vector3 operator *(Quaternion q, Vector3 v) => v;
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color black => new Color(0, 0, 0, 1);
        public static Color clear => new Color(0, 0, 0, 0);
        public static Color operator *(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a * f);
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Rad2Deg = 57.29578f;
        public const float Deg2Rad = 0.0174533f;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float MoveTowards(float a, float b, float d) => a;
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d)
        {
            var delta = b - a;
            float m = delta.magnitude;
            if (m <= d || m < 1e-6f) return b;
            return a + delta / m * d;
        }
        public static float SmoothDampAngle(float c, float t, ref float v, float s) => t;
        public static float SmoothDamp(float c, float t, ref float v, float s) => t;
        public static float SmoothStep(float a, float b, float t) => a;
        public static float Sin(float f) => (float)Math.Sin(f);
        public static float Cos(float f) => (float)Math.Cos(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Abs(float f) => Math.Abs(f);
        public static int Abs(int i) => Math.Abs(i);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Sign(float f) => f < 0 ? -1f : 1f;
        public static bool Approximately(float a, float b) => Math.Abs(a - b) < 1e-5f;
        public static float Repeat(float t, float len) => t;
        public static float Atan(float f) => (float)Math.Atan(f);
        public static float Tan(float f) => (float)Math.Tan(f);
        public static float InverseLerp(float a, float b, float v) =>
            Math.Abs(b - a) < 1e-6f ? 0f : Clamp01((v - a) / (b - a));
    }

    public static class Time
    {
        public static float deltaTime { get; set; } = 0.016f;
        public static float unscaledDeltaTime => 0.016f;
        public static float time => 0f;
        public static float unscaledTime => 0f;
        public static float fixedDeltaTime => 0.02f;
        public static float timeScale { get; set; }
    }

    public static class Debug
    {
        public static void Log(object m) => Console.WriteLine("[LOG] " + m);
        public static void LogWarning(object m) => Console.WriteLine("[WARN] " + m);
        public static void LogError(object m) => Console.WriteLine("[ERR] " + m);
        public static void DrawLine(Vector3 a, Vector3 b, Color c) { }
    }

    public enum LogType { Error = 0, Assert = 1, Warning = 2, Log = 3, Exception = 4 }

    public static class ScreenCapture
    {
        public static void CaptureScreenshot(string filename) { }
        public static void CaptureScreenshot(string filename, int superSize) { }
    }

    public static class Application
    {
        public static string persistentDataPath => "/tmp/tlc";
        public static string dataPath => "/tmp/tlc";
        public static bool isMobilePlatform => false;
        public static bool isEditor => false;
        public static bool isPlaying => false;
        public static bool CanStreamedLevelBeLoaded(string levelName) => true;
        public static string unityVersion => "6000.0.28f1";
        public static int targetFrameRate { get; set; }
        public static void Quit() { }
        public delegate void LogCallback(string condition, string stackTrace, LogType type);
        public static event LogCallback logMessageReceived;
    }

    public static class SystemInfo
    {
        public static int systemMemorySize => 8000;
        public static int graphicsMemorySize => 4000;
        public static int processorCount => 8;
        public static string deviceModel => "stub";
    }

    public static class Input
    {
        public static bool GetKey(KeyCode k) => false;
        public static bool GetKeyDown(KeyCode k) => false;
        public static bool GetKeyUp(KeyCode k) => false;
        public static bool GetMouseButton(int b) => false;
        public static bool GetMouseButtonDown(int b) => false;
        public static bool GetMouseButtonUp(int b) => false;
        public static float GetAxis(string a) => 0f;
        public static float GetAxisRaw(string a) => 0f;
        public static Vector3 mousePosition => Vector3.zero;
        public static int touchCount => 0;
    }

    public enum KeyCode { W, A, S, D, E, Q, F, R, Space, Return, LeftShift, RightShift, UpArrow, DownArrow, LeftArrow, RightArrow, Escape, F1, F5, F9 }

    // ── رسومات ──
    public class Shader : Object { public static Shader Find(string n) => new Shader(); }

    public class Material : Object
    {
        public Material(Shader s) { }
        public Color color { get; set; }
        public bool HasProperty(string n) => true;
        public float GetFloat(string n) => 0f;
        public bool GetBool(string n) => false;
        public int GetInteger(string n) => 0;
        public void SetFloat(string n, float v) { }
        public void SetColor(string n, Color c) { }
        public void EnableKeyword(string k) { }
        public void DisableKeyword(string k) { }
        public void SetTexture(string n, Texture t) { }
        public void SetTextureScale(string n, Vector2 s) { }
        public void SetTextureOffset(string n, Vector2 o) { }
    }

    public class MeshRenderer : Renderer
    {
        public Material sharedMaterial { get; set; }
        public Material material { get; set; }
        public Rendering.ShadowCastingMode shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
    }

    public class MeshFilter : Component { public Mesh mesh { get; set; } }
    public class Mesh : Object { }

    public class Renderer : Component
    {
        public Material sharedMaterial { get; set; }
        public Material material { get; set; }
        public bool enabled { get; set; }
    }

    public class Collider : Component { public bool isTrigger { get; set; } public bool enabled { get; set; } }
    public class BoxCollider : Collider { public Vector3 size { get; set; } public Vector3 center { get; set; } }
    public class SphereCollider : Collider { public float radius { get; set; } }
    public class CapsuleCollider : Collider { public float height { get; set; } public float radius { get; set; } }

    public class CharacterController : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
        public Vector3 center { get; set; }
        public float slopeLimit { get; set; }
        public float stepOffset { get; set; }
        public float skinWidth { get; set; }
        public bool isGrounded => true;

        /// <summary>في Unity: Move بتنقل ترانسفورم الكائن بنفس الإزاحة. الستاب بيعمل نفس الحاجة
        /// عشان محاكاة الحركة بدون محرك تقيس إزاحة حقيقية.</summary>
        public void Move(Vector3 motion)
        {
            if (transform != null) transform.position = transform.position + motion;
        }
    }

    public struct RaycastHit
    {
        public float distance => 0f;
        public Vector3 point => Vector3.zero;
        public Vector3 normal => Vector3.up;
        public Collider collider => null;
    }

    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }

    public struct LayerMask
    {
        public int value;
        public static implicit operator int(LayerMask m) => m.value;
        public static implicit operator LayerMask(int v) => new LayerMask { value = v };
    }

    public static class Physics
    {
        public static bool Raycast(Vector3 o, Vector3 d, float max, int mask) => false;
        public static bool Raycast(Vector3 o, Vector3 d, float max, int mask, QueryTriggerInteraction q) => false;
        public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit hit, float max) { hit = new RaycastHit(); return false; }
        public static bool Raycast(Vector3 o, Vector3 d, out RaycastHit hit, float max, int mask, QueryTriggerInteraction q)
        { hit = new RaycastHit(); return false; }
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit hit, float max, int mask, QueryTriggerInteraction q)
        { hit = new RaycastHit(); return false; }
        public static bool OverlapSphere(Vector3 p, float r, int m) => false;
    }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public float fieldOfView { get; set; }
        public bool allowHDR { get; set; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
    }

    public enum CameraClearFlags { Skybox, Color, SolidColor, Depth, Nothing }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public float range { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
        public LightRenderMode renderMode { get; set; }
    }

    public enum LightType { Spot, Directional, Point, Area, Rectangle, Disc }
    public enum LightShadows { None, Hard, Soft }
    public enum LightRenderMode { Auto, ForcePixel, ForceVertex }

    public class AudioListener : Behaviour { }
    public class AudioSource : Behaviour
    {
        public AudioClip clip { get; set; }
        public float volume { get; set; }
        public bool loop { get; set; }
        public bool playOnAwake { get; set; }
        public void Play() { }
        public void PlayOneShot(AudioClip c) { }
        public void Stop() { }
    }
    public class AudioClip : Object { public float length => 1.5f; public int channels => 1; }

    public class Animator : Behaviour
    {
        public Transform GetBoneTransform(HumanBodyBones bone) => null;
        public RuntimeAnimatorController runtimeAnimatorController { get; set; }
        public bool isHuman => false;
        public Avatar avatar { get; set; }
        public AnimatorControllerParameter[] parameters => System.Array.Empty<AnimatorControllerParameter>();
        public float GetFloat(string n) => 0f;
        public bool GetBool(string n) => false;
        public int GetInteger(string n) => 0;
        public void SetFloat(string n, float v) { }
        public void SetFloat(string n, float v, float damp, float dt) { }
        public void SetBool(string n, bool v) { }
        public void SetTrigger(string n) { }
        public void SetInteger(string n, int v) { }
    }

    public class ReflectionProbe : Behaviour
    {
        public Vector3 size { get; set; }
        public Rendering.ReflectionProbeMode mode { get; set; }
        public int resolution { get; set; }
        public float intensity { get; set; }
    }

    public class ParticleSystemRenderer : Renderer
    {
        public ParticleSystemRenderMode renderMode { get; set; }
        public float lengthScale { get; set; }
        public float velocityScale { get; set; }
    }

    public class ParticleSystem : Component
    {
        public MainModule main => new MainModule();
        public EmissionModule emission => new EmissionModule();
        public ShapeModule shape => new ShapeModule();
        public void Play() { }
        public void Stop() { }
        public void Emit(int count) { }

        public struct MinMaxCurve
        {
            public MinMaxCurve(float c) { constant = c; }
            public float constant { get; set; }
            public static implicit operator MinMaxCurve(float f) => new MinMaxCurve(f);
        }
        public struct MinMaxGradient
        {
            public MinMaxGradient(Color c) { color = c; }
            public Color color { get; set; }
            public static implicit operator MinMaxGradient(Color c) => new MinMaxGradient(c);
        }
        public struct MainModule
        {
            public bool loop { get; set; }
            public MinMaxCurve startLifetime { get; set; }
            public MinMaxCurve startSpeed { get; set; }
            public MinMaxCurve startSize { get; set; }
            public MinMaxGradient startColor { get; set; }
            public int maxParticles { get; set; }
            public ParticleSystemSimulationSpace simulationSpace { get; set; }
            public float gravityModifier { get; set; }
        }
        public struct EmissionModule
        {
            public MinMaxCurve rateOverTime { get; set; }
            public bool enabled { get; set; }
        }
        public struct ShapeModule
        {
            public ParticleSystemShapeType shapeType { get; set; }
            public Vector3 scale { get; set; }
            public float radius { get; set; }
        }
    }

    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemShapeType { Sphere, Hemisphere, Cone, Box, Mesh }
    public enum ParticleSystemRenderMode { Billboard, Stretch, HorizontalBillboard, VerticalBillboard, Mesh, None }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static Color ambientLight { get; set; }
        public static float ambientIntensity { get; set; }
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; }
        public static float fogDensity { get; set; }
        public static Color fogColor { get; set; }
        public static float fogStartDistance { get; set; }
        public static float fogEndDistance { get; set; }
        public static Material skybox { get; set; }
    }

    public enum FogMode { Linear, Exponential, ExponentialSquared }

    public static class QualitySettings
    {
        public static float shadowDistance { get; set; }
        public static ShadowResolution shadowResolution { get; set; }
        public static int pixelLightCount { get; set; }
        public static float lodBias { get; set; }
        public static int antiAliasing { get; set; }
        public static int vSyncCount { get; set; }
        public static int globalTextureMipmapLimit { get; set; }
        public static void SetQualityLevel(int l) { }
    }

    public enum ShadowResolution { Low, Medium, High, VeryHigh }
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }

    /// <summary>
    /// تنفيذ حقيقي لـ JsonUtility بدل المحاكاة الفارغة — ضروري للاختبار الآلي
    /// عشان الكود الفعلي للمشروع يقرا القصة فعلاً.
    /// </summary>
    public static class JsonUtility
    {
        static readonly System.Text.Json.JsonSerializerOptions Opt =
            new System.Text.Json.JsonSerializerOptions
            {
                IncludeFields = true,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true
            };

        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, Opt);
        }

        public static string ToJson(object o) =>
            System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), Opt);

        public static string ToJson(object o, bool pretty)
        {
            var opt = new System.Text.Json.JsonSerializerOptions(Opt) { WriteIndented = pretty };
            return System.Text.Json.JsonSerializer.Serialize(o, o.GetType(), opt);
        }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object => null;
    }

    public class TextAsset : Object { public string text => ""; }

    public class ScriptableObject : Object { }

    // ── الخصائص ──
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    [AttributeUsage(AttributeTargets.Field)] public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) { } }
    [AttributeUsage(AttributeTargets.Field)] public class SpaceAttribute : Attribute { public SpaceAttribute(float s = 8) { } }
    [AttributeUsage(AttributeTargets.Class)]
    public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    [AttributeUsage(AttributeTargets.Class)]
    public class CreateAssetMenuAttribute : Attribute { public string menuName; public string fileName; }
    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { }
    }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, SubsystemRegistration }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum AmbientMode { Skybox, Trilight, Flat, Custom }
    public enum ReflectionProbeMode { Baked, Realtime, Custom }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T>(T arg);
    public class UnityEvent
    {
        public void AddListener(UnityAction a) { }
        public void RemoveListener(UnityAction a) { }
        public void RemoveAllListeners() { }
        public void Invoke() { }
    }
    public class UnityEvent<T>
    {
        public void AddListener(UnityAction<T> a) { }
        public void RemoveAllListeners() { }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;

    public class Graphic : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
    }

    public class Image : Graphic
    {
        public Sprite sprite { get; set; }
        public Type type { get; set; }
        public enum Type { Simple, Sliced, Tiled, Filled }
    }

    public class Sprite : Object { }

    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
    }

    public class Selectable : Behaviour
    {
        public ColorBlock colors { get; set; }
        public Graphic targetGraphic { get; set; }
        public bool interactable { get; set; }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
        public Camera worldCamera { get; set; }
    }

    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public enum ScreenMatchMode { MatchWidthOrHeight, Expand, Shrink }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public ScreenMatchMode screenMatchMode { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : Behaviour { }

    public class LayoutGroup : Behaviour
    {
        public TextAnchor childAlignment { get; set; }
        public RectOffset padding { get; set; }
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }

    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class LayoutElement : Behaviour
    {
        public float minWidth { get; set; }
        public float minHeight { get; set; }
        public float preferredWidth { get; set; }
        public float preferredHeight { get; set; }
        public float flexibleWidth { get; set; }
        public float flexibleHeight { get; set; }
    }

    public class ContentSizeFitter : Behaviour
    {
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }

    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform rt) { }
    }

    public static class RectTransformUtility
    {
        public static bool ScreenPointToLocalPointInRectangle(RectTransform rt, Vector2 screenPoint,
            Camera cam, out Vector2 local) { local = Vector2.zero; return true; }
    }

    public class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int l, int r, int t, int b) { left = l; right = r; top = t; bottom = b; }
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }
}

namespace UnityEngine
{
    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight
    }
}

namespace UnityEngine.EventSystems
{
    public class EventSystem : Behaviour
    {
        public static EventSystem current { get; set; }
    }

    public class BaseInputModule : Behaviour { }
    public class StandaloneInputModule : BaseInputModule { }
    public class InputSystemUIInputModule : BaseInputModule { }

    public class PointerEventData
    {
        public Vector2 position { get; set; }
        public Vector2 delta { get; set; }
        public Vector2 pressPosition { get; set; }
        public Camera pressEventCamera { get; set; }
        public GameObject pointerPress { get; set; }
        public int pointerId { get; set; }
    }

    public interface IEventSystemHandler { }
    public interface IPointerDownHandler : IEventSystemHandler { void OnPointerDown(PointerEventData e); }
    public interface IPointerUpHandler : IEventSystemHandler { void OnPointerUp(PointerEventData e); }
    public interface IPointerClickHandler : IEventSystemHandler { void OnPointerClick(PointerEventData e); }
    public interface IDragHandler : IEventSystemHandler { void OnDrag(PointerEventData e); }
    public interface IBeginDragHandler : IEventSystemHandler { void OnBeginDrag(PointerEventData e); }
    public interface IEndDragHandler : IEventSystemHandler { void OnEndDrag(PointerEventData e); }
}

namespace TMPro
{
    using UnityEngine;
    using UnityEngine.UI;

    public enum TextAlignmentOptions
    {
        TopLeft, Top, TopRight, TopJustified, TopFlush, TopGeoAligned,
        Left, Center, Right, Justified, Flush, CenterGeoAligned,
        BottomLeft, Bottom, BottomRight, BottomJustified, BottomFlush, BaselineGeoAligned,
        MidlineLeft, Midline, MidlineRight, CaplineLeft, Capline, CaplineRight
    }

    public enum TextOverflowModes { Overflow, Ellipsis, Masking, Truncate, ScrollRect, Page, Linked }
    public enum FontStyles { Normal, Bold, Italic, Underline, LowerCase, UpperCase, SmallCaps, Strikethrough, Superscript, Subscript, Highlight }

    public class TMP_TextInfo
    {
        public int characterCount { get; set; }
        public int lineCount { get; set; }
        public int wordCount { get; set; }
    }

    public class TMP_Text : MonoBehaviour
    {
        public string text { get; set; }
        public float fontSize { get; set; }
        public Color color { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public bool enableWordWrapping { get; set; }
        public bool raycastTarget { get; set; }
        public bool isRightToLeftText { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public float lineSpacing { get; set; }
        public FontStyles fontStyle { get; set; }
        public int maxVisibleCharacters { get; set; }
        public TMP_TextInfo textInfo { get; } = new TMP_TextInfo();
        public RectTransform rectTransform { get; set; } = new RectTransform();
        public TMP_FontAsset font { get; set; }
        public void ForceMeshUpdate() { }
    }

    public class TMP_FontAsset : UnityEngine.Object
    {
        public static TMP_FontAsset CreateFontAsset(UnityEngine.Font font) => null;
        public static TMP_FontAsset defaultFontAsset { get; set; }
    }

    public class TextMeshProUGUI : TMP_Text { }
    public class TextMeshPro : TMP_Text { }
}

namespace Unity.Cinemachine
{
    using UnityEngine;

    public struct LensSettings
    {
        public float FieldOfView;
        public float NearClipPlane;
        public float FarClipPlane;
    }

    public class CinemachineCamera : MonoBehaviour
    {
        public int Priority { get; set; }
        public Transform Follow { get; set; }
        public Transform LookAt { get; set; }
        public LensSettings Lens { get; set; }
        public bool IsValid => true;
    }

    public class CinemachineBrain : MonoBehaviour { public Camera OutputCamera { get; set; } }
    public class CinemachineImpulseSource : MonoBehaviour { public void GenerateImpulse() { } }
}
