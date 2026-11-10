using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// مصوّر مشهد اللعب من **جوه Unity** (Play Mode حقيقي) — مش محاكاة خارجية.
    /// بيتشغّل من سطر الأوامر على سيرفر بلا شاشة:
    ///   Unity -projectPath . -executeMethod TLC.EditorTools.PlayModeCapture.Run -captureDir Screenshots/unity
    /// وبيدخل Play Mode، يستنى الـBootstrap يبني المشهد، يمشي على لستة لقطات،
    /// يحفظ كل لقطة PNG بـScreenCapture، ويقيس FPS حقيقي، وبعدين يخرج.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeCapture
    {
        static string _outDir = "Screenshots/unity";
        static List<ShotDef> _shots;
        static int _index;
        static int _frameInShot;
        static bool _playStarted;
        static double _playStartTime;
        static readonly List<string> _log = new List<string>();
        static readonly List<float> _frameTimes = new List<float>();

        static string _scene = "Assets/_Project/Scenes/Play.unity";
        static int _warmupFrames = 90;
        static int _framesPerShot = 8;
        static bool _running;

        class ShotDef
        {
            public string name;
            public Vector3 pos;
            public Vector3 look;
            public float fov = 48f;
            public string dialogueSpeaker;   // لو مش فاضي: يظهر حوار على الشاشة
            public string dialogueText;
        }

        const string SessionKey = "TLC_CaptureActive";
        const string SessionDirKey = "TLC_CaptureDir";

        static PlayModeCapture()
        {
            // الدخول في Play Mode بيعمل Domain Reload ⇒ الاشتراك في update بيتمسح.
            // بنستأنف من SessionState عشان التصوير يكمل بدل ما يقف صامت.
            if (SessionState.GetBool(SessionKey, false))
            {
                _outDir = SessionState.GetString(SessionDirKey, "Screenshots/unity");
                _running = true;
                _playStarted = false;
                _index = 0;
                _frameInShot = 0;
                _frameTimes.Clear();
                EditorApplication.update += Tick;
                Debug.Log("[TLC/Capture] استأنفت بعد إعادة تحميل الأسمبلي");
            }
        }

        // ────────────────────────── نقطة الدخول ──────────────────────────
        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-captureDir" && i + 1 < args.Length) _outDir = args[i + 1];
                if (args[i] == "-captureScene" && i + 1 < args.Length) _scene = args[i + 1];
            }
            Directory.CreateDirectory(_outDir);

            if (!File.Exists(_scene))
            {
                Log("✗ المشهد مش موجود: " + _scene);
                Flush();
                EditorApplication.Exit(2);
                return;
            }

            EditorSceneManager.OpenScene(_scene);
            Log("فتحت المشهد: " + _scene);
            Log("Unity: " + Application.unityVersion + " · Graphics: " + SystemInfo.graphicsDeviceName +
                " · API: " + SystemInfo.graphicsDeviceType);

            SessionState.SetBool(SessionKey, true);
            SessionState.SetString(SessionDirKey, _outDir);
            EditorApplication.update += Tick;
            _running = true;
            EditorApplication.isPlaying = true;
        }

        // ────────────────────────── الحلقة ──────────────────────────
        static void Tick()
        {
            if (!_running) return;

            if (!EditorApplication.isPlaying)
            {
                Log("✗ Play Mode ما اشتغلش");
                Finish(3);
                return;
            }

            if (!_playStarted)
            {
                _playStarted = true;
                _playStartTime = EditorApplication.timeSinceStartup;
                ForceQualityHigh();
                BuildShots();
                _frameInShot = 0;
                _index = 0;
                Log("Play Mode بدأ — هستنى الـBootstrap يبني المشهد…");
                return;
            }

            // نسجّل توقيت الإطارات بعد الإحماء (بيانات أداء حقيقية)
            if (Time.frameCount > _warmupFrames && Time.unscaledDeltaTime > 0f && Time.unscaledDeltaTime < 1f)
                _frameTimes.Add(Time.unscaledDeltaTime);

            if (Time.frameCount < _warmupFrames) return;

            if (_index >= _shots.Count)
            {
                Finish(0);
                return;
            }

            var shot = _shots[_index];

            if (_frameInShot == 0) ApplyShot(shot);

            _frameInShot++;
            if (_frameInShot >= _framesPerShot)
            {
                Capture(shot);
                _index++;
                _frameInShot = 0;
            }
        }

        /// <summary>التصوير محتاج أعلى جودة — مش نخلي «الجهاز تحت الحمل» ينزّل المستوى.</summary>
        static void ForceQualityHigh()
        {
            var qm = UnityEngine.Object.FindFirstObjectByType<TLC.Settings.QualityManager>();
            if (qm == null) { Log("(مفيش QualityManager)"); return; }
            try
            {
                qm.SetTierFromUser(TLC.Settings.QualityManager.Tier.High);
                Log("الجودة اتثبّتت على: " + qm.CurrentTier);
            }
            catch (System.Exception e) { Log("(تعذر تثبيت الجودة: " + e.Message + ")"); }
        }

        static void ApplyShot(ShotDef shot)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.enabled) { cam = c; break; }
            }
            if (cam == null) { Log("✗ مفيش كاميرا في المشهد"); return; }

            // نعطّل ريج الكاميرا مؤقتًا عشان اللقطة تفضل ثابتة
            var rig = cam.GetComponent<TLC.CameraRig.ThirdPersonCameraRig>();
            if (rig != null) rig.enabled = false;
            var rigOnParent = cam.GetComponentInParent<TLC.CameraRig.ThirdPersonCameraRig>();
            if (rigOnParent != null) rigOnParent.enabled = false;

            Vector3 safePos = SafeCameraPos(shot.look, shot.pos);
            cam.transform.position = safePos;
            cam.transform.LookAt(shot.look);
            cam.fieldOfView = shot.fov;
            cam.nearClipPlane = 0.05f;

            if (!string.IsNullOrEmpty(shot.dialogueText)) ShowDialogue(shot.dialogueSpeaker, shot.dialogueText);
        }

        /// <summary>
        /// لو مكان الكاميرا جوه حيطة/مجسم، نقرّبها من نقطة التصويب لحد أول سطح حر.
        /// ده نفس مبدأ ThirdPersonCameraRig عشان اللقطات تبقى من مساحة حقيقية.
        /// </summary>
        static Vector3 SafeCameraPos(Vector3 lookAt, Vector3 want)
        {
            Vector3 dir = want - lookAt;
            float dist = dir.magnitude;
            if (dist < 0.01f) return want;
            dir /= dist;

            if (Physics.SphereCast(lookAt, 0.14f, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                float safe = Mathf.Max(0.5f, hit.distance - 0.18f);
                Log("   ⚠ الكاميرا كانت جوه " + hit.collider.name + " — اتقرّبت لـ" + safe.ToString("0.00") + "م");
                return lookAt + dir * safe;
            }
            return want;
        }

        static void ShowDialogue(string speaker, string text)
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<TLC.UI.DialogueUI>();
            if (ui == null) { Log("  (مفيش DialogueUI في المشهد)"); return; }
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance;
            try
            {
                var startTyping = ui.GetType().GetMethod("StartTyping", flags);
                var show = ui.GetType().GetMethod("ShowDialogue", flags);
                if (startTyping != null) startTyping.Invoke(ui, new object[] { text });
                if (show != null) show.Invoke(ui, null);
                Log("  (الحوار اتعرض على الشاشة)");
            }
            catch (Exception e) { Log("  (تعذر إظهار الحوار: " + e.Message + ")"); }
        }

        static void Capture(ShotDef shot)
        {
            var cam = Camera.main;
            if (cam == null)
                foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.enabled) { cam = c; break; }
            if (cam == null) { Log("✗ مفيش كاميرا للتصوير"); return; }

            const int W = 1280, H = 720;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 1;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;

                var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();

                var path = Path.Combine(_outDir, shot.name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                Log("📷 " + shot.name + ".png  (FOV " + shot.fov + " · " + W + "×" + H + ")");

                // تشخيص: هل الصورة فاضية؟
                Log("   سطوع عيّنات: " + SampleBrightness(rt));
            }
            catch (Exception e) { Log("✗ فشل التصوير: " + e.Message); }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        /// <summary>بيقرأ ٥ بكسلات من اللقطة عشان نعرف إذا كانت الصورة فاضية (أسود/أبيض).</summary>
        static string SampleBrightness(RenderTexture rt)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var t = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            t.Apply();
            RenderTexture.active = prev;

            float sum = 0f; int n = 0;
            var pts = new[] { (10, 10), (rt.width / 2, rt.height / 3), (rt.width - 10, 10),
                              (rt.width / 2, rt.height / 2), (rt.width - 20, rt.height - 20) };
            var sb = new System.Text.StringBuilder();
            foreach (var (x, y) in pts)
            {
                var c = t.GetPixel(Mathf.Clamp(x, 0, rt.width - 1), Mathf.Clamp(y, 0, rt.height - 1));
                float l = (c.r + c.g + c.b) / 3f;
                sum += l; n++;
                sb.Append(l.ToString("0.00")).Append(' ');
            }
            UnityEngine.Object.DestroyImmediate(t);
            return sb.ToString().Trim() + " (متوسط " + (sum / n).ToString("0.00") + ")";
        }

        static void Finish(int code)
        {
            _running = false;
            EditorApplication.update -= Tick;

            // إحصاء الأداء الحقيقي
            if (_frameTimes.Count > 10)
            {
                float total = 0f, worst = 0f;
                foreach (var t in _frameTimes) { total += t; if (t > worst) worst = t; }
                float avg = total / _frameTimes.Count;
                Log("── الأداء (Play Mode حقيقي) ──");
                Log("  إطارات مقيسة : " + _frameTimes.Count);
                Log("  متوسط FPS    : " + (1f / avg).ToString("0.0"));
                Log("  أضعف إطار    : " + (1f / worst).ToString("0.0") + " FPS (" + (worst * 1000f).ToString("0") + " ms)");
                Log("  متوسط الإطار : " + (avg * 1000f).ToString("1") + " ms");
                Log("  Renderer     : " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsDeviceType + ")");
            }
            else Log("  (عدد إطارات غير كافٍ لقياس الأداء)");

            Log("── خلص: " + _index + " لقطة من " + (_shots?.Count ?? 0) + " ──");
            Flush();
            SessionState.EraseBool(SessionKey);

            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
            // شبكة أمان لو delayCall ما اتنفذش
            var deadline = EditorApplication.timeSinceStartup + 8;
            EditorApplication.update += () =>
            {
                if (EditorApplication.timeSinceStartup > deadline) EditorApplication.Exit(code);
            };
        }

        static void Log(string s)
        {
            _log.Add(s);
            Debug.Log("[TLC/Capture] " + s);
        }

        static void Flush()
        {
            try
            {
                Directory.CreateDirectory(_outDir);
                var path = Path.Combine(_outDir, "capture-log.txt");
                File.WriteAllLines(path, _log);
                Debug.Log("[TLC/Capture] التقرير اتكتب في " + path);
            }
            catch (Exception e) { Debug.LogWarning("[TLC/Capture] تعذر كتابة التقرير: " + e.Message); }
        }

        // ────────────────────────── اللقطات ──────────────────────────
        static void BuildShots()
        {
            _shots = new List<ShotDef>();
            Vector3 adam = FindAdam(out Vector3 head, out Vector3 forward, out float height);
            Log("آدم: " + adam.ToString("0.00") + " · الراس: " + head.ToString("0.00") + " · الطول: " + height.ToString("0.00"));

            // آدم مش موجود؟ نرجع لمركز المشهد
            if (adam == Vector3.zero) { adam = new Vector3(0, 0, 2f); head = adam + Vector3.up * 1.6f; forward = Vector3.forward; }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (right.sqrMagnitude < 0.01f) right = Vector3.right;

            // ١ — لقطة اللعب الأساسية (من ورا الكتف)
            _shots.Add(new ShotDef
            {
                name = "01_gameplay_over_shoulder",
                pos = adam - forward * 3.1f + Vector3.up * 1.35f + right * 0.30f,
                look = adam + Vector3.up * 1.25f,
                fov = 48f
            });

            // ٢ — مواجهة (وش آدم)
            _shots.Add(new ShotDef
            {
                name = "02_gameplay_front",
                pos = adam + forward * 2.05f + Vector3.up * 1.52f,
                look = adam + Vector3.up * 1.35f,
                fov = 42f
            });

            // ٣ — لقطة قريبة للوجه (حوار)
            _shots.Add(new ShotDef
            {
                name = "03_dialogue_closeup",
                pos = adam + forward * 1.15f + Vector3.up * (height * 0.93f) + right * 0.22f,
                look = head,
                fov = 34f,
                dialogueSpeaker = "آدم",
                dialogueText = "أنا مأخدتش القرار ده لوحدي… فيه حاجات أقوى مني بتحرّك الخيوط."
            });

            // ٤ — لقطة عريضة للشقة
            _shots.Add(new ShotDef
            {
                name = "04_apartment_wide",
                pos = adam - forward * 1.5f + Vector3.up * 2.15f - right * 1.6f,
                look = adam + Vector3.up * 0.9f,
                fov = 62f
            });

            // ٥ — فوق الكتف من الناحية التانية (لقطة سينمائية)
            _shots.Add(new ShotDef
            {
                name = "05_cinematic_reverse",
                pos = adam + forward * 1.85f - right * 1.15f + Vector3.up * 1.62f,
                look = head,
                fov = 40f,
                dialogueSpeaker = "ليلى",
                dialogueText = "لو قلت الحقيقة دلوقتي، ممكن تخسر كل حاجة… وممكن تنجّي حد."
            });

            // ٦ — لقطة منخفضة (زاوية دراماتيكية)
            _shots.Add(new ShotDef
            {
                name = "06_low_angle",
                pos = adam + forward * 2.0f - right * 0.9f + Vector3.up * 0.55f,
                look = head + Vector3.up * 0.15f,
                fov = 38f
            });

            // ٧ — داخل الشقة: نظرة على التفاصيل
            _shots.Add(new ShotDef
            {
                name = "07_interior_detail",
                pos = adam + right * 2.4f - forward * 1.8f + Vector3.up * 1.55f,
                look = adam + right * 3.2f + Vector3.up * 1.0f,
                fov = 55f
            });

            // ٨ — الطريق اللي آدم بيمشي فيه (اختبار اصطدام الكاميرا)
            _shots.Add(new ShotDef
            {
                name = "08_wall_proximity",
                pos = adam - right * 1.35f + forward * 1.4f + Vector3.up * 1.6f,
                look = head,
                fov = 45f
            });
        }

        static Vector3 FindAdam(out Vector3 head, out Vector3 forward, out float height)
        {
            head = Vector3.zero; forward = Vector3.forward; height = 1.8f;
            var ctrl = UnityEngine.Object.FindFirstObjectByType<TLC.Player.ThirdPersonController>();
            if (ctrl != null)
            {
                var t = ctrl.transform;
                forward = t.forward; forward.y = 0f;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                forward.Normalize();

                var anim = ctrl.GetComponentInChildren<Animator>();
                if (anim != null && anim.isHuman) head = anim.GetBoneTransform(HumanBodyBones.Head).position;
                else head = t.position + Vector3.up * 1.62f;

                var renderers = ctrl.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    var b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    height = Mathf.Max(0.5f, b.max.y - t.position.y);
                }
                return t.position;
            }
            return Vector3.zero;
        }
    }
}
