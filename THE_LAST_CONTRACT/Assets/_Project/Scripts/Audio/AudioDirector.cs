using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLC.Audio
{
    /// <summary>
    /// مدير الصوت: موسيقى بتتبدّل بتلاشي، أجواء بتلف في حلقة، مؤثرات مرة واحدة.
    /// كل المقاطع مولّدة أصلياً وموجودة في Resources/Audio.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instance { get; private set; }

        const string MusicPath = "Audio/Music/";
        const string AmbiencePath = "Audio/Ambience/";
        const string SfxPath = "Audio/SFX/";

        AudioSource _musicA, _musicB, _ambience, _sfx;
        bool _aIsActive = true;
        Coroutine _fade;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

        public float MusicVolume { get; private set; } = 0.70f;
        public float SfxVolume { get; private set; } = 0.85f;
        public float VoiceVolume { get; private set; } = 0.90f;

        public string CurrentMusic { get; private set; }
        public string CurrentAmbience { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Build()
        {
            if (_musicA != null) return;

            _musicA = MakeSource("MusicA", true);
            _musicB = MakeSource("MusicB", true);
            _ambience = MakeSource("Ambience", true);
            _sfx = MakeSource("Sfx", false);

            _musicA.volume = 0f;
            _musicB.volume = 0f;
            _ambience.volume = 0f;
        }

        AudioSource MakeSource(string n, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.volume = 0f;
            return src;
        }

        AudioClip Load(string path)
        {
            if (_clips.TryGetValue(path, out var c)) return c;
            var clip = Resources.Load<AudioClip>(path);
            _clips[path] = clip;      // بنخزّن حتى الـ null عشان ما نكررش المحاولة
            return clip;
        }

        // ─────────────────────── الموسيقى ───────────────────────

        public void PlayMusic(string name, float fadeSeconds = 2.5f)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (CurrentMusic == name) return;

            var clip = Load(MusicPath + name);
            if (clip == null)
            {
                // مفيش مقطع — بنسكت بأدب بدل ما نرمي استثناء
                CurrentMusic = null;
                return;
            }

            var incoming = _aIsActive ? _musicB : _musicA;
            var outgoing = _aIsActive ? _musicA : _musicB;
            _aIsActive = !_aIsActive;

            incoming.clip = clip;
            incoming.volume = 0f;
            incoming.Play();

            CurrentMusic = name;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(outgoing, incoming, fadeSeconds));
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds)
        {
            float fromStart = from != null ? from.volume : 0f;
            float t = 0f;
            seconds = Mathf.Max(0.05f, seconds);

            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / seconds);
                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, k);
                to.volume = Mathf.Lerp(0f, MusicVolume, k);
                yield return null;
            }

            if (from != null) { from.volume = 0f; from.Stop(); }
            to.volume = MusicVolume;
            _fade = null;
        }

        public void StopMusic(float fadeSeconds = 1.5f)
        {
            CurrentMusic = null;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeOut(_aIsActive ? _musicA : _musicB, fadeSeconds));
        }

        IEnumerator FadeOut(AudioSource src, float seconds)
        {
            if (src == null) yield break;
            float start = src.volume, t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            src.volume = 0f;
            src.Stop();
        }

        /// <summary>يختار مقطع الموسيقى حسب مستوى التوتر في المشهد.</summary>
        public void PlayMusicForTension(float tension01)
        {
            string track = tension01 > 0.62f ? "act_tension"
                         : tension01 > 0.25f ? "title_theme"
                         : "act_resolve";
            PlayMusic(track);
        }

        // ─────────────────────── الأجواء ───────────────────────

        public void PlayAmbience(string name, float fadeSeconds = 2f)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (CurrentAmbience == name && _ambience.clip != null) return;

            var clip = Load(AmbiencePath + name);
            if (clip == null) return;

            _ambience.clip = clip;
            _ambience.volume = 0f;
            _ambience.Play();
            CurrentAmbience = name;
            StartCoroutine(FadeTo(_ambience, MusicVolume * 0.45f, fadeSeconds));
        }

        IEnumerator FadeTo(AudioSource src, float target, float seconds)
        {
            float start = src.volume, t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(start, target, Mathf.Clamp01(t / seconds));
                yield return null;
            }
            src.volume = target;
        }

        // ─────────────────────── المؤثرات ───────────────────────

        public void PlaySfx(string name, float volumeScale = 1f)
        {
            if (string.IsNullOrEmpty(name)) return;
            var clip = Load(SfxPath + name);
            if (clip == null) return;
            _sfx.volume = SfxVolume * Mathf.Clamp01(volumeScale);
            _sfx.PlayOneShot(clip);
        }

        public void PlayClick() => PlaySfx("ui_click", 0.7f);
        public void PlayBack() => PlaySfx("ui_back", 0.7f);
        public void PlayHover() => PlaySfx("ui_hover", 0.5f);

        /// <summary>ستينج انتقال الفصل — بيتشغّل من مجلد الموسيقى.</summary>
        public void PlayChapterSting()
        {
            var clip = Load(MusicPath + "chapter_sting");
            if (clip == null) return;
            _sfx.volume = SfxVolume;
            _sfx.PlayOneShot(clip);
        }

        // ─────────────────────── المستويات ───────────────────────

        public void SetVolumes(float music, float sfx, float voice)
        {
            MusicVolume = Mathf.Clamp01(music);
            SfxVolume = Mathf.Clamp01(sfx);
            VoiceVolume = Mathf.Clamp01(voice);

            if (_musicA != null) _musicA.volume = _aIsActive ? MusicVolume : 0f;
            if (_musicB != null) _musicB.volume = _aIsActive ? 0f : MusicVolume;
            if (_ambience != null) _ambience.volume = MusicVolume * 0.45f;
        }

        public float VoiceLevel => VoiceVolume;
    }
}
