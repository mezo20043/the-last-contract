using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using TLC.Narrative;
using TLC.Player;

namespace TLC.Cinematics
{
    [System.Serializable]
    public class Shot
    {
        public string name;
        public Vector3 position;
        public Vector3 lookAt;
        public float fieldOfView = 45f;
        public float duration = 3f;
        [Range(0f, 1f)] public float ease = 0.35f;
    }

    /// <summary>
    /// مخرج السينمائيات: بينقل الكاميرا بين لقطات (CinemachineCamera لكل لقطة)،
    /// بيقفل اللاعب، ويرجّع التحكم بنفس اللحظة — من غير قطع.
    /// اللقطات بتتبني بالكود، فمفيش ملفات Timeline ناقصة.
    /// </summary>
    public class CinematicDirector : MonoBehaviour
    {
        [Header("المراجع")]
        [SerializeField] Camera mainCamera;
        [SerializeField] ThirdPersonController player;
        [SerializeField] PlayerInputReader input;
        [SerializeField] CinematicCameraRig gameplayCamera;

        readonly List<CinemachineCamera> _shotCameras = new List<CinemachineCamera>();
        Coroutine _playing;

        public bool IsPlaying { get; private set; }

        public void Setup(Camera cam, ThirdPersonController p, PlayerInputReader i, CinematicCameraRig gameplay)
        {
            mainCamera = cam; player = p; input = i; gameplayCamera = gameplay;

            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null) brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
        }

        /// <summary>بيشغّل قائمة لقطات ويرجّع للتحكم بعدها.</summary>
        public void PlayShots(List<Shot> shots, NarrativeRunner runner)
        {
            if (_playing != null) StopCoroutine(_playing);
            _playing = StartCoroutine(PlayRoutine(shots, runner));
        }

        IEnumerator PlayRoutine(List<Shot> shots, NarrativeRunner runner)
        {
            IsPlaying = true;

            if (player != null) player.CanMove = false;
            if (input != null) input.GameplayEnabled = false;

            var created = new List<GameObject>();

            foreach (var shot in shots)
            {
                var go = new GameObject($"Shot_{shot.name}", typeof(CinemachineCamera));
                go.transform.position = shot.position;
                // حماية: اتجاه بصر صفري (كاميرا فوق نقطة النظر بالظبط) بيطلّع
                // Assertion 'IsNormalized' في المحرك كل فريم. بنزحزح الوجهة شبر بسيط.
                var aim = shot.lookAt;
                if ((aim - shot.position).sqrMagnitude < 0.0004f) aim += new Vector3(0.01f, 0.01f, 0.35f);
                go.transform.LookAt(aim);

                var vcam = go.GetComponent<CinemachineCamera>();
                var lens = vcam.Lens;
                lens.FieldOfView = shot.fieldOfView;
                vcam.Lens = lens;
                vcam.Priority = 100;

                created.Add(go);
                _shotCameras.Add(vcam);

                yield return new WaitForSeconds(shot.duration);

                vcam.Priority = 0;
            }

            foreach (var go in created) Destroy(go);
            _shotCameras.Clear();

            if (gameplayCamera != null) gameplayCamera.RestorePriority();

            yield return new WaitForSeconds(0.6f);

            if (player != null) player.CanMove = true;
            if (input != null) input.GameplayEnabled = true;

            IsPlaying = false;
            _playing = null;

            runner?.ResumeAfterEvent();
        }

        /// <summary>لقطة واحدة سريعة عند تفاعل مهم (زوم على تفاصيل).</summary>
        public void PlaySingle(Shot shot, float extraDelay = 0f)
        {
            var list = new List<Shot> { shot };
            PlayShots(list, null);
            if (extraDelay > 0f) Debug.Log($"[TLC] لقطة '{shot.name}' اشتغلت.");
        }
    }

    /// <summary>
    /// واجهة صغيرة عشان CinematicDirector يقدر يرجّع أولوية كاميرا اللعب
    /// من غير ما يعرف تفاصيل ThirdPersonCameraRig.
    /// </summary>
    public interface CinematicCameraRig
    {
        void RestorePriority();
    }
}
