using UnityEngine;
using TLC.Player;

namespace TLC.World
{
    /// <summary>
    /// بناء آدم من بدائيات: ريج بشري كامل بمفاصل حقيقية (كتف/كوع/حوض/ركبة)
    /// عشان ProceduralRig يحرّكه. الطول 1.78م، نسب بشرية معقولة.
    ///
    /// ده بديل مؤقت مقصود (placeholder) — Architecture جاهزة لتركيب
    /// موديل Char، وكل اللي هيتغير هو استدعاء واحد.
    /// </summary>
    public class AdamBuilder : MonoBehaviour
    {
        public ThirdPersonController Controller { get; private set; }
        public ProceduralRig Rig { get; private set; }
        public CharacterAnimatorBinder AnimatorBinder { get; private set; }
        public Transform Root => transform;

        /// <summary>الموديل البشري المستخدم (null لو بنستخدم البديل).</summary>
        public GameObject Model { get; private set; }
        public HumanoidPuppet Puppet { get; private set; }
        public bool UsingRealisticModel => Model != null;

        void Update()
        {
            if (Puppet != null && Controller != null)
                Puppet.Speed = Controller.CurrentSpeed;
        }

        Material _skin, _jacket, _shirt, _trousers, _shoes, _hair;

        /// <summary>
        /// بيبني آدم. ⚠️ كان فيه باج خطير هنا:
        /// في مسار الموديل الحقيقي كانت الدالة بترجع قبل إضافة ThirdPersonController،
        /// يعني <c>Controller</c> كان بيفضل null — ومن أول ما الموديلات تبقى موجودة
        /// كانت الكاميرا بتقع بـ NullReference. اتصلّح: المتحكم بيتضمن في كل المسارات.
        /// </summary>
        public void Build(Vector3 spawnPosition)
        {
            CreateMaterials();

            transform.position = spawnPosition;
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            EnsureCharacterController();

            // الأفضلية للموديل البشري الحقيقي؛ البدائيات احتياطي فقط
            var prefab = CharacterLibrary.Load(CharacterLibrary.Cast.Adam);
            if (prefab != null && SpawnModel(prefab))
            {
                Debug.Log("[TLC] آدم: موديل بشري واقعي.");
            }
            else
            {
                Debug.LogWarning("[TLC] آدم: مفيش موديل — بيستخدم الريج البدائي.");
                CreateMaterials();
                BuildBody();
            }

            EnsureController();
            BindAnimator();
        }

        void EnsureCharacterController()
        {
            var cc = GetComponent<CharacterController>();
            if (cc == null) cc = gameObject.AddComponent<CharacterController>();

            cc.height = 1.78f;
            cc.radius = 0.30f;
            cc.center = new Vector3(0f, 0.89f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.22f;   // أقل من ارتفاع أرخص قطعة أثاث
            cc.skinWidth = 0.02f;
        }

        /// <summary>بيتأكد إن المتحكم موجود (كل المسارات بتعدي من هنا).</summary>
        void EnsureController()
        {
            var c = GetComponent<ThirdPersonController>();
            if (c == null) c = gameObject.AddComponent<ThirdPersonController>();
            Controller = c;
        }

        /// <summary>
        /// بيربط الـ Animator Controller بالموديل. لو الكليبات مش موجودة
        /// (مثلاً المشروع اتفتح قبل ما AnimatorControllerBuilder يشتغل)
        /// الحركة بتفضل إجرائية عن طريق HumanoidPuppet — مفيش انهيار.
        /// </summary>
        void BindAnimator()
        {
            AnimatorBinder = GetComponent<CharacterAnimatorBinder>();
            if (AnimatorBinder == null) AnimatorBinder = gameObject.AddComponent<CharacterAnimatorBinder>();

            AnimatorBinder.Bind(Model, Controller, Puppet);
        }

        /// <summary>يركّب الموديل الواقعي فوق نفس الـ CharacterController.</summary>
        bool SpawnModel(GameObject prefab)
        {
            var model = Instantiate(prefab, transform);
            model.name = "Adam_Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // كولادرات الموديل بتتعارض مع الـ CharacterController
            foreach (var c in model.GetComponentsInChildren<Collider>())
                if (c != null) Destroy(c);

            Puppet = gameObject.AddComponent<HumanoidPuppet>();
            Puppet.Bind(model);

            if (!Puppet.IsBound)
            {
                Debug.LogWarning("[TLC] تعذّر ربط عظام الموديل — رجوع للبديل.");
                Destroy(model);
                Puppet = null;
                return false;
            }

            // المحور في موديلات الشخصيات عند الحوض — بننزّله لحد القدم
            // (داخل نود وسيطة عشان الـAnimator ما يمسحش التعديل)
            ModelFit.DisableRootMotion(model);
            var pivot = ModelFit.WrapInPivot(model, "Adam_Pivot");
            ModelFit.FitHeight(model.transform, 1.80f);
            ModelFit.DropToGround(pivot.transform, transform.position.y);

            Model = model;
            return true;
        }

        void CreateMaterials()
        {
            _skin     = Mat(new Color(0.62f, 0.46f, 0.36f), 0.25f, 0f);
            _jacket   = Mat(new Color(0.075f, 0.078f, 0.085f), 0.35f, 0f);
            _shirt    = Mat(new Color(0.13f, 0.13f, 0.14f), 0.15f, 0f);
            _trousers = Mat(new Color(0.12f, 0.13f, 0.16f), 0.20f, 0f);
            _shoes    = Mat(new Color(0.05f, 0.05f, 0.055f), 0.55f, 0.1f);
            _hair     = Mat(new Color(0.045f, 0.04f, 0.038f), 0.30f, 0f);
        }

        static Material Mat(Color c, float smooth, float metal)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh) { color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            return m;
        }

        // ────────────────────────── الجسم ──────────────────────────

        void BuildBody()
        {
            var rigRoot = new GameObject("Rig").transform;
            rigRoot.SetParent(transform, false);

            var rig = gameObject.AddComponent<ProceduralRig>();
            Rig = rig;

            // حوض
            var hips = Joint(rigRoot, "Hips", new Vector3(0f, 0.95f, 0f));
            rig.hips = hips;
            Visual(hips, PrimitiveType.Cube, new Vector3(0f, 0f, 0f),
                   new Vector3(0.34f, 0.20f, 0.24f), _trousers);

            // عمود فقري + صدر
            var spine = Joint(hips, "Spine", new Vector3(0f, 0.12f, 0f));
            rig.spine = spine;
            Visual(spine, PrimitiveType.Cube, new Vector3(0f, 0.20f, 0f),
                   new Vector3(0.40f, 0.42f, 0.26f), _jacket);

            // رقبة + راس
            var neck = Joint(spine, "Neck", new Vector3(0f, 0.43f, 0f));
            Visual(neck, PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f),
                   new Vector3(0.085f, 0.05f, 0.085f), _skin);

            var head = Joint(neck, "Head", new Vector3(0f, 0.12f, 0f));
            rig.head = head;
            Visual(head, PrimitiveType.Sphere, new Vector3(0f, 0.11f, 0f),
                   new Vector3(0.20f, 0.226f, 0.205f), _skin);

            // شعر
            Visual(head, PrimitiveType.Sphere, new Vector3(0f, 0.155f, -0.005f),
                   new Vector3(0.208f, 0.185f, 0.212f), _hair);

            // ذراعين
            rig.leftUpperArm  = BuildArm(spine, "L", -0.245f, _jacket);
            rig.rightUpperArm = BuildArm(spine, "R",  0.245f, _jacket);

            // رجلين
            rig.leftThigh = BuildLeg(hips, "L", -0.105f);
            rig.rightThigh = BuildLeg(hips, "R", 0.105f);

            rig.leftLowerArm  = rig.leftUpperArm.GetChild(0);
            rig.rightLowerArm = rig.rightUpperArm.GetChild(0);
            rig.leftShin      = rig.leftThigh.GetChild(0);
            rig.rightShin     = rig.rightThigh.GetChild(0);

            EnsureController();
        }

        Transform BuildArm(Transform parent, string side, float xOffset, Material sleeve)
        {
            float sign = Mathf.Sign(xOffset);

            var shoulder = Joint(parent, $"Shoulder_{side}", new Vector3(xOffset, 0.36f, 0f));

            // العضد
            Visual(shoulder, PrimitiveType.Capsule, new Vector3(0f, -0.14f, 0f),
                   new Vector3(0.088f, 0.15f, 0.088f), sleeve);

            // الكوع
            var elbow = Joint(shoulder, $"Elbow_{side}", new Vector3(0f, -0.29f, 0f));
            Visual(elbow, PrimitiveType.Capsule, new Vector3(0f, -0.135f, 0f),
                   new Vector3(0.078f, 0.14f, 0.078f), sleeve);

            // اليد
            var hand = Joint(elbow, $"Hand_{side}", new Vector3(0f, -0.28f, 0f));
            Visual(hand, PrimitiveType.Cube, new Vector3(0f, -0.05f, 0f),
                   new Vector3(0.075f, 0.11f, 0.05f), _skin);

            // دوران بسيط لبرّه عشان الأيدين ما تخبطش في الجسم
            shoulder.localRotation = Quaternion.Euler(0f, 0f, sign * 5f);

            return shoulder;
        }

        Transform BuildLeg(Transform parent, string side, float xOffset)
        {
            var thigh = Joint(parent, $"Thigh_{side}", new Vector3(xOffset, -0.05f, 0f));

            Visual(thigh, PrimitiveType.Capsule, new Vector3(0f, -0.21f, 0f),
                   new Vector3(0.115f, 0.22f, 0.115f), _trousers);

            var knee = Joint(thigh, $"Knee_{side}", new Vector3(0f, -0.44f, 0f));
            Visual(knee, PrimitiveType.Capsule, new Vector3(0f, -0.20f, 0f),
                   new Vector3(0.098f, 0.21f, 0.098f), _trousers);

            var ankle = Joint(knee, $"Ankle_{side}", new Vector3(0f, -0.42f, 0f));
            Visual(ankle, PrimitiveType.Cube, new Vector3(0f, -0.04f, 0.045f),
                   new Vector3(0.105f, 0.08f, 0.27f), _shoes);

            return thigh;
        }

        static Transform Joint(Transform parent, string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }

        void Visual(Transform parent, PrimitiveType type, Vector3 localPos,
                    Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Visual";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        /// <summary>نقطة تركيب موديل آدم الحقيقي مستقبلاً.</summary>
        public void AttachDetailedModel(GameObject modelPrefab)
        {
            if (modelPrefab == null) return;
            var inst = Instantiate(modelPrefab, transform);
            inst.transform.localPosition = Vector3.zero;

            var r = inst.GetComponentInChildren<Animator>();
            if (r != null && Controller != null)
            {
                var f = Controller.GetType().GetField("animator",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                f?.SetValue(Controller, r);
            }
            Rig.enabled = false;
        }
    }
}
