// 4.4 RECOVERY — AudioLevels.cs — **مُعاد حرفيًا**
using UnityEngine;

namespace TLC.Settings
{
    /// <summary>
    /// مستويات الصوت الأربعة كواجهة واحدة للقوائم — بتقرأ وتكتب على
    /// <see cref="TLC.Audio.AudioDirector"/> الحقيقي (واللي بيتحفظ في PlayerPrefs).
    /// مفيش قيمة ديكور هنا: كل تغيير بيمشي على نفس الباص اللي اللعبة بتسمعه.
    /// </summary>
    public static class AudioLevels
    {
        public const float Step = 0.10f;

        public static readonly string[] Keys = { "master", "music", "sfx", "voice" };

        static TLC.Audio.AudioDirector Director => TLC.Audio.AudioDirector.Instance;

        public static float Get(string which)
        {
            var d = Director;
            if (d == null) return 1f;
            switch (which)
            {
                case "master": return d.MasterVolume;
                case "music":  return d.MusicVolume;
                case "sfx":    return d.SfxVolume;
                case "voice":  return d.VoiceVolume;
                default:       return 1f;
            }
        }

        /// <summary>يعدّل مستوى بخطوة ويرجّع القيمة الجديدة.</summary>
        public static float Adjust(string which, float delta)
        {
            var d = Director;
            if (d == null) return 1f;

            float v = Mathf.Clamp01(Get(which) + delta);

            switch (which)
            {
                case "master": d.SetMasterVolume(v); break;
                case "music":  d.SetVolumes(d.MasterVolume, v, d.SfxVolume, d.VoiceVolume); break;
                case "sfx":    d.SetVolumes(d.MasterVolume, d.MusicVolume, v, d.VoiceVolume); break;
                case "voice":  d.SetVolumes(d.MasterVolume, d.MusicVolume, d.SfxVolume, v); break;
            }

            return v;
        }

        public static string Label(string which) => Mathf.RoundToInt(Get(which) * 100f) + "%";

        public static bool AllPersisted() =>
            PlayerPrefs.HasKey(TLC.Audio.AudioDirector.KeyMaster) &&
            PlayerPrefs.HasKey(TLC.Audio.AudioDirector.KeyMusic) &&
            PlayerPrefs.HasKey(TLC.Audio.AudioDirector.KeySfx) &&
            PlayerPrefs.HasKey(TLC.Audio.AudioDirector.KeyVoice);
    }
}
