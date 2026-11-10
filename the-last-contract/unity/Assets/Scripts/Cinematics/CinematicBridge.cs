using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using Unity.Cinemachine;

namespace TLC.Cinematics
{
    /// <summary>
    /// الجسر بين اللعب والسينمائية: بيقفل اللاعب، يشغّل Timeline،
    /// ويرجّع التحكم بنفس الموضع والكاميرا — بدون قطع.
    /// </summary>
    public class CinematicBridge : MonoBehaviour
    {
        [Header("مراجع")]
        [SerializeField] PlayableDirector director;
        [SerializeField] TLC.Player.PlayerMotor player;
        [SerializeField] MonoBehaviour playerInput;      // أي سكربت تحكم
        [SerializeField] CinemachineCamera gameplayCamera;

        [Header("خيارات")]
        [SerializeField] bool freezePlayerDuring = true;
        [SerializeField] float blendBackSeconds = 1.0f;

        public bool IsPlaying { get; private set; }
        public System.Action OnCinematicFinished;

        void Awake()
        {
            if (director) director.stopped += OnDirectorStopped;
        }

        void OnDestroy()
        {
            if (director) director.stopped -= OnDirectorStopped;
        }

        public void Play()
        {
            if (director == null || IsPlaying) return;
            StartCoroutine(PlayRoutine());
        }

        IEnumerator PlayRoutine()
        {
            IsPlaying = true;

            if (freezePlayerDuring)
            {
                if (player) player.CanMove = false;
                if (playerInput) playerInput.enabled = false;
            }

            director.Play();
            yield return new WaitWhile(() => director.state == PlayState.Playing);
        }

        void OnDirectorStopped(PlayableDirector d)
        {
            if (!IsPlaying) return;
            StartCoroutine(RestoreRoutine());
        }

        IEnumerator RestoreRoutine()
        {
            if (gameplayCamera) gameplayCamera.Priority = 100;

            yield return new WaitForSeconds(blendBackSeconds);

            if (freezePlayerDuring)
            {
                if (player) player.CanMove = true;
                if (playerInput) playerInput.enabled = true;
            }

            IsPlaying = false;
            OnCinematicFinished?.Invoke();
        }
    }
}
