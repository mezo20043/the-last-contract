using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TLC.Bootstrap;
using TLC.World;
using EnvironmentLibrary = TLC.World.EnvironmentLibrary;

namespace TLC.EditorTools
{
    /// <summary>
    /// اختبار تصادم حقيقي في Play Mode (PHASE 4.2).
    ///
    /// كل محاولة بتتم على **إطارات حقيقية**: اللاعب بيتنقل، الفيزياء بتتزامن،
    /// وبعدين <see cref="CharacterController.Move"/> بيتنادى كل إطار بحركة اللعب
    /// نفسها (3.2 م/ث) — يعني نفس مسار التصادم اللي بيحصل في اللعب الفعلي.
    ///
    /// بنفحص:
    ///   ١) أشعة على كل حيطة (تأكيد إن الحيطة موجودة فيزيائيًا في مكانها).
    ///   ٢) مشي فعلي ناحية كل حيطة — لازم يتوقف قبلها.
    ///   ٣) مشي من الداخل للخارج من الباب، ومن الشارع للداخل.
    ///   ٤) مشي جوّه قطعة أثاث — لازم يتوقف قبلها.
    /// </summary>
    [InitializeOnLoad]
    public static class CollisionTest
    {
        const string SessionKey = "TLC_CollisionTest";
        const string PlayScene = "Assets/_Project/Scenes/Play.unity";

        static string _outDir = "Screenshots/collision";
        static bool _running, _playSeen;
        static int _state, _frames;
        static GameBootstrap _boot;
        static CharacterController _cc;
        static Transform _player;

        static readonly string[] Acts = { "scene03", "scene04", "scene05", "scene06", "scene08" };
        static readonly string[] Names = { "cafe", "newsroom", "archive", "police", "office" };

        static readonly List<string> _log = new List<string>();
        static readonly List<string> _failures = new List<string>();
        static int _checks, _passes;

        // ── حالة المحاولة الحالية ──
        class Trial
        {
            public string loc;
            public string name;
            public Vector3 start;
            public Vector3 dir;
            public float maxDistance;
            public bool expectBlocked;
            public Func<Vector3, bool> customCheck;   // لو موجود بيتجاوز فحص المسافة
            public string customPassText;
            public string customFailText;
        }

        static readonly List<Trial> _trials = new List<Trial>();
        static int _trialIndex;
        static Vector3 _trialStart;
        static float _travelled;
        static int _trialFrames;
        static int _stillFrames;
        static float _verticalVel;

        static CollisionTest()
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

            Log("═══ اختبار التصادم — PHASE 4.2 ═══");
            Log("Unity " + Application.unityVersion + " · " + SystemInfo.graphicsDeviceName);

            EditorSceneManager.OpenScene(PlayScene);
            _running = true; _state = 0; _frames = 0; _playSeen = false;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!_running) return;
            if (!EditorApplication.isPlaying) { if (_playSeen) Finish(3); return; }
            if (!_playSeen) { _playSeen = true; return; }
            _frames++;

            switch (_state)
            {
                case 0:
                    if (_frames > 2400) { Log("✗ مفيش GameBootstrap جاهز"); Finish(4); return; }
                    if (_frames < 90) return;

                    foreach (var b in UnityEngine.Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None))
                        if (b != null && b.DebugWorldReady && b.DebugController != null) { _boot = b; break; }
                    if (_boot == null) return;

                    _cc = _boot.DebugController;
                    _player = _cc.transform;
                    Log($"العالم جاهز · اللاعب على {_player.position} · كابسولة r={_cc.radius:0.00} h={_cc.height:0.00}");

                    // إيقاف متحكم اللعب عشان مايتحركش بالمدخلات أثناء الاختبار
                    if (_boot.DebugPlayerController != null) _boot.DebugPlayerController.enabled = false;
                    _boot.DebugLockLocation = true;   // السرد ماينقلش اللاعب في نص الاختبار

                    _state = 1; _frames = 0;
                    break;

                case 1:   // ابنِ محاولات الموقع اللي بعده
                    if (_frames < 8) return;
                    if (_locIndex >= Acts.Length) { Finish(0); return; }

                    _boot.DebugGotoAct(Acts[_locIndex]);
                    _frames = 0; _state = 2;
                    break;

                case 2:   // استنى الموقع يتبني
                    if (_frames < 6) return;
                    BuildTrials(Acts[_locIndex], Names[_locIndex]);
                    _locIndex++;
                    _trialIndex = 0; _frames = 0; _state = 3;
                    break;

                case 3:   // ابدأ المحاولة الحالية
                    if (_frames < 3) return;
                    if (_trialIndex >= _trials.Count) { _state = 1; _frames = 0; return; }

                    StartTrial(_trials[_trialIndex]);
                    _trialFrames = 0;
                    _state = 4; _frames = 0;
                    break;

                case 4:   // امشي إطار بإطار
                    {
                        var t = _trials[_trialIndex];
                        if (_trialFrames < 200 && _travelled < t.maxDistance && _stillFrames < 10)
                        {
                            StepWalk(t.dir);
                            _trialFrames++;
                            return;   // فريم كامل لكل خطوة — عشان الفيزياء تتزامن
                        }

                        EndTrial(t);
                        _trialIndex++;
                        _state = 3; _frames = 0;
                    }
                    break;
            }
        }

        static int _locIndex;

        // ────────────────────────────────────────────────────────────

        static void BuildTrials(string act, string shortName)
        {
            var set = EnvironmentLibrary.Current;
            if (set == null) { Log($"✗ {shortName}: مفيش موقع"); return; }

            _trials.Clear();          // مهم: كل موقع بيبدأ بقائمة محاولات فاضية

            Log("");
            Log($"── {shortName} · {set.DisplayName} ──");
            Log($"   الغرفة {set.RoomSize.x:0.#}×{set.RoomSize.y:0.#}م · الباب {set.Door.width:0.##}×{set.Door.height:0.##}م ({set.Door.side}) · " +
                $"باب خارجي على بعد {Vector3.Distance(set.DoorCenter, set.ExteriorSpawn):0.0}م من نقطة الشارع");

            Vector3 c = set.Root.position;
            float hx = set.RoomSize.x / 2f, hz = set.RoomSize.y / 2f;

            // ── ١) أشعة تأكيد إن الحيطة موجودة في مكانها ──
            // الأشعة بتتحرك ٣م على الجنب عشان ما تعدّيش من فتحة الباب أو الشباك
            float side = Mathf.Min(3f, hx - 1.2f);
            RaycastWall(shortName, "شمال", c + new Vector3(side, 2.55f, 0f), Vector3.forward, hz - 0.1f);
            RaycastWall(shortName, "جنوب", c + new Vector3(-side, 2.55f, 0f), Vector3.back, hz - 0.1f);
            RaycastWall(shortName, "شرق", c + new Vector3(0f, 2.55f, side * 0.6f), Vector3.right, hx - 0.1f);
            RaycastWall(shortName, "غرب", c + new Vector3(0f, 2.55f, -side * 0.6f), Vector3.left, hx - 0.1f);

            // ── ٢) مشي ناحية كل حيطة (من نقطة منتصف الغرفة) ──
            AddWallTrial(shortName, "حيطة شمال", c + new Vector3(0f, 0.1f, -0.6f), Vector3.forward, hz, c, false);
            AddWallTrial(shortName, "حيطة جنوب", c + new Vector3(0f, 0.1f, 0.6f), Vector3.back, hz, c, false);
            AddWallTrial(shortName, "حيطة شرق", c + new Vector3(-0.6f, 0.1f, 0f), Vector3.right, hx, c, true);
            AddWallTrial(shortName, "حيطة غرب", c + new Vector3(0.6f, 0.1f, 0f), Vector3.left, hx, c, true);

            // ── ٣) الباب: خروج ودخول ──
            bool north = set.Door.side == EnvironmentLibrary.Side.North;
            Vector3 door = set.DoorCenter;
            Vector3 inside = door + new Vector3(0f, 0.1f, north ? -2.0f : 2.0f);
            Vector3 dirOut = north ? Vector3.forward : Vector3.back;

            _trials.Add(new Trial
            {
                loc = shortName,
                name = "الخروج من الباب للشارع",
                start = inside,
                dir = dirOut,
                maxDistance = 5.5f,
                expectBlocked = false,
                customCheck = end =>
                {
                    float outside = north ? end.z - set.Root.position.z : set.Root.position.z - end.z;
                    return outside > hz + 1.0f;
                },
                customPassText = "خرج من المدخل ووصل الشارع",
                customFailText = "مش قادر يخرج من المدخل"
            });

            _trials.Add(new Trial
            {
                loc = shortName,
                name = "الدخول من الشارع للمبنى",
                start = set.ExteriorSpawn,
                dir = -dirOut,
                maxDistance = 5.5f,
                expectBlocked = false,
                customCheck = end =>
                {
                    float insideDepth = north ? set.Root.position.z + hz - end.z : end.z - (set.Root.position.z - hz);
                    return insideDepth > 1.0f;
                },
                customPassText = "دخل جوّه المبنى من المدخل",
                customFailText = "مش قادر يدخل من المدخل"
            });

            // ── ٤) الأثاث ──
            Collider target = NearestFurniture(set);
            if (target != null)
            {
                Vector3 tc = target.bounds.center;
                Vector3 flat = new Vector3(tc.x, 0.1f, tc.z);
                Vector3 start = flat + new Vector3(2.0f, 0f, 2.0f);
                Vector3 dir = (flat - start).normalized;

                _trials.Add(new Trial
                {
                    loc = shortName,
                    name = $"أثاث ({target.gameObject.name})",
                    start = start,
                    dir = dir,
                    maxDistance = 3.2f,
                    expectBlocked = true,
                    customCheck = end =>
                    {
                        Vector3 probe = new Vector3(end.x, tc.y, end.z);
                        float gap = Vector3.Distance(probe, target.ClosestPoint(probe));
                        return gap > 0.05f;
                    },
                    customPassText = "اتوقف قبل الأثاث وما دخلش جوّه",
                    customFailText = "دخل جوّه الأثاث"
                });
            }

            // ── ٥) قطعة أثاث تانية (رف/دولاب) ──
            Collider second = SecondFurniture(set, target);
            if (second != null)
            {
                Vector3 sc = second.bounds.center;
                Vector3 flat = new Vector3(sc.x, 0.1f, sc.z);
                Vector3 dir = new Vector3(1f, 0f, 1f).normalized;
                if (Vector3.Dot(flat - (flat - dir * 2f), dir) < 0f) dir = -dir;

                Vector3 start = flat - dir * 2.0f;
                _trials.Add(new Trial
                {
                    loc = shortName,
                    name = $"أثاث تاني ({second.gameObject.name})",
                    start = start,
                    dir = dir,
                    maxDistance = 3.0f,
                    expectBlocked = true,
                    customCheck = end =>
                    {
                        Vector3 probe = new Vector3(end.x, sc.y, end.z);
                        return Vector3.Distance(probe, second.ClosestPoint(probe)) > 0.05f;
                    },
                    customPassText = "اتوقف قبل القطعة التانية",
                    customFailText = "دخل جوّه القطعة التانية"
                });
            }
        }

        static Collider NearestFurniture(EnvironmentLibrary.LocationSet set)
        {
            Collider target = null;
            float best = float.MaxValue;
            Vector3 center = set.Root.position;

            foreach (var col in set.Root.GetComponentsInChildren<Collider>(true))
            {
                if (col == null || !col.enabled) continue;
                string n = col.gameObject.name;
                bool isFurniture = n.StartsWith("Wooden") || n.StartsWith("SchoolDesk") ||
                                   n.StartsWith("Shelf") || n.StartsWith("Gothic") ||
                                   n.StartsWith("Sofa") || n.StartsWith("ArmChair") ||
                                   n.StartsWith("GreenChair") || n.StartsWith("CoffeeTable");
                if (!isFurniture) continue;

                float d = Vector3.Distance(col.bounds.center, center);
                if (d < best && col.bounds.size.y > 0.4f) { best = d; target = col; }
            }
            return target;
        }

        static Collider SecondFurniture(EnvironmentLibrary.LocationSet set, Collider exclude)
        {
            Collider best = null;
            float bestScore = -1f;
            Vector3 center = set.Root.position;

            foreach (var col in set.Root.GetComponentsInChildren<Collider>(true))
            {
                if (col == null || !col.enabled || col == exclude) continue;
                string n = col.gameObject.name;
                bool tall = n.StartsWith("Shelf") || n.StartsWith("GothicCabinet") || n.StartsWith("Sofa");
                if (!tall) continue;

                float d = Vector3.Distance(col.bounds.center, center);
                if (best == null || d < bestScore || bestScore < 0f) { best = col; bestScore = d; }
            }
            return best;
        }

        static void AddWallTrial(string loc, string name, Vector3 start, Vector3 dir,
                                 float half, Vector3 center, bool alongX)
        {
            _trials.Add(new Trial
            {
                loc = loc,
                name = name,
                start = start,
                dir = dir,
                maxDistance = Mathf.Max(1.6f, half - 0.35f),
                expectBlocked = true,
                customCheck = end =>
                {
                    // اللاعب لازم يفضل جوّه حدود الغرفة (مش يعدّي مستوى الحيطة)
                    float coord = alongX ? Mathf.Abs(end.x - center.x) : Mathf.Abs(end.z - center.z);
                    return coord < half + 0.05f;
                },
                customPassText = "اتوقف عند الحيطة",
                customFailText = "عدّى الحيطة"
            });
        }

        static void RaycastWall(string loc, string side, Vector3 from, Vector3 dir, float expect)
        {
            float maxRay = expect + 6f;

            // بنتجاهل كولادرات اللاعب نفسه (وإلا الشعاع بيصطدم بآدم الواقف في الغرفة)
            RaycastHit hit = default;
            bool found = false;
            foreach (var h in Physics.RaycastAll(from, dir, maxRay, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider == null) continue;
                if (h.collider.transform.root == _player.root) continue;
                if (h.collider is CharacterController) continue;
                hit = h; found = true; break;
            }

            if (found)
            {
                bool ok = Mathf.Abs(hit.distance - expect) < 0.45f;
                _checks++;
                if (ok) _passes++; else _failures.Add($"{loc} · حيطة {side} · على مسافة {hit.distance:0.00}م بدل {expect:0.00}");

                Log($"   [{(ok ? "PASS" : "FAIL")}] حيطة {side}: أشعة اصطدمت بـ«{hit.collider.gameObject.name}» " +
                    $"على {hit.distance:0.00}م (متوقع {expect:0.00}م)");
            }
            else
            {
                _checks++;
                _failures.Add($"{loc} · حيطة {side} · مفيش كولادر");
                Log($"   [FAIL] حيطة {side}: الشعاع ما لاقاش أي كولادر (يعني الحيطة مفيهاش تصادم!)");
            }
        }

        // ────────────────────────────────────────────────────────────

        static void StartTrial(Trial t)
        {
            _trialStart = t.start;
            _travelled = 0f;
            _stillFrames = 0;
            _verticalVel = -0.5f;

            _cc.enabled = false;
            _player.position = t.start;
            _cc.enabled = true;
            Physics.SyncTransforms();
        }

        static void StepWalk(Vector3 dir)
        {
            const float dt = 1f / 60f;
            const float speed = 3.2f;

            // جاذبية بسيطة: لازم اللاعب يبقى ملزوق بالأرض عشان الـCharacterController
            // يقدر يطلع الدرج بتاع المدخل (الـstep-up محتاج isGrounded).
            if (_cc.isGrounded) _verticalVel = -0.5f;
            else _verticalVel -= 9.81f * dt;

            var before = _player.position;
            _cc.Move(dir.normalized * speed * dt + Vector3.up * _verticalVel * dt);
            float moved = Vector3.Distance(before, _player.position);
            _travelled += moved;

            // لو اللاعب واقف مكانه (اتصادم) بنوقف المحاولة بدل ما نستهلك إطارات
            if (moved < 0.004f) _stillFrames++; else _stillFrames = 0;
        }

        static void EndTrial(Trial t)
        {
            Vector3 end = _player.position;
            bool pass;
            string verdict;

            if (t.customCheck != null)
            {
                pass = t.customCheck(end);
                verdict = pass ? t.customPassText : t.customFailText;
            }
            else
            {
                pass = t.expectBlocked ? _travelled < t.maxDistance - 0.3f : _travelled > t.maxDistance - 0.3f;
                verdict = pass ? "زي المتوقع" : "مخالف للمتوقع";
            }

            _checks++;
            if (pass) _passes++;
            else
            {
                _failures.Add($"{t.loc} · {t.name} · {verdict}");

                // مين اللي واقف في الطريق؟
                string blocker = "مفيش";
                Vector3 chest = end + Vector3.up * 1.0f;
                foreach (var h in Physics.RaycastAll(chest, t.dir.normalized, 3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider == null) continue;
                    if (h.collider.transform.root == _player.root) continue;
                    if (h.collider is CharacterController) continue;
                    blocker = $"{h.collider.gameObject.name} على {h.distance:0.00}م";
                    break;
                }
                // وكمان تحت الأرجل
                string floorHit = "مفيش";
                if (Physics.Raycast(end + Vector3.up * 0.3f, Vector3.down, out var fh, 2f, ~0, QueryTriggerInteraction.Ignore))
                    floorHit = $"{fh.collider.gameObject.name} (y={fh.point.y:0.00})";

                Log($"        ↳ العائق: {blocker} · الأرض تحته: {floorHit}");
            }

            Log($"   [{(pass ? "PASS" : "FAIL")}] {t.name}: من {Fmt(t.start)} إلى {Fmt(end)} · " +
                $"مشى {_travelled:0.00}م في {_trialFrames} إطار · {verdict}");
        }

        static string Fmt(Vector3 v) => $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";

        static void Finish(int code)
        {
            _running = false;
            EditorApplication.update -= Tick;
            SessionState.SetBool(SessionKey, false);

            Log("");
            Log($"═══ النتيجة: {_passes}/{_checks} اختبار ناجح ═══");
            if (_failures.Count == 0) Log("✅ اللاعب مابيعدّيش أي حيطة أو قطعة أثاث، وبيمر من الأبواب في كل المواقع");
            else
            {
                Log($"❌ {_failures.Count} إخفاق:");
                foreach (var f in _failures) Log("   · " + f);
            }

            var sb = new StringBuilder();
            foreach (var l in _log) sb.AppendLine(l);
            Directory.CreateDirectory(_outDir);
            File.WriteAllText(Path.Combine(_outDir, "collision_report.txt"), sb.ToString());
            Debug.Log(sb.ToString());

            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }

        static void Log(string m) => _log.Add(m);
    }
}
