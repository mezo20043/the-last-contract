using UnityEngine;
using TLC.Player;

namespace TLC.Audio
{
    /// <summary>
    /// خطوات حسب السطح: بيقيس اللي تحت اللاعب ويختار مجموعة الأصوات المناسبة،
    /// مع تنويع عشوائي عشان ما يبقاش نفس الصوت مكرر.
    /// </summary>
    public class FootstepSystem : MonoBehaviour
    {
        ThirdPersonController _controller;
        AudioDirector _audio;

        int _lastConcrete = -1;
        int _lastWood = -1;
        float _minInterval = 0.22f;
        float _lastTime;

        public string LastSurface { get; private set; } = "concrete";
        public int StepsPlayed { get; private set; }

        public void Setup(ThirdPersonController controller)
        {
            _controller = controller;
            _audio = AudioDirector.Instance ??
                     Object.FindObjectOfType<AudioDirector>();

            if (_controller != null)
                _controller.OnFootstep += HandleFootstep;
        }

        void OnDestroy()
        {
            if (_controller != null)
                _controller.OnFootstep -= HandleFootstep;
        }

        void HandleFootstep()
        {
            if (Time.time - _lastTime < _minInterval) return;
            _lastTime = Time.time;

            string surface = DetectSurface();
            LastSurface = surface;

            if (_audio == null) { StepsPlayed++; return; }

            float volume = 0.55f + 0.25f * Mathf.Clamp01(_controller != null
                ? _controller.CurrentSpeed / 6f : 0.5f);

            if (surface == "wood") PlayVaried("footstep_wood_0", ref _lastWood, volume);
            else PlayVaried("footstep_concrete_0", ref _lastConcrete, volume * 0.95f);
        }

        void PlayVaried(string prefix, ref int lastIndex, float volume)
        {
            int idx;
            do { idx = Random.Range(1, 5); } while (idx == lastIndex);
            lastIndex = idx;

            _audio.PlaySfx(prefix + idx, volume);
            StepsPlayed++;

            if (Random.value < 0.06f)
                _audio.PlaySfx("paper_rustle", 0.12f);   // حفيف خفيف يكسّر التكرار
        }

        string DetectSurface()
        {
            if (_controller == null) return "concrete";

            var origin = _controller.transform.position + Vector3.up * 0.6f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 2.2f))
            {
                var renderer = hit.collider != null ? hit.collider.GetComponent<Renderer>() : null;
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    string matName = renderer.sharedMaterial.name;
                    if (!string.IsNullOrEmpty(matName))
                    {
                        if (matName.Contains("wood") || matName.Contains("fabric")) return "wood";
                        if (matName.Contains("tile") || matName.Contains("metal")) return "concrete";
                        if (matName.Contains("concrete") || matName.Contains("plaster")) return "concrete";
                    }
                }
            }
            return "concrete";
        }
    }
}
