using System.Collections.Generic;
using UnityEngine;
using TLC.Narrative;

namespace TLC.Audio
{
    /// <summary>
    /// نظام الصوت الكامل: تعليق صوتي + مزيكا طبقات + محيط.
    ///
    /// التصميم: كل ملف صوت بيتجاب من Resources باسم عقدة القصة.
    ///   Resources/Audio/Voice/c1_murad_1  → بيتشغّل مع السطر c1_murad_1
    ///   Resources/Audio/Music/ambient_drone
    ///   Resources/Audio/SFX/phone_ring
    ///
    /// لو الملف مش موجود، مفيش خطأ — بس صمت. عشان اللعبة تشتغل من أول يوم
    /// والصوت يتركّب بعدين من غير ما يتغير أي كود.
    /// </summary>
    public class VoiceOverSystem : MonoBehaviour
    {
        [Header("المصادر")]
        [SerializeField] AudioSource voiceSource;
        [SerializeField] AudioSource musicSourceA;
        [SerializeField] AudioSource musicSourceB;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] AudioSource ambienceSource;

        [Header("الإعدادات")]
        [SerializeField] float musicFadeSeconds = 2.5f;
        [SerializeField] float duckingVolume = 0.25f;

        readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        NarrativeRunner _runner;
        float _musicTargetVolume = 0.4f;
        bool _usingA = true;
        Coroutine _duck;

        const string VoicePath = "Audio/Voice/";
        const string SfxPath = "Audio/SFX/";
        const string MusicPath = "Audio/Music/";

        public void Build(NarrativeRunner runner)
        {
            _runner = runner;
            CreateSources();

            runner.LineShown += OnLine;
            runner.EventRaised += OnEvent;
        }

        void CreateSources()
        {
            var root = new GameObject("AudioSources").transform;
            root.SetParent(transform, false);

            voiceSource    = MakeSource(root, "Voice", false);
            musicSourceA   = MakeSource(root, "MusicA", true);
            musicSourceB   = MakeSource(root, "MusicB", true);
            sfxSource      = MakeSource(root, "SFX", false);
            ambienceSource = MakeSource(root, "Ambience", true);

            musicSourceA.volume = 0f;
            musicSourceB.volume = 0f;
            ambienceSource.volume = 0.18f;
        }

        AudioSource MakeSource(Transform parent, string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.volume = 0.8f;
            return src;
        }

        // ────────────────────────── تعليق صوتي ──────────────────────────

        void OnLine(StoryNode node)
        {
            if (node == null) return;

            var clip = Clip(VoicePath + node.id);
            if (clip == null)
            {
                voiceSource.Stop();
                RestoreMusic();
                return;
            }

            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();

            // نوطّي الموسيقى أثناء الكلام
            DuckMusic(true);
            CancelInvoke(nameof(RestoreMusic));
            Invoke(nameof(RestoreMusic), clip.length + 0.3f);
        }

        void DuckMusic(bool duck)
        {
            float target = duck ? _musicTargetVolume * duckingVolume : _musicTargetVolume;
            if (musicSourceA != null) musicSourceA.volume = target;
            if (musicSourceB != null) musicSourceB.volume = target;
        }

        void RestoreMusic() => DuckMusic(false);

        // ────────────────────────── أحداث ──────────────────────────

        void OnEvent(string action, string param)
        {
            switch (action)
            {
                case NarrativeRunner.ActAudio:
                    PlaySfx(param);
                    break;
            }
        }

        public void PlaySfx(string clipName)
        {
            var clip = Clip(SfxPath + clipName);
            if (clip == null)
            {
                Debug.Log($"[TLC/Audio] مفيش ملف صوت '{clipName}' — صمت.");
                return;
            }
            sfxSource.PlayOneShot(clip);
        }

        // ────────────────────────── مزيكا ──────────────────────────

        public void PlayMusic(string clipName, float volume = 0.4f)
        {
            var clip = Clip(MusicPath + clipName);
            if (clip == null) return;

            _musicTargetVolume = volume;
            var next = _usingA ? musicSourceB : musicSourceA;
            var current = _usingA ? musicSourceA : musicSourceB;

            next.clip = clip;
            next.volume = 0f;
            next.Play();

            if (_duck != null) StopCoroutine(_duck);
            _duck = StartCoroutine(Crossfade(current, next));

            _usingA = !_usingA;
        }

        public void PlayAmbience(string clipName, float volume = 0.18f)
        {
            var clip = Clip(MusicPath + clipName) ?? Clip("Audio/Ambience/" + clipName);
            if (clip == null) return;

            ambienceSource.clip = clip;
            ambienceSource.volume = volume;
            ambienceSource.Play();
        }

        System.Collections.IEnumerator Crossfade(AudioSource from, AudioSource to)
        {
            float t = 0f;
            float fromStart = from.volume;

            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / musicFadeSeconds;
                to.volume = Mathf.Lerp(0f, _musicTargetVolume, t);
                from.volume = Mathf.Lerp(fromStart, 0f, t);
                yield return null;
            }

            from.Stop();
            from.volume = 0f;
            to.volume = _musicTargetVolume;
            _duck = null;
        }

        // ────────────────────────── تحميل ──────────────────────────

        AudioClip Clip(string resourcePath)
        {
            if (_cache.TryGetValue(resourcePath, out var cached)) return cached;

            var clip = Resources.Load<AudioClip>(resourcePath);
            _cache[resourcePath] = clip;   // كاش حتى لو null عشان ما نعملش Load كل مرة
            return clip;
        }

        /// <summary>للتحقق في المحرر: كام ملف صوت موجود من إجمالي سطور الفصل.</summary>
        public int CountAvailableVoiceClips(int storyLines)
        {
            int found = 0;
            foreach (var key in _cache.Keys)
                if (key.StartsWith(VoicePath) && _cache[key] != null) found++;
            return found;
        }
    }
}
