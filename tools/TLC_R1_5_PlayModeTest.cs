// TLC — R1.5 Play Mode Test (Editor only · بيعدّل مشهد مؤقت بس)
// بيشغّل Play Mode فعلي، يحمّل الشخصيات الأربعة عن طريق CharacterLibrary (نفس كود اللعبة)،
// يربط TLC_Character.controller، ويجرّب GetBoneTransform — وبعدين يخرج.
// الاستخدام: xvfb-run -a Unity -force-glcore -projectPath ... -executeMethod TLC.EditorTools.R15PlayModeTest.Run
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TLC.EditorTools
{
    [InitializeOnLoad]
    public static class R15PlayModeTest
    {
        static readonly (string key, string file, string cast)[] Roster =
        {
            ("adam",   "man-base-character-rigged", "Adam"),
            ("murad",  "elderly-man-rigged",        "Murad"),
            ("laila",  "woman-rigged",              "Laila"),
            ("yassin", "man-rigged",                "Yassin"),
        };

        static string Root => Path.GetDirectoryName(Application.dataPath);
        static int _frame = -1;
        static StringBuilder _log = new StringBuilder();
        static bool _started;

        public static void Run()
        {
            var camGo = new GameObject("MainCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f, 1f);
            cam.fieldOfView = 35f;
            camGo.transform.position = new Vector3(1.5f, 1.6f, 6.2f);
            camGo.transform.LookAt(new Vector3(1.5f, 0.95f, 0f));

            var lgo = new GameObject("KeyLight");
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Directional; l.intensity = 1.5f;
            lgo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Path.Combine(Root, "R1_5_PLAYMODE_TEMP.unity"));

            _log.AppendLine($"Unity {Application.unityVersion} · graphics={SystemInfo.graphicsDeviceType} · started={DateTime.UtcNow:o}");
            Debug.Log("[TLC/R1.5P] الدخول في Play Mode...");

            _started = true;
            _frame = 0;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!_started) return;
            if (!EditorApplication.isPlaying) { _frame = 0; return; }
            _frame++;
            if (_frame < 45) return;       // نسيب اللعبة تشتغل شوية

            _started = false;
            EditorApplication.update -= Tick;

            try
            {
                // ١) تحميل الشخصيات بنفس كود اللعبة
                int available = TLC.World.CharacterLibrary.AvailableCount();
                _log.AppendLine($"CharacterLibrary.AvailableCount() = {available}");

                var ctrl = Resources.Load<RuntimeAnimatorController>("Animators/TLC_Character");
                _log.AppendLine($"RuntimeAnimatorController TLC_Character loaded = {(ctrl != null)}");

                foreach (var (key, file, cast) in Roster)
                {
                    var prefab = TLC.World.CharacterLibrary.Load(cast);
                    if (prefab == null) { _log.AppendLine($"{cast}: FAIL (null)"); Debug.LogError($"[TLC/R1.5P] {cast} = null"); continue; }

                    var go = UnityEngine.Object.Instantiate(prefab);
                    go.name = "PLAY_" + cast;
                    var anim = go.GetComponent<Animator>();
                    if (anim != null && ctrl != null) anim.runtimeAnimatorController = ctrl;

                    bool avatarHuman = anim != null && anim.avatar != null && anim.avatar.isHuman;
                    bool boneOk = false; string boneName = "-";
                    if (anim != null && anim.avatar != null && anim.isHuman)
                    {
                        var head = anim.GetBoneTransform(HumanBodyBones.Head);
                        var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
                        boneOk = head != null && hips != null;
                        boneName = head != null ? head.name : "-";
                    }
                    _log.AppendLine($"{cast}: loaded ✔ · animator={(anim != null)} · controllerAssigned={(anim != null && anim.runtimeAnimatorController != null)} · avatarHuman={avatarHuman} · isHuman(initialized)={(anim != null && anim.isHuman)} · GetBoneTransform(Head)={boneOk} ({boneName})");
                    Debug.Log($"[TLC/R1.5P] {cast} → avatarHuman={avatarHuman} · controller={(anim != null && anim.runtimeAnimatorController != null)} · isHuman={(anim != null && anim.isHuman)} · headBone={boneOk}");
                }

                // ٢) التحقق من إن الأنيميشن شغّال فعلاً (time بيتحرك)
                var first = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None).FirstOrDefault();
                if (first != null)
                {
                    var st = first.GetCurrentAnimatorStateInfo(0);
                    _log.AppendLine($"Animator: layer0 state hash={st.fullPathHash} · normalizedTime={st.normalizedTime:0.000} · speed={first.speed}");
                }
            }
            catch (Exception e) { _log.AppendLine("EXCEPTION: " + e); Debug.LogError(e); }

            var path = Path.Combine(Root, "R1_5_PLAYMODE.txt");
            File.WriteAllText(path, _log.ToString());
            Debug.Log("[TLC/R1.5P] النتيجة اتكتبت: " + path);
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => { Debug.Log("[TLC/R1.5P] خروج"); EditorApplication.Exit(0); };
        }
    }
}
