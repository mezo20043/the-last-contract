using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// بناء شقة آدم في مدينة نصر بالكود: صالة + أوضة نوم + مطبخ + حمّام،
    /// أثاث، شبابيك على القاهرة، مدينة ليلية برّه.
    /// كل الأصول بدائية (primitives) — مكانها بيتستبدل بموديلات حقيقية بعدين
    /// من غير تغيير في أي سكربت تاني.
    /// </summary>
    public class ApartmentBuilder : MonoBehaviour
    {
        [Header("نقاط مرجعية (بتتملي وقت البناء)")]
        public Transform PlayerSpawn;
        public Transform PhotoAnchor;      // مكان صورة العيلة
        public Transform EnvelopeAnchor;   // مكان الظرف عند الباب
        public Transform PhoneAnchor;      // التليفون
        public Transform DoorAnchor;

        Material _floorMat, _wallMat, _ceilMat, _woodMat, _fabricMat, _metalMat, _glowMat, _cityMat;

        const float WallH = 2.9f;
        const float T = 0.12f;

        public void Build()
        {
            CreateMaterials();
            BuildFloorsAndCeilings();
            BuildWalls();
            BuildLivingRoom();
            BuildBedroom();
            BuildKitchen();
            BuildCityOutside();
            CreateAnchors();
        }

        // ────────────────────────── مواد ──────────────────────────

        /// <summary>خامة من المكتبة، وإلا لون احتياطي — عشان اللعبة ما تبقاش وردي أبداً.</summary>
        Material Pbr(string key, Color fallback, float smoothness, float metallic)
        {
            var mat = MaterialLibrary.Get(key);
            if (mat != null) return mat;
            return MakeMat(fallback, smoothness, metallic);
        }

        void CreateMaterials()
        {
            // خامات PBR مولّدة (albedo + normal) — مع بديل لوني لو الملفات مش موجودة
            _floorMat  = Pbr("wood_floor",    new Color(0.30f, 0.26f, 0.22f), 0.55f, 0f);  // باركيه
            _wallMat   = Pbr("plaster_wall",  new Color(0.46f, 0.44f, 0.41f), 0.15f, 0f);  // جبس بيج
            _ceilMat   = Pbr("plaster_wall",  new Color(0.55f, 0.54f, 0.52f), 0.05f, 0f);  // سقف
            _woodMat   = Pbr("wood_dark",     new Color(0.20f, 0.14f, 0.10f), 0.45f, 0f);  // خشب غامق
            _fabricMat = Pbr("fabric_dark",   new Color(0.13f, 0.15f, 0.19f), 0.08f, 0f);  // كتان
            _metalMat  = Pbr("metal_brushed", new Color(0.35f, 0.36f, 0.38f), 0.75f, 0.9f); // معدن
            _glowMat  = MakeMat(new Color(1f, 0.86f, 0.62f), 0.2f, 0f);
            _glowMat.EnableKeyword("_EMISSION");
            _glowMat.SetColor("_EmissionColor", new Color(1f, 0.80f, 0.50f) * 1.6f);

            _cityMat = Pbr("city_facade", new Color(0.03f, 0.035f, 0.05f), 0.1f, 0f);
            _cityMat.EnableKeyword("_EMISSION");
            _cityMat.SetColor("_EmissionColor", new Color(0.10f, 0.12f, 0.18f) * 1.2f);
        }

        static Material MakeMat(Color c, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader);
            m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        // ────────────────────────── مساعدات بناء ──────────────────────────

        GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat,
                       bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Destroy(c);
            }

            return go;
        }

        void Wall(Vector3 a, Vector3 b, float height = WallH, float thickness = T)
        {
            Vector3 mid = (a + b) * 0.5f;
            float len = Vector3.Distance(a, b);
            Vector3 dir = (b - a).normalized;

            var go = Box("Wall", transform, mid + Vector3.up * (height * 0.5f),
                         new Vector3(thickness, height, len), _wallMat);

            go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        // ────────────────────────── الأرضيات ──────────────────────────

        void BuildFloorsAndCeilings()
        {
            // صالة 8×6
            Box("Floor_Living", transform, new Vector3(0f, -0.05f, 0f),
                new Vector3(8f, 0.1f, 6f), _floorMat);
            Box("Ceil_Living", transform, new Vector3(0f, WallH, 0f),
                new Vector3(8f, 0.1f, 6f), _ceilMat, false);

            // أوضة نوم 5×4 (شرق الصالة)
            Box("Floor_Bed", transform, new Vector3(6.5f, -0.05f, -1f),
                new Vector3(5f, 0.1f, 4f), _floorMat);
            Box("Ceil_Bed", transform, new Vector3(6.5f, WallH, -1f),
                new Vector3(5f, 0.1f, 4f), _ceilMat, false);

            // مطبخ 4×3.5 (غرب الصالة)
            Box("Floor_Kitchen", transform, new Vector3(-6f, -0.05f, -1.2f),
                new Vector3(4f, 0.1f, 3.5f), _floorMat);
            Box("Ceil_Kitchen", transform, new Vector3(-6f, WallH, -1.2f),
                new Vector3(4f, 0.1f, 3.5f), _ceilMat, false);
        }

        // ────────────────────────── حوائط ──────────────────────────

        void BuildWalls()
        {
            // الصالة — الجدار الشمالي فيه فتحة شباك كبيرة (من x=-1.5 لـ x=1.5)
            Wall(new Vector3(-4f, 0f, 3f), new Vector3(-1.5f, 0f, 3f));
            Wall(new Vector3(1.5f, 0f, 3f), new Vector3(4f, 0f, 3f));
            // عتبة الشباك
            Box("WindowSill", transform, new Vector3(0f, 2.55f, 3f), new Vector3(3f, 0.7f, T), _wallMat);

            // الجدار الجنوبي فيه باب الشقة (فتحة من x=-0.8 لـ x=0.8)
            Wall(new Vector3(-4f, 0f, -3f), new Vector3(-0.8f, 0f, -3f));
            Wall(new Vector3(0.8f, 0f, -3f), new Vector3(4f, 0f, -3f));
            Box("DoorLintel", transform, new Vector3(0f, 2.5f, -3f), new Vector3(1.6f, 0.8f, T), _wallMat);

            // جدار غرب الصالة — فتحة للمطبخ
            Wall(new Vector3(-4f, 0f, -3f), new Vector3(-4f, 0f, -0.9f));
            Wall(new Vector3(-4f, 0f, 0.9f), new Vector3(-4f, 0f, 3f));

            // جدار شرق الصالة — فتحة للأوضة
            Wall(new Vector3(4f, 0f, -3f), new Vector3(4f, 0f, -1.2f));
            Wall(new Vector3(4f, 0f, 1.2f), new Vector3(4f, 0f, 3f));

            // أوضة النوم
            Wall(new Vector3(4f, 0f, -3f), new Vector3(9f, 0f, -3f));
            Wall(new Vector3(9f, 0f, -3f), new Vector3(9f, 0f, 1f));
            Wall(new Vector3(9f, 0f, 1f), new Vector3(4f, 0f, 1f));

            // المطبخ
            Wall(new Vector3(-8f, 0f, -2.95f), new Vector3(-8f, 0f, 0.6f));
            Wall(new Vector3(-8f, 0f, 0.6f), new Vector3(-4f, 0f, 0.6f));
            Wall(new Vector3(-8f, 0f, -2.95f), new Vector3(-4f, 0f, -2.95f));
        }

        // ────────────────────────── أثاث ──────────────────────────

        void BuildLivingRoom()
        {
            var root = new GameObject("LivingRoom_Furniture").transform;
            root.SetParent(transform, false);

            // كنباية
            Box("Sofa_Base", root, new Vector3(-2.6f, 0.35f, 0.6f), new Vector3(0.9f, 0.7f, 2.6f), _fabricMat);
            Box("Sofa_Back", root, new Vector3(-3.0f, 0.78f, 0.6f), new Vector3(0.25f, 0.9f, 2.6f), _fabricMat);
            Box("Sofa_ArmN", root, new Vector3(-2.6f, 0.62f, 1.85f), new Vector3(0.9f, 0.55f, 0.25f), _fabricMat);
            Box("Sofa_ArmS", root, new Vector3(-2.6f, 0.62f, -0.65f), new Vector3(0.9f, 0.55f, 0.25f), _fabricMat);

            // طربيزة وسط
            Box("CoffeeTable", root, new Vector3(-0.6f, 0.24f, 0.6f), new Vector3(1.4f, 0.08f, 0.8f), _woodMat);
            Box("TableLeg1", root, new Vector3(-0.05f, 0.11f, 0.32f), new Vector3(0.07f, 0.22f, 0.07f), _woodMat);
            Box("TableLeg2", root, new Vector3(-1.15f, 0.11f, 0.32f), new Vector3(0.07f, 0.22f, 0.07f), _woodMat);
            Box("TableLeg3", root, new Vector3(-0.05f, 0.11f, 0.88f), new Vector3(0.07f, 0.22f, 0.07f), _woodMat);
            Box("TableLeg4", root, new Vector3(-1.15f, 0.11f, 0.88f), new Vector3(0.07f, 0.22f, 0.07f), _woodMat);

            // سجادة
            Box("Rug", root, new Vector3(-0.6f, 0.012f, 0.6f), new Vector3(3.2f, 0.02f, 2.2f),
                MakeMat(new Color(0.22f, 0.15f, 0.14f), 0.05f, 0f), false);

            // وحدة تلفزيون + شاشة
            Box("TVUnit", root, new Vector3(2.4f, 0.28f, 0.6f), new Vector3(0.5f, 0.56f, 1.8f), _woodMat);
            Box("TVPanel", root, new Vector3(2.1f, 1.15f, 0.6f), new Vector3(0.08f, 0.72f, 1.35f), _metalMat);

            // رف كتب
            Box("Bookshelf", root, new Vector3(3.7f, 0.9f, -2.2f), new Vector3(0.4f, 1.8f, 1.4f), _woodMat);

            // شباك + إطلالة
            Box("WindowGlass", root, new Vector3(0f, 1.55f, 3.02f), new Vector3(3f, 2f, 0.03f),
                MakeMat(new Color(0.05f, 0.07f, 0.10f), 0.9f, 0f), false);

            // لمبة أرضية
            Box("FloorLampPole", root, new Vector3(-3.5f, 0.75f, -2.4f), new Vector3(0.06f, 1.5f, 0.06f), _metalMat);
            Box("FloorLampShade", root, new Vector3(-3.5f, 1.62f, -2.4f), new Vector3(0.42f, 0.34f, 0.42f), _glowMat, false);

            // باب الشقة
            Box("FrontDoor", root, new Vector3(0f, 1.05f, -2.94f), new Vector3(1.55f, 2.1f, 0.08f), _woodMat);
        }

        void BuildBedroom()
        {
            var root = new GameObject("Bedroom_Furniture").transform;
            root.SetParent(transform, false);

            Box("Bed_Base", root, new Vector3(7.0f, 0.28f, -1.6f), new Vector3(2.2f, 0.45f, 1.8f), _woodMat);
            Box("Mattress", root, new Vector3(7.0f, 0.60f, -1.6f), new Vector3(2.05f, 0.24f, 1.7f), _fabricMat);
            Box("Headboard", root, new Vector3(8.15f, 0.75f, -1.6f), new Vector3(0.12f, 1.1f, 1.8f), _woodMat);
            Box("Nightstand", root, new Vector3(7.6f, 0.28f, -0.35f), new Vector3(0.6f, 0.56f, 0.5f), _woodMat);

            Box("Wardrobe", root, new Vector3(4.5f, 1.05f, -2.3f), new Vector3(0.65f, 2.1f, 1.2f), _woodMat);
            Box("BedroomWindow", root, new Vector3(6.5f, 1.6f, 0.98f), new Vector3(2.2f, 1.7f, 0.03f),
                MakeMat(new Color(0.05f, 0.07f, 0.10f), 0.9f, 0f), false);
        }

        void BuildKitchen()
        {
            var root = new GameObject("Kitchen_Furniture").transform;
            root.SetParent(transform, false);

            Box("Counter", root, new Vector3(-6f, 0.45f, -2.55f), new Vector3(3.4f, 0.9f, 0.62f), _woodMat);
            Box("CounterTop", root, new Vector3(-6f, 0.93f, -2.55f), new Vector3(3.5f, 0.06f, 0.68f), _metalMat);
            Box("Fridge", root, new Vector3(-7.5f, 0.9f, -1.0f), new Vector3(0.7f, 1.8f, 0.7f), _metalMat);
            Box("UpperCabinets", root, new Vector3(-6f, 1.95f, -2.72f), new Vector3(3.2f, 0.7f, 0.35f), _woodMat);
        }

        void BuildCityOutside()
        {
            var cityGo = new GameObject("Cairo_Outside");
            cityGo.transform.SetParent(transform, false);
            var city = cityGo.transform;

            // شريط أبنية بعيدة
            for (int i = 0; i < 14; i++)
            {
                float x = -14f + i * 2.1f;
                float h = 4f + ((i * 37) % 9);
                float z = 12f + ((i * 13) % 6);

                Box($"Building_{i}", city, new Vector3(x, h * 0.5f, z),
                    new Vector3(1.8f, h, 1.8f), _cityMat, false);
            }

            // شبابيك مضيئة (قليل)
            var litMat = MakeMat(new Color(1f, 0.85f, 0.55f), 0.1f, 0f);
            litMat.EnableKeyword("_EMISSION");
            litMat.SetColor("_EmissionColor", new Color(1f, 0.78f, 0.42f) * 2.2f);

            for (int i = 0; i < 22; i++)
            {
                float x = -13f + ((i * 17) % 26);
                float y = 1.2f + ((i * 7) % 7);
                Box($"CityLight_{i}", city, new Vector3(x, y, 11.5f + (i % 3) * 2f),
                    new Vector3(0.35f, 0.3f, 0.05f), litMat, false);
            }

            // هلال باهت فوق المدينة
            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon";
            moon.transform.SetParent(city, false);
            moon.transform.localPosition = new Vector3(9f, 16f, 28f);
            moon.transform.localScale = Vector3.one * 2.4f;
            var moonMat = MakeMat(new Color(0.85f, 0.88f, 0.95f), 0f, 0f);
            moonMat.EnableKeyword("_EMISSION");
            moonMat.SetColor("_EmissionColor", new Color(0.7f, 0.75f, 0.9f) * 1.4f);
            moon.GetComponent<MeshRenderer>().sharedMaterial = moonMat;
            Destroy(moon.GetComponent<Collider>());
        }

        // ────────────────────────── نقاط مرجعية ──────────────────────────

        void CreateAnchors()
        {
            PlayerSpawn = NewAnchor("Spawn_Adam", new Vector3(-0.6f, 0f, -1.4f));
            PhoneAnchor = NewAnchor("Anchor_Phone", new Vector3(-0.6f, 0.9f, 0.6f));
            PhotoAnchor = NewAnchor("Anchor_Photo", new Vector3(3.72f, 1.55f, -2.2f));
            EnvelopeAnchor = NewAnchor("Anchor_Envelope", new Vector3(0f, 0.06f, -2.7f));
            DoorAnchor = NewAnchor("Anchor_Door", new Vector3(0f, 1f, -2.94f));

            // الصورة العائلية على الرف
            var frame = Box("FamilyPhoto", PhotoAnchor, new Vector3(0f, 0f, 0.24f),
                new Vector3(0.42f, 0.34f, 0.03f),
                MakeMat(new Color(0.55f, 0.48f, 0.38f), 0.3f, 0.1f), false);
            frame.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            // التليفون على الطربيزة
            Box("Phone", PhoneAnchor, Vector3.zero, new Vector3(0.16f, 0.02f, 0.30f),
                MakeMat(new Color(0.06f, 0.06f, 0.07f), 0.8f, 0.3f), false);
        }

        Transform NewAnchor(string name, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }
    }
}
