using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TLC.CameraRig;
using TLC.Settings;

namespace TLC.Cinematics
{
    /// <summary>
    /// مخرج لقطات الحوار (Shot / Reverse-Shot).
    ///
    /// بيقطع بالكاميرا بين لقطات مبنية على **وش المتكلم الفعلي** (عظمة الراس من الـAvatar):
    ///   • OTS  — فوق كتف المستمع على وش المتكلم
    ///   • CU   — قريبة على الوش (للحظات القرار)
    ///   • TWO  — لقطة مزدوجة (الاتنين في الكادر)
    /// وكل لقطة بتعدّي على فحص اصطدام: لو مكان الكاميرا جوه حيطة، بتتقرّب من الوش
    /// لحد ما تبقى في مساحة حرة — فمفيش لقطة من جوه جدار.
    ///
    /// الانتقال بين اللقطات بـLerp ناعم (0.45 ث) + ضبابية الخلفية بتزيد أثناء الحوار،
    /// وبترجع كاميرا اللعب زي ما كانت لما الحوار يخلص.
    /// </summary>
    public class DialogueShotDirector : MonoBehaviour
    {
        [Header("التوقيت")]
        [SerializeField] float secondsPerShot = 3.4f;
        [SerializeField] float transitionSeconds = 0.45f;
        [Tooltip("أقل مسافة من الوش عشان الوش ما يبقاش مشوّه")]
        [SerializeField] float minFaceDistance = 0.65f;

        [Header("الكادر")]
        [Tooltip("مسافة الراس من أعلى الكادر (Headroom) كنسبة من طول الكادر")]
        [Range(0.02f, 0.25f)][SerializeField] float headroom = 0.09f;

        Camera _cam;
        ThirdPersonCameraRig _rig;
        CinematicLook _look;
        Coroutine _playing;
        bool _wasRigEnabled;

        public bool IsPlaying => _playing != null;

        public void Setup(Camera cam, ThirdPersonCameraRig rig, CinematicLook look)
        {
            _cam = cam;
            _rig = rig;
            _look = look;
        }

        // ────────────────────────── الواجهة ──────────────────────────

        /// <summary>يشغّل حوار سينمائي بين شخصيتين ويقطع بين اللقطات.</summary>
        public void PlayConversation(Transform a, Transform b, int shotCount = 3)
        {
            if (_cam == null || a == null || b == null) return;
            Stop();
            _playing = StartCoroutine(ConversationRoutine(a, b, Mathf.Max(2, shotCount)));
        }

        /// <summary>
        /// قطع (أو انتقال سريع) على لقطة واحدة محددة، وبيفضل ثابت عليها.
        /// ده اللي بيستخدمه الفصل الأول: كل عقدة في السيناريو بتحمل اسم لقطة،
        /// فالكاميرا بتتغيّر مع السرد بدل ما تفضل لقطة واحدة.
        /// بيوقف كاميرا اللعب (ThirdPersonCameraRig) طول ما اللقطة شغالة،
        /// وإلا هي اللي كانت بترسم في الآخر وتبوّظ الكادر.
        /// </summary>
        public void CutTo(Shot shot, float transition = 0.32f)
        {
            if (_cam == null) return;
            if (_playing != null) { StopCoroutine(_playing); _playing = null; }
            _playing = StartCoroutine(CutRoutine(shot, Mathf.Max(0.01f, transition)));
        }

        IEnumerator CutRoutine(Shot shot, float transition)
        {
            if (_rig != null) { _wasRigEnabled = _rig.enabled; _rig.enabled = false; }
            if (_look != null) _look.SetDialogueFocus(true);

            Vector3 fromPos = _cam.transform.position;
            Quaternion fromRot = _cam.transform.rotation;
            float fromFov = _cam.fieldOfView;

            Vector3 toPos = SafePosition(shot.position, shot.lookAt);
            Vector3 dir = shot.lookAt - toPos;
            if (dir.sqrMagnitude < 0.0004f) dir = _cam.transform.forward;
            Quaternion toRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

            float t = 0f;
            while (t < transition)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / transition);
                _cam.transform.position = Vector3.Lerp(fromPos, toPos, k);
                _cam.transform.rotation = Quaternion.Slerp(fromRot, toRot, k);
                _cam.fieldOfView = Mathf.Lerp(fromFov, shot.fieldOfView, k);
                yield return null;
            }

            _cam.transform.position = toPos;
            _cam.transform.rotation = toRot;
            _cam.fieldOfView = shot.fieldOfView;

            _playing = null;
        }

        public void Stop()
        {
            if (_playing != null) { StopCoroutine(_playing); _playing = null; }
            if (_look != null) _look.SetDialogueFocus(false);
            if (_rig != null) _rig.enabled = _wasRigEnabled;
        }

        // ────────────────────────── التنفيذ ──────────────────────────

        IEnumerator ConversationRoutine(Transform a, Transform b, int shotCount)
        {
            if (_rig != null) { _wasRigEnabled = _rig.enabled; _rig.enabled = false; }
            if (_look != null) _look.SetDialogueFocus(true);

            var shots = new List<Shot>();
            for (int i = 0; i < shotCount; i++)
            {
                switch (i % 3)
                {
                    case 0: shots.Add(OverTheShoulder(a, b)); break;
                    case 1: shots.Add(OverTheShoulder(b, a)); break;
                    default: shots.Add(i % 2 == 0 ? CloseUp(b) : TwoShot(a, b)); break;
                }
            }

            // نبدأ من مكان الكاميرا الحالي عشان أول انتقال يبقى ناعم
            Vector3 fromPos = _cam.transform.position;
            Quaternion fromRot = _cam.transform.rotation;
            float fromFov = _cam.fieldOfView;

            for (int i = 0; i < shots.Count; i++)
            {
                var s = shots[i];
                Vector3 toPos = SafePosition(s.position, s.lookAt);
                Quaternion toRot = Quaternion.LookRotation((s.lookAt - toPos).normalized, Vector3.up);

                float t = 0f;
                while (t < transitionSeconds)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / transitionSeconds);
                    _cam.transform.position = Vector3.Lerp(fromPos, toPos, k);
                    _cam.transform.rotation = Quaternion.Slerp(fromRot, toRot, k);
                    _cam.fieldOfView = Mathf.Lerp(fromFov, s.fieldOfView, k);
                    yield return null;
                }

                _cam.transform.position = toPos;
                _cam.transform.rotation = toRot;
                _cam.fieldOfView = s.fieldOfView;

                // ثبات اللقطة (مع نفس خفيف جداً — إحساس كاميرا محمولة)
                float hold = 0f;
                while (hold < secondsPerShot)
                {
                    hold += Time.deltaTime;
                    float breath = Mathf.Sin(hold * 0.9f) * 0.006f;
                    _cam.transform.position = toPos + Vector3.up * breath;
                    yield return null;
                }

                fromPos = _cam.transform.position;
                fromRot = _cam.transform.rotation;
                fromFov = _cam.fieldOfView;
            }

            // رجوع ناعم لكاميرا اللعب
            float back = 0f;
            Vector3 startPos = _cam.transform.position;
            Quaternion startRot = _cam.transform.rotation;
            float startFov = _cam.fieldOfView;

            if (_rig != null)
            {
                _rig.enabled = true;
                _rig.SnapBehind();
                Vector3 targetPos = _cam.transform.position;
                Quaternion targetRot = _cam.transform.rotation;
                float targetFov = _cam.fieldOfView;

                while (back < 0.5f)
                {
                    back += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, back / 0.5f);
                    _cam.transform.position = Vector3.Lerp(startPos, targetPos, k);
                    _cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, k);
                    _cam.fieldOfView = Mathf.Lerp(startFov, targetFov, k);
                    yield return null;
                }
            }

            if (_look != null) _look.SetDialogueFocus(false);
            _playing = null;
        }

        // ────────────────────────── بناء اللقطات ──────────────────────────

        Shot OverTheShoulder(Transform speaker, Transform listener)
        {
            Vector3 speakerHead = Head(speaker);
            Vector3 listenerHead = Head(listener);
            Vector3 axis = (speakerHead - listenerHead);
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.001f) axis = speaker.forward;
            axis.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, axis).normalized;

            float d = 1.85f;
            Vector3 pos = listenerHead - axis * d + right * 0.46f + Vector3.up * (height(speaker) - 1.62f) * 0.5f;

            return new Shot
            {
                name = "OTS_" + speaker.name,
                position = pos,
                lookAt = speakerHead + Vector3.up * 0.02f,
                fieldOfView = 33f,
                duration = secondsPerShot
            };
        }

        Shot CloseUp(Transform subject)
        {
            Vector3 head = Head(subject);
            Vector3 fwd = subject.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
            fwd.Normalize();

            Vector3 angled = Quaternion.Euler(0f, 26f, 0f) * fwd;
            Vector3 pos = head - angled * 1.05f + Vector3.up * 0.05f;

            return new Shot
            {
                name = "CU_" + subject.name,
                position = pos,
                lookAt = head + Vector3.up * 0.015f,
                fieldOfView = 29f,
                duration = secondsPerShot
            };
        }

        Shot TwoShot(Transform a, Transform b)
        {
            Vector3 mid = (Head(a) + Head(b)) * 0.5f;
            Vector3 axis = (Head(b) - Head(a)); axis.y = 0f;
            if (axis.sqrMagnitude < 0.001f) axis = a.forward;
            axis.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, axis).normalized;

            Vector3 pos = mid - right * 2.7f + Vector3.up * 0.22f;

            return new Shot
            {
                name = "TWO_" + a.name + "_" + b.name,
                position = pos,
                lookAt = mid + Vector3.up * 0.06f,
                fieldOfView = 40f,
                duration = secondsPerShot
            };
        }

        /// <summary>لو المكان محجوز بحيطة، نقرّب الكاميرا من الوش بدل ما نطلّع لقطة جوّه جدار.</summary>
        Vector3 SafePosition(Vector3 wantPos, Vector3 lookAt)
        {
            var mask = ~0;
            Vector3 dir = (wantPos - lookAt);
            float dist = dir.magnitude;
            if (dist < 0.01f) return wantPos;
            dir /= dist;

            if (Physics.SphereCast(lookAt, 0.16f, dir, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
            {
                float safe = Mathf.Max(minFaceDistance, hit.distance - 0.12f);
                return lookAt + dir * safe;
            }
            return wantPos;
        }

        Vector3 Head(Transform t)
        {
            return DialogueCameraSystem.HeadPoint(t);
        }

        float height(Transform t)
        {
            var rends = t.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return 1.62f;
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return Mathf.Clamp(b.max.y - t.position.y, 1.2f, 2.2f);
        }
    }
}
