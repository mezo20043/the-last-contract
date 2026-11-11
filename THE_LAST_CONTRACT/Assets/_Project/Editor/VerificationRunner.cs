#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TLC.Bootstrap;
using TLC.CameraRig;
using TLC.Player;
using TLC.UI;
using TLC.World;

namespace TLC.EditorTools
{
    /// <summary>
    /// أداة التحقق النهائي — بتطلّع الأدلة اللي مطلوبة على أي جهاز فيه Unity:
    /// تقرير أنظمة، سجل Console، ولقطات شاشة من مشهد اللعب نفسه.
    ///
    /// ⚠️ ليه الأداة دي موجودة:
    /// المشروع ده اتجهّز في بيئة **مفيهاش Unity** (تقدر تشوف الإثبات في
    /// VERIFICATION/ENVIRONMENT.txt). كل اللي اتقال عن الكود مبني على فحص
    /// ساكن واختبار آلي 208 فحص — لكن مفيش لقطة واحدة من المحرك، ولا سجل
    /// Console حقيقي، ولا APK. الأداة دي بتخلي أول واحد يفتح المشروع على
    /// جهاز فيه Unity يطلّع الأدلة دي كلها بضغطة واحدة، بدل ما نزعم حاجة
    /// إحنا ما شفناهاش.
    ///
    /// الاستخدام:
    ///   TLC → 8 · تقرير التحقق الشامل            (فحص محرر، من غير تشغيل)
    ///   TLC → 9 · تشغيل تلقائي + لقطات + سجل     (بيقتح Play ويصوّر ويخرج)
    ///
    /// المخرجات في مجلد VerificationOut/ جوه المشروع (اسم مميّز عن VERIFICATION/ المتعقَّب).
    /// </summary>
    public static class VerificationRunner
    {
        const string MainScene = "Assets/_Project/Scenes/Main.unity";
        const string PlayScene = "Assets/_Project/Scenes/Play.unity";
        const string ControllerPath = "Assets/_Project/Resources/Animators/TLC_Character.controller";

        static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
        static string OutDir => Path.Combine(ProjectRoot, "VerificationOut");
        static string ShotDir => Path.Combine(OutDir, "Screenshots");

        // ═══════════════════════ ١) فحص محرر ═══════════════════════

        [MenuItem("TLC/12 · تقرير التحقق الشامل", priority = 120)]
        public static void RunEditorVerification()
        {
            Directory.CreateDirectory(OutDir);
            var sb = new StringBuilder();
            int pass = 0, fail = 0;

            void Line(bool ok, string label, string detail = "")
            {
                if (ok) pass++; else fail++;
                sb.AppendLine($"  {(ok ? "✓" : "✗")} {label}{(string.IsNullOrEmpty(detail) ? "" : " — " + detail)}");
            }

            sb.AppendLine("════════ THE LAST CONTRACT — تقرير التحقق (محرر) ════════");
            sb.AppendLine($"التاريخ : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Unity   : {Application.unityVersion}");
            sb.AppendLine($"المنصة  : {EditorUserBuildSettings.activeBuildTarget}");
            sb.AppendLine();

            // ── ١) المشاهد ──
            sb.AppendLine("── ١) المشاهد ──");
            Line(File.Exists(MainScene), "Main.unity موجود", MainScene);
            Line(File.Exists(PlayScene), "Play.unity موجود", PlayScene);

            bool mainInBuild = false, playInBuild = false;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.enabled) continue;
                if (s.path == MainScene) mainInBuild = true;
                if (s.path == PlayScene) playInBuild = true;
            }
            Line(mainInBuild, "Main في Build Settings");
            Line(playInBuild, "Play في Build Settings");

            // ── ٢) Animator Controller ──
            sb.AppendLine();
            sb.AppendLine("── ٢) Animator Controller ──");
            bool hadController = File.Exists(ControllerPath);
            if (!hadController)
            {
                sb.AppendLine("  … المشروع لسه ماولّدش الـ controller — بجربه دلوقتي");
                AnimatorControllerBuilder.Build();
            }

            bool hasController = File.Exists(ControllerPath);
            Line(hasController, "TLC_Character.controller موجود في Resources/Animators", ControllerPath);

            if (hasController)
            {
                var rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                Line(rac != null, "الـ controller بيتحمّل كـ RuntimeAnimatorController");

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/_Project/Resources/Animators/idle.anim");
                Line(clip != null, "كليب Idle المولّد موجود",
                     clip != null ? $"{clip.length:0.00} ثانية" : "مش موجود");
            }

            // ── ٣) الـ Avatars ──
            sb.AppendLine();
            sb.AppendLine("── ٣) Humanoid Avatars ──");
            string charRoot = "Assets/_Project/Resources/Characters";
            var fbxFiles = Directory.Exists(Path.Combine(ProjectRoot, charRoot))
                ? Directory.GetFiles(Path.Combine(ProjectRoot, charRoot), "*.fbx", SearchOption.AllDirectories)
                : new string[0];

            Line(fbxFiles.Length == 4, $"عدد موديلات FBX: {fbxFiles.Length}/4");

            foreach (var abs in fbxFiles)
            {
                string path = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                string name = Path.GetFileName(path);

                if (importer == null)
                {
                    Line(false, $"{name}: مش مقروء كموديل");
                    continue;
                }

                bool humanoid = importer.animationType == ModelImporterAnimationType.Human;
                Line(humanoid, $"{name}: نوع الحركة Humanoid", importer.animationType.ToString());

                Avatar avatar = null;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is Avatar a) { avatar = a; break; }

                if (avatar == null)
                    Line(false, $"{name}: مفيش Avatar متولّد — اصلح الـ Rig ثم Apply");
                else
                    Line(avatar.isHuman, $"{name}: Avatar صالح و Humanoid",
                         avatar.isHuman ? "isHuman = true" : "isHuman = false");
            }

            // ── ٤) مكوّنات مشهد اللعب ──
            sb.AppendLine();
            sb.AppendLine("── ٤) محتوى مشهد اللعب ──");
            if (File.Exists(PlayScene))
            {
                var scene = EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);
                int missing = 0;
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var c in root.GetComponentsInChildren<Component>(true))
                        if (c == null) missing++;

                Line(missing == 0, "Missing Scripts", missing == 0 ? "0" : $"{missing} كائن");

                Line(UnityEngine.Object.FindObjectOfType<AdamBuilder>() != null, "آدم موجود في المشهد");
                Line(UnityEngine.Object.FindObjectOfType<Camera>() != null, "كاميرا موجودة");
                Line(UnityEngine.Object.FindObjectOfType<ThirdPersonCameraRig>() != null, "متحكم الكاميرا موجود");
                Line(UnityEngine.Object.FindObjectOfType<Light>() != null, "إضاءة موجودة");
                Line(UnityEngine.Object.FindObjectOfType<ApartmentBuilder>() != null, "بيئة الشقة موجودة");
                Line(UnityEngine.Object.FindObjectOfType<MobileControls>() != null, "واجهة تحكم الموبايل موجودة");
                Line(UnityEngine.Object.FindObjectOfType<OrientationWatcher>() != null, "مدير الاتجاه موجود");
                Line(UnityEngine.Object.FindObjectOfType<GameBootstrap>() != null, "GameBootstrap موجود");
            }

            // ── ٥) موارد ──
            sb.AppendLine();
            sb.AppendLine("── ٥) الموارد ──");
            Line(Resources.Load<TextAsset>("Story/chapter01") != null, "الفصل الأول");
            Line(Resources.Load<TextAsset>("Story/chapter02") != null, "الفصل الثاني");
            Line(Resources.Load<TextAsset>("Story/chapter03") != null, "الفصل الثالث");
            Line(Resources.Load<Font>("Fonts/Amiri-Regular") != null, "خط عربي Amiri");

            // ── ٦) الإعدادات ──
            sb.AppendLine();
            sb.AppendLine("── ٦) إعدادات المنصة ──");
            Line(PlayerSettings.colorSpace == ColorSpace.Linear, "Color Space = Linear");
            Line(PlayerSettings.defaultInterfaceOrientation == UIOrientation.AutoRotation,
                 "الدوران تلقائي (رأسي + أفقي)");
            Line(PlayerSettings.GetPropertyInt("activeInputHandler") != 0,
                 "Active Input Handling", PlayerSettings.GetPropertyInt("activeInputHandler").ToString());

            sb.AppendLine();
            sb.AppendLine($"── النتيجة: {pass} نجح · {fail} فشل ──");

            string report = sb.ToString();
            File.WriteAllText(Path.Combine(OutDir, "editor-report.txt"), report, Encoding.UTF8);

            if (fail == 0) Debug.Log(report);
            else Debug.LogError(report);

            Debug.Log($"[TLC/Verify] التقرير اتكتب في VerificationOut/editor-report.txt ({pass}/{pass + fail})");
        }

        // ═══════════════════ ٢) تشغيل تلقائي + لقطات ═══════════════════

        static float _t;
        static int _step;
        static bool _running;
        static GameObject _player;
        static PlayerInputReader _input;
        static ThirdPersonCameraRig _cameraRig;
        static MobileControls _mobile;
        static Animator _animator;
        static Vector3 _startPos;
        static float _maxSpeed;
        static float _startYaw;
        static float _yawAfterLook;
        static readonly List<string> _console = new List<string>();
        static readonly List<string> _steps = new List<string>();
        static bool _cinematicWasOn;

        [MenuItem("TLC/13 · تشغيل تلقائي + لقطات + سجل", priority = 130)]
        public static void RunPlayTest()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[TLC/Verify] المحرر في وضع التشغيل بالفعل — اقفله الأول.");
                return;
            }

            if (!File.Exists(PlayScene))
            {
                Debug.LogError($"[TLC/Verify] مشهد اللعب مش موجود: {PlayScene}");
                return;
            }

            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(ShotDir);

            _console.Clear();
            _steps.Clear();
            _t = 0f; _step = 0; _running = true;
            _maxSpeed = 0f;
            _yawAfterLook = 0f;

            EditorSceneManager.OpenScene(PlayScene, OpenSceneMode.Single);

            // بنقفل السينمائية الافتتاحية **في الذاكرة بس** عشان الاختبار
            // يقيس الحركة من أول ثانية. الملف نفسه ما بيتغيّرش.
            var boot = UnityEngine.Object.FindObjectOfType<GameBootstrap>();
            if (boot != null)
            {
                var so = new SerializedObject(boot);
                var prop = so.FindProperty("openWithCinematic");
                if (prop != null) { _cinematicWasOn = prop.boolValue; prop.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            Application.logMessageReceived += OnLog;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;

            Debug.Log("[TLC/Verify] بدأ الاختبار التلقائي — لقطات + قياسات في VerificationOut/.");
        }

        static void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Log && stack.Length == 0)
                _console.Add($"[{type}] {condition}");
            else
                _console.Add($"[{type}] {condition}\n{stack}");
        }

        static void Tick()
        {
            if (!_running) return;
            if (!EditorApplication.isPlaying) return;

            _t += Time.unscaledDeltaTime;

            // أول فريم بعد البداية: نلقط المراجع
            if (_step == 0 && _t > 0.5f)
            {
                var controller = UnityEngine.Object.FindObjectOfType<ThirdPersonController>();
                _input = UnityEngine.Object.FindObjectOfType<PlayerInputReader>();
                _cameraRig = UnityEngine.Object.FindObjectOfType<ThirdPersonCameraRig>();
                _mobile = UnityEngine.Object.FindObjectOfType<MobileControls>();

                if (controller != null)
                {
                    _player = controller.gameObject;
                    _startPos = _player.transform.position;
                    _animator = _player.GetComponentInChildren<Animator>();
                }

                _startYaw = _cameraRig != null ? _cameraRig.transform.eulerAngles.y : 0f;

                _steps.Add($"[t={_t:0.0}s] آدم: {(_player != null ? "موجود" : "مفقود")}");
                _steps.Add($"[t={_t:0.0}s] إدخال: {(_input != null ? "موجود" : "مفقود")}");
                _steps.Add($"[t={_t:0.0}s] كاميرا: {(_cameraRig != null ? "موجودة" : "مفقودة")}");
                _steps.Add($"[t={_t:0.0}s] واجهة موبايل مبنية: {(_mobile != null && _mobile.IsBuilt)}");
                _steps.Add($"[t={_t:0.0}s] Animator مربوط: {(_animator != null && _animator.runtimeAnimatorController != null)}");

                Shot("01_idle");
                _step = 1;
                return;
            }

            if (_input == null) { if (_t > 3f && _step < 9) { _step = 9; } return; }

            switch (_step)
            {
                case 1 when _t > 2.0f:
                    Shot("01_idle");
                    _steps.Add($"[t={_t:0.0}s] سكون: سرعة {Speed():0.00} · Animator.Speed {AnimSpeed():0.00}");
                    _input.SetMobileMove(new Vector2(0f, 1f));          // عصا لقدام
                    _step = 2;
                    break;

                case 2 when _t > 3.6f:
                    Shot("02_walk");
                    _steps.Add($"[t={_t:0.0}s] مشي: سرعة {Speed():0.00} · Animator.Speed {AnimSpeed():0.00}");
                    _input.SetMobileRun(true);                            // جري
                    _step = 3;
                    break;

                case 3 when _t > 5.2f:
                    Shot("03_run");
                    _steps.Add($"[t={_t:0.0}s] جري: سرعة {Speed():0.00} · Animator.Speed {AnimSpeed():0.00}");
                    _maxSpeed = Mathf.Max(_maxSpeed, Speed());
                    _input.SetMobileLook(new Vector2(6f, 0f));            // دوران كاميرا
                    _yawAfterLook = _cameraRig != null ? _cameraRig.transform.eulerAngles.y : 0f;
                    _step = 4;
                    break;

                case 4 when _t > 6.4f:
                    Shot("04_camera_turn");
                    _steps.Add($"[t={_t:0.0}s] كاميرا: yaw {_startYaw:0.0}° → {_yawAfterLook:0.0}°");
                    _input.SetMobileMove(Vector2.zero);
                    _input.SetMobileRun(false);
                    _input.TriggerInteract();                              // زر التفاعل
                    _steps.Add($"[t={_t:0.0}s] زر التفاعل اتبعت");
                    _step = 5;
                    break;

                case 5 when _t > 7.6f:
                    Shot("05_stop");
                    _steps.Add($"[t={_t:0.0}s] وقوف: سرعة {Speed():0.00} · Animator.Speed {AnimSpeed():0.00}");
                    float moved = _player != null ? Vector3.Distance(_startPos, _player.transform.position) : 0f;
                    _steps.Add($"[t={_t:0.0}s] إجمالي الحركة: {moved:0.00} متر · أعلى سرعة {_maxSpeed:0.00}");
                    _step = 6;
                    break;

                case 6 when _t > 8.6f:
                    Finish();
                    break;
            }
        }

        static float Speed()
        {
            var c = UnityEngine.Object.FindObjectOfType<ThirdPersonController>();
            return c != null ? c.CurrentSpeed : -1f;
        }

        static float AnimSpeed()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return -1f;
            try { return _animator.GetFloat("Speed"); } catch { return -1f; }
        }

        static void Shot(string name)
        {
            try
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(ShotDir, name + ".png"));
                _steps.Add($"  📷 {name}.png");
            }
            catch (Exception e)
            {
                _steps.Add($"  ✗ فشلت اللقطة {name}: {e.Message}");
            }
        }

        static void Finish()
        {
            _running = false;
            EditorApplication.update -= Tick;

            var sb = new StringBuilder();
            sb.AppendLine("════════ تقرير التشغيل التلقائي ════════");
            sb.AppendLine($"التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Unity  : {Application.unityVersion}");
            sb.AppendLine($"المشهد : {PlayScene}");
            sb.AppendLine("ملاحظة: السينمائية الافتتاحية اتقفلت في الذاكرة بس عشان القياس يبدأ فوراً.");
            sb.AppendLine();
            sb.AppendLine("── الخطوات والقياسات ──");
            foreach (var s in _steps) sb.AppendLine("  " + s);
            sb.AppendLine();
            sb.AppendLine("── سجل Console (" + _console.Count + " سطر) ──");
            foreach (var c in _console) sb.AppendLine("  " + c);

            File.WriteAllText(Path.Combine(OutDir, "play-report.txt"), sb.ToString(), Encoding.UTF8);
            File.WriteAllText(Path.Combine(OutDir, "console.txt"),
                              string.Join(Environment.NewLine, _console), Encoding.UTF8);

            Application.logMessageReceived -= OnLog;
            EditorApplication.isPlaying = false;

            Debug.Log("[TLC/Verify] خلص. المخرجات في VerificationOut/ " +
                      "(play-report.txt · console.txt · Screenshots/). " +
                      "شوف اللقطات واتأكد إن آدم والشقة باينين فعلاً قبل أي حكم.");
        }

        [MenuItem("TLC/14 · فتح مجلد التحقق", priority = 140)]
        public static void OpenOutDir()
        {
            Directory.CreateDirectory(OutDir);
            EditorUtility.RevealInFinder(OutDir);
        }
    }
}
#endif
