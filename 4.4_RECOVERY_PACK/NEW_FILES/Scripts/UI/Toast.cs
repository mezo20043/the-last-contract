// 4.4 RECOVERY — Toast.cs — **مُعاد حرفيًا**
using System.Collections;
using TMPro;
using UnityEngine;

namespace TLC.UI
{
    /// <summary>رسالة قصيرة على الشاشة (اتحفظ / فشل / ممنوع) — تغذية راجعة حقيقية.</summary>
    public class Toast : MonoBehaviour
    {
        Canvas _canvas;
        CanvasGroup _group;
        TextMeshProUGUI _text;
        Coroutine _hide;

        public string LastMessage { get; private set; }
        public int ShownCount { get; private set; }

        public void Build()
        {
            if (_canvas != null) return;
            UIFactory.EnsureEventSystem();
            _canvas = UIFactory.CreateCanvas("ToastCanvas", 400);
            _canvas.transform.SetParent(transform, false);

            _group = _canvas.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            var safe = UIFactory.CreateSafeArea(_canvas);
            var bg = UIFactory.CreatePanel(safe, "Bg",
                new Vector2(0.18f, 0.78f), new Vector2(0.82f, 0.88f),
                Vector2.zero, Vector2.zero, new Color(0.04f, 0.045f, 0.06f, 0.92f));

            _text = UIFactory.CreateText(bg.transform, "Text", "", 30f,
                TextAlignmentOptions.Center, UIFactory.Paper);
        }

        public void Show(string message, float seconds = 2.6f, bool error = false)
        {
            Build();
            if (_canvas == null) return;

            LastMessage = message;
            ShownCount++;
            _text.text = message;
            _text.color = error ? new Color(0.95f, 0.55f, 0.45f) : UIFactory.Gold;

            _canvas.gameObject.SetActive(true);
            _group.alpha = 1f;

            if (_hide != null) StopCoroutine(_hide);
            _hide = StartCoroutine(HideAfter(seconds));
            Debug.Log($"[TLC/Toast] {message}");
        }

        IEnumerator HideAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = 1f - Mathf.Clamp01(t / 0.35f);
                yield return null;
            }
            _group.alpha = 0f;
            _canvas.gameObject.SetActive(false);
        }

        public void HideImmediate()
        {
            if (_hide != null) StopCoroutine(_hide);
            _hide = null;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }
    }
}
