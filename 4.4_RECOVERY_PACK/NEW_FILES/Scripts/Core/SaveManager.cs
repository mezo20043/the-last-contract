// ═══════════════════════════════════════════════════════════════════════════════
// 4.4 RECOVERY — SaveManager.cs
// الحالة: **مُعاد من العقد البرمجي** (النص الأصلي مش كامل في المحفوظات).
//   • الـAPI والسلوك والثوابت مؤكدة (نقاط الاستدعاء اللي نجحت في جولة 4.4).
//   • السطور الداخلية محتاجة مراجعة diff على نسخة 0a7490f قبل الاعتماد النهائي.
//   • الأصل: ٥٢٢ سطر · CurrentVersion = 2 · ٤ خانات.
// ═══════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using TLC.Settings;

namespace TLC.Core
{
    /// <summary>ملخّص الحفظ اللي بتقراه الواجهة بدون تحميل اللعبة.</summary>
    [Serializable]
    public class SaveHeader
    {
        public int version = SaveManager.CurrentVersion;
        public string slotId;           // auto · a · b · c
        public string chapterId;        // chapter01
        public string chapterTitle;     // الفصل الأول — الظرف
        public string locationName;     // شقة آدم
        public string nodeId;
        public string nodeLabel;        // آخر سطر (مختصر)
        public string endingId;
        public string checkpoint;       // manual · decision · line · chapter · ending
        public float playtimeSeconds;
        public string savedAtUtc;       // ISO-8601
        public bool isAuto;
        public int saveCount;
        public string gameVersion;
        public string thumbFile;        // slot_N.png
    }

    /// <summary>تقدّم اللاعب وقت الحفظ.</summary>
    [Serializable]
    public class ProgressSnapshot
    {
        public int unlockedChapters = 1;
        public List<string> seenEndings = new List<string>();
        public List<string> seenPostCredits = new List<string>();
        public List<string> viewedCinematics = new List<string>();
        public List<string> visitedScenes = new List<string>();
        public bool teaserSeen;
    }

    /// <summary>إعدادات اللاعب وقت الحفظ.</summary>
    [Serializable]
    public class SettingsSnapshot
    {
        public float master = 1f, music = 0.7f, sfx = 0.85f, voice = 0.9f;
        public float lookSensitivity = 1f, stickScale = 1f;
        public bool invertY, autoRun;
        public int qualityTier = 1;
        public string language = "ar";
    }

    /// <summary>الملف الكامل اللي بيتكتب على القرص.</summary>
    [Serializable]
    public class SaveFile
    {
        public SaveHeader header;
        public GameStateDto state;
        public ProgressSnapshot progress;
        public SettingsSnapshot settings;
    }

    /// <summary>بيانات إضافية بتيجي من اللعبة وقت الحفظ.</summary>
    public struct SaveMeta
    {
        public string chapterId;
        public string chapterTitle;
        public string locationName;
        public string nodeLabel;
        public string endingId;
        public string checkpoint;
        public bool isAuto;
        public byte[] thumbnailPng;
    }

    /// <summary>
    /// إدارة الحفظ الحقيقية: ٤ خانات (تلقائي + ٣ يدوي)، كتابة ذرّية، نسخة احتياطية،
    /// رصد التلف، وترقية الإصدارات القديمة بدل ما نضيّع حفظ اللاعب.
    /// </summary>
    public static class SaveManager
    {
        public const int CurrentVersion = 2;
        public const int AutoSlot = 0;
        public const int ManualFirst = 1;
        public const int ManualCount = 3;
        public const int SlotCount = 1 + ManualCount;

        public static readonly string[] SlotIds = { "auto", "a", "b", "c" };

        public static string DirPath => Path.Combine(Application.persistentDataPath, "saves");

        public static string PathFor(int slot) => Path.Combine(DirPath, $"slot_{slot}.json");
        public static string BackupPathFor(int slot) => Path.Combine(DirPath, $"slot_{slot}.json.bak");
        public static string ThumbPathFor(int slot) => Path.Combine(DirPath, $"slot_{slot}.png");
        public static string CorruptPathFor(int slot) =>
            Path.Combine(DirPath, $"slot_{slot}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}.json");

        public static bool IsManual(int slot) => slot >= ManualFirst && slot < SlotCount;

        public static bool Exists(int slot) => File.Exists(PathFor(slot)) || File.Exists(BackupPathFor(slot));

        public static string SlotId(int slot) => slot >= 0 && slot < SlotIds.Length ? SlotIds[slot] : "?";

        // ────────────────────────────── الكتابة ──────────────────────────────

        /// <summary>حفظ كامل. بيرجّع false لو فشل (والتفاصيل في اللوج).</summary>
        public static bool Save(int slot, GameState state, SaveMeta meta)
        {
            if (state == null) return false;

            try
            {
                Directory.CreateDirectory(DirPath);

                int prevCount = 0;
                var prev = ReadFile(slot, quarantine: false);
                if (prev?.header != null) prevCount = prev.header.saveCount;

                var file = new SaveFile
                {
                    header = new SaveHeader
                    {
                        version = CurrentVersion,
                        slotId = SlotId(slot),
                        chapterId = !string.IsNullOrEmpty(meta.chapterId) ? meta.chapterId : state.ChapterId,
                        chapterTitle = meta.chapterTitle,
                        locationName = meta.locationName,
                        nodeId = state.CurrentNodeId,
                        nodeLabel = meta.nodeLabel,
                        endingId = !string.IsNullOrEmpty(meta.endingId) ? meta.endingId : state.EndingId,
                        checkpoint = meta.checkpoint,
                        playtimeSeconds = state.PlaytimeSeconds,
                        savedAtUtc = DateTime.UtcNow.ToString("o"),
                        isAuto = slot == AutoSlot || meta.isAuto,
                        saveCount = prevCount + 1,
                        gameVersion = Application.version
                    },
                    state = state.ToDto(),
                    progress = SnapshotProgress(),
                    settings = SnapshotSettings()
                };

                string json = JsonUtility.ToJson(file, true);
                WriteAtomic(PathFor(slot), json);

                if (meta.thumbnailPng != null && meta.thumbnailPng.Length > 0)
                {
                    WriteAtomicBytes(ThumbPathFor(slot), meta.thumbnailPng);
                    file.header.thumbFile = Path.GetFileName(ThumbPathFor(slot));
                    WriteAtomic(PathFor(slot), JsonUtility.ToJson(file, true));
                }
                else if (!File.Exists(ThumbPathFor(slot)))
                {
                    file.header.thumbFile = null;
                }

                Debug.Log($"[TLC/Save] ✔ اتحفظ slot={slot} ({SlotId(slot)}) · {file.header.chapterId}/{state.CurrentNodeId}" +
                          $" · أعلام={state.Flags.Count} · قرارات={state.Decisions.Count} · {json.Length} بايت");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC/Save] ✘ فشل الحفظ slot={slot}: {e.GetType().Name}: {e.Message}");
                return false;
            }
        }

        /// <summary>كتابة ذرّية: tmp → نسخة احتياطية → استبدال. مفيش حالة نصف مكتوبة.</summary>
        static void WriteAtomic(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp";
            string bak = path + ".bak";

            var bytes = new UTF8Encoding(false).GetBytes(content);
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);                 // fsync — البيانات تشيّك على القرص قبل الاستبدال
            }

            if (File.Exists(path))
            {
                try
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(path, bak);
                }
                catch (Exception e) { Debug.LogWarning("[TLC/Save] النسخة الاحتياطية فشلت: " + e.Message); }
            }

            File.Move(tmp, path);
        }

        static void WriteAtomicBytes(string path, byte[] bytes)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp";
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush(true);
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        // ────────────────────────────── القراءة ──────────────────────────────

        static SaveFile ReadFile(int slot, bool quarantine)
        {
            string path = PathFor(slot);
            string bak = BackupPathFor(slot);

            var f = TryRead(path, out string err);
            if (f != null) return f;

            // الأساسي مش مقروء → بنجرّب الاحتياطي قبل أي حكم بالتلف
            if (File.Exists(bak))
            {
                var b = TryRead(bak, out string err2);
                if (b != null)
                {
                    Debug.LogWarning($"[TLC/Save] slot={slot}: الأساسي تالف ({err}) — اتحمّل من الاحتياطي.");
                    return b;
                }
                err = err + " | الاحتياطي: " + err2;
            }

            if (File.Exists(path) && quarantine)
            {
                try
                {
                    string q = CorruptPathFor(slot);
                    File.Move(path, q);
                    Debug.LogError($"[TLC/Save] slot={slot} تالف — اتعزل في {Path.GetFileName(q)} · السبب: {err}");
                }
                catch (Exception e) { Debug.LogError("[TLC/Save] العزل فشل: " + e.Message); }
            }
            return null;
        }

        static SaveFile TryRead(string path, out string error)
        {
            error = null;
            if (!File.Exists(path)) { error = "مفيش ملف"; return null; }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(json)) { error = "ملف فاضي"; return null; }
                var f = Migrate(json);
                if (f == null) { error = "صيغة مش مفهومة/إصدار أحدث"; return null; }
                if (f.header == null || f.state == null) { error = "محتوى ناقص"; return null; }
                return f;
            }
            catch (Exception e) { error = e.GetType().Name + ": " + e.Message; return null; }
        }

        /// <summary>يقرأ الهيدر بس (للواجهة). بيرجّع null لو الخانة فاضية أو تالفة.</summary>
        public static SaveHeader Peek(int slot)
        {
            var f = ReadFile(slot, quarantine: false);
            return f != null ? f.header : null;
        }

        /// <summary>يقرأ الهيدر + بيانات الخانة (للعرض في القوائم).</summary>
        public static bool Peek(int slot, out SaveHeader header, out SaveMeta meta)
        {
            header = null;
            meta = default;

            var f = ReadFile(slot, quarantine: false);
            if (f == null || f.header == null) return false;

            header = f.header;
            meta = new SaveMeta
            {
                chapterId = f.header.chapterId,
                chapterTitle = f.header.chapterTitle,
                locationName = f.header.locationName,
                nodeLabel = f.header.nodeLabel,
                endingId = f.header.endingId,
                checkpoint = f.header.checkpoint,
                isAuto = f.header.isAuto
            };
            return true;
        }

        /// <summary>فيه ملف (حتى لو تالف) في الخانة؟</summary>
        public static bool FileExistsFor(int slot) => Exists(slot);

        /// <summary>الملف موجود بس مش مقروء؟</summary>
        public static bool IsCorrupt(int slot) => Exists(slot) && Peek(slot) == null;

        public static bool Load(int slot, GameState state) => Load(slot, state, null, null, true);

        public static bool Load(int slot, GameState state, out SaveHeader header) =>
            Load(slot, state, out header, true);

        public static bool Load(int slot, GameState state, out SaveHeader header, bool applySideEffects)
        {
            bool ok = Load(slot, state, null, null, applySideEffects);
            header = ok ? Peek(slot) : null;
            return ok;
        }

        public static bool Load(int slot, GameState state, ProgressSnapshot restoreProgress,
                                SettingsSnapshot restoreSettings, bool applySideEffects)
        {
            if (state == null) return false;

            var file = ReadFile(slot, quarantine: true);
            if (file == null || file.state == null)
            {
                Debug.LogWarning($"[TLC/Save] مفيش حفظ صالح في الخانة {slot}");
                return false;
            }

            state.FromDto(file.state);
            state.PlaytimeSeconds = file.header != null ? file.header.playtimeSeconds : 0f;
            state.EndingId = file.header != null ? file.header.endingId : null;
            state.ChapterId = file.header != null && !string.IsNullOrEmpty(file.header.chapterId)
                ? file.header.chapterId : state.ChapterId;

            if (applySideEffects)
            {
                if (file.progress != null) RestoreProgress(file.progress);
                if (file.settings != null) ApplySettings(file.settings);
            }

            Debug.Log($"[TLC/Save] ✔ اتحمّل slot={slot} · {state.ChapterId}/{state.CurrentNodeId}" +
                      $" · أعلام={state.Flags.Count} · قرارات={state.Decisions.Count}");
            return true;
        }

        /// <summary>أحدث حفظ صالح (تلقائي أو يدوي). بيرجّع -1 لو مفيش.</summary>
        public static int LatestSlot()
        {
            int best = -1;
            DateTime bestTime = DateTime.MinValue;

            for (int slot = 0; slot < SlotCount; slot++)
            {
                var h = Peek(slot);
                if (h == null) continue;
                if (!DateTime.TryParse(h.savedAtUtc, out DateTime t)) t = DateTime.MinValue;
                if (t > bestTime || (t == bestTime && slot == AutoSlot))
                {
                    bestTime = t;
                    best = slot;
                }
            }
            return best;
        }

        public static bool Delete(int slot)
        {
            try
            {
                if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot));
                if (File.Exists(BackupPathFor(slot))) File.Delete(BackupPathFor(slot));
                if (File.Exists(ThumbPathFor(slot))) File.Delete(ThumbPathFor(slot));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC/Save] فشل الحذف slot={slot}: {e.Message}");
                return false;
            }
        }

        public static List<int> ListSlots()
        {
            var found = new List<int>();
            for (int i = 0; i < SlotCount; i++) if (Peek(i) != null) found.Add(i);
            return found;
        }

        /// <summary>وصف مختصر للواجهة القديمة (توافق للخلف).</summary>
        public static string Describe(int slot)
        {
            var h = Peek(slot);
            if (h == null) return Exists(slot) ? "— تالف —" : "— فاضي —";
            return $"{PrettyDate(h.savedAtUtc)} · {h.chapterTitle ?? h.chapterId} · {PrettyPlaytime(h.playtimeSeconds)}";
        }

        // ────────────────────────────── الترقية ──────────────────────────────

        /// <summary>
        /// يقرأ أي إصدار قديم ويحوّله لنسخة الحالية. بيرجّع null لو الإصدار أحدث مننا
        /// (مش بنلمس حفظ جاي من نسخة تطبيق أحدث عشان ما نفسدوش).
        /// </summary>
        public static SaveFile Migrate(string json)
        {
            int version = PeekVersion(json);

            if (version > CurrentVersion)
            {
                Debug.LogError($"[TLC/Save] الحفظ إصداره {version} أحدث من التطبيق ({CurrentVersion}) — مش هنلمسه.");
                return null;
            }

            if (version == CurrentVersion) return JsonUtility.FromJson<SaveFile>(json);

            // v1: GameStateDto مسطّح على الجذر (بدون header/progress/settings)
            if (version <= 1)
            {
                var legacy = JsonUtility.FromJson<GameStateDto>(json);
                if (legacy == null) return null;

                var file = new SaveFile
                {
                    header = new SaveHeader
                    {
                        version = CurrentVersion,
                        slotId = "?",
                        chapterId = legacy.chapterId,
                        chapterTitle = null,
                        locationName = null,
                        nodeId = legacy.currentNodeId,
                        endingId = legacy.endingId,
                        checkpoint = "migrated-v1",
                        playtimeSeconds = legacy.playtimeSeconds,
                        savedAtUtc = string.IsNullOrEmpty(legacy.savedAtUtc)
                            ? DateTime.UtcNow.ToString("o") : legacy.savedAtUtc,
                        isAuto = false,
                        saveCount = 1,
                        gameVersion = "migrated"
                    },
                    state = legacy,
                    progress = new ProgressSnapshot(),
                    settings = new SettingsSnapshot()
                };

                Debug.Log($"[TLC/Save] ↗ حفظ v1 اترقى لـv{CurrentVersion} · {legacy.chapterId}/{legacy.currentNodeId}");
                return file;
            }

            Debug.LogError($"[TLC/Save] إصدار {version} مش معروف للترقية.");
            return null;
        }

        static int PeekVersion(string json)
        {
            if (string.IsNullOrEmpty(json)) return -1;

            // "version" في الهيدر، أو في الجذر لو v1 مسطّح
            int idx = json.IndexOf("\"version\"", StringComparison.Ordinal);
            if (idx < 0) return 1;

            int colon = json.IndexOf(':', idx);
            if (colon < 0) return 1;

            int i = colon + 1;
            while (i < json.Length && (json[i] == ' ' || json[i] == '\t' || json[i] == '\r' || json[i] == '\n')) i++;

            var sb = new StringBuilder();
            while (i < json.Length && char.IsDigit(json[i])) sb.Append(json[i++]);
            return sb.Length > 0 && int.TryParse(sb.ToString(), out int v) ? v : 1;
        }

        // ────────────────────────────── مُساعدات ──────────────────────────────

        static ProgressSnapshot SnapshotProgress()
        {
            var p = Progress.Current;
            return new ProgressSnapshot
            {
                unlockedChapters = p.unlockedChapters,
                seenEndings = new List<string>(p.seenEndings ?? new List<string>()),
                seenPostCredits = new List<string>(p.seenPostCredits ?? new List<string>()),
                viewedCinematics = new List<string>(p.viewedCinematics ?? new List<string>()),
                visitedScenes = new List<string>(p.visitedScenes ?? new List<string>()),
                teaserSeen = p.teaserSeen
            };
        }

        static void RestoreProgress(ProgressSnapshot s)
        {
            if (s == null) return;
            Progress.InjectForTests(new Progress.Data
            {
                unlockedChapters = s.unlockedChapters,
                seenEndings = s.seenEndings ?? new List<string>(),
                seenPostCredits = s.seenPostCredits ?? new List<string>(),
                viewedCinematics = s.viewedCinematics ?? new List<string>(),
                visitedScenes = s.visitedScenes ?? new List<string>(),
                teaserSeen = s.teaserSeen,
                lastChapter = Progress.LastChapter
            });
            Progress.Save();
        }

        static SettingsSnapshot SnapshotSettings()
        {
            var a = TLC.Audio.AudioDirector.Instance;
            return new SettingsSnapshot
            {
                master = a != null ? a.MasterVolume : 1f,
                music = a != null ? a.MusicVolume : 0.7f,
                sfx = a != null ? a.SfxVolume : 0.85f,
                voice = a != null ? a.VoiceVolume : 0.9f,
                lookSensitivity = ControlSettings.LookSensitivity,
                stickScale = ControlSettings.StickScale,
                invertY = ControlSettings.InvertY,
                autoRun = ControlSettings.AutoRun,
                qualityTier = (int)QualityManager.SavedTier(1),
                language = Localization.IsRtl ? "ar" : "en"
            };
        }

        public static void ApplySettings(SettingsSnapshot s)
        {
            if (s == null) return;

            var a = TLC.Audio.AudioDirector.Instance;
            if (a != null) a.SetVolumes(s.master, s.music, s.sfx, s.voice);

            ControlSettings.SetLookSensitivity(s.lookSensitivity);
            ControlSettings.SetStickScale(s.stickScale);
            ControlSettings.SetInvertY(s.invertY);
            ControlSettings.SetAutoRun(s.autoRun);
            QualitySettings.SetQualityLevel(Mathf.Clamp(s.qualityTier, 0, 2), true);

            Localization.SetLanguage(s.language == "en" ? Localization.Lang.English : Localization.Lang.Arabic);
        }

        public static string PrettyDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "—";
            return DateTime.TryParse(iso, out DateTime d) ? d.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : iso;
        }

        public static string PrettyPlaytime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int h = total / 3600, m = (total % 3600) / 60;
            return h > 0 ? $"{h}س {m:00}د" : $"{m}د {total % 60:00}ث";
        }

        // ────────────────────────────── أدوات الاختبار ──────────────────────────────

        public static void CorruptForTests(int slot)
        {
            Directory.CreateDirectory(DirPath);
            File.WriteAllText(PathFor(slot), "{ \"header\": { \"versio", Encoding.UTF8);
            if (File.Exists(BackupPathFor(slot))) File.Delete(BackupPathFor(slot));
        }

        public static void WriteLegacyForTests(int slot, GameState state)
        {
            Directory.CreateDirectory(DirPath);
            var dto = state.ToDto();
            dto.version = 1;
            dto.savedAtUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(PathFor(slot), JsonUtility.ToJson(dto, true), Encoding.UTF8);
            if (File.Exists(BackupPathFor(slot))) File.Delete(BackupPathFor(slot));
        }

        public static void ClearAll()
        {
            try
            {
                if (!Directory.Exists(DirPath)) return;
                foreach (var f in Directory.GetFiles(DirPath, "slot_*")) File.Delete(f);
                Debug.Log("[TLC/Save] كل الخانات اتمسحت (اختبار).");
            }
            catch (Exception e) { Debug.LogWarning("[TLC/Save] تصفير الخانات فشل: " + e.Message); }
        }
    }
}
