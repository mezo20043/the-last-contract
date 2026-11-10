using UnityEngine;

namespace TLC.Player
{
    /// <summary>
    /// نقطة تفاعل في العالم (درج، موبايل، جثة، باب).
    /// بتظهر مؤشر لما اللاعب يقرب، وبتضيف دليل للسرد.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractableHotspot : MonoBehaviour
    {
        [Header("الهوية")]
        [SerializeField] string hotspotId;
        [SerializeField] string displayName = "افتح";
        [SerializeField] string flagToSet;

        [Header("الاستخدام")]
        [SerializeField] bool oneShot = true;
        [SerializeField] float duration = 1.2f;    // ثانية اللازمة للتفاعل
        [SerializeField] bool requiresHold = false;

        [Header("المراجع")]
        [SerializeField] Transform player;
        [SerializeField] float interactRadius = 2.2f;
        [SerializeField] GameObject highlight;

        public bool IsAvailable => !(oneShot && _used);
        public string DisplayName => displayName;
        public string HotspotId => hotspotId;

        public System.Action<InteractableHotspot> OnInteracted;

        bool _used;
        float _holdTimer;
        bool _playerInside;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Update()
        {
            if (player == null) return;

            float d = Vector3.Distance(player.position, transform.position);
            bool inside = d <= interactRadius && IsAvailable;

            if (inside != _playerInside)
            {
                _playerInside = inside;
                if (highlight) highlight.SetActive(inside);
            }
        }

        public bool CanInteract() => _playerInside && IsAvailable;

        /// <summary>بينده زر التفاعل.</summary>
        public void Interact()
        {
            if (!CanInteract()) return;

            if (flagToSet != null && flagToSet.Length > 0)
                TLC.Core.GameStateBus.Current?.SetFlag(flagToSet);

            if (oneShot) _used = true;
            OnInteracted?.Invoke(this);
            // ملاحظة: منطق "الدليل اتشاف" بيتكتب في الـ StoryRunner
            // عن طريق flag — الـ hotspot مش عارف حاجة عن القصة.
        }
    }
}
