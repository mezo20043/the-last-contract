using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using TLC.Core;
using TLC.Narrative;
using TLC.Player;
using TLC.UI;
using TLC.World;
using TLC.Settings;
using TLC.CameraRig;
using TLC.Cinematics;
using TLC.Audio;
using TLC.Mobile;

namespace TLC.Bootstrap
{
    /// <summary>
    /// نقطة الدخول الوحيدة. بتبني المشهد كله وقت التشغيل:
    /// الشقة + آدم + الكاميرا + الواجهة + السرد + السينمائيات.
    /// السبب: المشروع يشتغل من أول Play من غير prefabs ولا مراجع ناقصة،
    /// وأي حد يفتحه يفهمه من ملف واحد.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("القصة")]
        [SerializeField] string storyResourcePath = "Story/chapter01";
        [SerializeField] string chapterTitle = "الفصل الأول · العقد الأخير";

        [Header("الفصول بالترتيب")]
        [SerializeField] string[] chapterResources =
        {
            "Story/chapter01", "Story/chapter02", "Story/chapter03"
        };
        [SerializeField] string[] chapterTitles =
        {
            "الفصل الأول · العقد الأخير", "الفصل الثاني · البيت القديم", "الفصل الثالث · الحساب"
        };
        [SerializeField] float delayBeforeNextChapter = 3.5f;
        int _chapterIndex;

        [Header("المظهر السينمائي")]
        [SerializeField] bool useCinematicLook = true;

        [Header("الخيارات")]
        [SerializeField] bool buildMobileControls = true;
        [SerializeField] bool autoQuality = true;

        [Header("المشاهد")]
        [Tooltip("قائمة بس: بيبني الواجهة والصوت ويحمّل مشهد اللعب لما اللاعب يختار. مشهد Main.")]
        [SerializeField] bool menuOnly = false;

        [Tooltip("بيستخدم كائنات المشهد الموجودة (WORLD / ADAM_MANSOUR / Main Camera) بدل ما يعملها. مشهد Play.")]
        [SerializeField] bool useSceneObjects = false;

        [Tooltip("اسم مشهد اللعب اللي بيتحمّل من القائمة.")]
        [SerializeField] string playSceneName = SceneFlow.PlayScene;

        // الأنظمة
        GameState _state;
        NarrativeRunner _runner;
        DialogueUI _ui;
        ThirdPersonController _player;
        PlayerInputReader _input;
        ThirdPersonCameraRig _camera;
        CinematicDirector _cinematics;
        DialogueShotDirector _shotDirector;
        CinematicLook _look;
        QualityManager _quality;
        MobileOptimizations _mobile;
        VoiceOverSystem _voice;
        DialogueCameraSystem _dialogueCamera;
        LightingRig _lighting;
        ApartmentBuilder _apartment;

        [Header("الواجهة")]
        [SerializeField] bool showTitleMenu = true;
        [SerializeField] bool openWithCinematic = true;

        AudioDirector _audio;
        CastDirector _cast;
        FootstepSystem _footsteps;
        MenuSystem _menu;
        MobileControls _mobileControls;
        OrientationWatcher _orientation;

        readonly Dictionary<string, Interactable> _hotspots = new Dictionary<string, Interactable>();
        TextMeshProUGUI _interactPrompt;

        void Awake()
        {
            EnsureDefaults();

            // مشهد القائمة (Main): مش بنبني عالم ولا لاعب — بس واجهة وصوت وخلفية.
            // ده اللي بيخلي الانتقال لمشهد Play انتقال حقيقي بين مشهدين،
            // مش مجرد إخفاء واجهة.
            if (menuOnly)
            {
                BuildMenuBackdrop();
                BuildQuality();
                BuildAudio();
                BuildMenu();
                Debug.Log("[TLC] مشهد القائمة جاهز — اللعب في مشهد منفصل.");
                return;
            }

            _state = new GameState();
            _state.ResetToDefaults();

            BuildQuality();
            BuildOrientation();
            BuildAudio();
            BuildWorld();
            BuildPlayer();
            BuildCamera();
            BuildUI();
            BuildCinematics();
            BuildNarrative();
            BuildHotspots();
            BuildMenu();
        }

        /// <summary>كاميرا بسيطة بخلفية داكنة عشان شاشة القائمة ما تبقاش "No cameras rendering".</summary>
        void BuildMenuBackdrop()
        {
            var camGo = GameObject.Find("Menu Camera");
            if (camGo == null) camGo = new GameObject("Menu Camera");

            var cam = camGo.GetComponent<Camera>();
            if (cam == null) cam = camGo.AddComponent<Camera>();

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.025f, 0.035f, 1f);
            cam.fieldOfView = 50f;
            camGo.transform.position = new Vector3(0f, 1.6f, -3f);

            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
        }

        void BuildOrientation()
        {
            var go = FindInScene("OrientationWatcher") ?? new GameObject("OrientationWatcher");
            _orientation = go.GetComponent<OrientationWatcher>() ?? go.AddComponent<OrientationWatcher>();
        }

        void Start()
        {
            StartCorroutine_BindUI();

            if (menuOnly)
            {
                if (_menu != null) { RefreshMenuContext(); _menu.Show(UIScreen.Title); }
                return;
            }

            if (showTitleMenu && _menu != null)
            {
                RefreshMenuContext();
                _menu.Show(UIScreen.Title);
                return;      // اللعبة بتبدأ لما اللاعب يختار
            }

            // وصلنا من مشهد القائمة؟ نفّذ الطلب اللي اتساب هناك.
            if (PlaySession.Consume(out PlayEntryMode mode, out int chapter))
            {
                Debug.Log($"[TLC] طلب من مشهد القائمة: {PlaySession.Describe(mode, chapter)}");
                ApplySessionRequest(mode, chapter);
                return;
            }

            StartCoroutine(OpeningRoutine());
        }

        /// <summary>بينفّذ طلب مشهد القائمة — نفس المسارات الموجودة، مفيش منطق جديد.</summary>
        /// <summary>
        /// بعد إطار واحد من بناء كل الأنظمة، بنربط كل الكانفسات بكاميرا اللعب.
        /// السبب: الكانفسات بتتبني في BuildUI/BuildNarrative/BuildMenu اللي بتحصل
        /// قبل ما كل حاجة تخلص، ولازم الربط يتم بعد ما الكاميرا تكون موجودة وثابتة.
        /// </summary>
        void StartCorroutine_BindUI()
        {
            StartCoroutine(BindUINextFrame());
        }

        System.Collections.IEnumerator BindUINextFrame()
        {
            yield return null;
            var cam = _cameraTransform != null ? _cameraTransform.GetComponent<Camera>() : Camera.main;
            if (cam == null) { Debug.LogWarning("[TLC/UI] مفيش كاميرا لربط الواجهة"); yield break; }
            int n = UIFactory.BindAllCanvasesToCamera(cam);
            Debug.Log($"[TLC/UI] اتربط {n} كانفس بكاميرا {cam.name} (Screen Space - Camera)");
            UIFactory.FixBrokenTextLayouts();
        }

        void ApplySessionRequest(PlayEntryMode mode, int chapter)
        {
            switch (mode)
            {
                case PlayEntryMode.Continue: ContinueGame();  break;
                case PlayEntryMode.Chapter:  StartChapter(chapter); break;
                default:                     StartNewGame();  break;
            }
        }

        void UpdateMenuContext() => RefreshMenuContext();

        void RefreshMenuContext()
        {
            if (_menu == null) return;
            _menu.SetContext(c =>
            {
                c.HasSave = SaveSystem.Exists(SaveSystem.AutoSlot) ||
                            SaveSystem.Exists(SaveSystem.ManualSlotA);
                c.UnlockedChapters = Progress.Current.unlockedChapters;
                c.SeenEndings = new HashSet<string>(Progress.Current.seenEndings);
                c.TotalEndings = MenuModel.EndingCount;
                c.InGame = _runner != null && !_menu.IsOpen;
            });
        }

        IEnumerator OpeningRoutine()
        {
            if (_runner == null || _ui == null)
            {
                Debug.LogError("[TLC] الفصل ماقدرش يبدأ — السرد أو الواجهة مش موجودين.");
                yield break;
            }

            if (_menu != null) _menu.Hide();
            if (_audio != null)
            {
                _audio.PlayAmbience("apartment_hum_loop");
                _audio.PlayChapterSting();
            }

            // مشهد افتتاحي سينمائي قبل أول سطر
            if (openWithCinematic) yield return OpeningCinematic();

            yield return _ui.PlayChapterCard(chapterTitle, 2.6f);
            _runner.Begin();
        }

        /// <summary>
        /// المشهد بيحمل المرجع من غير قيم محفوظة، فلو Unity صفّر المصفوفات
        /// بنرجّع الافتراضيات هنا — عشان اللعبة تشتغل حتى لو المشهد اتعدّل يدوياً.
        /// </summary>
        void EnsureDefaults()
        {
            if (chapterResources == null || chapterResources.Length == 0)
                chapterResources = new[]
                {
                    "Story/chapter01", "Story/chapter02", "Story/chapter03"
                };

            if (chapterTitles == null || chapterTitles.Length == 0)
                chapterTitles = new[]
                {
                    "الفصل الأول · العقد الأخير",
                    "الفصل الثاني · البيت القديم",
                    "الفصل الثالث · الحساب"
                };

            if (string.IsNullOrEmpty(storyResourcePath))
                storyResourcePath = "Story/chapter01";

            if (string.IsNullOrEmpty(chapterTitle))
                chapterTitle = chapterTitles[0];
        }

        // ────────────────────────── الجودة ──────────────────────────

        void BuildQuality()
        {
            if (!autoQuality) return;
            var go = new GameObject("QualityManager");
            _quality = go.AddComponent<QualityManager>();

            var mobileGo = new GameObject("MobileOptimizations");
            _mobile = mobileGo.AddComponent<MobileOptimizations>();
            _mobile.Setup(_quality);
        }

        void BuildAudio()
        {
            var go = new GameObject("AudioDirector");
            _audio = go.AddComponent<AudioDirector>();
            _audio.Build();
            if (_audio != null) _audio.PlayMusic("title_theme", 1.5f);
        }

        // ────────────────────────── العالم ──────────────────────────

        void BuildWorld()
        {
            var worldRoot = FindInScene("WORLD") ?? new GameObject("WORLD");

            _apartment = Adopt<ApartmentBuilder>(worldRoot.transform, "Apartment");
            _apartment.Build();

            _lighting = Adopt<LightingRig>(worldRoot.transform, "Lighting");
            _lighting.Build();

            // الشخصيات الثانوية الواقعية
            _cast = Adopt<CastDirector>(worldRoot.transform, "CAST");
            _cast.SpawnAll();

            if (useSceneObjects && worldRoot != null)
                Debug.Log($"[TLC] العالم من المشهد: {worldRoot.name}");
            Debug.Log($"[TLC] الشخصيات: {_cast.SpawnedCount} شخصية واقعية في المشهد");

            if (_quality != null)
            {
                var f = typeof(QualityManager).GetField("rain",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(_quality, _lighting.Rain);
            }
        }

        // ────────────────────────── آدم ──────────────────────────

        void BuildPlayer()
        {
            // في مشهد Play الكائن موجود وبموضع مكتوب في المشهد،
            // فبنتبنّاه بدل ما نعمل واحد جديد فوقه.
            var playerGo = FindInScene("ADAM_MANSOUR");
            if (playerGo == null) playerGo = new GameObject("ADAM_MANSOUR");

            var builder = playerGo.GetComponent<AdamBuilder>();
            if (builder == null) builder = playerGo.AddComponent<AdamBuilder>();

            Vector3 spawn = _apartment != null ? _apartment.PlayerSpawn.position
                                               : playerGo.transform.position;
            builder.Build(spawn);

            _player = builder.Controller;
            if (_player == null)
            {
                // أمان أخير: لو البناء فشل لأي سبب، بنضيف المتحكم على الكائن
                // عشان اللعبة تفضل قابلة للعب بدل ما تقع بـ NullReference.
                _player = playerGo.GetComponent<ThirdPersonController>();
                if (_player == null) _player = playerGo.AddComponent<ThirdPersonController>();
                Debug.LogError("[TLC] AdamBuilder مطلّعش ThirdPersonController — اتضاف يدوياً.");
            }
            _input = playerGo.AddComponent<PlayerInputReader>();

            // ربط المراجع الداخلية
            var camTransform = CreateCameraTransform();

            SetPrivateField(_player, "input", _input);
            SetPrivateField(_player, "cameraTransform", camTransform);

            _player.OnFootstep += () => Debug.Log("[TLC] خطوة");
        }

        Transform _cameraTransform;

        Transform CreateCameraTransform()
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null)
            {
                camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.AddComponent<Camera>();
            }

            var cam = camGo.GetComponent<Camera>();
            if (cam == null) cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.06f;
            cam.farClipPlane = 70f;
            cam.fieldOfView = BaseFieldOfView;

            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();

            _cameraTransform = camGo.transform;
            return camGo.transform;
        }

        // ────────────────────────── الكاميرا ──────────────────────────

        /// <summary>FOV الأساسي — ثابت مرجعي بستخدمه مدير الاتجاه عند تغيّر نسبة الشاشة.</summary>
        public const float BaseFieldOfView = 48f;

        void BuildCamera()
        {
            var camGo = _cameraTransform.gameObject;
            _camera = camGo.GetComponent<ThirdPersonCameraRig>();
            if (_camera == null) _camera = camGo.AddComponent<ThirdPersonCameraRig>();

            SetPrivateField(_camera, "target", _player.transform);
            SetPrivateField(_camera, "input", _input);
            _camera.SnapBehind();

            if (_input != null) _input.GameplayEnabled = true;

            // نقطة التصويب على طول آدم الحقيقي مش رقم ثابت
            _camera.AutoFitToTarget();

            // المظهر السينمائي (Tonemapping/Bloom/Vignette/AO) على كاميرا اللعب
            var cam = camGo.GetComponent<Camera>();
            if (cam != null && useCinematicLook)
            {
                _look = camGo.GetComponent<CinematicLook>();
                if (_look == null) _look = camGo.AddComponent<CinematicLook>();
                var tier = _quality != null ? _quality.CurrentTier : QualityManager.Tier.Medium;
                _look.Build(tier);
            }

            // الكاميرا بتتظبط تلقائياً على الشاشات الرأسية
            if (_orientation != null)
                _orientation.TrackCamera(camGo.GetComponent<Camera>(), BaseFieldOfView);

            if (useSceneObjects)
                Debug.Log($"[TLC] الكاميرا من المشهد: {camGo.name} · FOV {BaseFieldOfView}");
        }

        // ────────────────────────── الواجهة ──────────────────────────

        void BuildUI()
        {
            // الواجهة بتتبني في BuildNarrative بعد ما السرد يتهيّئ — لأنها بتشترك في أحداثه
            var uiGo = new GameObject("UI");
            _ui = uiGo.AddComponent<DialogueUI>();
        }

        void BuildCinematics()
        {
            var go = new GameObject("CinematicDirector");
            _cinematics = go.AddComponent<CinematicDirector>();

            // مخرج لقطات الحوار (Shot / Reverse-Shot) — بياخد الكاميرا وريج اللعب والمظهر
            var dirGo = new GameObject("DialogueShotDirector");
            _shotDirector = dirGo.AddComponent<DialogueShotDirector>();
            _shotDirector.Setup(_cameraTransform.GetComponent<Camera>(), _camera, _look);
        }

        // ────────────────────────── السرد ──────────────────────────

        void BuildNarrative()
        {
            var textAsset = Resources.Load<TextAsset>(storyResourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"[TLC] ملف القصة مش موجود في Resources/{storyResourcePath}.json");
                return;
            }

            var runnerGo = new GameObject("NarrativeRunner");
            _runner = runnerGo.AddComponent<NarrativeRunner>();

            if (!_runner.Initialize(_state, textAsset.text))
            {
                Debug.LogError("[TLC] فشل تهيئة السرد.");
                return;
            }

            _ui.Build(_runner);

            // الصوت: تعليق صوتي + مزيكا + محيط (بيشتغل بصمت لو الملفات مش موجودة)
            var audioGo = new GameObject("VoiceOverSystem");
            _voice = audioGo.AddComponent<VoiceOverSystem>();
            _voice.Build(_runner);

            // كاميرا الحوارات السينمائية
            var camSysGo = new GameObject("DialogueCameraSystem");
            _dialogueCamera = camSysGo.AddComponent<DialogueCameraSystem>();

            _runner.EventRaised += OnNarrativeEvent;
            _runner.EndingReached += OnEnding;
            _runner.LineShown += OnLineShown;
            _runner.ChoicesShown += OnChoicesShown;

            _cinematics.Setup(_cameraTransform.GetComponent<Camera>(), _player, _input, null);

            // خطوات حسب السطح
            var stepGo = new GameObject("FootstepSystem");
            _footsteps = stepGo.AddComponent<FootstepSystem>();
            _footsteps.Setup(_player);

            // واجهة تحكم الموبايل — كانت معرّفة بفلاج ومش بتتبني خالص
            BuildMobileControls();
        }

        /// <summary>
        /// بتبني عصا التحكم وزر التفاعل ومنطقة الكاميرا.
        /// بتحتاج السرد والواجهة واللاعب — عشان كده بتتنادى آخر حاجة في التهيئة.
        /// </summary>
        void BuildMobileControls()
        {
            if (!buildMobileControls)
            {
                Debug.Log("[TLC/Mobile] بناء واجهة التحكم مقفول من المشهد.");
                return;
            }

            var go = FindInScene("MOBILE_CONTROLS") ?? new GameObject("MOBILE_CONTROLS");
            _mobileControls = go.GetComponent<MobileControls>();
            if (_mobileControls == null) _mobileControls = go.AddComponent<MobileControls>();

            _mobileControls.Build(_input, _ui, _player);
        }

        // ────────────────────── مساعدات المشهد ──────────────────────

        /// <summary>بيلاقي كائن بالاسم في المشهد — بديل آمن لـ GameObject.Find اللي بيرمي لو الاسم فاضي.</summary>
        static GameObject FindInScene(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return GameObject.Find(name);
        }

        /// <summary>
        /// بيرجّع المكوّن الموجود في المشهد، أو بيعمل كائن جديد ويضيفه.
        /// الهدف: المشهد يبقى فيه كائنات حقيقية، والكود يفضل شغال لو المشهد ناقص حاجة.
        /// </summary>
        T Adopt<T>(Transform parent, string childName) where T : Component
        {
            if (useSceneObjects && parent != null)
            {
                var existing = parent.Find(childName);
                if (existing != null)
                {
                    var comp = existing.GetComponent<T>();
                    if (comp != null) return comp;
                    return existing.gameObject.AddComponent<T>();
                }
            }

            var go = new GameObject(childName);
            if (parent != null) go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        // ────────────────────────── الافتتاحية ──────────────────────────

        /// <summary>
        /// تسلسل افتتاحي: لقطة تأسيسية للشقة → تفصيلة على المكتب → قريبة على آدم.
        /// الكاميرا بتتبع عظام الراس الحقيقية، مش ارتفاعات مقدّرة.
        /// </summary>
        IEnumerator OpeningCinematic()
        {
            if (_cinematics == null) yield break;

            var shots = new List<Shot>();

            // ١) لقطة تأسيسية: من ناحية الشباك على الصالة
            shots.Add(new Shot
            {
                name = "OPEN_establish",
                position = new Vector3(-3.0f, 2.05f, 2.45f),
                lookAt   = new Vector3(0.2f, 1.15f, -0.4f),
                fieldOfView = 46f,
                duration = 4.2f
            });

            // ٢) تفصيلة: المكتب/الظرف
            if (_apartment != null && _apartment.DoorAnchor != null)
            {
                var door = _apartment.DoorAnchor.position;
                shots.Add(new Shot
                {
                    name = "OPEN_envelope",
                    position = door + new Vector3(-1.1f, 1.15f, 0.9f),
                    lookAt   = door + new Vector3(0f, 1.05f, 0f),
                    fieldOfView = 30f,
                    duration = 3.0f
                });
            }

            // ٣) قريبة على آدم — على عظمة الراس
            if (_player != null)
                shots.Add(_dialogueCamera != null
                    ? _dialogueCamera.CloseUp(_player.transform)
                    : new Shot
                    {
                        name = "OPEN_adam",
                        position = _player.transform.position + new Vector3(0.55f, 1.70f, 0.95f),
                        lookAt = _player.transform.position + Vector3.up * 1.68f,
                        fieldOfView = 30f, duration = 3.0f
                    });

            float total = 0f;
            foreach (var sh in shots) total += sh.duration;

            if (_audio != null) _audio.PlayMusic("title_theme", 2.5f);

            _cinematics.PlayShots(shots, null);
            Debug.Log($"[TLC] الافتتاحية: {shots.Count} لقطة · {total:0.0} ثانية");

            yield return new WaitForSeconds(total);
        }

        // ────────────────────────── القوائم ──────────────────────────

        void BuildMenu()
        {
            var go = new GameObject("MenuSystem");
            _menu = go.AddComponent<MenuSystem>();
            _menu.Build();
            _menu.ActionRequested += OnMenuAction;
        }

        void OnMenuAction(MenuAction action, int value)
        {
            // إعدادات التحكم مشتركة بين المشهدين — بتتطبّق في مكانها
            if (ApplyControlSetting(action)) return;

            // في مشهد القائمة: الاختيار بيحمّل مشهد اللعب فعلياً
            if (menuOnly)
            {
                switch (action)
                {
                    case MenuAction.NewGame:
                        PlaySession.RequestNewGame();
                        LeaveMenuForPlay();
                        return;
                    case MenuAction.Continue:
                        PlaySession.RequestContinue();
                        LeaveMenuForPlay();
                        return;
                    case MenuAction.StartChapter:
                        PlaySession.RequestChapter(value);
                        LeaveMenuForPlay();
                        return;
                    case MenuAction.Quit:
                        Application.Quit();
                        return;
                }
                return;
            }

            switch (action)
            {
                case MenuAction.NewGame:       StartNewGame();      break;
                case MenuAction.Continue:      ContinueGame();      break;
                case MenuAction.StartChapter:  StartChapter(value); break;
                case MenuAction.Resume:        CloseMenus();        break;
                case MenuAction.SaveToSlot:    SaveTo(value);       break;
                case MenuAction.LoadFromSlot:  LoadFrom(value);     break;
                case MenuAction.Quit:          QuitToTitle();       break;
            }
        }

        /// <summary>بيطبّق إعداد تحكم ويحدّث القائمة — بيرجّع true لو الفعل كان إعداد.</summary>
        bool ApplyControlSetting(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.CycleLook:
                    ControlSettings.SetLookSensitivity(ControlSettings.NextLook(ControlSettings.LookSensitivity));
                    break;
                case MenuAction.CycleStick:
                    ControlSettings.SetStickScale(ControlSettings.NextStick(ControlSettings.StickScale));
                    break;
                case MenuAction.ToggleStickSide:
                    ControlSettings.ToggleStickSide();
                    break;
                case MenuAction.ToggleInvertY:
                    ControlSettings.ToggleInvertY();
                    break;
                case MenuAction.ToggleAutoRun:
                    ControlSettings.ToggleAutoRun();
                    break;
                case MenuAction.ResetControls:
                    ControlSettings.ResetToDefaults();
                    break;
                default:
                    return false;
            }

            Debug.Log($"[TLC/Controls] حساسية {ControlSettings.LookLabel} · عصا {ControlSettings.StickLabel}" +
                      $" · جهة {(ControlSettings.StickSide == JoystickSide.Left ? "يسار" : "يمين")}" +
                      $" · عكس {ControlSettings.InvertY} · جري تلقائي {ControlSettings.AutoRun}");

            if (_menu != null) _menu.Refresh();
            return true;
        }

        /// <summary>الانتقال الحقيقي من القائمة لمشهد اللعب.</summary>
        void LeaveMenuForPlay()
        {
            string target = string.IsNullOrEmpty(playSceneName) ? SceneFlow.PlayScene : playSceneName;

            if (!SceneFlow.CanLoad(target))
            {
                // مشهد اللعب مش في Build Settings — بدل ما اللعبة تقع،
                // بنكمل في نفس المشهد ونسجّل خطأ واضح.
                Debug.LogError($"[TLC/Scenes] مشهد '{target}' مش متاح — بنكمل في نفس المشهد. " +
                               "أضفه من File → Build Profiles أو منيو TLC → 5.");
                if (_menu != null) _menu.Hide();
                StartNewGame();
                return;
            }

            Debug.Log($"[TLC/Scenes] {SceneFlow.ActiveName()} → {target}");
            if (_audio != null) _audio.PlaySfx("ui_click");
            SceneManager.LoadScene(target, LoadSceneMode.Single);
        }

        void CloseMenus()
        {
            if (_menu != null) _menu.Hide();
            if (_runner != null) _runner.ResumeAfterEvent();
        }

        public void OpenPause()
        {
            if (_menu == null) return;
            RefreshMenuContext();
            _menu.ShowPause();
        }

        public void StartNewGame()
        {
            if (_runner == null) return;

            _state.ResetToDefaults();
            _chapterIndex = 0;

            var asset = Resources.Load<TextAsset>(chapterResources[0]);
            if (asset == null) { Debug.LogError("[TLC] الفصل الأول مش موجود."); return; }

            _runner.SwitchStory(asset.text);
            Progress.UnlockChapter(1);

            if (_menu != null) _menu.Hide();
            StartCoroutine(OpeningRoutine());
        }

        public void ContinueGame()
        {
            bool loaded = SaveSystem.Load(SaveSystem.AutoSlot, _state) ||
                          SaveSystem.Load(SaveSystem.ManualSlotA, _state);
            if (!loaded)
            {
                Debug.LogWarning("[TLC] مفيش حفظ — بنبدأ لعبة جديدة.");
                StartNewGame();
                return;
            }

            int idx = System.Array.IndexOf(chapterResources, "Story/" + _state.ChapterId);
            if (idx < 0) idx = 0;

            var asset = Resources.Load<TextAsset>(chapterResources[idx]);
            if (asset == null)
            {
                Debug.LogError($"[TLC] الفصل {_state.ChapterId} مش موجود.");
                StartNewGame();
                return;
            }

            _chapterIndex = idx;
            _runner.SwitchStory(asset.text);

            if (_menu != null) _menu.Hide();
            if (_ui != null) _ui.Build(_runner);

            string node = _state.CurrentNodeId;
            if (!string.IsNullOrEmpty(node)) _runner.BeginAt(node);
            else _runner.Begin();

            if (_audio != null)
            {
                _audio.PlayMusic("title_theme", 1.2f);
                _audio.PlayAmbience("apartment_hum_loop");
            }
            Debug.Log($"[TLC] اتنقل للحفظ: {_state.ChapterId} / {node}");
        }

        public void StartChapter(int chapterNumber)
        {
            int idx = Mathf.Clamp(chapterNumber - 1, 0, chapterResources.Length - 1);
            if (!Progress.IsChapterUnlocked(chapterNumber))
            {
                Debug.LogWarning($"[TLC] الفصل {chapterNumber} لسه مقفول.");
                return;
            }

            var asset = Resources.Load<TextAsset>(chapterResources[idx]);
            if (asset == null) return;

            _state.ResetToDefaults();
            _chapterIndex = idx;
            _runner.SwitchStory(asset.text);

            if (_menu != null) _menu.Hide();
            if (_ui != null) _ui.Build(_runner);

            // ندخل من أول عقدة دخول للفصل
            var doc = _runner.Story != null ? _runner.Story.Document : null;
            string entry = doc != null ? doc.entry : null;
            if (!string.IsNullOrEmpty(entry)) _runner.BeginAt(entry);
            else _runner.Begin();

            if (_audio != null) _audio.PlayChapterSting();
        }

        void SaveTo(int menuSlot)
        {
            int slot = SaveSystem.ManualSlotA + Mathf.Clamp(menuSlot, 0, 2);
            _state.ChapterId = chapterResources[_chapterIndex].Replace("Story/", "");
            bool ok = SaveSystem.Save(slot, _state);
            if (_audio != null) _audio.PlaySfx(ok ? "ui_click" : "ui_back");
            RefreshMenuContext();
            if (_menu != null) _menu.Refresh();
        }

        void LoadFrom(int menuSlot)
        {
            int slot = SaveSystem.ManualSlotA + Mathf.Clamp(menuSlot, 0, 2);
            if (!SaveSystem.Load(slot, _state)) return;

            CloseMenus();
            int idx = System.Array.IndexOf(chapterResources, "Story/" + _state.ChapterId);
            if (idx < 0) idx = 0;

            var asset = Resources.Load<TextAsset>(chapterResources[idx]);
            if (asset == null) return;

            _chapterIndex = idx;
            _runner.SwitchStory(asset.text);
            if (_ui != null) _ui.Build(_runner);
            _runner.BeginAt(_state.CurrentNodeId);
        }

        void QuitToTitle()
        {
            if (_audio != null) _audio.PlayMusic("title_theme", 1.8f);
            RefreshMenuContext();
            if (_menu != null) _menu.Show(UIScreen.Title);
        }

        // ────────────────────────── نقاط التفاعل ──────────────────────────

        void BuildHotspots()
        {
            CreatePromptLabel();

            RegisterHotspot("photo", _apartment.PhotoAnchor.position, "افحص الصورة",
                            new Vector3(0.42f, 0.34f, 0.06f));
            RegisterHotspot("envelope", _apartment.EnvelopeAnchor.position, "افتح الظرف",
                            new Vector3(0.30f, 0.02f, 0.22f));
        }

        void CreatePromptLabel()
        {
            var canvas = UIFactory.CreateCanvas("UI_InteractPrompt", 8);
            var safe = UIFactory.CreateSafeArea(canvas);
            _interactPrompt = UIFactory.CreateText(safe, "Prompt", "", 36f,
                TextAlignmentOptions.Center, UIFactory.Gold);
            _interactPrompt.rectTransform.anchorMin = new Vector2(0.2f, 0.52f);
            _interactPrompt.rectTransform.anchorMax = new Vector2(0.8f, 0.60f);
            _interactPrompt.rectTransform.offsetMin = Vector2.zero;
            _interactPrompt.rectTransform.offsetMax = Vector2.zero;
            _interactPrompt.enabled = false;
        }

        void RegisterHotspot(string id, Vector3 worldPos, string prompt, Vector3 size)
        {
            var go = new GameObject($"Hotspot_{id}");
            go.transform.position = worldPos;

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size * 1.8f;

            var inter = go.AddComponent<Interactable>();
            inter.hotspotId = id;
            inter.prompt = prompt;
            inter.radius = 2.4f;
            inter.visual = go.transform;

            inter.Setup(_player.transform, _input, _runner, _interactPrompt);
            go.SetActive(false);   // بيشتغل لما القصة تطلبه

            _hotspots[id] = inter;
        }

        void UpdateInteractPrompt(string hotspotId)
        {
            if (_mobileControls != null)
                _mobileControls.SetInteractAvailable(!string.IsNullOrEmpty(hotspotId));

            foreach (var kv in _hotspots)
                if (kv.Value != null && kv.Key != hotspotId)
                    kv.Value.gameObject.SetActive(false);

            if (hotspotId != null && _hotspots.TryGetValue(hotspotId, out var target))
            {
                target.gameObject.SetActive(true);
                if (!string.IsNullOrEmpty(target.prompt))
                {
                    _interactPrompt.text = target.prompt;
                    _interactPrompt.enabled = false;
                }
            }
        }

        // ────────────────────────── أحداث القصة ──────────────────────────

        void OnNarrativeEvent(string action, string param)
        {
            switch (action)
            {
                case NarrativeRunner.ActWaitInteract:
                    UpdateInteractPrompt(param);
                    break;

                case NarrativeRunner.ActCinematic:
                    PlayCinematic(param);
                    break;

                case NarrativeRunner.ActAudio:
                    Debug.Log($"[TLC] صوت: {param} (مفيش ملفات صوت بعد — placeholder)");
                    _runner.ResumeAfterEvent();
                    break;

                case NarrativeRunner.ActSave:
                    SaveSystem.Save(SaveSystem.AutoSlot, _state);
                    _runner.ResumeAfterEvent();
                    break;

                default:
                    _runner.ResumeAfterEvent();
                    break;
            }
        }

        void PlayCinematic(string shotId)
        {
            var p = _player.transform.position;
            var shots = new List<Shot>();

            switch (shotId)
            {
                case "door_knock":
                    shots.Add(new Shot
                    {
                        name = "DoorClose",
                        position = new Vector3(0f, 1.5f, -1.4f),
                        lookAt = _apartment.DoorAnchor.position,
                        fieldOfView = 32f,
                        duration = 2.4f
                    });
                    break;

                case "dialogue":
                    if (_dialogueCamera != null)
                    {
                        var subject = FindSpeakerTransform(shotId);
                        if (subject != null)
                            shots.Add(_dialogueCamera.CloseUp(subject));
                    }
                    break;

                case "photo_closeup":
                    shots.Add(new Shot
                    {
                        name = "PhotoClose",
                        position = _apartment.PhotoAnchor.position + new Vector3(-0.8f, 0.1f, 0f),
                        lookAt = _apartment.PhotoAnchor.position,
                        fieldOfView = 26f,
                        duration = 2.6f
                    });
                    break;

                default:
                    shots.Add(new Shot
                    {
                        name = "Wide",
                        position = p + new Vector3(2.2f, 1.9f, -2.6f),
                        lookAt = p + Vector3.up * 1.2f,
                        fieldOfView = 38f,
                        duration = 2f
                    });
                    break;
            }

            _cinematics.PlayShots(shots, _runner);
        }

        Transform FindSpeakerTransform(string key)
        {
            // آدم دايماً موجود. الشخصيات التانية بتتحطّ في المشهد بعدين،
            // ولحد ساعتها الكاميرا بتستخدم لقطة آدم — مفيش كراش ومفيش مشهد فاضي.
            if (string.IsNullOrEmpty(key) || key == "adam")
                return _player != null ? _player.transform : null;

            // شخصيات حقيقية موزّعة في المشهد
            if (_cast != null)
            {
                string k = key.ToLowerInvariant();
                if (k.Contains("murad"))  return _cast.Puppet(CharacterLibrary.Cast.Murad)?.transform;
                if (k.Contains("laila"))  return _cast.Puppet(CharacterLibrary.Cast.Laila)?.transform;
                if (k.Contains("yassin")) return _cast.Puppet(CharacterLibrary.Cast.Yassin)?.transform;
            }

            var anim = GameObject.Find(key.ToUpperInvariant());
            if (anim != null) return anim.transform;

            return _player != null ? _player.transform : null;
        }

        /// <summary>يخلي الشخصية اللي بتتكلم تعمل إيماءات، والباقي يسكت.</summary>
        void OnChoicesShown(StoryNode node, System.Collections.Generic.List<ChoiceOption> options)
        {
            ApplySceneCue(node);
        }

        void OnLineShown(StoryNode node)
        {
            UpdateInteractPrompt(null);
            ApplySceneCue(node);
            if (_cast == null) return;

            _cast.StopAllTalking();

            string speaker = node != null ? node.speaker : null;
            string k = speaker == null ? "" : speaker.ToLowerInvariant();

            var head = _player != null ? _player.transform : null;

            if (k.Contains("murad"))       _cast.SetTalking(CharacterLibrary.Cast.Murad, true, head);
            else if (k.Contains("laila"))  _cast.SetTalking(CharacterLibrary.Cast.Laila, true, head);
            else if (k.Contains("yassin")) _cast.SetTalking(CharacterLibrary.Cast.Yassin, true, head);

            _cast.FaceAllTowards(head);
        }

        // ─────────────────── ديكور المشهد + لقطات الكاميرا ───────────────────
        //
        // كل عقدة في chapter01.json بتحمل حقلين:
        //   act    = رقم/اسم المشهد (scene01 … scene08, ending_a/b/c)
        //   camera = اسم اللقطة (cu_adam · ots_murad · wide_cafe · cu_prop_envelope …)
        // الدالة دي بتترجم الحقلين لحركة فعلية في اللعبة: مين واقف في الكادر،
        // والكاميرا بتقطع على أنهي لقطة. لو الحقل فاضي مفيش تغيير — يعني
        // العُقد القديمة (اللي ملهاش cues) بتشتغل زي ما هي.

        string _lastAct;
        string _lastCameraHint;

        // ── مواقع الفصل (PHASE 4.2) ──
        readonly Dictionary<EnvironmentLibrary.Location, EnvironmentLibrary.LocationSet> _locations
            = new Dictionary<EnvironmentLibrary.Location, EnvironmentLibrary.LocationSet>();
        EnvironmentLibrary.LocationSet _currentLocation;
        EnvironmentLibrary.Location _currentLocationKind = EnvironmentLibrary.Location.Apartment;

        // إضاءة الشقة الأصلية (بنرجّعها لما نرجع للشقة)
        UnityEngine.Rendering.AmbientMode _homeAmbientMode;
        Color _homeAmbientLight;
        Color _homeSky, _homeEquator, _homeGround;
        bool _ambientSaved;

        void ApplySceneCue(StoryNode node)
        {
            if (node == null) return;
            if (!string.IsNullOrEmpty(node.act)) ApplyAct(node.act);
            ApplyCameraHint(node.camera);
        }

        /// <summary>
        /// مين موجود في المشهد + **الموقع نفسه**.
        /// كل act ليه موقع مستقل (مقهى/مكتب صحفي/أرشيف/تحقيق/مكتب يوسف/الشقة) —
        /// مش نفس الشقة لكل المشاهد. بنشيل الموقع القديم ونبني الجديد وننقل
        /// اللاعب والشخصيات لنقاط الوقوف بتاعته.
        /// </summary>
        void ApplyAct(string act) => ApplyAct(act, false);

        void ApplyAct(string act, bool force)
        {
            if (!force && DebugLockLocation) return;
            if (act == _lastAct) return;
            _lastAct = act;

            var locKind = EnvironmentLibrary.LocationForAct(act);
            var set = UseLocation(locKind);

            Debug.Log($"[TLC/Act] المشهد: {act} → الموقع: {set.DisplayName}");

            // اللاعب على نقطة الظهور بتاعة الموقع
            if (_player != null && set != null)
            {
                var cc = _player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                _player.transform.position = set.Spawn;
                _player.transform.rotation = Quaternion.Euler(0f, set.SpawnYaw, 0f);
                if (cc != null) cc.enabled = true;

                if (_camera != null) _camera.SnapBehind();
                _lastCameraHint = null;   // نسمح بأول لقطة في الموقع الجديد
            }

            if (_cast == null) _cast = FindFirstObjectByType<CastDirector>();
            if (_cast == null)
            {
                Debug.LogWarning("[TLC/Act] CastDirector مش موجود — الشخصيات مش هتظهر.");
                return;
            }
            if (set == null) return;

            CharacterLibrary.Cast[] everyone =
            {
                CharacterLibrary.Cast.Murad, CharacterLibrary.Cast.Laila,
                CharacterLibrary.Cast.Yassin, CharacterLibrary.Cast.Yusuf
            };
            foreach (var c in everyone) _cast.SetVisible(c, false);

            foreach (var pair in set.CastSpots)
            {
                var cast = CastFromKey(pair.Key);
                if (cast == null) continue;

                bool seated = set.CastSeated.TryGetValue(pair.Key, out bool s) && s;
                float yaw = set.CastYaw.TryGetValue(pair.Key, out float y) ? y : 0f;

                _cast.SetVisible(cast.Value, true);
                _cast.Place(cast.Value, pair.Value, null, seated);

                var rootT = _cast.Model(cast.Value)?.transform;
                if (rootT != null)
                {
                    var parent = rootT.parent != null ? rootT.parent : rootT;
                    parent.rotation = Quaternion.Euler(0f, yaw, 0f);
                }

                Debug.Log($"[TLC/Act] {cast.Value} في {set.DisplayName} على {pair.Value} " +
                          $"(قاعد={seated}) · كاست متسجّل={_cast.SpawnedCount}");
            }
        }

        static CharacterLibrary.Cast? CastFromKey(string key)
        {
            switch (key)
            {
                case "murad":  return CharacterLibrary.Cast.Murad;
                case "laila":  return CharacterLibrary.Cast.Laila;
                case "yassin": return CharacterLibrary.Cast.Yassin;
                case "yusuf":  return CharacterLibrary.Cast.Yusuf;
                default:       return null;
            }
        }

        /// <summary>يخلي الموقع المطلوب هو الظاهر ويخفي الباقي (ومنه شقة آدم).</summary>
        EnvironmentLibrary.LocationSet UseLocation(EnvironmentLibrary.Location kind)
        {
            if (kind == _currentLocationKind && _currentLocation != null) return _currentLocation;

            // الشقة: موجودة أصلًا من ApartmentBuilder
            if (!_ambientSaved)
            {
                _homeAmbientMode = RenderSettings.ambientMode;
                _homeAmbientLight = RenderSettings.ambientLight;
                _homeSky = RenderSettings.ambientSkyColor;
                _homeEquator = RenderSettings.ambientEquatorColor;
                _homeGround = RenderSettings.ambientGroundColor;
                _ambientSaved = true;
            }

            bool apartmentOn = kind == EnvironmentLibrary.Location.Apartment;
            if (_apartment != null) _apartment.gameObject.SetActive(apartmentOn);

            if (apartmentOn)
            {
                EnvironmentLibrary.ClearCurrent();

                // إضاءة الشقة الليلية زي ما LightingRig ظبطها
                RenderSettings.ambientMode = _homeAmbientMode;
                RenderSettings.ambientLight = _homeAmbientLight;
                RenderSettings.ambientSkyColor = _homeSky;
                RenderSettings.ambientEquatorColor = _homeEquator;
                RenderSettings.ambientGroundColor = _homeGround;

                _currentLocationKind = kind;
                _currentLocation = null;
                _currentLocationKind = kind;
                Debug.Log("[TLC/Loc] شقة آدم");
                return new EnvironmentLibrary.LocationSet
                {
                    Kind = kind,
                    Root = _apartment != null ? _apartment.transform : null,
                    DisplayName = "شقة آدم",
                    Spawn = _apartment != null && _apartment.PlayerSpawn != null
                        ? _apartment.PlayerSpawn.position : Vector3.zero,
                    SpawnYaw = 0f
                };
            }

            if (!_locations.TryGetValue(kind, out var set) || set == null)
            {
                var worldRoot = GameObject.Find("WORLD");
                var parent = worldRoot != null ? worldRoot.transform : transform;
                set = EnvironmentLibrary.Build(kind, parent);
                _locations[kind] = set;
                Debug.Log($"[TLC/Loc] اتبنى موقع جديد: {set.DisplayName}");
            }

            foreach (var kv in _locations)
                EnvironmentLibrary.SetActive(kv.Value, kv.Key == kind);

            // كل موقع ليه إضاءة محيطة خاصة بيه
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = set.Ambient;

            _currentLocation = set;
            _currentLocationKind = kind;
            Debug.Log($"[TLC/Loc] الموقع الحالي: {set.DisplayName} · إضاءة {set.Ambient}");
            return set;
        }

        // ── واجهة اختبار المواقع (بتستخدمها أداة التصوير في المحرر) ──

        /// <summary>ينقل اللعبة لمشهد معيّن بالموقع بتاعه — للاختبار البصري.</summary>
        /// <summary>
        /// لو true، سرد القصة مايقدرش ينقل اللاعب/يبدّل الموقع (بيستخدمه الاختبار
        /// والتصوير عشان الحوار اللي شغّال في الخلفية ما يخطفش الكادر).
        /// </summary>
        public bool DebugLockLocation;

        public void DebugGotoAct(string act)
        {
            _lastAct = null;
            ApplyAct(act, true);
        }

        /// <summary>هل اللاعب والكاست اتبنوا فعلاً؟ (المواقع بتتبنى بعد بداية اللعبة)</summary>
        public bool DebugWorldReady => _player != null && _cast != null;

        public string DebugLocationName =>
            _currentLocation != null ? _currentLocation.DisplayName : "شقة آدم";

        /// <summary>ينقل اللاعب لمكان محدد (لاختبار التصادم/الكاميرا الثالثة).</summary>
        public void DebugTeleport(Vector3 pos, float yaw)
        {
            if (_player == null) return;
            var cc = _player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            _player.transform.position = pos;
            _player.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (cc != null) cc.enabled = true;
            if (_camera != null) _camera.SnapBehind();
        }

        public CharacterController DebugController =>
            _player != null ? _player.GetComponent<CharacterController>() : null;

        public TLC.Player.ThirdPersonController DebugPlayerController => _player;

        public bool DebugCameraAnchor(string key, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (_currentLocation == null || _currentLocation.CameraAnchors == null) return false;
            return _currentLocation.CameraAnchors.TryGetValue(key, out pos);
        }

        public Vector3 DebugPlayerPosition =>
            _player != null ? _player.transform.position : Vector3.zero;

        /// <summary>أقرب نقطة دليل في الموقع الحالي (للكاميرا وللتفاعل).</summary>
        public bool TryGetLocationProp(string key, out Vector3 pos)
        {
            pos = Vector3.zero;
            if (_currentLocation == null) return false;
            if (!_currentLocation.Props.TryGetValue(key, out pos)) return false;
            return true;
        }

        void ShowAt(CharacterLibrary.Cast cast, Vector3 pos, Transform lookAt, bool seated)
        {
            _cast.SetVisible(cast, true);
            _cast.Place(cast, pos, lookAt, seated);
            var h = _cast.HeadOf(cast);
            if (h != null) Debug.Log($"[TLC/Act] {cast} في الكادر على {pos}");
        }

        /// <summary>يترجم اسم اللقطة من JSON للقطة كاميرا حقيقية.</summary>
        void ApplyCameraHint(string hint)
        {
            if (string.IsNullOrEmpty(hint) || hint == _lastCameraHint) return;
            _lastCameraHint = hint;

            if (_cinematics == null || _dialogueCamera == null) return;
            var focus = _player != null ? _player.transform : null;
            if (focus == null) return;

            Shot shot = default;
            bool has = false;

            if (hint.StartsWith("cu_"))
            {
                var s = ResolveShotSubject(hint.Substring(3));
                shot = _dialogueCamera.CloseUp(s); has = true;
            }
            else if (hint.StartsWith("ots_"))
            {
                var s = ResolveShotSubject(hint.Substring(4));
                var other = s == focus ? NearestVisibleCharacter() : focus;
                if (other != null && s != null) { shot = _dialogueCamera.OverTheShoulder(s, other); has = true; }
            }
            else if (hint.StartsWith("wide_"))
            {
                var s = ResolveShotSubject(hint.Substring(5));
                var p = s != null ? s.position : focus.position;
                shot = _dialogueCamera.Wide(p, 4.6f, 2.1f); has = true;
            }
            else if (hint == "black")
            {
                // قطع للسواد: لقطة بعيدة جدًا وإضاءة مطفية مؤقتًا — بديل مش مثالي بس مقصود
                shot = _dialogueCamera.Wide(focus.position + Vector3.up * 6f, 14f, 6f); has = true;
                shot.fieldOfView = 18f;
            }

            if (!has) return;

            shot.duration = 1.9f;
            if (_shotDirector != null) _shotDirector.CutTo(shot);
            else _cinematics.PlayShots(new List<Shot> { shot }, null);
            Debug.Log($"[TLC/Camera] لقطة '{hint}' على {(shot.lookAt)}");
        }

        Transform NearestVisibleCharacter()
        {
            if (_cast == null || _player == null) return null;
            CharacterLibrary.Cast[] all =
                { CharacterLibrary.Cast.Murad, CharacterLibrary.Cast.Laila,
                  CharacterLibrary.Cast.Yassin, CharacterLibrary.Cast.Yusuf };

            Transform best = null; float bestD = float.MaxValue;
            foreach (var c in all)
            {
                if (!_cast.IsVisible(c)) continue;
                var h = _cast.HeadOf(c);
                if (h == null) continue;
                float d = (h.position - _player.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = h; }
            }
            return best;
        }

        /// <summary>"adam" / "murad" / "prop_envelope" / "prop_photo" … → نقطة في المشهد.</summary>
        Transform ResolveShotSubject(string key)
        {
            var head = _player != null ? _player.transform : null;
            if (string.IsNullOrEmpty(key)) return head;

            if (key.StartsWith("prop_"))
            {
                string p = key.Substring(5);

                // الأولوية لموقع المشهد الحالي (مقهى/أرشيف/مكتب…) — أهم من مراسي الشقة
                if (_currentLocation != null && _currentLocation.PropAnchors != null)
                {
                    foreach (var kv in _currentLocation.PropAnchors)
                        if (p.Contains(kv.Key) || kv.Key.Contains(p))
                            return kv.Value;
                }

                if (_apartment == null) return head;
                if (p.Contains("envelope") || p.Contains("note") || p.Contains("contents")) return _apartment.EnvelopeAnchor;
                if (p.Contains("phone")) return _apartment.PhoneAnchor;
                if (p.Contains("door")) return _apartment.DoorAnchor;
                return _apartment.PhotoAnchor;
            }

            if (key == "adam") return head;

            if (_cast != null)
            {
                if (key.Contains("murad"))  return _cast.HeadOf(CharacterLibrary.Cast.Murad) ?? head;
                if (key.Contains("laila"))  return _cast.HeadOf(CharacterLibrary.Cast.Laila) ?? head;
                if (key.Contains("yassin")) return _cast.HeadOf(CharacterLibrary.Cast.Yassin) ?? head;
                if (key.Contains("yusuf"))  return _cast.HeadOf(CharacterLibrary.Cast.Yusuf) ?? head;
            }
            return head;
        }

        void OnEnding(StoryNode node)
        {
            _lastCameraHint = null;
            if (_shotDirector != null) _shotDirector.Stop();
            Debug.Log($"[TLC] نهاية: {node.endingId} — {node.endingTitle}");

            if (Progress.MarkEnding(node.endingId))
                Debug.Log($"[TLC] نهاية جديدة اتكشفت ({Progress.SeenCount}/{MenuModel.EndingCount})");

            if (_audio != null) _audio.PlaySfx("ending_sting", 0.9f);
            SaveSystem.Save(SaveSystem.AutoSlot, _state);   // حفظ تلقائي عند النهاية
            RefreshMenuContext();

            string nextEntry = MapEndingToNextChapter(node.endingId, out int nextIndex);
            if (nextEntry == null)
            {
                Debug.Log($"[TLC] ══ نهاية اللعبة: {node.endingId} ══");
                return;
            }

            StartCoroutine(AdvanceChapter(nextIndex, nextEntry));
        }

        /// <summary>CH1_A → ("ch2_from_ch1_a", 1) · CH3_* → (null, -1)</summary>
        static string MapEndingToNextChapter(string endingId, out int nextIndex)
        {
            nextIndex = -1;
            if (string.IsNullOrEmpty(endingId)) return null;

            var parts = endingId.Split('_');
            if (parts.Length < 2 || !parts[0].StartsWith("CH")) return null;

            if (!int.TryParse(parts[0].Substring(2), out int chapterNumber)) return null;
            if (chapterNumber >= 3) return null;      // الفصل الثالث = نهاية اللعبة

            nextIndex = chapterNumber;                // CH1 → index 1 (الفصل الثاني)
            return $"ch{chapterNumber + 1}_from_{parts[0].ToLowerInvariant()}_{parts[1].ToLowerInvariant()}";
        }

        System.Collections.IEnumerator AdvanceChapter(int chapterIndex, string entryNode)
        {
            yield return new WaitForSeconds(delayBeforeNextChapter);

            if (chapterIndex < 0 || chapterIndex >= chapterResources.Length)
            {
                Debug.LogWarning($"[TLC] الفصل رقم {chapterIndex + 1} مش موجود في القائمة.");
                yield break;
            }

            var asset = Resources.Load<TextAsset>(chapterResources[chapterIndex]);
            if (asset == null)
            {
                Debug.LogError($"[TLC] ملف الفصل مش موجود: {chapterResources[chapterIndex]}");
                yield break;
            }

            if (!_runner.SwitchStory(asset.text))
            {
                Debug.LogError("[TLC] فشل تحميل الفصل الجديد.");
                yield break;
            }

            _chapterIndex = chapterIndex;
            Progress.UnlockChapter(chapterIndex + 1);
            Progress.SetLastChapter(chapterResources[chapterIndex]);
            RefreshMenuContext();

            if (_ui != null && chapterIndex < chapterTitles.Length)
                yield return _ui.PlayChapterCard(chapterTitles[chapterIndex], 2.2f);

            _runner.BeginAt(entryNode);
            Debug.Log($"[TLC] بدأ {chapterResources[chapterIndex]} من '{entryNode}'");
        }

        // ────────────────────────── اللوب ──────────────────────────

        void Update()
        {
            if (menuOnly) return;
            if (_input == null || _ui == null || _runner == null) return;

            // المتابعة في الحوار
            if (_input.AdvancePressed && _runner.IsWaitingForInput)
                _ui.OnAdvancePressed();
            else if (_input.AdvancePressed && _ui.IsTyping)
                _ui.OnAdvancePressed();

            // تخزين سريع للتجربة
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveSystem.Save(SaveSystem.ManualSlotA, _state);
                Debug.Log("[TLC] حفظ يدوي → خانة A");
            }
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (SaveSystem.Load(SaveSystem.ManualSlotA, _state))
                    Debug.Log("[TLC] تحميل من خانة A");
            }
            if (Input.GetKeyDown(KeyCode.F1)) _quality?.Next();

            // زر الرجوع / Escape: يقفل القائمة أو يفتح الإيقاف
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_menu != null && _menu.IsOpen) _menu.Back();
                else OpenPause();
            }
        }

        // ────────────────────────── مساعدات ──────────────────────────

        static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var f = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            if (f != null) f.SetValue(target, value);
            else Debug.LogWarning($"[TLC] الحقل '{fieldName}' مش موجود في {target.GetType().Name}");
        }
    }
}
