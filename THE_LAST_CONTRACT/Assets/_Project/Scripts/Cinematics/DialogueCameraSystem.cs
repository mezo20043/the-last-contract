using System.Collections.Generic;
using UnityEngine;
using TLC.World;

namespace TLC.Cinematics
{
    /// <summary>
    /// كاميرا الحوارات السينمائية: لقطات كتف فوق الكتف (OTS) وأخرى قريبة،
    /// بتتبني رياضياً من موضع المتكلم والمستمع — مش محتاجة أي Timeline.
    /// </summary>
    public class DialogueCameraSystem : MonoBehaviour
    {
        [Header("الإعدادات")]
        [SerializeField] float overShoulderDistance = 1.9f;
        [SerializeField] float overShoulderHeight = 1.62f;
        [SerializeField] float shoulderSideOffset = 0.42f;
        [SerializeField] float closeUpDistance = 1.15f;
        [SerializeField] float closeUpHeight = 1.66f;

        /// <summary>
        /// نقطة الراس الحقيقية: عظمة Head من الـ Avatar لو الشخصية موديل بشري،
        /// وإلا ارتفاع تقديري. عشان اللقطات القريبة تطلع على الوش مش على الصدر.
        /// </summary>
        public static Vector3 HeadPoint(Transform character)
        {
            if (character == null) return Vector3.zero;

            Transform head = null;

            var puppet = character.GetComponentInParent<HumanoidPuppet>();
            if (puppet == null) puppet = character.GetComponentInChildren<HumanoidPuppet>();
            if (puppet != null) head = puppet.HeadBone;

            if (head == null)
            {
                var animator = character.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    try { head = animator.GetBoneTransform(HumanBodyBones.Head); }
                    catch (System.Exception) { head = null; }
                }
            }

            return head != null ? head.position : character.position + Vector3.up * 1.62f;
        }

        /// <summary>لقطة على المتكلم من فوق كتف المستمع.</summary>
        public Shot OverTheShoulder(Transform speaker, Transform listener)
        {
            Vector3 speakerHead = HeadPoint(speaker);
            Vector3 listenerHead = HeadPoint(listener);

            Vector3 axis = (speakerHead - listenerHead).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, axis).normalized;

            Vector3 pos = listenerHead
                          + right * shoulderSideOffset
                          - axis * overShoulderDistance
                          + Vector3.up * (overShoulderHeight - 1.62f);

            return new Shot
            {
                name = "OTS_" + speaker.name,
                position = pos,
                lookAt = speakerHead,
                fieldOfView = 34f,
                duration = 2.5f
            };
        }

        /// <summary>لقطة قريبة على الوش — للحظات القرار.</summary>
        public Shot CloseUp(Transform subject)
        {
            Vector3 head = HeadPoint(subject);           // عظمة الراس الحقيقية
            Vector3 forward = subject.forward;

            // كسر الزاوية شوية عشان الوش ما يبقاش مسطّح
            Vector3 offset = Quaternion.Euler(0f, 22f, 0f) * forward;
            Vector3 pos = head - offset * closeUpDistance + Vector3.up * 0.04f;

            return new Shot
            {
                name = "CU_" + subject.name,
                position = pos,
                lookAt = head,
                fieldOfView = 30f,
                duration = 2.2f
            };
        }

        /// <summary>لقطة واسعة للبيئة — للتنفّس بين الحوارات.</summary>
        public Shot Wide(Vector3 focusPoint, float distance = 4.5f, float height = 2.3f)
        {
            Vector3 pos = focusPoint + new Vector3(-distance * 0.7f, height, -distance * 0.7f);

            return new Shot
            {
                name = "Wide",
                position = pos,
                lookAt = focusPoint + Vector3.up * 1.2f,
                fieldOfView = 42f,
                duration = 3f
            };
        }

        /// <summary>تسلسل حوار كامل جاهز للتمرير للمخرج.</summary>
        public List<Shot> ConversationSequence(Transform speaker, Transform listener, bool includeWide)
        {
            var shots = new List<Shot>();

            if (includeWide)
                shots.Add(Wide((speaker.position + listener.position) * 0.5f));

            shots.Add(OverTheShoulder(speaker, listener));
            shots.Add(OverTheShoulder(listener, speaker));
            shots.Add(CloseUp(speaker));

            return shots;
        }
    }
}
