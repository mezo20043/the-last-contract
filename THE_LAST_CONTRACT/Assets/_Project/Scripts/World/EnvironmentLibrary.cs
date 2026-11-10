using System.Collections.Generic;
using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// مواقع الفصل الأول — كل موقع بيئة مستقلة مبنيّة بالكامل (أرضية/حيطان/سقف/
    /// أبواب/شبابيك/أثاث/إضاءة/نقاط وقوف/نقاط كاميرا/تفاعلات) ومش مجرد تغيير
    /// خلفية جوه شقة آدم.
    ///
    ///   Café      — مقهى مدينة نصر (مراد)
    ///   Newsroom  — مكتب صحفي وسط البلد (ليلى)
    ///   Archive   — مبنى الأرشيف القديم
    ///   Police    — مكتب التحقيقات (ياسين)
    ///   Office    — مكتب يوسف الفاخر
    ///
    /// كل موقع بيتبني في إحداثيات عالم منفصلة (x = 200, 400, …) عشان مفيش
    /// تراكب، والانتقال بينهم بيحصل بتحريك اللاعب لنقطة الظهور بتاعة الموقع.
    /// الخامات CC0 من Poly Haven (MaterialLibrary) والأثاث CC0 (PropLibrary).
    /// </summary>
    public static partial class EnvironmentLibrary
    {
        public enum Location { Apartment, Cafe, Newsroom, Archive, Police, Office }

        public class LocationSet
        {
            public Location Kind;
            public Transform Root;
            public Vector3 Spawn;
            public float SpawnYaw;

            /// <summary>مكان وقوف/جلوس الشخصيات التانية (مفتاح باسم الشخصية في JSON).</summary>
            public readonly Dictionary<string, Vector3> CastSpots = new Dictionary<string, Vector3>();
            public readonly Dictionary<string, float> CastYaw = new Dictionary<string, float>();
            public readonly Dictionary<string, bool> CastSeated = new Dictionary<string, bool>();

            /// <summary>مرساة كاميرا لكل نوع لقطة (wide / desk / window).</summary>
            public readonly Dictionary<string, Vector3> CameraAnchors = new Dictionary<string, Vector3>();

            /// <summary>أماكن الأدلة/التفاعل (ظرف، ملف، هاتف، صورة…).</summary>
            public readonly Dictionary<string, Vector3> Props = new Dictionary<string, Vector3>();

            /// <summary>مراسي الكاميرا للأدلة (Transform عشان اللقطات القريبة).</summary>
            public readonly Dictionary<string, Transform> PropAnchors = new Dictionary<string, Transform>();

            public string DisplayName;

            /// <summary>إضاءة محيطة خاصة بالموقع (كل مكان ليه مزاج ضوء مختلف).</summary>
            public Color Ambient = new Color(0.10f, 0.10f, 0.11f);

            // ── الخارج (PHASE 4.2) ──
            /// <summary>جذر البناء الخارجي (واجهة + رصيف + شارع).</summary>
            public Transform Exterior;
            /// <summary>نقطة وقوف اللاعب بره المبنى (على الرصيف قدام الباب).</summary>
            public Vector3 ExteriorSpawn;
            public float ExteriorYaw;
            /// <summary>موضع مركز الواجهة في العالم (للكاميرا الخارجية).</summary>
            public Vector3 FacadeCenter;
            public Vector3 DoorLookAt;

            // ── بيانات هندسية للاختبار الآلي (تصادم/أبواب) ──
            public Vector2 RoomSize;
            public float RoomHeight;
            public DoorSpec Door;
            public Vector3 DoorCenter;        // في العالم
            public Vector3[] FurnitureProbes = new Vector3[0];
        }

        // ────────────────────────── الاختيار حسب المشهد ──────────────────────────

        public static Location LocationForAct(string act)
        {
            switch (act)
            {
                case "scene03": return Location.Cafe;
                case "scene04": return Location.Newsroom;
                case "scene05": return Location.Archive;
                case "scene06": return Location.Police;
                case "scene08": return Location.Office;
                case "ending_a": return Location.Newsroom;
                case "ending_b": return Location.Police;
                case "ending_c": return Location.Apartment;
                default: return Location.Apartment;   // 01 · 02 · 07
            }
        }

        static Vector3 OriginOf(Location loc)
        {
            switch (loc)
            {
                case Location.Cafe:     return new Vector3(200f, 0f, 0f);
                case Location.Newsroom: return new Vector3(400f, 0f, 0f);
                case Location.Archive:  return new Vector3(600f, 0f, 0f);
                case Location.Police:   return new Vector3(800f, 0f, 0f);
                case Location.Office:   return new Vector3(1000f, 0f, 0f);
                default:                return Vector3.zero;
            }
        }

        // ────────────────────────── البناء ──────────────────────────

        /// <summary>آخر موقع اتبنى — بستخدمه أدوات الاختبار للوصول لكاميرا/جذور المكان.</summary>
        public static LocationSet Current;

        public static LocationSet Build(Location loc, Transform parent)
        {
            var root = new GameObject("LOC_" + loc).transform;
            root.SetParent(parent, false);
            root.position = OriginOf(loc);

            var set = new LocationSet { Kind = loc, Root = root };
            set.Spawn = root.position + new Vector3(0f, 0.1f, -3.2f);
            set.SpawnYaw = 0f;

            switch (loc)
            {
                case Location.Cafe:     BuildCafe(root, set); break;
                case Location.Newsroom: BuildNewsroom(root, set); break;
                case Location.Archive:  BuildArchive(root, set); break;
                case Location.Police:   BuildPolice(root, set); break;
                case Location.Office:   BuildOffice(root, set); break;
                default:                BuildFallback(root, set); break;
            }

            // مرساة Transform لكل دليل عشان الكاميرا تقدر تعمل عليها close-up
            foreach (var kv in set.Props)
            {
                var go = new GameObject("PROP_" + kv.Key);
                go.transform.SetParent(root, false);
                go.transform.position = kv.Value;
                set.PropAnchors[kv.Key] = go.transform;
            }

            Current = set;
            Debug.Log($"[TLC/Env] {set.DisplayName}: {set.CastSpots.Count} نقطة وقوف · " +
                      $"{set.Props.Count} نقطة دليل · {set.CameraAnchors.Count} مرساة كاميرا");

            return set;
        }

        // ─────────────── مقهى مدينة نصر ───────────────
        static void BuildCafe(Transform root, LocationSet set)
        {
            set.DisplayName = "مقهى مدينة نصر";
            set.Ambient = new Color(0.16f, 0.13f, 0.09f);   // أصفر دافي زي لمبات المقهى

            var door = new DoorSpec(Side.South, 0f, 1.5f, 2.35f);
            var window = new WindowSpec(Side.South, 4.0f, 3.6f, 1.6f, 0.95f);
            Room(root, "Cafe", new Vector2(13f, 9f), 3.1f, "wood_floor", "plaster_wall", "wood_dark",
                 door, window);
            WindowGlass(root, window, new Vector3(4.0f, 1.75f, -4.45f), 3.6f, 1.6f);
            Doorway(root, door, new Vector3(0f, 0f, -4.4f), "wood_dark");
            set.RoomSize = new Vector2(13f, 9f); set.RoomHeight = 3.1f; set.Door = door;
            set.DoorCenter = root.position + new Vector3(0f, 0f, -4.4f);
            BuildExterior(root, set, door, 13f, 9f, ExteriorStyle.Cafe);

            // بار المقهى + أرفف
            Box(root, "BarCounter", new Vector3(-4.6f, 0.55f, 3.1f), new Vector3(3.4f, 1.1f, 0.75f), "wood_dark");
            Box(root, "BarTop", new Vector3(-4.6f, 1.14f, 3.1f), new Vector3(3.6f, 0.08f, 0.9f), "metal_brushed");
            Box(root, "BackShelf", new Vector3(-4.6f, 1.9f, 3.85f), new Vector3(3.2f, 1.6f, 0.25f), "wood_dark");

            // طاولات وكراسي (أصول CC0)
            TableSet(root, new Vector3(-2.2f, 0f, -1.6f), 3);
            TableSet(root, new Vector3(1.9f, 0f, -1.4f), 3);
            TableSet(root, new Vector3(4.4f, 0f, 1.6f), 2);
            PropLibrary.Spawn("bar_chair_round_01", root, new Vector3(-3.4f, 0f, 2.7f), 30f);
            PropLibrary.Spawn("bar_chair_round_01", root, new Vector3(-5.4f, 0f, 2.7f), -20f);

            // نبات/ديكور بسيط على الحيط
            Box(root, "MenuBoard", new Vector3(5.6f, 1.7f, 3.9f), new Vector3(1.6f, 1.1f, 0.08f), "wood_dark");

            WarmLights(root, new Vector3(0f, 2.85f, 0f), 3, 3.4f);

            // كرسي حقيقي تحت مراد — القعود من غير كرسي بيبان معلّق في الهوا
            PropLibrary.Spawn("GreenChair_01", root, new Vector3(1.9f, 0f, -2.55f), 180f);

            set.CastSpots["murad"] = root.position + new Vector3(1.9f, 0f, -2.55f);
            set.CastYaw["murad"] = 180f;               // قاعد قدام آدم
            set.CastSeated["murad"] = true;

            set.CameraAnchors["wide"] = root.position + new Vector3(-5.2f, 2.3f, -3.4f);
            set.CameraAnchors["desk"] = root.position + new Vector3(1.5f, 1.6f, -0.4f);
            set.CameraAnchors["window"] = root.position + new Vector3(0.5f, 1.8f, 3.6f);

            set.Props["photo"] = root.position + new Vector3(1.9f, 0.85f, -1.4f);   // الصورة على الطاولة
            set.Props["note"] = root.position + new Vector3(-2.2f, 0.85f, -1.6f);   // ورقة مراد
            set.Props["phone"] = root.position + new Vector3(0f, 1f, -3.2f);
        }

        // ─────────────── مكتب ليلى الصحفي ───────────────
        static void BuildNewsroom(Transform root, LocationSet set)
        {
            set.DisplayName = "مكتب الصحافة — وسط البلد";
            set.Ambient = new Color(0.15f, 0.16f, 0.18f);   // نيوترال نهار

            var door = new DoorSpec(Side.North, -4.4f, 1.3f, 2.2f);
            var window = new WindowSpec(Side.East, 0f, 4.2f, 1.5f, 1.0f);   // جنب المبنى
            Room(root, "Newsroom", new Vector2(12f, 9f), 3.0f, "wood_floor", "plaster_wall", "wood_dark",
                 door, window);
            WindowGlass(root, window, new Vector3(5.95f, 1.75f, 0f), 4.2f, 1.5f);
            Doorway(root, door, new Vector3(-4.4f, 0f, 4.4f), "wood_dark");
            set.RoomSize = new Vector2(12f, 9f); set.RoomHeight = 3.0f; set.Door = door;
            set.DoorCenter = root.position + new Vector3(-4.4f, 0f, 4.4f);
            BuildExterior(root, set, door, 12f, 9f, ExteriorStyle.Newsroom);

            // مكاتب الصحفيين
            DeskSet(root, new Vector3(-2.4f, 0f, 1.2f), 0f);
            DeskSet(root, new Vector3(1.4f, 0f, 1.2f), 0f);
            DeskSet(root, new Vector3(1.4f, 0f, -1.8f), 0f);

            // أرشيف ورقي على الحيطة + خزائن
            for (int i = 0; i < 3; i++)
                PropLibrary.Spawn("Shelf_01", root, new Vector3(5.2f, 0f, -2.6f + i * 2.1f), -90f);
            PropLibrary.Spawn("GothicCabinet_01", root, new Vector3(-5.2f, 0f, -1.8f), 90f);

            // لوحة تحقيقات على الحيطة (كادر)
            Box(root, "PinBoard", new Vector3(0f, 1.85f, 4.35f), new Vector3(2.6f, 1.4f, 0.06f), "fabric_dark");

            NeutralLights(root, new Vector3(0f, 2.75f, 0f), 3, 3.6f);

            set.CastSpots["laila"] = root.position + new Vector3(-1.1f, 0f, 0.4f);
            set.CastYaw["laila"] = 160f;
            set.CastSeated["laila"] = false;

            set.CameraAnchors["wide"] = root.position + new Vector3(-4.8f, 2.2f, -3.6f);
            set.CameraAnchors["desk"] = root.position + new Vector3(-0.6f, 1.6f, 2.4f);
            set.CameraAnchors["window"] = root.position + new Vector3(0f, 1.8f, 4.2f);

            set.Props["photos"] = root.position + new Vector3(-1.1f, 0.85f, 0.4f);
            set.Props["file"] = root.position + new Vector3(-2.4f, 0.85f, 1.2f);
            set.Props["log"] = root.position + new Vector3(1.4f, 0.85f, 1.2f);
        }

        // ─────────────── مبنى الأرشيف القديم ───────────────
        static void BuildArchive(Transform root, LocationSet set)
        {
            set.DisplayName = "مبنى الأرشيف القديم";
            set.Ambient = new Color(0.07f, 0.09f, 0.09f);   // بارد وخافت

            var door = new DoorSpec(Side.South, 0f, 1.6f, 2.5f);
            var window = new WindowSpec(Side.East, 0f, 4.0f, 0.9f, 2.1f);
            Room(root, "Archive", new Vector2(13f, 11f), 3.4f, "concrete", "plaster_wall", "concrete",
                 door, window);
            WindowGlass(root, window, new Vector3(6.45f, 2.55f, 0f), 4.0f, 0.9f);
            Doorway(root, door, new Vector3(0f, 0f, -5.4f), "metal_brushed");
            set.RoomSize = new Vector2(13f, 11f); set.RoomHeight = 3.4f; set.Door = door;
            set.DoorCenter = root.position + new Vector3(0f, 0f, -5.4f);
            BuildExterior(root, set, door, 13f, 11f, ExteriorStyle.Archive);

            // صفوف أرفف معدنية
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 4; i++)
                    PropLibrary.Spawn("Shelf_01", root,
                        new Vector3(-4.2f + row * 4.4f, 0f, -3.0f + i * 2.0f), 90f, 1f,
                        new Color(0.42f, 0.40f, 0.37f));   // أرفف معدنية داكنة (مش بيضاء)

            // خزائن جانبية وصندوق طاولة
            PropLibrary.Spawn("GothicCabinet_01", root, new Vector3(4.9f, 0f, 4.3f), -135f);
            PropLibrary.Spawn("WoodenTable_03", root, new Vector3(-3.4f, 0f, -3.9f), 25f);   // بعيد عن الباب

            // ممر بين الأرفف
            Box(root, "CorridorMark", new Vector3(0f, 0.02f, 0.4f), new Vector3(1.6f, 0.02f, 8.4f), "metal_brushed");

            FlickerLights(root, new Vector3(0f, 3.1f, 0f), 2, 5.2f);

            set.CastSpots["adam"] = root.position + new Vector3(0f, 0f, -1.2f);
            // كاميرا عامة من ركن علوي بtbين صفوف الأرفف والممر
            set.CameraAnchors["wide"] = root.position + new Vector3(-5.3f, 2.7f, -4.9f);
            set.CameraAnchors["desk"] = root.position + new Vector3(0f, 1.6f, -2.2f);
            set.CameraAnchors["window"] = root.position + new Vector3(5.0f, 2.2f, -4.4f);

            set.Props["file"] = root.position + new Vector3(-4.2f, 1.2f, -3.0f);
            set.Props["photo"] = root.position + new Vector3(-4.2f, 1.2f, -1.0f);
            set.Props["contract"] = root.position + new Vector3(0.2f, 0.9f, -4.2f);
            set.Props["log"] = root.position + new Vector3(4.9f, 1.1f, 4.3f);
            set.Props["phone"] = root.position + new Vector3(0f, 1.15f, -4.2f);
            set.Props["wall_photo"] = root.position + new Vector3(5.6f, 1.8f, -1.2f);
        }

        // ─────────────── مكتب التحقيقات ───────────────
        static void BuildPolice(Transform root, LocationSet set)
        {
            set.DisplayName = "مكتب التحقيقات";
            set.Ambient = new Color(0.11f, 0.13f, 0.16f);   // فلورسنت بارد

            var door = new DoorSpec(Side.South, 0f, 1.25f, 2.2f);
            var window = new WindowSpec(Side.West, 0f, 2.2f, 1.1f, 1.25f);
            Room(root, "Police", new Vector2(8f, 7f), 2.9f, "tile_ceramic", "plaster_wall", "metal_brushed",
                 door, window);
            WindowGlass(root, window, new Vector3(-3.95f, 1.8f, 0f), 2.2f, 1.1f);
            Doorway(root, door, new Vector3(0f, 0f, -3.4f), "metal_brushed");
            set.RoomSize = new Vector2(8f, 7f); set.RoomHeight = 2.9f; set.Door = door;
            set.DoorCenter = root.position + new Vector3(0f, 0f, -3.4f);
            BuildExterior(root, set, door, 8f, 7f, ExteriorStyle.Police);

            // طاولة التحقيق + كرسيين
            PropLibrary.Spawn("WoodenTable_03", root, new Vector3(0f, 0f, 0.6f), 0f);
            PropLibrary.Spawn("WoodenChair_01", root, new Vector3(0f, 0f, -0.7f), 180f);
            PropLibrary.Spawn("WoodenChair_01", root, new Vector3(0f, 0f, 1.9f), 0f);
            PropLibrary.Spawn("desk_lamp_arm_01", root, new Vector3(1.1f, 0.78f, 0.6f), -90f, 0.85f);

            PropLibrary.Spawn("Shelf_01", root, new Vector3(-3.3f, 0f, 2.6f), 90f);
            Box(root, "FileBoxes", new Vector3(3.2f, 0.35f, 2.9f), new Vector3(1.2f, 0.7f, 0.9f), "wood_dark");

            CoolLights(root, new Vector3(0f, 2.6f, 0.6f), 1, 3.2f);

            set.CastSpots["yassin"] = root.position + new Vector3(0f, 0f, 2.1f);
            set.CastYaw["yassin"] = 180f;
            set.CastSeated["yassin"] = true;

            set.CameraAnchors["wide"] = root.position + new Vector3(-3.4f, 2.2f, -2.6f);
            set.CameraAnchors["desk"] = root.position + new Vector3(1.6f, 1.7f, 1.4f);

            set.Props["file"] = root.position + new Vector3(0.3f, 0.82f, 0.6f);
            set.Props["photo"] = root.position + new Vector3(-0.3f, 0.82f, 0.6f);
        }

        // ─────────────── مكتب يوسف ───────────────
        static void BuildOffice(Transform root, LocationSet set)
        {
            set.DisplayName = "مكتب يوسف — رجل الأعمال";
            set.Ambient = new Color(0.18f, 0.14f, 0.10f);   // فخم دافي

            var door = new DoorSpec(Side.North, -5.2f, 1.5f, 2.5f);
            var window = new WindowSpec(Side.West, 1.0f, 5.5f, 2.2f, 0.85f);   // بانوراما على الجنب
            Room(root, "Office", new Vector2(14f, 11f), 3.6f, "wood_floor", "plaster_wall", "wood_dark",
                 door, window);
            WindowGlass(root, window, new Vector3(-6.95f, 1.95f, 1.0f), 5.5f, 2.2f);
            Doorway(root, door, new Vector3(-5.2f, 0f, 5.2f), "wood_dark");
            set.RoomSize = new Vector2(14f, 11f); set.RoomHeight = 3.6f; set.Door = door;
            set.DoorCenter = root.position + new Vector3(-5.2f, 0f, 5.2f);
            BuildExterior(root, set, door, 14f, 11f, ExteriorStyle.Office);

            // ركن الضيافة
            PropLibrary.Spawn("Sofa_01", root, new Vector3(-3.2f, 0f, 2.2f), 160f);
            PropLibrary.Spawn("ArmChair_01", root, new Vector3(-1.0f, 0f, 3.2f), 200f);
            PropLibrary.Spawn("CoffeeTable_01", root, new Vector3(-2.2f, 0f, 1.0f), 0f);

            // مكتب يوسف الكبير
            Box(root, "DeskBase", new Vector3(3.0f, 0.4f, 1.4f), new Vector3(3.2f, 0.8f, 1.4f), "wood_dark");
            Box(root, "DeskTop", new Vector3(3.0f, 0.83f, 1.4f), new Vector3(3.4f, 0.08f, 1.55f), "metal_brushed");
            PropLibrary.Spawn("WoodenChair_01", root, new Vector3(3.0f, 0f, 2.9f), 180f, 1.05f);
            PropLibrary.Spawn("desk_lamp_arm_01", root, new Vector3(4.1f, 0.88f, 1.3f), -90f, 0.9f);

            // خزائن وأرفف فخمة
            for (int i = 0; i < 3; i++)
                PropLibrary.Spawn("GothicCabinet_01", root, new Vector3(-5.6f, 0f, -2.4f + i * 2.3f), 90f);
            for (int i = 0; i < 2; i++)
                PropLibrary.Spawn("Shelf_01", root, new Vector3(5.6f, 0f, -1.6f + i * 2.4f), -90f);

            WarmLights(root, new Vector3(0f, 3.2f, 0f), 4, 4.0f);

            set.CastSpots["yusuf"] = root.position + new Vector3(3.0f, 0f, 2.6f);
            set.CastYaw["yusuf"] = 180f;
            set.CastSeated["yusuf"] = true;

            set.CameraAnchors["wide"] = root.position + new Vector3(-4.6f, 2.6f, -3.8f);
            set.CameraAnchors["desk"] = root.position + new Vector3(3.0f, 1.6f, 0.2f);
            set.CameraAnchors["window"] = root.position + new Vector3(0f, 2.0f, 4.6f);

            set.Props["office_photo"] = root.position + new Vector3(3.0f, 0.9f, 1.2f);
            set.Props["note"] = root.position + new Vector3(-2.2f, 0.45f, 1.0f);
        }

        static void BuildFallback(Transform root, LocationSet set)
        {
            set.DisplayName = "موقع احتياطي";
            Room(root, "Fallback", new Vector2(8f, 8f), 3f, "concrete", "plaster_wall", "concrete");
            NeutralLights(root, new Vector3(0f, 2.6f, 0f), 2, 4f);
            set.CameraAnchors["wide"] = root.position + new Vector3(-4f, 2.2f, -4f);
        }

        // ────────────────────────── الأثاث المركّب ──────────────────────────

        static void TableSet(Transform root, Vector3 pos, int chairs)
        {
            PropLibrary.Spawn("WoodenTable_01", root, pos, Random.Range(0f, 40f));
            float radius = 0.95f;
            for (int i = 0; i < chairs; i++)
            {
                float a = (i * Mathf.PI * 2f) / chairs + 0.4f;
                var cpos = pos + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
                PropLibrary.Spawn("WoodenChair_01", root, cpos, -a * Mathf.Rad2Deg + 90f);
            }
        }

        static void DeskSet(Transform root, Vector3 pos, float yaw)
        {
            PropLibrary.Spawn("SchoolDesk_01", root, pos, yaw);
            PropLibrary.Spawn("WoodenChair_01", root, pos + new Vector3(0f, 0f, -0.85f), yaw + 180f);
            PropLibrary.Spawn("desk_lamp_arm_01", root, pos + new Vector3(0.45f, 0.72f, 0.15f), yaw - 90f, 0.8f);
        }

        // ────────────────────────── الهيكل ──────────────────────────

        public enum Side { North, South, East, West }

        /// <summary>باب حقيقي: فتحة في الحيطة + عضادة + ضلفة مفتوحة على الجنب.</summary>
        public struct DoorSpec
        {
            public Side side;
            public float offset;     // إزاحة مركز الباب على محور الحيطة
            public float width;
            public float height;

            public DoorSpec(Side side, float offset, float width, float height)
            {
                this.side = side; this.offset = offset;
                this.width = width; this.height = height;
            }
        }

        /// <summary>شباك حقيقي: فتحة في الحيطة بعتبة (تحتها حيطة، وفوقها حيطة).</summary>
        public struct WindowSpec
        {
            public Side side;
            public float offset;
            public float width;
            public float height;
            public float sill;      // ارتفاع العتبة عن الأرض

            public WindowSpec(Side side, float offset, float width, float height, float sill)
            {
                this.side = side; this.offset = offset; this.width = width;
                this.height = height; this.sill = sill;
            }
        }

        enum Axis { North, South, East, West }

        /// <summary>
        /// غرفة بأربع حيطان — ولو فيه باب، الحيطة بتتقسم لثلاث أجزاء
        /// (شمال الباب · يمين الباب · عتب علوي) عشان **فتحة حقيقية** اللاعب
        /// يقدر يمشي منها، مش باب مرسوم على حيطة سد.
        /// </summary>
        static void Room(Transform root, string name, Vector2 size, float height,
                         string floorMat, string wallMat, string ceilMat,
                         DoorSpec? door = null, WindowSpec? window = null)
        {
            float w = size.x, d = size.y;

            Box(root, name + "_Floor", new Vector3(0f, -0.05f, 0f), new Vector3(w, 0.1f, d), floorMat);
            Box(root, name + "_Ceiling", new Vector3(0f, height, 0f), new Vector3(w, 0.1f, d), ceilMat);

            Wall(root, name + "_WallN", Axis.North, w, d, height, wallMat, door, window);
            Wall(root, name + "_WallS", Axis.South, w, d, height, wallMat, door, window);
            Wall(root, name + "_WallE", Axis.East,  w, d, height, wallMat, door, window);
            Wall(root, name + "_WallW", Axis.West,  w, d, height, wallMat, door, window);
        }

        static void Wall(Transform root, string name, Axis axis, float w, float d, float height,
                         string mat, DoorSpec? door, WindowSpec? window = null)
        {
            bool horizontal = axis == Axis.North || axis == Axis.South;
            float length = horizontal ? w : d;
            float z = axis == Axis.North ? d / 2f : axis == Axis.South ? -d / 2f : 0f;
            float x = axis == Axis.East ? w / 2f : axis == Axis.West ? -w / 2f : 0f;

            var center = new Vector3(x, height / 2f, z);
            var full = horizontal ? new Vector3(length, height, 0.2f) : new Vector3(0.2f, height, length);

            bool hasDoor = door.HasValue && SideOf(axis) == door.Value.side;
            bool hasWin = window.HasValue && SideOf(axis) == window.Value.side;

            if (!hasDoor && !hasWin)
            {
                Box(root, name, center, full, mat);
                return;
            }

            if (hasWin && !hasDoor)
            {
                var ws = window.Value;
                float wh = ws.height, sill = ws.sill;
                float aLen = (ws.offset - ws.width / 2f) + length / 2f;
                float bLen = (length / 2f) - (ws.offset + ws.width / 2f);

                if (aLen > 0.02f)
                {
                    float c = -length / 2f + aLen / 2f;
                    Box(root, name + "_A", horizontal ? new Vector3(c, height / 2f, z) : new Vector3(x, height / 2f, c),
                        horizontal ? new Vector3(aLen, height, 0.2f) : new Vector3(0.2f, height, aLen), mat);
                }
                if (bLen > 0.02f)
                {
                    float c = length / 2f - bLen / 2f;
                    Box(root, name + "_B", horizontal ? new Vector3(c, height / 2f, z) : new Vector3(x, height / 2f, c),
                        horizontal ? new Vector3(bLen, height, 0.2f) : new Vector3(0.2f, height, bLen), mat);
                }
                // تحت العتبة
                if (sill > 0.02f)
                    Box(root, name + "_Sill",
                        horizontal ? new Vector3(ws.offset, sill / 2f, z) : new Vector3(x, sill / 2f, ws.offset),
                        horizontal ? new Vector3(ws.width, sill, 0.2f) : new Vector3(0.2f, sill, ws.width), mat);
                // فوق الشباك
                float above = height - (sill + wh);
                if (above > 0.02f)
                    Box(root, name + "_Head",
                        horizontal ? new Vector3(ws.offset, sill + wh + above / 2f, z)
                                   : new Vector3(x, sill + wh + above / 2f, ws.offset),
                        horizontal ? new Vector3(ws.width, above, 0.2f) : new Vector3(0.2f, above, ws.width), mat);
                return;
            }

            var ds = door.Value;
            float half = ds.width / 2f, off = ds.offset;
            float leftLen = (off - half) + length / 2f;      // من الطرف للباب
            float rightLen = (length / 2f) - (off + half);

            if (leftLen > 0.02f)
            {
                float c = -length / 2f + leftLen / 2f;
                Box(root, name + "_L",
                    horizontal ? new Vector3(c, height / 2f, z) : new Vector3(x, height / 2f, c),
                    horizontal ? new Vector3(leftLen, height, 0.2f) : new Vector3(0.2f, height, leftLen), mat);
            }
            if (rightLen > 0.02f)
            {
                float c = length / 2f - rightLen / 2f;
                Box(root, name + "_R",
                    horizontal ? new Vector3(c, height / 2f, z) : new Vector3(x, height / 2f, c),
                    horizontal ? new Vector3(rightLen, height, 0.2f) : new Vector3(0.2f, height, rightLen), mat);
            }

            // العتب فوق الباب
            float lintelH = height - ds.height;
            if (lintelH > 0.02f)
            {
                Box(root, name + "_Lintel",
                    horizontal ? new Vector3(off, ds.height + lintelH / 2f, z)
                               : new Vector3(x, ds.height + lintelH / 2f, off + 0f),
                    horizontal ? new Vector3(ds.width, lintelH, 0.2f)
                               : new Vector3(0.2f, lintelH, ds.width), mat);
            }
        }

        static Side SideOf(Axis a)
        {
            switch (a)
            {
                case Axis.North: return Side.North;
                case Axis.South: return Side.South;
                case Axis.East:  return Side.East;
                default:         return Side.West;
            }
        }

        /// <summary>زجاج الشباك الحقيقي (شفاف) جوّه الفتحة + إطار.</summary>
        static void WindowGlass(Transform root, WindowSpec spec, Vector3 center, float w, float h)
        {
            bool horizontal = spec.side == Side.North || spec.side == Side.South;

            var glass = Box(root, "Window_Glass", center,
                horizontal ? new Vector3(w * 0.97f, h * 0.97f, 0.05f) : new Vector3(0.05f, h * 0.97f, w * 0.97f),
                "metal_brushed", new Color(0.72f, 0.82f, 0.92f));

            var mr = glass.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                var m = mr.sharedMaterial;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.85f);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.2f);
                m.renderQueue = 3000;   // يتصنّف كشفاف بعد الأجسام الصلبة
                m.SetInt("_Surface", 1);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            // شيش + عتبة
            Box(root, "Window_Sill", center + (horizontal
                    ? new Vector3(0f, -h / 2f - 0.06f, 0f) : new Vector3(0f, -h / 2f - 0.06f, 0f)),
                horizontal ? new Vector3(w + 0.3f, 0.1f, 0.34f) : new Vector3(0.34f, 0.1f, w + 0.3f), "wood_dark");
            for (int i = -1; i <= 1; i += 2)
                Box(root, "Window_Mullion" + i, center + (horizontal
                        ? new Vector3(w * 0.25f * i, 0f, 0f) : new Vector3(0f, 0f, w * 0.25f * i)),
                    horizontal ? new Vector3(0.07f, h, 0.1f) : new Vector3(0.1f, h, 0.07f), "wood_dark");
        }

        static void WindowWall(Transform root, float w, float d, float height, Axis axis, string outsideMat)
        {
            // فتحة شباك كبيرة + مشهد خارجي مضاء خلفها
            float z = axis == Axis.South ? -d / 2f : d / 2f;
            float sign = axis == Axis.South ? -1f : 1f;

            Box(root, "WindowGlass", new Vector3(0f, 1.7f, z), new Vector3(w * 0.62f, 1.8f, 0.05f), "metal_brushed");
            Box(root, "Outside", new Vector3(0f, height * 0.5f, z + sign * 3.5f),
                new Vector3(w * 1.6f, height * 1.4f, 0.2f), outsideMat);

            var lightGo = new GameObject("WindowLight");
            lightGo.transform.SetParent(root, false);
            lightGo.transform.position = new Vector3(0f, 2.2f, z - sign * 0.8f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Rectangle;
            light.range = 9f;
            light.intensity = LightUtil.Point(1.6f);
            light.color = new Color(0.55f, 0.66f, 0.95f);
        }

        static void PanoramaWall(Transform root, float w, float d, float height, string outsideMat)
        {
            Box(root, "PanoramaGlass", new Vector3(0f, 1.9f, d / 2f), new Vector3(w * 0.78f, 2.6f, 0.06f), "metal_brushed");
            Box(root, "PanoramaOutside", new Vector3(0f, height * 0.5f, d / 2f + 4f),
                new Vector3(w * 1.8f, height * 1.5f, 0.2f), outsideMat);
        }

        /// <summary>عضادة الباب + ضلفة مفتوحة 90° للجنب (الفتحة نفسها فاضية للمرور).</summary>
        static void Doorway(Transform root, DoorSpec spec, Vector3 pos, string mat)
        {
            bool horizontal = spec.side == Side.North || spec.side == Side.South;
            float w = spec.width, h = spec.height;

            // عمودين + عتب
            Box(root, "DoorFrameL",
                horizontal ? pos + new Vector3(-w / 2f - 0.09f, h / 2f, 0f) : pos + new Vector3(0f, h / 2f, -w / 2f - 0.09f),
                horizontal ? new Vector3(0.18f, h, 0.34f) : new Vector3(0.34f, h, 0.18f), mat);
            Box(root, "DoorFrameR",
                horizontal ? pos + new Vector3(w / 2f + 0.09f, h / 2f, 0f) : pos + new Vector3(0f, h / 2f, w / 2f + 0.09f),
                horizontal ? new Vector3(0.18f, h, 0.34f) : new Vector3(0.34f, h, 0.18f), mat);
            Box(root, "DoorLintel",
                pos + new Vector3(0f, h + 0.09f, 0f),
                horizontal ? new Vector3(w + 0.36f, 0.18f, 0.34f) : new Vector3(0.34f, 0.18f, w + 0.36f), mat);

            // الضلفة مفتوحة لجوه على الحيطة — مش سادة الفتحة
            Box(root, "DoorLeaf",
                horizontal ? pos + new Vector3(-w / 2f - 0.09f, h / 2f, 0.62f) : pos + new Vector3(0.62f, h / 2f, -w / 2f - 0.09f),
                horizontal ? new Vector3(0.06f, h * 0.96f, 1.1f) : new Vector3(1.1f, h * 0.96f, 0.06f), mat);
        }

        static GameObject Box(Transform root, string name, Vector3 pos, Vector3 size, string matKey)
            => Box(root, name, pos, size, matKey, Color.white);

        static GameObject Box(Transform root, string name, Vector3 pos, Vector3 size,
                              string matKey, Color tint)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(root, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;

            var mat = MaterialLibrary.Get(matKey);
            var mr = go.GetComponent<MeshRenderer>();
            if (mat != null)
            {
                // تايلينج حسب أكبر بُعدين للسطح — URP بيستخدم _BaseMap مش _MainTex
                var m = new Material(mat);
                float u = Mathf.Max(1f, Mathf.Max(size.x, size.z) * 0.5f);
                float v = Mathf.Max(1f, Mathf.Max(size.y, Mathf.Min(size.x, size.z)) * 0.5f);
                m.mainTextureScale = new Vector2(u, v);
                if (m.HasProperty("_BaseMap")) { m.SetTextureScale("_BaseMap", new Vector2(u, v)); }
                if (m.HasProperty("_MainTex")) { m.SetTextureScale("_MainTex", new Vector2(u, v)); }
                if (tint != Color.white)
                {
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
                    m.color = tint;
                }
                mr.sharedMaterial = m;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            return go;
        }

        // ────────────────────────── الإضاءة ──────────────────────────

        static void WarmLights(Transform root, Vector3 center, int count, float radius)
        {
            MakeLights(root, center, count, radius, new Color(1f, 0.78f, 0.52f), 1.25f, LightShadows.Soft);
        }

        static void NeutralLights(Transform root, Vector3 center, int count, float radius)
        {
            MakeLights(root, center, count, radius, new Color(0.92f, 0.94f, 1f), 1.2f, LightShadows.Soft);
        }

        static void CoolLights(Transform root, Vector3 center, int count, float radius)
        {
            MakeLights(root, center, count, radius, new Color(0.68f, 0.80f, 1f), 1.0f, LightShadows.Soft);
        }

        static void FlickerLights(Transform root, Vector3 center, int count, float radius)
        {
            MakeLights(root, center, count, radius, new Color(0.72f, 0.82f, 0.78f), 0.26f, LightShadows.Hard);
        }

        static void MakeLights(Transform root, Vector3 center, int count, float radius,
                               Color color, float intensity, LightShadows shadows)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (Mathf.PI * 2f / count) * i;
                var pos = center + new Vector3(Mathf.Cos(a) * radius * 0.55f, 0f, Mathf.Sin(a) * radius * 0.55f);

                var go = new GameObject("Light_" + i);
                go.transform.SetParent(root, false);
                go.transform.position = root.position + pos + Vector3.up * (center.y > 1f ? center.y : 2.8f);

                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = color;
                l.intensity = LightUtil.Point(intensity);
                l.range = radius * 1.9f;
                l.shadows = shadows;
                l.shadowStrength = 0.75f;
            }
        }

        /// <summary>يلغي إضاءة الموقع (لما ننتقل لموقع تاني).</summary>
        /// <summary>بيمسح المرجع لما اللعبة ترجع للشقة.</summary>
        public static void ClearCurrent() => Current = null;

        public static void SetActive(LocationSet set, bool active)
        {
            if (set?.Root != null) set.Root.gameObject.SetActive(active);
        }
    }
}
