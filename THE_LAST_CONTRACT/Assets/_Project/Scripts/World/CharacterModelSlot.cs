using UnityEngine;
using TLC.Player;

namespace TLC.World
{
    /// <summary>
    /// نقطة تركيب الموديل الواقعي لآدم فوق الريج البدائي.
    ///
    /// الفكرة: العالم والسرد والمتحكم مش عارفين الشخصية شكلها إيه.
    /// لما تحضّر موديل Humanoid حقيقي (Mixamo / Character Creator 4 / Reallusion)،
    /// بتحطّه في الخانة دي وخلاص — الحركة والقرارات والكاميرا كلها بتكمّل زي ما هي.
    ///
    /// الاستخدام:
    ///   1. ضيف الكومبوننت ده على كائن ADAM_MANSOUR.
    ///   2. ارمي الـ Prefab في خانة Model Prefab.
    ///   3. اتأكد إن الـ Prefab عنده Animator بـ Humanoid Avatar.
    ///   4. شغّل — الريج البدائي بيختفي والموديل بياخد مكانه.
    /// </summary>
    public class CharacterModelSlot : MonoBehaviour
    {
        [Header("الموديل الواقعي")]
        [Tooltip("Prefab بشري Humanoid — Mixamo أو CC4. سيبها فاضية عشان تستخدم البديل المؤقت.")]
        [SerializeField] GameObject modelPrefab;

        [Header("البديل المؤقت")]
        [Tooltip("الريج البدائي اللي بيتخفي لما يتحطّ موديل حقيقي")]
        [SerializeField] Transform placeholderRoot;
        [SerializeField] ProceduralRig placeholderRig;

        [Header("الإعدادات")]
        [SerializeField] Vector3 modelLocalPosition = Vector3.zero;
        [SerializeField] Vector3 modelLocalEuler = Vector3.zero;
        [SerializeField] bool keepPlaceholderIfNoAnimator = true;

        public GameObject SpawnedModel { get; private set; }
        public Animator ModelAnimator { get; private set; }
        public bool UsingRealisticModel => SpawnedModel != null;

        void Awake()
        {
            if (modelPrefab != null) Apply(modelPrefab);
        }

        public void Apply(GameObject prefab)
        {
            if (prefab == null) return;

            SpawnedModel = Instantiate(prefab, transform);
            SpawnedModel.name = "RealisticModel";
            SpawnedModel.transform.localPosition = modelLocalPosition;
            SpawnedModel.transform.localRotation = Quaternion.Euler(modelLocalEuler);

            ModelAnimator = SpawnedModel.GetComponentInChildren<Animator>();

            if (ModelAnimator == null && keepPlaceholderIfNoAnimator)
            {
                // مفيش Animator → مش هنستفيد حاجة، نرجّع البديل
                Debug.LogWarning("[TLC/Character] الموديل ماعندهوش Animator — رجعنا للبديل المؤقت.");
                Destroy(SpawnedModel);
                SpawnedModel = null;
                return;
            }

            HidePlaceholder();
            WireAnimator(ModelAnimator);
        }

        void HidePlaceholder()
        {
            if (placeholderRoot != null)
                foreach (var r in placeholderRoot.GetComponentsInChildren<MeshRenderer>())
                    r.enabled = false;

            if (placeholderRig != null) placeholderRig.enabled = false;
        }

        void WireAnimator(Animator animator)
        {
            if (animator == null) return;

            var controller = GetComponent<ThirdPersonController>();
            if (controller == null) return;

            var f = typeof(ThirdPersonController).GetField("animator",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (f != null && f.GetValue(controller) == null)
                f.SetValue(controller, animator);
        }

        /// <summary>يرجّع الموديل الواقعي لعرضه (مثلاً في مشهد سينمائي قريب).</summary>
        public void ShowModelOnlyForCinematic(bool onlyModel)
        {
            if (placeholderRoot == null) return;
            foreach (var r in placeholderRoot.GetComponentsInChildren<MeshRenderer>())
                r.enabled = !onlyModel;
        }
    }
}
