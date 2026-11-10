using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TLC.Player
{
    public enum QteGesture { Tap, SwipeLeft, SwipeRight, SwipeUp, Hold }

    /// <summary>
    /// حدث سريع (QTE) مبني على الإيماءات — مناسب للموبايل أكتر من الأزرار.
    /// بيدّي نتيجة نجاح/فشل، والفرق بينهم في القصة مش "لعبة تخلص".
    /// </summary>
    public class QTEController : MonoBehaviour
    {
        [Header("الواجهة")]
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text promptLabel;
        [SerializeField] Image ringFill;            // حلقة تنقص مع الوقت
        [SerializeField] float duration = 1.6f;

        [Header("حدود الإيماءة")]
        [SerializeField] float swipeThreshold = 120f;

        public event Action<bool> OnResult;

        bool _active;
        float _remaining;
        Vector2 _startPos;
        bool _pointerDown;
        bool _resolved;
        QteGesture _gesture;

        public void Play(string prompt, QteGesture gesture, float time = -1f)
        {
            _gesture = gesture;
            _remaining = time > 0 ? time : duration;
            _resolved = false;
            _active = true;

            if (promptLabel) promptLabel.text = prompt;
            if (group) { group.alpha = 1f; group.blocksRaycasts = true; }
            StartCoroutine(Tick());
        }

        IEnumerator Tick()
        {
            float total = _remaining;

            while (_active && _remaining > 0f)
            {
                _remaining -= Time.deltaTime;
                if (ringFill) ringFill.fillAmount = Mathf.Clamp01(_remaining / total);
                yield return null;
            }

            if (!_resolved) Resolve(false);
        }

        void Update()
        {
            if (!_active) return;

            // إدخال بسيط بالأصابع — بنستخدم Touch مباشرة عشان مفيش اعتماد على Input System
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        _pointerDown = true;
                        _startPos = t.position;
                        if (_gesture == QteGesture.Tap) Resolve(true);
                        break;

                    case TouchPhase.Ended:
                        if (!_pointerDown) break;
                        _pointerDown = false;
                        if (_gesture == QteGesture.Tap) break;
                        EvaluateSwipe(t.position - _startPos, t.deltaTime);
                        break;
                }
                return;
            }

            // محرر / ماوس للتجربة على الكمبيوتر
            if (Input.GetMouseButtonDown(0))
            {
                _pointerDown = true;
                _startPos = Input.mousePosition;
                if (_gesture == QteGesture.Tap) Resolve(true);
            }
            else if (Input.GetMouseButtonUp(0) && _pointerDown)
            {
                _pointerDown = false;
                if (_gesture == QteGesture.Tap) return;
                EvaluateSwipe((Vector2)Input.mousePosition - _startPos, Time.deltaTime);
            }
        }

        void EvaluateSwipe(Vector2 delta, float dt)
        {
            if (delta.magnitude < swipeThreshold) { Resolve(false); return; }

            Vector2 n = delta.normalized;
            bool ok = _gesture switch
            {
                QteGesture.SwipeLeft  => n.x < -0.6f,
                QteGesture.SwipeRight => n.x >  0.6f,
                QteGesture.SwipeUp    => n.y >  0.6f,
                _ => false
            };

            Resolve(ok);
        }

        public void Resolve(bool success)
        {
            if (_resolved) return;
            _resolved = true;
            _active = false;
            if (group) { group.alpha = 0f; group.blocksRaycasts = false; }
            OnResult?.Invoke(success);
        }
    }
}
