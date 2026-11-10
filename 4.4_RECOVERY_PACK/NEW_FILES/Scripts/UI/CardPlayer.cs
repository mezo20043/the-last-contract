// 4.4 RECOVERY — CardPlayer.cs — **مُعاد حرفيًا**
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TLC.Cinematics;

namespace TLC.UI
{
    /// <summary>
    /// مشغّل الكروت السينمائية (ما بعد النهاية / التيزر). شاشة سودة كاملة مع عنوان ونص،
    /// بيتنقّل لوحده أو بالضغط، وبيبلّغ لما يخلص. مبيستخدمش Time.time عشان الإيقاف ما يعلّقوش.
    /// </summary>
    public class CardPlayer : MonoBehaviour
    {
        Canvas _canvas;
        CanvasGroup _group;
        TextMeshProUGUI _kicker, _title, _body, _hint, _progress;
        Button _skip;
        RectTransform _root;

        CardSequence _seq;
        int _index;
        float _t;
        bool _playing;

        public bool IsPlaying => _playing;
        public int CurrentIndex => _index;
        public string CurrentSequenceId => _seq != null ? _seq.id : null;
        public string CurrentBody => _seq != null && _index < _seq.cards.Count ? _seq.cards[_index].body : null;

        /// <summary>بيتنادى مرة واحدة لما السلسلة تخلص (أو تتخطى).</summary>
        public event Action<string> Finished;

        public void Build()
        {
            if (_canvas != null) return;

            UIFactory.EnsureEventSystem();
            _canvas = UIFactory.CreateCanvas("CardCanvas", 600);
            _canvas.transform.SetParent(transform, false);

            _group = _canvas.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _root = UIFactory.CreateSafeArea(_canvas);

            var black = UIFactory.CreatePanel(_root, "Black", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.012f, 0.014f, 0.019f, 1f));
            black.SetAsFirstSibling();

            _kicker = UIFactory.CreateText(_root, "Kicker", "", 26f, TextAlignmentOptions.Center, UIFactory.Gold);
            Place(_kicker.rectTransform, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.80f));

            _title = UIFactory.CreateText(_root, "Title", "", 62f, TextAlignmentOptions.Center, UIFactory.Paper);
            Place(_title.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.72f));

            _body = UIFactory.CreateText(_root, "Body", "", 40f, TextAlignmentOptions.Center, UIFactory.Paper);
            Place(_body.rectTransform, new Vector2(0.12f, 0.26f), new Vector2(0.88f, 0.54f));

            _progress = UIFactory.CreateText(_root, "Progress", "", 22f, TextAlignmentOptions.Center, UIFactory.Muted);
            Place(_progress.rectTransform, new Vector2(0.2f, 0.10f), new Vector2(0.8f, 0.15f));

            _hint = UIFactory.CreateText(_root, "Hint", "", 22f, TextAlignmentOptions.Center, UIFactory.Muted);
            Place(_hint.rectTransform, new Vector2(0.2f, 0.06f), new Vector2(0.8f, 0.11f));

            _skip = UIFactory.CreateButton(_root, "Skip", "تخطّي", new Vector2(190f, 66f), 26f);
            var srt = _skip.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(1f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(1f, 1f);
            srt.anchoredPosition = new Vector2(-30f, -30f);
            _skip.onClick.AddListener(Skip);

            _canvas.gameObject.SetActive(false);
        }

        static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void Play(CardSequence seq, Action<string> onFinished = null)
        {
            if (seq == null || seq.cards.Count == 0)
            {
                onFinished?.Invoke(seq != null ? seq.id : null);
                return;
            }

            Build();
            _seq = seq;
            _index = 0;
            _t = 0f;
            _playing = true;
            Finished = onFinished;

            _canvas.gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            Apply(_index);

            Debug.Log($"[TLC/Cards] ▶ '{seq.id}' — {seq.cards.Count} كارت · {seq.TotalSeconds:0.0}ث");
        }

        void Apply(int i)
        {
            if (_seq == null || i < 0 || i >= _seq.cards.Count) return;
            var c = _seq.cards[i];

            _kicker.text = string.IsNullOrEmpty(c.kicker) ? "" : c.kicker;
            _title.text = string.IsNullOrEmpty(c.title) ? "" : c.title;
            _body.text = string.IsNullOrEmpty(c.body) ? "" : c.body;
            _progress.text = $"{i + 1} / {_seq.cards.Count}";
            _hint.text = "اضغط للمتابعة";
            _t = 0f;
        }

        void Update()
        {
            if (!_playing || _seq == null) return;

            _t += Time.unscaledDeltaTime;

            float fadeIn = Mathf.Clamp01(_t / 0.35f);
            float remain = _seq.cards[_index].seconds - _t;
            float fadeOut = Mathf.Clamp01(remain / 0.35f);
            _group.alpha = Mathf.Min(fadeIn, fadeOut);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
            {
                Next();
                return;
            }

            if (_t >= _seq.cards[_index].seconds) Next();
        }

        public void Next()
        {
            if (!_playing) return;
            if (_index + 1 >= _seq.cards.Count) { Complete(); return; }
            _index++;
            Apply(_index);
        }

        public void Skip()
        {
            if (!_playing) return;
            Debug.Log($"[TLC/Cards] ⏭ تخطّي '{_seq.id}' عند الكارت {_index + 1}");
            Complete();
        }

        void Complete()
        {
            _playing = false;
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _canvas.gameObject.SetActive(false);

            string id = _seq != null ? _seq.id : null;
            Debug.Log($"[TLC/Cards] ✔ خلصت '{id}'");
            var cb = Finished;
            Finished = null;
            cb?.Invoke(id);
        }

        /// <summary>تشخيص للاختبارات: بنية الكانفس وحجم الخلفية السودة.</summary>
        public void DebugDump()
        {
            if (_canvas == null) return;
            var black = _root != null ? _root.Find("Black") : null;
            var img = black != null ? black.GetComponent<Image>() : null;
            Debug.Log($"[TLC/Cards-Dump] {_canvas.renderMode}/sort={_canvas.sortingOrder}/plane={_canvas.planeDistance}" +
                      $" · alpha={_group.alpha:0.00} · black={(black != null)}" +
                      $" · color={(img != null ? img.color.ToString() : "—")}");
        }

        /// <summary>إخفاء فوري بدون نداءات (تنظيف الاختبارات).</summary>
        public void HideImmediate()
        {
            _playing = false;
            Finished = null;
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }
    }
}
