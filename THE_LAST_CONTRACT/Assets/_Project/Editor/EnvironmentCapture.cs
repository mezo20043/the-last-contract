using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TLC.Bootstrap;
using TLC.World;

namespace TLC.EditorTools
{
    /// <summary>
    /// تصوير كل المواقع (PHASE 4.2) من داخل Play Mode الحقيقي:
    ///   • لقطة **لعب بمنظور الشخص الثالث** (آدم ظاهر من ورا) جوه كل موقع وبره المدخل.
    ///   • لقطة عامة داخلية + لقطة خارجية للواجهة والشارع.
    ///
    /// الكاميرا بتاعة اللعب نفسها هي اللي بتصوّر لقطات الشخص الثالث (مش كاميرا بديلة)،
    /// وباقي اللقطات بكاميرا مُركّبة على مراسي الموقع.
    /// </summary>
    [InitializeOnLoad]
    public static class EnvironmentCapture
    {
        const string SessionKey = "TLC_EnvCapture";
        const string PlayScene = "Assets/_Project/Scenes/Play.unity";

        static string _outDir = "Screenshots/phase4_2";
        static bool _running, _playSeen;
        static int _state, _frames, _idleTicks;
        static GameBootstrap _boot;
        static Camera _cam;
        static RenderTexture _rt;
        static TLC.CameraRig.ThirdPersonCameraRig _rig;

        static readonly List<string> _log = new List<string>();
        static readonly List<Shot> _queue = new List<Shot>();
        static int _shotIndex;
        static string _pendingTp, _pendingTpKind;

        struct Shot
        {
            public string act;
            public string name;
            public string kind;     // tp_inside · int_wide · tp_ext · ext_wide · ext_door
        }

        static EnvironmentCapture()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                _outDir = SessionState.GetString(SessionKey + "Dir", _outDir);
                _running = true; _state = 0; _frames = 0; _playSeen = false;
                EditorApplication.update += Tick;
            }
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-captureDir" && i + 1 < args.Length) _outDir = args[i + 1];

            Directory.CreateDirectory(_outDir);
            SessionState.SetBool(SessionKey, true);
            SessionState.SetString(SessionKey + "Dir", _outDir);

            Log("جولة تصوير المواقع · Unity " + Application.unityVersion);
            Log("الرسّام: " + SystemInfo.graphicsDeviceName);

            EditorSceneManager.OpenScene(PlayScene);
            _running = true; _state = 0; _frames = 0; _playSeen = false; _shotIndex = 0;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void BuildQueue()
        {
            _queue.Clear();
            var locs = new[]
            {
                new { act = "scene01", name = "01_apartment", ext = false },
                new { act = "scene03", name = "02_cafe",      ext = true  },
                new { act = "scene04", name = "03_newsroom",  ext = true  },
                new { act = "scene05", name = "04_archive",   ext = true  },
                new { act = "scene06", name = "05_police",    ext = true  },
                new { act = "scene08", name = "06_office",    ext = true  },
            };

            foreach (var l in locs)
            {
                _queue.Add(new Shot { act = l.act, name = l.name + "_tp_inside", kind = "tp_inside" });
                _queue.Add(new Shot { act = l.act, name = l.name + "_int_wide",  kind = "int_wide" });
                if (!l.ext) continue;
                _queue.Add(new Shot { act = l.act, name = l.name + "_tp_ext",   kind = "tp_ext" });
                _queue.Add(new Shot { act = l.act, name = l.name + "_ext_wide", kind = "ext_wide" });
                _queue.Add(new Shot { act = l.act, name = l.name + "_ext_door", kind = "ext_door" });
            }
        }

        static void Tick()
        {
            if (!_running) return;
            if (!EditorApplication.isPlaying)
            {
                _idleTicks++;
                if (_playSeen) { Log("✗ Play Mode وقف"); Finish(3); return; }
                if (_idleTicks > 900) { Log("✗ Play Mode ما بدأتش"); Finish(6); return; }
                return;
            }
            if (!_playSeen) { _playSeen = true; Log("Play Mode بدأ"); return; }
            _frames++;

            switch (_state)
            {
                case 0:   // استنى العالم
                    if (_frames > 1800) { Log("✗ مفيش GameBootstrap جاهز"); Finish(4); return; }
                    if (_frames < 90) return;

                    foreach (var b in UnityEngine.Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None))
                        if (b != null && b.DebugWorldReady) { _boot = b; break; }
                    if (_boot == null) return;

                    _boot.DebugLockLocation = true;   // السرد ماينقلش اللاعب في نص التصوير
                    BuildQueue();
                    SetupRenderTarget();
                    _rig = UnityEngine.Object.FindFirstObjectByType<TLC.CameraRig.ThirdPersonCameraRig>();
                    Log($"العالم جاهز · {_queue.Count} لقطة · كاميرا الشخص الثالث: {(_rig != null ? "موجودة" : "مفقودة")}");
                    _state = 1; _frames = 0;
                    break;

                case 1:   // نفّذ اللقطة الحالية
                    if (_pendingTp != null)
                    {
                        if (_frames < 10) return;      // كاميرا اللعب تلحق تتحرك ورا آدم
                        Render(_pendingTp, _pendingTpKind);
                        _pendingTp = null;
                        _shotIndex++;
                        _frames = 0;
                        return;
                    }

                    if (_frames < 22) return;
                    if (_shotIndex >= _queue.Count) { Finish(0); return; }
                    TakeShot(_queue[_shotIndex]);
                    if (_pendingTp == null) { _shotIndex++; _frames = 0; }
                    break;
            }
        }

        static void TakeShot(Shot shot)
        {
            // بننقل القصة للمشهد الأول (يبني الموقع لو محتاج) وبعدها بنقرأ الموقع
            _boot.DebugGotoAct(shot.act);
            var set = EnvironmentLibrary.Current;      // null = شقة آدم
            var player = _boot.DebugPlayerPosition;

            switch (shot.kind)
            {
                case "tp_inside":
                {
                    // في نص الموقع وبيفاوض الشخصية التانية — كده كاميرا «من ورا الكتف»
                    // تفضل جوّه الغرفة وقاعدة اللعب نفسها تفضل شغالة.
                    Vector3 spot;
                    float yaw = 0f;
                    if (set != null)
                    {
                        spot = set.Root.position + new Vector3(0f, 0.1f, 0f);
                        Vector3 face = Vector3.zero;
                        bool hasFace = false;
                        foreach (var kv in set.CastSpots) { face = kv.Value; hasFace = true; break; }
                        Vector3 d = hasFace ? face - spot : Vector3.forward;
                        yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    }
                    else spot = player;

                    _boot.DebugTeleport(spot, yaw);
                    EnableRig(true);
                    if (_rig != null) { _rig.SetDistance(4.4f); _rig.SetOrbit(yaw, 12f, true); }
                    HideDialogueOnly();
                    _pendingTp = shot.name;
                    _pendingTpKind = "منظور الشخص الثالث — جوه الموقع (آدم ظاهر من ورا)";
                    break;
                }

                case "int_wide":
                {
                    EnableRig(false);
                    HideAllUI();
                    Vector3 pos, look;
                    if (set != null && _boot.DebugCameraAnchor("wide", out pos))
                        look = set.Root.position + Vector3.up * 1.5f;
                    else
                    {
                        Vector3 c = set != null ? set.Root.position : player;
                        pos = c + new Vector3(-4.2f, 2.4f, -3.4f);
                        look = c + Vector3.up * 1.4f;
                    }
                    PlaceCamera(pos, look, 46f);
                    Render(shot.name, "لقطة عامة داخلية");
                    break;
                }

                case "tp_ext":
                {
                    Vector3 spawn = set != null ? set.ExteriorSpawn : player;
                    float yaw = set != null ? set.ExteriorYaw : 0f;
                    _boot.DebugTeleport(spawn, yaw);
                    EnableRig(true);
                    if (_rig != null) { _rig.SetDistance(4.4f); _rig.SetOrbit(yaw, 12f, true); }
                    HideDialogueOnly();
                    _pendingTp = shot.name;
                    _pendingTpKind = "منظور الشخص الثالث — بره المبنى";
                    break;
                }

                case "ext_wide":
                case "ext_door":
                {
                    if (set == null) { Log($"✗ {shot.name}: مفيش خارج"); return; }
                    EnableRig(false);
                    HideAllUI();
                    Vector3 pos;
                    string key = shot.kind == "ext_wide" ? "exterior" : "exterior_door";
                    if (!_boot.DebugCameraAnchor(key, out pos)) { Log($"   ⚠ مفيش مرساة {key}"); return; }
                    PlaceCamera(pos, shot.kind == "ext_wide" ? set.FacadeCenter : (set.DoorLookAt != Vector3.zero ? set.DoorLookAt : set.FacadeCenter), shot.kind == "ext_wide" ? 54f : 44f);
                    Render(shot.name, shot.kind == "ext_wide" ? "لقطة خارجية عامة" : "المدخل من الشارع");
                    break;
                }
            }
        }

        static void PlaceCamera(Vector3 pos, Vector3 lookAt, float fov)
        {
            if (_cam == null) return;
            _cam.transform.position = pos;
            var dir = lookAt - pos;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            _cam.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            _cam.fieldOfView = fov;
        }

        static void EnableRig(bool on)
        {
            if (_rig != null) _rig.enabled = on;
        }

        static void HideDialogueOnly()
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                var n = c.name;
                if (n.Contains("Dialogue") || n.Contains("Menu") || n.Contains("Choice"))
                    c.gameObject.SetActive(false);
            }
        }

        static void HideAllUI()
        {
            foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.name.StartsWith("UI_")) c.gameObject.SetActive(false);
        }

        static void SetupRenderTarget()
        {
            _cam = Camera.main;
            if (_cam == null)
                foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.isActiveAndEnabled) { _cam = c; break; }
            if (_cam == null) { Log("✗ مفيش كاميرا"); return; }

            _rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _rt.Create();
            _cam.targetTexture = _rt;
            Log($"الكاميرا بترسم في RT {_rt.width}x{_rt.height}");
        }

        static void Render(string name, string kind = "")
        {
            if (_cam == null || _rt == null) return;
            var prev = RenderTexture.active;
            try
            {
                _cam.Render();
                RenderTexture.active = _rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();

                string file = name + ".png";
                File.WriteAllBytes(Path.Combine(_outDir, file), tex.EncodeToPNG());

                Vector3 p = _cam.transform.position;
                Log($"📷 {file} · {kind} · كاميرا ({p.x:0.0}, {p.y:0.0}, {p.z:0.0}) · تباين={Contrast(tex):0.000}");
                UnityEngine.Object.DestroyImmediate(tex);
            }
            catch (Exception e) { Log("✗ تصوير فشل: " + e.Message); }
            finally { RenderTexture.active = prev; }
        }

        static float Contrast(Texture2D tex)
        {
            float min = 1f, max = 0f;
            for (int y = tex.height / 8; y < tex.height * 7 / 8; y += 21)
                for (int x = tex.width / 10; x < tex.width * 9 / 10; x += 29)
                {
                    var c = tex.GetPixel(x, y);
                    float l = (c.r + c.g + c.b) / 3f;
                    if (l < min) min = l;
                    if (l > max) max = l;
                }
            return max - min;
        }

        static void Finish(int code)
        {
            _running = false;
            EditorApplication.update -= Tick;
            SessionState.SetBool(SessionKey, false);

            var sb = new StringBuilder();
            foreach (var l in _log) sb.AppendLine(l);
            Directory.CreateDirectory(_outDir);
            File.WriteAllText(Path.Combine(_outDir, "env_capture.log"), sb.ToString());
            Debug.Log(sb.ToString());

            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }

        static void Log(string msg)
        {
            _log.Add(msg);
            Debug.Log("[TLC/Shot] " + msg);
        }
    }
}
