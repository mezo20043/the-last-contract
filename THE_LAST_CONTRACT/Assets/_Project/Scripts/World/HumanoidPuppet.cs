using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// محرّك حركة بشرية على هيكل عظمي حقيقي.
    ///
    /// بياخد موديل Humanoid (بهيكل 24 عظمة) ويحرّكه في ست حالات:
    ///   Idle · Walk · Run · Sit · Turn · Talk
    ///
    /// العظام بتتجاب من Avatar عن طريق <c>Animator.GetBoneTransform</c> — يعني مستقل
    /// عن أسماء العظام في الملف. لو الـ Avatar مش متعرّف عليه، بيرجع لمطابقة الأسماء.
    ///
    /// الحركة إجرائية (رياضية) مش كليبات مسجّلة — الطريقة الوحيدة الممكنة
    /// من غير تسجيل موكاب. الجسم والميش والجلد حقيقيين.
    /// </summary>
    public class HumanoidPuppet : MonoBehaviour
    {
        public enum State { Idle, Walk, Run, Sit, Turn, Talk }

        // ── مراجع ──
        Animator _animator;
        Transform _modelRoot;
        readonly Dictionary<HumanBodyBones, Transform> _bones = new Dictionary<HumanBodyBones, Transform>();
        readonly Dictionary<HumanBodyBones, Quaternion> _rest = new Dictionary<HumanBodyBones, Quaternion>();
        readonly Dictionary<HumanBodyBones, Vector3> _restPos = new Dictionary<HumanBodyBones, Vector3>();

        // ── حالة ──
        public State Current { get; private set; } = State.Idle;
        public float Speed { get; set; }
        public bool IsTalking { get; set; }
        public bool IsSeated { get; set; }

        bool _usesAnimator;
        public bool UsingAnimator => _usesAnimator;

        /// <summary>
        /// مين بيكتب البارامترات في الـ Animator: الـ Puppet ولا ThirdPersonController.
        /// CharacterAnimatorBinder بيقفلها على Puppet لما ThirdPersonController يبقى مالك الحركة.
        /// </summary>
        public bool DrivesAnimator { get; set; } = true;

        float _phase;
        float _turnTarget;
        float _blinkTimer;
        int _boneCount;

        public int BoneCount => _boneCount;
        public bool IsBound => _boneCount > 0;
        public Transform HeadBone => Bone(HumanBodyBones.Head);
        public Transform ChestBone => Bone(HumanBodyBones.Chest) ?? Bone(HumanBodyBones.Spine);

        // ─────── ربط الموديل ───────

        public void Bind(GameObject model)
        {
            if (model == null) return;

            _modelRoot = model.transform;
            _animator = model.GetComponentInChildren<Animator>();

            _bones.Clear();
            _rest.Clear();
            _restPos.Clear();

            foreach (HumanBodyBones bone in BoneOrder)
            {
                Transform t = null;

                if (_animator != null)
                {
                    try { t = _animator.GetBoneTransform(bone); }
                    catch (Exception) { t = null; }
                }

                if (t == null) t = FindBoneByName(bone);
                if (t == null) continue;

                _bones[bone] = t;
                _rest[bone] = t.localRotation;
                _restPos[bone] = t.localPosition;
            }

            _boneCount = _bones.Count;

            if (_boneCount == 0)
            {
                Debug.LogWarning("[TLC/Puppet] مفيش عظام اتعرّفت — الموديل هيبقى ساكن.");
                return;
            }

            // لو الموديل جاي بـ Animator Controller جاهز، نسلّم له الحركة
            if (_animator != null)
            {
                if (_animator.runtimeAnimatorController == null)
                {
                    var controller = Resources.Load<RuntimeAnimatorController>("Animators/TLC_Character");
                    if (controller != null) _animator.runtimeAnimatorController = controller;
                }
                _usesAnimator = _animator.runtimeAnimatorController != null;
            }

            Debug.Log($"[TLC/Puppet] اتربط {_boneCount} عظمة · موديل: {model.name}" +
                      $" · حركة: {(_usesAnimator ? "كليبات Animator" : "إجرائية")}");
        }

        static readonly HumanBodyBones[] BoneOrder =
        {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest,
            HumanBodyBones.Neck, HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
        };

        static readonly Dictionary<HumanBodyBones, string[]> NameHints =
            new Dictionary<HumanBodyBones, string[]>
        {
            { HumanBodyBones.Hips,          new[] { "hips", "pelvis", "root" } },
            { HumanBodyBones.Spine,         new[] { "spine", "spine1", "spine01" } },
            { HumanBodyBones.Chest,         new[] { "chest", "spine2", "spine02" } },
            { HumanBodyBones.Neck,          new[] { "neck" } },
            { HumanBodyBones.Head,          new[] { "head" } },
            { HumanBodyBones.LeftShoulder,  new[] { "l_shoulder", "leftshoulder", "shoulder_l", "l_clavicle" } },
            { HumanBodyBones.RightShoulder, new[] { "r_shoulder", "rightshoulder", "shoulder_r", "r_clavicle" } },
            { HumanBodyBones.LeftUpperArm,  new[] { "leftarm", "l_arm", "larm", "l_upperarm", "upperarm_l" } },
            { HumanBodyBones.RightUpperArm, new[] { "rightarm", "r_arm", "rarm", "r_upperarm", "upperarm_r" } },
            { HumanBodyBones.LeftLowerArm,  new[] { "leftforearm", "l_forearm", "lforearm", "lowerarm_l" } },
            { HumanBodyBones.RightLowerArm, new[] { "rightforearm", "r_forearm", "rforearm", "lowerarm_r" } },
            { HumanBodyBones.LeftHand,      new[] { "l_hand", "lefthand", "hand_l" } },
            { HumanBodyBones.RightHand,     new[] { "r_hand", "righthand", "hand_r" } },
            { HumanBodyBones.LeftUpperLeg,  new[] { "leftupleg", "l_thigh", "upleg_l", "thigh_l" } },
            { HumanBodyBones.RightUpperLeg, new[] { "rightupleg", "r_thigh", "upleg_r", "thigh_r" } },
            { HumanBodyBones.LeftLowerLeg,  new[] { "leftleg", "l_shin", "leg_l", "calf_l" } },
            { HumanBodyBones.RightLowerLeg, new[] { "rightleg", "r_shin", "leg_r", "calf_r" } },
            { HumanBodyBones.LeftFoot,      new[] { "leftfoot", "l_foot", "foot_l" } },
            { HumanBodyBones.RightFoot,     new[] { "rightfoot", "r_foot", "foot_r" } },
        };

        Transform FindBoneByName(HumanBodyBones bone)
        {
            if (!NameHints.TryGetValue(bone, out var hints)) return null;
            if (_modelRoot == null) return null;

            foreach (var t in _modelRoot.GetComponentsInChildren<Transform>())
            {
                string n = t.name.ToLowerInvariant();
                foreach (var h in hints) if (n.Contains(h)) return t;
            }
            return null;
        }

        Transform Bone(HumanBodyBones b) =>
            _bones.TryGetValue(b, out var t) ? t : null;

        // ─────── الحلقة ───────

        void LateUpdate()
        {
            if (!IsBound) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            UpdateState(dt);

            // مسار الكليبات الحقيقية (walk/run من الملف) — الأولوية عليه
            if (_usesAnimator)
            {
                // لما ThirdPersonController يبقى مالك الحركة، هو اللي بيكتب البارامترات
                // (عنده كشف أرض حقيقي وقيم أدق) — الـ Puppet بيسكت عشان مايحصلش تعارض.
                if (DrivesAnimator)
                {
                    _animator.SetFloat("Speed", Speed);
                    _animator.SetBool("IsRunning", Speed > 4.2f);
                    _animator.SetBool("Grounded", true);
                    _animator.SetBool("Talking", IsTalking);
                }

                // الكليبات الجاهزة في الملفات هي walk/run بس؛ وضعية الوقوف/القعود
                // الافتراضية كانت بترجع لوضعية الـbind (دراعات مرفوعة / رجلين مطبقة).
                // هنا بنصحّح الوضعية فوق الكليب — وقوف طبيعي وقعود طبيعي.
                ApplyNaturalPose();
                SettleFeet(dt);
                return;
            }

            switch (Current)
            {
                case State.Walk: PoseWalk(dt, 1.0f); break;
                case State.Run:  PoseWalk(dt, 1.75f); break;
                case State.Sit:  PoseSit(dt); break;
                case State.Talk: PoseIdle(dt); PoseTalk(dt); break;
                case State.Turn: PoseTurn(dt); break;
                default:         PoseIdle(dt); break;
            }

            ApplyOffsets();   // ← التنفيذ الفعلي: لازم يتنادى كل إطار
            SettleFeet(dt);
        }

        // ─────── تصحيح الوضعية (فوق كليبات الـAnimator) ───────

        /// <summary>
        /// بيلف عظمة في المساحة العالمية بحيث يبقى اتجاه الطرف (من–إلى) هو المطلوب.
        /// الطريقة دي مستقلة عن أسماء المحاور المحلية في الريك، فبتشتغل مع أي موديل.
        /// </summary>
        void AlignLimb(HumanBodyBones from, HumanBodyBones to, Vector3 wantDir)
        {
            var a = Bone(from);
            var b = Bone(to);
            if (a == null || b == null) return;

            Vector3 cur = b.position - a.position;
            if (cur.sqrMagnitude < 1e-6f) return;

            var delta = Quaternion.FromToRotation(cur.normalized, wantDir.normalized);
            a.rotation = delta * a.rotation;
        }

        void ApplyNaturalPose()
        {
            if (_modelRoot == null || !IsBound) return;

            var fwd = transform.forward;
            var right = transform.right;
            var down = Vector3.down;

            if (IsSeated)
            {
                // قعود: فخذين أفقيين لقدام، ساقين لتحت، دراعات مستريحة على الحضن
                AlignLimb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                          (fwd * 0.94f + down * 0.10f).normalized);
                AlignLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                          (fwd * 0.94f + down * 0.10f).normalized);

                AlignLimb(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                          (down * 0.97f + fwd * 0.12f).normalized);
                AlignLimb(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
                          (down * 0.97f + fwd * 0.12f).normalized);

                AlignLimb(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                          (down * 0.72f + fwd * 0.55f + right * -0.10f).normalized);
                AlignLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                          (down * 0.72f + fwd * 0.55f + right * 0.10f).normalized);

                AlignLimb(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                          (fwd * 0.75f + down * 0.35f).normalized);
                AlignLimb(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                          (fwd * 0.75f + down * 0.35f).normalized);

                AlignSitSpine();
                return;
            }

            if (Speed > 0.22f) return;   // المشي/الجري بيسيبهم لكليبات الملف

            // وقوف طبيعي: دراعات متهدلة على الجنبين مع ثنية كوع خفيفة
            float breath = Mathf.Sin(Time.time * 1.4f) * 0.02f;

            AlignLimb(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                      (down + right * -0.16f + fwd * (0.06f + breath)).normalized);
            AlignLimb(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                      (down + right * 0.16f + fwd * (0.06f + breath)).normalized);

            AlignLimb(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                      (down + fwd * 0.20f + right * -0.10f).normalized);
            AlignLimb(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                      (down + fwd * 0.20f + right * 0.10f).normalized);

            // رجلين مستقيمين (الموديلات بتيجي بعض الأحيان بركبة مثنية في الـbind)
            AlignLimb(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                      (down + fwd * 0.03f + right * 0.035f).normalized);
            AlignLimb(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                      (down + fwd * 0.03f + right * -0.035f).normalized);
            AlignLimb(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, down);
            AlignLimb(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, down);

            // كتفين متهدلين شوية (مش مرفوعين زي وضعية الـbind)
            var lSh = Bone(HumanBodyBones.LeftShoulder);
            var rSh = Bone(HumanBodyBones.RightShoulder);
            if (lSh != null) lSh.rotation = Quaternion.AngleAxis(-4f, fwd) * lSh.rotation;
            if (rSh != null) rSh.rotation = Quaternion.AngleAxis(4f, fwd) * rSh.rotation;
        }

        void AlignSitSpine()
        {
            var spine = Bone(HumanBodyBones.Spine);
            var chest = Bone(HumanBodyBones.Chest);
            var fwd = transform.forward;
            var right = transform.right;

            if (spine != null) spine.rotation = Quaternion.AngleAxis(6f, right) * spine.rotation;
            if (chest != null) chest.rotation = Quaternion.AngleAxis(3f, right) * chest.rotation;
        }

        /// <summary>
        /// بينزّل الموديل لحد ما أطراف القدمين تلمس الأرض — مرة واحدة عند تغيير
        /// الوضعية، وبتخميد عشان مايحصلش تذبذب بين الإطارات.
        /// </summary>
        void SettleFeet(float dt)
        {
            if (_modelRoot == null) return;

            var pivot = _modelRoot.parent != null ? _modelRoot.parent : _modelRoot;
            if (pivot.parent == null) return;    // مش عايزين نحرّك جذر الشخصية نفسها

            var rs = _modelRoot.GetComponentsInChildren<Renderer>(true);
            bool first = true;
            var b = new Bounds();
            foreach (var r in rs)
            {
                if (r == null || !r.enabled) continue;
                if (first) { b = r.bounds; first = false; }
                else b.Encapsulate(r.bounds);
            }
            if (first) return;

            float groundY = transform.position.y;
            float dy = groundY - b.min.y;

            if (Mathf.Abs(dy) < 0.01f) return;
            if (Mathf.Abs(dy) > 1.2f) return;    // قراءة مش منطقية — نتخطاها

            float step = dy * Mathf.Clamp01(dt * 12f);
            pivot.position += Vector3.up * step;
        }

        void UpdateState(float dt)
        {
            if (IsSeated) { Current = State.Sit; return; }
            if (Speed > 4.2f) { Current = State.Run; return; }
            if (Speed > 0.18f) { Current = State.Walk; return; }
            Current = IsTalking ? State.Talk : State.Idle;
        }

        // ─────── الحالات ───────

        void PoseIdle(float dt)
        {
            float t = Time.time;

            // تنفّس: صدر بيطلع وينزل + كتفين
            float breath = Mathf_Sin(t * 1.55f) * 0.9f;
            Add(HumanBodyBones.Spine, -1.2f + breath * 0.7f, 0f, 0f);
            Add(HumanBodyBones.Chest, breath * 0.9f, 0f, 0f);
            Add(HumanBodyBones.Neck, -breath * 0.5f, 0f, 0f);

            // ميل بسيط في الوزن بين الرجلين
            float sway = Mathf_Sin(t * 0.42f);
            Add(HumanBodyBones.Hips, 0f, sway * 1.6f, sway * 1.1f);

            // راس بتلف بلطف
            Add(HumanBodyBones.Head, Mathf_Sin(t * 0.31f) * 1.5f,
                                     Mathf_Sin(t * 0.23f) * 4.0f, 0f);

            // أذرع متهدلة مع حركة طفيفة
            float armSway = Mathf_Sin(t * 0.8f) * 1.8f;
            Add(HumanBodyBones.LeftUpperArm, armSway, 0f, -3.0f);
            Add(HumanBodyBones.RightUpperArm, -armSway, 0f, 3.0f);
            Add(HumanBodyBones.LeftLowerArm, -4f, 0f, 0f);
            Add(HumanBodyBones.RightLowerArm, -4f, 0f, 0f);
        }

        void PoseWalk(float dt, float intensity)
        {
            _phase += dt * (2.1f * intensity);
            float s = Mathf_Sin(_phase * 6.28318f);
            float c = Mathf_Cos(_phase * 6.28318f);

            float thighAmp = 26f * intensity;
            float kneeAmp = 34f * intensity;
            float armAmp = 19f * intensity;

            Add(HumanBodyBones.LeftUpperLeg, -s * thighAmp, 0f, 0f);
            Add(HumanBodyBones.RightUpperLeg, s * thighAmp, 0f, 0f);

            // الركبة بتثني بس في اتجاه واحد
            Add(HumanBodyBones.LeftLowerLeg, Mathf_Max(0f, -c) * kneeAmp, 0f, 0f);
            Add(HumanBodyBones.RightLowerLeg, Mathf_Max(0f, c) * kneeAmp, 0f, 0f);

            Add(HumanBodyBones.LeftFoot, s * 9f, 0f, 0f);
            Add(HumanBodyBones.RightFoot, -s * 9f, 0f, 0f);

            // أذرع عكس الرجلين
            Add(HumanBodyBones.LeftUpperArm, s * armAmp, 0f, -5f);
            Add(HumanBodyBones.RightUpperArm, -s * armAmp, 0f, 5f);
            Add(HumanBodyBones.LeftLowerArm, -12f - Mathf_Max(0f, s) * 10f, 0f, 0f);
            Add(HumanBodyBones.RightLowerArm, -12f - Mathf_Max(0f, -s) * 10f, 0f, 0f);

            // جسم: ميل خفيف للأمام + دوران حوض + ارتداد رأسي
            Add(HumanBodyBones.Spine, 4f * intensity, 0f, 0f);
            Add(HumanBodyBones.Hips, 0f, c * 3.2f * intensity, c * 1.4f * intensity);
            Add(HumanBodyBones.Head, -3f * intensity, -c * 2.0f, 0f);

            if (_modelRoot != null && _restPos.ContainsKey(HumanBodyBones.Hips))
            {
                var hp = _restPos[HumanBodyBones.Hips];
                var hips = Bone(HumanBodyBones.Hips);
                if (hips != null)
                    hips.localPosition = hp + new Vector3(0f, Mathf_Abs(c) * 0.035f * intensity, 0f);
            }
        }

        void PoseSit(float dt)
        {
            // قعدة: فخذين أفقيين، ركبتين ٩٠°، جسم مائل لقدام شوية
            float bob = Mathf_Sin(Time.time * 1.1f) * 0.6f;

            Add(HumanBodyBones.LeftUpperLeg, -78f, 0f, 4f);
            Add(HumanBodyBones.RightUpperLeg, -78f, 0f, -4f);
            Add(HumanBodyBones.LeftLowerLeg, 82f, 0f, 0f);
            Add(HumanBodyBones.RightLowerLeg, 82f, 0f, 0f);
            Add(HumanBodyBones.LeftFoot, 12f, 0f, 0f);
            Add(HumanBodyBones.RightFoot, 12f, 0f, 0f);

            Add(HumanBodyBones.Spine, 6f + bob, 0f, 0f);
            Add(HumanBodyBones.Chest, 2f, 0f, 0f);

            float arm = Mathf_Sin(Time.time * 0.5f) * 1.2f;
            Add(HumanBodyBones.LeftUpperArm, -22f + arm, 0f, -14f);
            Add(HumanBodyBones.RightUpperArm, -22f - arm, 0f, 14f);
            Add(HumanBodyBones.LeftLowerArm, -38f, 0f, 0f);
            Add(HumanBodyBones.RightLowerArm, -38f, 0f, 0f);
        }

        void PoseTalk(float dt)
        {
            float t = Time.time;

            // إيماءات إيديم بتتبدّل — مش متزامنة ولا ميكانيكية
            float g1 = Mathf_Sin(t * 1.7f);
            float g2 = Mathf_Sin(t * 0.93f + 1.3f);

            Add(HumanBodyBones.RightUpperArm, -26f - g1 * 11f, 0f, 9f + g2 * 5f);
            Add(HumanBodyBones.RightLowerArm, -52f - g2 * 18f, 0f, 0f);
            Add(HumanBodyBones.RightHand, g1 * 12f, 0f, 0f);

            Add(HumanBodyBones.LeftUpperArm, -14f + g2 * 7f, 0f, -7f);
            Add(HumanBodyBones.LeftLowerArm, -30f - g1 * 9f, 0f, 0f);

            // الراس بتتحرك مع الكلام
            Add(HumanBodyBones.Head, Mathf_Sin(t * 2.3f) * 3.5f,
                                     Mathf_Sin(t * 1.1f) * 6.5f, Mathf_Sin(t * 0.7f) * 2f);
            Add(HumanBodyBones.Neck, Mathf_Sin(t * 2.3f) * 1.8f, 0f, 0f);
        }

        void PoseTurn(float dt)
        {
            // لف الجزء العلوي ناحية الهدف
            Add(HumanBodyBones.Hips, 0f, _turnTarget * 0.35f, 0f);
            Add(HumanBodyBones.Spine, 0f, _turnTarget * 0.30f, 0f);
            Add(HumanBodyBones.Chest, 0f, _turnTarget * 0.20f, 0f);
            Add(HumanBodyBones.Head, 0f, _turnTarget * 0.55f, 0f);
        }

        // ─────── تطبيق الإزاحات ───────

        readonly Dictionary<HumanBodyBones, Vector3> _offset = new Dictionary<HumanBodyBones, Vector3>();

        void Add(HumanBodyBones bone, float x, float y, float z)
        {
            if (!_bones.ContainsKey(bone)) return;
            if (_offset.TryGetValue(bone, out var cur))
                _offset[bone] = new Vector3(cur.x + x, cur.y + y, cur.z + z);
            else
                _offset[bone] = new Vector3(x, y, z);
        }

        // التطبيق الفعلي بيتم في نهاية LateUpdate
        void ApplyOffsets()
        {
            foreach (var kv in _bones)
            {
                if (!_offset.TryGetValue(kv.Key, out var off)) continue;
                if (!_rest.TryGetValue(kv.Key, out var rest)) continue;
                if (off.x == 0f && off.y == 0f && off.z == 0f) continue;

                kv.Value.localRotation = rest * Quaternion.Euler(off.x, off.y, off.z);
            }
            _offset.Clear();
        }

        /// <summary>لوّح ناحية نقطة (للحوارات).</summary>
        public void LookTowards(Vector3 worldPoint)
        {
            var head = HeadBone;
            if (head == null) return;

            var dir = worldPoint - head.position;
            if (dir.sqrMagnitude < 0.0001f) return;

            float yaw = Mathf_Atan2(dir.x, dir.z) * 57.29578f;
            float local = Mathf_DeltaAngle(transform.eulerAngles.y, yaw);
            _turnTarget = Clamp(local, -55f, 55f);
        }

        /// <summary>قعدة: الحركة + إنزال الجسم لمستوى كرسي (~0.45 م).</summary>
        public void SetSeated(bool seated, float seatHeight = 0.45f)
        {
            IsSeated = seated;

            if (_modelRoot != null)
                _modelRoot.localPosition = new Vector3(0f, seated ? -seatHeight : 0f, 0f);

            if (!seated) ResetState();
        }

        /// <summary>يرجّع العظام لوضع الراحة (وضع A/T الافتراضي).</summary>
        void ResetState()
        {
            foreach (var kv in _bones)
            {
                if (_rest.TryGetValue(kv.Key, out var r)) kv.Value.localRotation = r;
                if (_restPos.TryGetValue(kv.Key, out var p)) kv.Value.localPosition = p;
            }
            _offset.Clear();
        }

        public void ResetToRest() => ResetState();

        // ── دوال رياضية محلية (مش محتاجين Mathf كامل) ──
        static float Mathf_Sin(float x) => (float)Math.Sin(x);
        static float Mathf_Cos(float x) => (float)Math.Cos(x);
        static float Mathf_Abs(float x) => Math.Abs(x);
        static float Mathf_Max(float a, float b) => Math.Max(a, b);
        static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;
        static float Mathf_Atan2(float y, float x) => (float)Math.Atan2(y, x);
        static float Mathf_DeltaAngle(float a, float b)
        {
            float d = (b - a) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }
    }
}
