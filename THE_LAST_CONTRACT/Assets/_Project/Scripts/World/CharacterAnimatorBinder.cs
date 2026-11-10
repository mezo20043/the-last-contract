using System.Collections.Generic;
using UnityEngine;
using TLC.Player;

namespace TLC.World
{
    /// <summary>
    /// بيربط Animator Controller بالموديل وقت التشغيل.
    ///
    /// ليه الكلاس ده موجود لوحده:
    /// الـ controller نفسه بيتولّد في المحرر (AnimatorControllerBuilder)،
    /// لكن في مشهد اللعب محتاجين نتأكد إنه **اتحمّل واتربط فعلاً** على كائن آدم،
    /// وإن مالك الحركة واحد — إما ThirdPersonController أو HumanoidPuppet،
    /// مش الاتنين. قبل كده كان الـ Animation بتاعت الحركة ماشية على الـ Puppet
    /// والـ Controller مش عارف بحاجة؛ دلوقتي المالك بيتحدد صراحةً هنا.
    ///
    /// لو الـ controller مش موجود (مثلاً اتحذف بالغلط)، الزرع مابيفشلش:
    /// بيرجع للحركة الإجرائية وبيسجّل تحذير واضح.
    /// </summary>
    public class CharacterAnimatorBinder : MonoBehaviour
    {
        public const string DefaultControllerPath = "Animators/TLC_Character";

        [SerializeField] string controllerResourcePath = DefaultControllerPath;

        /// <summary>الـ Animator اللي اتلقى أو اتعمل.</summary>
        public Animator Animator { get; private set; }

        /// <summary>الـ controller اتحمّل من Resources.</summary>
        public bool ControllerFound { get; private set; }

        /// <summary>الـ Animator بقى ليه controller — يعني الحركة من الكليبات.</summary>
        public bool Bound { get; private set; }

        /// <summary>الـ Avatar من نوع Humanoid.</summary>
        public bool AvatarIsHuman { get; private set; }

        /// <summary>آخر رسالة حالة — بتتطبع وب تتسجّل في التقرير.</summary>
        public string StatusMessage { get; private set; } = "لسه ما اتربطش";

        readonly Dictionary<string, bool> _paramCache = new Dictionary<string, bool>();

        /// <summary>
        /// بيربط الموديل. بيرجّع true لو الحركة بقت من الـ controller.
        /// </summary>
        public bool Bind(GameObject modelRoot, ThirdPersonController controller = null,
                         HumanoidPuppet puppet = null)
        {
            if (modelRoot == null)
            {
                ControllerFound = false;
                Bound = false;
                AvatarIsHuman = false;
                StatusMessage = "مفيش موديل — الحركة إجرائية";
                Debug.LogWarning($"[TLC/Animator] {StatusMessage}");
                return false;
            }

            Animator = modelRoot.GetComponent<Animator>();
            if (Animator == null) Animator = modelRoot.GetComponentInChildren<Animator>();
            if (Animator == null)
            {
                Animator = modelRoot.AddComponent<Animator>();
                Debug.Log("[TLC/Animator] الموديل مكنش فيه Animator — اتعمل واحد.");
            }

            var controllerAsset = Resources.Load<RuntimeAnimatorController>(controllerResourcePath);
            ControllerFound = controllerAsset != null;

            if (ControllerFound) Animator.runtimeAnimatorController = controllerAsset;

            AvatarIsHuman = Animator.avatar != null && Animator.avatar.isHuman;
            Bound = Animator.runtimeAnimatorController != null;

            // مالك واحد للحركة: لو الـ controller اتحمّل، هو اللي بيحرّك،
            // والـ Puppet يبقى مسؤول عن الاستعلام عن العظام بس (نظرة/جلوس).
            if (puppet != null) puppet.DrivesAnimator = !Bound;

            if (controller != null)
            {
                controller.SetAnimator(Bound ? Animator : null);
                controller.SetAnimatorParameters(AnimatorParameterNames);
            }

            StatusMessage = Bound
                ? $"controller: {controllerResourcePath} · Avatar humanoid: {(AvatarIsHuman ? "أيوه" : "لأ")} · مالك: ThirdPersonController"
                : "controller مش موجود — رجوع للحركة الإجرائية";

            if (Bound)
                Debug.Log($"[TLC/Animator] {StatusMessage}");
            else
                Debug.LogWarning($"[TLC/Animator] {StatusMessage}");

            return Bound;
        }

        /// <summary>
        /// أسماء البارامترات اللي AnimatorControllerBuilder بيولّدها.
        /// محدّدة هنا عشان لو اتغيّر اسم في مكان، الاختبار الآلي يمسكه.
        /// </summary>
        public static readonly string[] AnimatorParameterNames = { "Speed", "IsRunning", "Grounded", "Talking" };

        /// <summary>هل الـ Animator فيه البارامتر ده فعلاً؟ (بيتجنّب تحذير كل فريم).</summary>
        public bool HasParameter(Animator animator, string name)
        {
            if (animator == null || string.IsNullOrEmpty(name)) return false;

            string key = animator.GetInstanceID() + ":" + name;
            if (_paramCache.TryGetValue(key, out bool cached)) return cached;

            bool has = false;
            var ps = animator.parameters;
            if (ps != null)
                for (int i = 0; i < ps.Length; i++)
                    if (ps[i].name == name) { has = true; break; }

            _paramCache[key] = has;
            return has;
        }
    }
}
