using UnityEngine;

namespace TLC.Player
{
    /// <summary>
    /// تحريك بدائي للشخصية (ذراعين ورجلين) من غير ملفات أنيميشن.
    /// ده مش موكاب — ده حل مؤقت شغّال بيدي إحساس الحركة لحد ما يتركّب
    /// موديل آدم الحقيقي + Animator Controller.
    /// لو الأنيميتور موجود، الكلاس ده بيسيب التحكم له.
    /// </summary>
    public class ProceduralRig : MonoBehaviour
    {
        [Header("المفاصل (بتتظبط من AdamBuilder)")]
        public Transform hips;
        public Transform spine;
        public Transform head;
        public Transform leftUpperArm, rightUpperArm;
        public Transform leftLowerArm, rightLowerArm;
        public Transform leftThigh, rightThigh;
        public Transform leftShin, rightShin;

        [Header("الإعدادات")]
        [SerializeField] float swingAmplitude = 28f;
        [SerializeField] float idleBreatheAmplitude = 2.2f;
        [SerializeField] float blendSpeed = 6f;

        float _phase;
        float _speed01;
        bool _running;
        float _idleTime;

        public void SetLocomotion(float speed01, bool running)
        {
            _speed01 = Mathf.Lerp(_speed01, speed01, Time.deltaTime * blendSpeed);
            _running = running;
        }

        void Update()
        {
            _idleTime += Time.deltaTime;

            // تردد الخطوة بيتناسب مع السرعة
            _phase += Time.deltaTime * Mathf.Lerp(5.5f, 10f, _speed01) * Mathf.Max(0.15f, _speed01);

            float swing = Mathf.Sin(_phase) * swingAmplitude * _speed01;
            float counter = -swing;

            Apply(leftUpperArm,  swing, 0f, 0f);
            Apply(rightUpperArm, counter, 0f, 0f);
            Apply(leftLowerArm,  Mathf.Abs(swing) * 0.35f, 0f, 0f);
            Apply(rightLowerArm, Mathf.Abs(counter) * 0.35f, 0f, 0f);

            Apply(leftThigh,  counter * 1.1f, 0f, 0f);
            Apply(rightThigh, swing * 1.1f, 0f, 0f);
            Apply(leftShin,  Mathf.Max(0f, -counter) * 0.9f, 0f, 0f);
            Apply(rightShin, Mathf.Max(0f, -swing) * 0.9f, 0f, 0f);

            // تنفّس وقت السكون
            if (spine != null)
            {
                float breathe = Mathf.Sin(_idleTime * 1.4f) * idleBreatheAmplitude * (1f - _speed01);
                Vector3 e = spine.localEulerAngles;
                spine.localEulerAngles = new Vector3(e.x + breathe * 0.25f, e.y, e.z);
            }

            if (head != null && _speed01 < 0.05f)
            {
                float look = Mathf.Sin(_idleTime * 0.31f) * 4f;
                Vector3 e = head.localEulerAngles;
                head.localEulerAngles = new Vector3(e.x, e.y + look * Time.deltaTime * 8f, e.z);
            }
        }

        static void Apply(Transform t, float x, float y, float z)
        {
            if (t == null) return;
            t.localRotation = Quaternion.Euler(x, y, z);
        }
    }
}
