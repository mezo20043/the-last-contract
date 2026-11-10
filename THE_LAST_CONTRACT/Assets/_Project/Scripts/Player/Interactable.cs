using UnityEngine;
using TMPro;
using TLC.UI;
using TLC.Narrative;

namespace TLC.Player
{
    /// <summary>
    /// نقطة تفاعل في العالم (صورة، ظرف، تليفون، باب).
    /// بتظهر مؤشر لما آدم يقرب، وبتبلّغ NarrativeRunner.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Interactable : MonoBehaviour
    {
        [Header("الهوية")]
        public string hotspotId;
        public string prompt = "تفاعل";
        public string flagToSet;

        [Header("الاستخدام")]
        public bool oneShot = true;
        public float radius = 2.2f;

        [Header("العرض")]
        public Transform visual;
        public float pulseSpeed = 2.4f;
        public Color glowColor = new Color(0.85f, 0.71f, 0.42f);

        Transform _player;
        PlayerInputReader _input;
        NarrativeRunner _runner;
        TextMeshProUGUI _promptLabel;
        bool _used;
        bool _inRange;
        float _pulse;

        public bool IsAvailable => !(oneShot && _used);

        public void Setup(Transform player, PlayerInputReader input, NarrativeRunner runner,
                          TextMeshProUGUI promptLabel)
        {
            _player = player;
            _input = input;
            _runner = runner;
            _promptLabel = promptLabel;
        }

        void Update()
        {
            if (_player == null || !IsAvailable) { SetPrompt(false); return; }

            _inRange = Vector3.Distance(_player.position, transform.position) <= radius;
            SetPrompt(_inRange);

            if (_inRange)
            {
                _pulse += Time.deltaTime * pulseSpeed;
                if (visual != null)
                {
                    float s = 1f + Mathf.Sin(_pulse) * 0.035f;
                    visual.localScale = new Vector3(s, s, s);
                }
            }

            if (_inRange && _input != null && _input.InteractPressed) Interact();
        }

        void SetPrompt(bool show)
        {
            if (_promptLabel != null && _promptLabel.enabled != show)
                _promptLabel.enabled = show;
        }

        public void Interact()
        {
            if (!IsAvailable || _runner == null) return;

            if (!string.IsNullOrEmpty(flagToSet) && _runner.State != null)
                _runner.State.SetFlag(flagToSet);

            if (oneShot) _used = true;
            SetPrompt(false);

            _runner.NotifyInteraction(hotspotId);
        }
    }
}
