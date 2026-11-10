using System.Collections.Generic;
using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// الجزء الخارجي من كل موقع (PHASE 4.2): **هندسة 3D حقيقية** مش خامة مسطحة —
    /// واجهة المبنى بعمق حقيقي، فتحات شبابيك غاطسة بعتبات ورفوف، مدخل بدرج
    /// ومظلة ولوحة، رصيف وبردورة وشارع بعلامات، أعمدة إضاءة، صناديق قمامة،
    /// سيارات واقفة، ومباني بعيدة بحجم فعلي ونوافذ مبنية.
    ///
    /// الباب الخارجي بيتحسب من نفس <see cref="DoorSpec"/> بتاع الداخل، ففتحة
    /// الواجهة على نفس محور باب الغرفة — يعني اللاعب بيمشي من الشارع للداخل
    /// في فتحة واحدة مفيهاش أي حيطة في النص.
    /// </summary>
    public static partial class EnvironmentLibrary
    {
        public enum ExteriorStyle { Cafe, Newsroom, Archive, Police, Office }

        struct ExteriorSkin
        {
            public string wall;        // خامة الواجهة
            public string trim;        // الكرانيش/الأحزمة
            public string pavement;    // الرصيف
            public string road;        // الأسفلت
            public Color wallTint;
            public Color roadTint;
            public Color lightColor;
            public int floors;         // عدد الأدوار فوق الأرضي
            public bool metalDoor;     // أبواب معدنية (أرشيف/تحقيقات)
        }

        static ExteriorSkin SkinOf(ExteriorStyle style)
        {
            switch (style)
            {
                case ExteriorStyle.Cafe:
                    return new ExteriorSkin
                    {
                        wall = "plaster_wall", trim = "wood_dark", pavement = "concrete",
                        road = "concrete", wallTint = new Color(0.86f, 0.72f, 0.55f),
                        roadTint = new Color(0.22f, 0.22f, 0.24f),
                        lightColor = new Color(1f, 0.79f, 0.52f), floors = 2
                    };
                case ExteriorStyle.Newsroom:
                    return new ExteriorSkin
                    {
                        wall = "plaster_wall", trim = "metal_brushed", pavement = "concrete",
                        road = "concrete", wallTint = new Color(0.74f, 0.77f, 0.82f),
                        roadTint = new Color(0.20f, 0.21f, 0.24f),
                        lightColor = new Color(0.85f, 0.92f, 1f), floors = 3
                    };
                case ExteriorStyle.Archive:
                    return new ExteriorSkin
                    {
                        wall = "concrete", trim = "concrete", pavement = "concrete",
                        road = "concrete", wallTint = new Color(0.62f, 0.64f, 0.63f),
                        roadTint = new Color(0.19f, 0.20f, 0.21f),
                        lightColor = new Color(0.72f, 0.84f, 0.80f), floors = 2, metalDoor = true
                    };
                case ExteriorStyle.Police:
                    return new ExteriorSkin
                    {
                        wall = "concrete", trim = "metal_brushed", pavement = "tile_ceramic",
                        road = "concrete", wallTint = new Color(0.70f, 0.73f, 0.78f),
                        roadTint = new Color(0.20f, 0.20f, 0.22f),
                        lightColor = new Color(0.88f, 0.94f, 1f), floors = 2, metalDoor = true
                    };
                default:
                    return new ExteriorSkin
                    {
                        wall = "plaster_wall", trim = "wood_dark", pavement = "concrete",
                        road = "concrete", wallTint = new Color(0.80f, 0.70f, 0.55f),
                        roadTint = new Color(0.21f, 0.21f, 0.23f),
                        lightColor = new Color(1f, 0.82f, 0.58f), floors = 4
                    };
            }
        }

        /// <summary>
        /// يبني الشارع والواجهة بره باب الموقع، ويحدّث نقاط الظهور/الكاميرا الخارجية.
        /// </summary>
        static void BuildExterior(Transform root, LocationSet set, DoorSpec door,
                                  float roomW, float roomD, ExteriorStyle style)
        {
            var skin = SkinOf(style);

            var ext = new GameObject("EXTERIOR").transform;
            ext.SetParent(root, false);
            ext.localPosition = Vector3.zero;
            set.Exterior = ext;

            // اتجاه الخروج من الباب
            bool north = door.side == Side.North;
            float outSign = north ? 1f : -1f;
            float wallZ = outSign * (roomD / 2f + 0.1f);      // السطح الخارجي لحيطة الغرفة
            float facadeDepth = 0.6f;                          // تخانة الواجهة (عمق حقيقي)
            float facadeZ = wallZ + outSign * facadeDepth / 2f;

            float facadeW = roomW + 3f;                        // الواجهة أعرض من الغرفة
            float floorH = 3.2f;                               // ارتفاع الدور
            float groundH = 3.4f;                              // الدور الأرضي
            float totalH = groundH + floorH * skin.floors;

            // ── الواجهة: أرضي + أدوار فوق ──
            BuildFacade(ext, skin, door, facadeW, facadeDepth, facadeZ, groundH, floorH, skin.floors, outSign);

            // ── حوائط جانبية بتربط الواجهة بالمبنى (عشان المبنى يبان له عمق) ──
            float sideDepth = outSign * 6f;
            Box(ext, "Ext_SideW", new Vector3(-facadeW / 2f, totalH / 2f, (wallZ + sideDepth) / 2f),
                new Vector3(0.4f, totalH, Mathf.Abs(sideDepth - wallZ)), skin.wall, skin.wallTint);
            Box(ext, "Ext_SideE", new Vector3(facadeW / 2f, totalH / 2f, (wallZ + sideDepth) / 2f),
                new Vector3(0.4f, totalH, Mathf.Abs(sideDepth - wallZ)), skin.wall, skin.wallTint);

            // ── سقف المبنى ──
            Box(ext, "Ext_Roof", new Vector3(0f, totalH, (wallZ + sideDepth) / 2f),
                new Vector3(facadeW + 0.4f, 0.35f, Mathf.Abs(sideDepth - wallZ) + 0.4f),
                skin.trim, new Color(0.55f, 0.55f, 0.58f));

            // ── المدخل: درجات + مظلة + لوحة ──
            BuildEntrance(ext, skin, door, facadeZ, outSign);

            // ── الرصيف والشارع ──
            BuildStreet(ext, skin, facadeZ, outSign, facadeW);

            // ── تفاصيل الشارع ──
            BuildStreetProps(ext, skin, facadeZ, outSign, facadeW);

            // ── مباني بعيدة بالحجم الفعلي ──
            BuildBackdropBlocks(ext, skin, facadeZ, outSign);

            // ── إضاءة خارجية ──
            float lampY = 4.1f;
            for (int i = -1; i <= 1; i++)
            {
                float lx = i * (facadeW / 2f - 0.6f);
                // عمود الإضاءة ممنوع يقف قصاد الباب — كان بيقفل المدخل فعليًا
                if (Mathf.Abs(lx - door.offset) < 2.4f) continue;
                StreetLamp(ext, skin, new Vector3(lx, 0f, facadeZ + outSign * 0.55f), outSign, lampY);
            }

            // إضاءة تعبئة للشارع — من غيرها الواجهات بتبان سودة في الليل
            for (int i = -1; i <= 1; i += 2)
                ExteriorLight(ext, skin, new Vector3(i * facadeW * 0.32f, 5.6f, facadeZ + outSign * 6.5f), 1.15f, 26f, false);
            ExteriorLight(ext, skin, new Vector3(door.offset, 3.2f, facadeZ + outSign * 2.2f), 1.6f, 9f, false);

            // نقطة ظهور اللاعب على الرصيف قدام الباب
            float doorX = door.offset;
            set.ExteriorSpawn = root.position + new Vector3(doorX, 0.05f, facadeZ + outSign * 2.4f);
            set.ExteriorYaw = north ? 180f : 0f;
            set.FacadeCenter = root.position + new Vector3(doorX, 1.8f, facadeZ + outSign * 1.2f);

            set.CameraAnchors["exterior"] = root.position + new Vector3(doorX - 7.5f, 3.4f, facadeZ + outSign * 8.5f);
            set.CameraAnchors["exterior_door"] = root.position + new Vector3(doorX - 3.4f, 2.3f, facadeZ + outSign * 7.0f);
            set.DoorLookAt = root.position + new Vector3(doorX, 1.55f, facadeZ + outSign * 0.2f);

            Debug.Log($"[TLC/Ext] {set.DisplayName}: واجهة {facadeW:0.#}×{totalH:0.#}م · " +
                      $"{skin.floors} أدوار · مدخل على {(north ? "الشمال" : "الجنوب")} · شارع + رصيف");
        }

        // ────────────────────────── الواجهة ──────────────────────────

        static void BuildFacade(Transform ext, ExteriorSkin skin, DoorSpec door,
                                float w, float depth, float z, float groundH, float floorH, int floors,
                                float outSign)
        {
            float half = door.width / 2f;
            float doorX = door.offset;
            float leftLen = (doorX - half) + w / 2f;
            float rightLen = (w / 2f) - (doorX + half);

            // الدور الأرضي: جزءين حول فتحة الباب + عتب
            if (leftLen > 0.05f)
                Box(ext, "Facade_GroundL", new Vector3(-w / 2f + leftLen / 2f, groundH / 2f, z),
                    new Vector3(leftLen, groundH, depth), skin.wall, skin.wallTint);
            if (rightLen > 0.05f)
                Box(ext, "Facade_GroundR", new Vector3(w / 2f - rightLen / 2f, groundH / 2f, z),
                    new Vector3(rightLen, groundH, depth), skin.wall, skin.wallTint);
            if (groundH - door.height > 0.05f)
                Box(ext, "Facade_GroundLintel", new Vector3(doorX, door.height + (groundH - door.height) / 2f, z),
                    new Vector3(door.width, groundH - door.height, depth), skin.wall, skin.wallTint);

            // حزام أفقي بين الأرضي والأدوار (بروز 12 سم — هندسة مش خامة)
            Box(ext, "Facade_Belt", new Vector3(0f, groundH + 0.09f, z),
                new Vector3(w + 0.3f, 0.18f, depth + 0.3f), skin.trim, new Color(0.42f, 0.40f, 0.38f));

            // الأدوار فوق: كل دور حيطة بفتحات شبابيك **حقيقية** (شرائح حوالين كل فتحة)
            int perFloor = Mathf.Clamp(Mathf.RoundToInt(w / 2.6f), 3, 6);
            float sill = 0.95f, winH = 1.65f;
            float spacing = w / perFloor;
            float wWin = spacing * 0.54f;

            for (int f = 0; f < floors; f++)
            {
                float y0 = groundH + f * floorH;

                // حيطة تحت الشبابيك
                Box(ext, $"Facade_F{f}_Base", new Vector3(0f, y0 + sill / 2f, z),
                    new Vector3(w, sill, depth), skin.wall, skin.wallTint);

                // حيطة فوق الشبابيك
                float topH = floorH - (sill + winH);
                if (topH > 0.02f)
                    Box(ext, $"Facade_F{f}_Top", new Vector3(0f, y0 + sill + winH + topH / 2f, z),
                        new Vector3(w, topH, depth), skin.wall, skin.wallTint);

                // أعمدة بين فتحات الشبابيك
                for (int i = 0; i <= perFloor; i++)
                {
                    float gapStart = -w / 2f + i * spacing + (i == 0 ? 0f : wWin / 2f);
                    float gapEnd = (i == perFloor) ? w / 2f : -w / 2f + (i + 1) * spacing - wWin / 2f;
                    float gw = gapEnd - gapStart;
                    if (gw <= 0.02f) continue;

                    Box(ext, $"Facade_F{f}_Col{i}", new Vector3(gapStart + gw / 2f, y0 + sill + winH / 2f, z),
                        new Vector3(gw, winH, depth), skin.wall, skin.wallTint);
                }

                for (int i = 0; i < perFloor; i++)
                {
                    float cx = -w / 2f + spacing * (i + 0.5f);
                    WindowUnit(ext, skin, new Vector3(cx, y0 + sill + winH / 2f, z), wWin, winH, depth, outSign);
                }
            }

            // كرانيش علوية
            float topY = groundH + floors * floorH;
            Box(ext, "Facade_Cornice", new Vector3(0f, topY + 0.16f, z),
                new Vector3(w + 0.6f, 0.32f, depth + 0.5f), skin.trim, new Color(0.38f, 0.36f, 0.34f));

            // قاعدة المبنى (البلينث) — بتتقسم حوالين الباب عشان الفتحة تفضل ممر فعلي
            float plinthZ = depth + 0.16f;
            if (leftLen > 0.05f)
                Box(ext, "Facade_PlinthL", new Vector3(-w / 2f + leftLen / 2f, 0.35f, z),
                    new Vector3(leftLen, 0.7f, plinthZ), skin.trim, new Color(0.30f, 0.29f, 0.28f));
            if (rightLen > 0.05f)
                Box(ext, "Facade_PlinthR", new Vector3(w / 2f - rightLen / 2f, 0.35f, z),
                    new Vector3(rightLen, 0.7f, plinthZ), skin.trim, new Color(0.30f, 0.29f, 0.28f));

            // شبابيك الدور الأرضي على يمين وشمال المدخل
            float gWinY = groundH * 0.55f;
            // (شبابيك الدور الأرضي بتتفتح في الشرائح يمين وشمال المدخل — بنكتفي بالإطار
            //  والزجاج على السطح هنا، والفتحات الحقيقية في الأدوار العلوية)
            if (leftLen > 2.4f)
                GroundWindow(ext, skin, new Vector3(-w / 2f + leftLen * 0.5f, gWinY, z + outSign * (depth / 2f)), leftLen * 0.4f);
            if (rightLen > 2.4f)
                GroundWindow(ext, skin, new Vector3(w / 2f - rightLen * 0.5f, gWinY, z + outSign * (depth / 2f)), rightLen * 0.4f);
        }

        /// <summary>
        /// وحدة شباك جوّه فتحة حقيقية في الواجهة: خلفية داكنة جوّه العمق + زجاج
        /// في مقدمة الفتحة + إطار وعتبة ورف. الفتحة نفسها مقطوعة من الحيطة،
        /// فمفيش أي تراكب أو زفايتنج.
        /// </summary>
        static void WindowUnit(Transform ext, ExteriorSkin skin, Vector3 center,
                               float w, float h, float depth, float outSign)
        {
            float front = center.z + outSign * (depth / 2f);

            // خلفية الفتحة (جوّه العمق) — بتدي إحساس إن الشباك له عمق حقيقي
            Box(ext, "Win_Back", new Vector3(center.x, center.y, center.z - outSign * (depth * 0.42f)),
                new Vector3(w * 0.99f, h * 0.99f, 0.06f), skin.wall, new Color(0.10f, 0.10f, 0.12f));

            // الزجاج
            var glass = Box(ext, "Win_Glass", new Vector3(center.x, center.y, front - outSign * 0.03f),
                new Vector3(w * 0.96f, h * 0.96f, 0.04f), "city_facade", new Color(0.52f, 0.62f, 0.76f));
            var mr = glass.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null)
            {
                var gm = mr.sharedMaterial;
                if (gm.HasProperty("_Metallic")) gm.SetFloat("_Metallic", 0.6f);
                if (gm.HasProperty("_Smoothness")) gm.SetFloat("_Smoothness", 0.75f);
            }

            // فاصل رأسي + إطار أعلى/أسفل + عتبة
            Box(ext, "Win_Mullion", new Vector3(center.x, center.y, front),
                new Vector3(0.06f, h * 0.96f, 0.07f), skin.trim, new Color(0.30f, 0.29f, 0.28f));
            Box(ext, "Win_FrameT", new Vector3(center.x, center.y + h / 2f + 0.06f, front),
                new Vector3(w + 0.2f, 0.12f, 0.12f), skin.trim, new Color(0.34f, 0.33f, 0.32f));
            Box(ext, "Win_Sill", new Vector3(center.x, center.y - h / 2f - 0.05f, front + outSign * 0.06f),
                new Vector3(w + 0.26f, 0.1f, 0.24f), skin.trim, new Color(0.40f, 0.39f, 0.37f));
        }

        /// <summary>شباك الدور الأرضي: زجاج + إطار بارز على سطح الواجهة (بدون فتحة).</summary>
        static void GroundWindow(Transform ext, ExteriorSkin skin, Vector3 front, float width)
        {
            float w = Mathf.Clamp(width, 1.1f, 2.4f), h = 1.5f;

            Box(ext, "GWin_Frame", new Vector3(front.x, front.y, front.z),
                new Vector3(w + 0.22f, h + 0.22f, 0.12f), skin.trim, new Color(0.34f, 0.33f, 0.32f));
            Box(ext, "GWin_Glass", new Vector3(front.x, front.y, front.z + Mathf.Sign(front.z) * 0.05f),
                new Vector3(w, h, 0.05f), "city_facade", new Color(0.52f, 0.62f, 0.76f));
            Box(ext, "GWin_Sill", new Vector3(front.x, front.y - h / 2f - 0.12f, front.z),
                new Vector3(w + 0.34f, 0.1f, 0.26f), skin.trim, new Color(0.40f, 0.39f, 0.37f));
            Box(ext, "GWin_Bar", new Vector3(front.x, front.y - h / 2f - 0.55f, front.z),
                new Vector3(w + 0.3f, 0.12f, 0.3f), "metal_brushed", new Color(0.26f, 0.27f, 0.28f));
        }

        // ────────────────────────── المدخل ──────────────────────────

        static void BuildEntrance(Transform ext, ExteriorSkin skin, DoorSpec door, float facadeZ, float outSign)
        {
            float x = door.offset, h = door.height, w = door.width;

            // عتبة مسطحة قدام الباب (ارتفاع ٤سم فقط) — المدخل لازم يفضل ممر فعلي
            Box(ext, "Ent_Threshold", new Vector3(x, -0.02f, facadeZ + outSign * 0.55f),
                new Vector3(w + 1.2f, 0.06f, 1.9f), skin.pavement, new Color(0.62f, 0.60f, 0.57f));

            // عضادة المدخل البارزة (برواز حجري حوالين الباب)
            Box(ext, "Ent_JambL", new Vector3(x - w / 2f - 0.22f, h / 2f + 0.1f, facadeZ + outSign * 0.14f),
                new Vector3(0.32f, h + 0.2f, 0.34f), skin.trim, new Color(0.46f, 0.44f, 0.41f));
            Box(ext, "Ent_JambR", new Vector3(x + w / 2f + 0.22f, h / 2f + 0.1f, facadeZ + outSign * 0.14f),
                new Vector3(0.32f, h + 0.2f, 0.34f), skin.trim, new Color(0.46f, 0.44f, 0.41f));
            Box(ext, "Ent_Head", new Vector3(x, h + 0.26f, facadeZ + outSign * 0.14f),
                new Vector3(w + 0.9f, 0.34f, 0.36f), skin.trim, new Color(0.46f, 0.44f, 0.41f));

            // ضلفة الباب الخارجي مفتوحة للخارج على الحيطة
            Box(ext, "Ent_DoorLeaf", new Vector3(x + w / 2f + 0.06f, h / 2f, facadeZ + outSign * 0.75f),
                new Vector3(0.08f, h * 0.96f, 1.35f),
                skin.metalDoor ? "metal_brushed" : "wood_dark",
                skin.metalDoor ? new Color(0.42f, 0.45f, 0.48f) : new Color(0.30f, 0.20f, 0.13f));

            // مظلة المدخل + عمودين
            float canopyY = h + 0.75f;
            Box(ext, "Ent_Canopy", new Vector3(x, canopyY, facadeZ + outSign * 0.95f),
                new Vector3(w + 1.8f, 0.16f, 2.0f), skin.trim, new Color(0.35f, 0.33f, 0.31f));
            Box(ext, "Ent_CanopyP1", new Vector3(x - (w / 2f + 0.7f), canopyY / 2f, facadeZ + outSign * 1.75f),
                new Vector3(0.1f, canopyY, 0.1f), "metal_brushed", new Color(0.28f, 0.29f, 0.30f));
            Box(ext, "Ent_CanopyP2", new Vector3(x + (w / 2f + 0.7f), canopyY / 2f, facadeZ + outSign * 1.75f),
                new Vector3(0.1f, canopyY, 0.1f), "metal_brushed", new Color(0.28f, 0.29f, 0.30f));

            // لوحة اسم المؤسسة فوق المظلة (جسم 3D بحروف مرفوعة)
            Box(ext, "Ent_SignBoard", new Vector3(x, h + 1.75f, facadeZ + outSign * 0.18f),
                new Vector3(w + 2.4f, 0.85f, 0.18f), "wood_dark", new Color(0.22f, 0.19f, 0.16f));
            for (int i = 0; i < 6; i++)
                Box(ext, "Ent_SignGlyph" + i,
                    new Vector3(x - 1.2f + i * 0.48f, h + 1.75f, facadeZ + outSign * 0.28f),
                    new Vector3(0.26f, 0.34f, 0.06f), skin.trim, new Color(0.85f, 0.78f, 0.55f));

            // لمبة المدخل
            ExteriorLight(ext, skin, new Vector3(x, h + 0.5f, facadeZ + outSign * 0.55f), 0.85f, 7f, true);
        }

        // ────────────────────────── الشارع ──────────────────────────

        static void BuildStreet(Transform ext, ExteriorSkin skin, float facadeZ, float outSign, float w)
        {
            float sidewalkStart = facadeZ + outSign * 0.05f;
            float sidewalkLen = 4.5f;
            float sidewalkEnd = sidewalkStart + outSign * sidewalkLen;
            float roadLen = 9f;
            float roadEnd = sidewalkEnd + outSign * roadLen;
            float totalLen = Mathf.Abs(roadEnd - sidewalkStart);
            float centerZ = (sidewalkStart + roadEnd) / 2f;
            float streetW = w + 34f;    // الشارع أطول من المبنى

            // الرصيف (سطحه عند y=0 زي أرضية المبنى بالظبط ⇒ مفيش تعثّر)
            Box(ext, "Street_Sidewalk", new Vector3(0f, -0.09f, (sidewalkStart + sidewalkEnd) / 2f),
                new Vector3(streetW, 0.18f, sidewalkLen), skin.pavement, new Color(0.58f, 0.57f, 0.55f));

            // بردورة
            Box(ext, "Street_Curb", new Vector3(0f, -0.16f, sidewalkEnd),
                new Vector3(streetW, 0.32f, 0.3f), "concrete", new Color(0.72f, 0.71f, 0.68f));

            // الأسفلت
            Box(ext, "Street_Road", new Vector3(0f, -0.24f, (sidewalkEnd + roadEnd) / 2f),
                new Vector3(streetW, 0.2f, roadLen), skin.road, skin.roadTint);

            // خط منتصف متقطّع
            for (int i = -6; i <= 6; i++)
                Box(ext, "Street_Line" + (i + 6),
                    new Vector3(i * 3.2f, -0.135f, sidewalkEnd + outSign * roadLen * 0.5f),
                    new Vector3(1.6f, 0.02f, 0.18f), "concrete", new Color(0.88f, 0.86f, 0.70f));

            // رصيف الجهة المقابلة
            Box(ext, "Street_FarSidewalk", new Vector3(0f, -0.09f, roadEnd + outSign * 2.2f),
                new Vector3(streetW, 0.18f, 4.4f), skin.pavement, new Color(0.52f, 0.51f, 0.49f));
            Box(ext, "Street_FarCurb", new Vector3(0f, -0.16f, roadEnd),
                new Vector3(streetW, 0.32f, 0.3f), "concrete", new Color(0.68f, 0.67f, 0.64f));

            Debug.Log($"[TLC/Ext] شارع: رصيف {sidewalkLen:0.#}م + أسفلت {roadLen:0.#}م · عرض {streetW:0.#}م");
        }

        static void BuildStreetProps(Transform ext, ExteriorSkin skin, float facadeZ, float outSign, float w)
        {
            float sx = facadeZ + outSign * 1.9f;          // خط منتصف الرصيف

            // حدايل حماية قدام المدخل
            for (int i = -1; i <= 1; i++)
            {
                if (i == 0) continue;
                Box(ext, "Prop_Bollard" + i, new Vector3(i * 2.4f, 0.35f, facadeZ + outSign * 2.6f),
                    new Vector3(0.16f, 0.7f, 0.16f), "metal_brushed", new Color(0.35f, 0.36f, 0.38f));
            }

            // صندوق قمامة
            Box(ext, "Prop_Bin", new Vector3(-w / 2f + 1.6f, 0.45f, sx),
                new Vector3(0.55f, 0.9f, 0.55f), "metal_brushed", new Color(0.26f, 0.30f, 0.27f));
            Box(ext, "Prop_BinLid", new Vector3(-w / 2f + 1.6f, 0.94f, sx),
                new Vector3(0.62f, 0.08f, 0.62f), "metal_brushed", new Color(0.20f, 0.22f, 0.21f));

            // أشتال زرع
            for (int i = 0; i < 2; i++)
            {
                float px = w / 2f - 2.2f + i * 1.6f;
                Box(ext, "Prop_Planter" + i, new Vector3(px, 0.28f, sx),
                    new Vector3(0.8f, 0.56f, 0.8f), "concrete", new Color(0.48f, 0.46f, 0.42f));
                Box(ext, "Prop_Plant" + i, new Vector3(px, 0.75f, sx),
                    new Vector3(0.6f, 0.62f, 0.6f), "fabric_dark", new Color(0.20f, 0.34f, 0.20f));
            }

            // عربية واقفة على الأسفلت
            float carZ = facadeZ + outSign * 7.4f;
            float carX = -w * 0.28f;
            Box(ext, "Car_Body", new Vector3(carX, 0.45f, carZ),
                new Vector3(4.1f, 0.75f, 1.8f), "metal_brushed", new Color(0.16f, 0.22f, 0.30f));
            Box(ext, "Car_Cabin", new Vector3(carX - 0.15f, 1.02f, carZ),
                new Vector3(2.1f, 0.62f, 1.62f), "metal_brushed", new Color(0.12f, 0.17f, 0.24f));
            for (int i = 0; i < 4; i++)
            {
                float wx = carX + (i < 2 ? 1.35f : -1.35f);
                float wz = carZ + (i % 2 == 0 ? 0.82f : -0.82f);
                Box(ext, "Car_Wheel" + i, new Vector3(wx, 0.33f, wz),
                    new Vector3(0.62f, 0.62f, 0.22f), "metal_brushed", new Color(0.07f, 0.07f, 0.08f));
            }
            ExteriorLight(ext, skin, new Vector3(carX + 2.0f, 0.7f, carZ), 0.25f, 4f, false);
        }

        static void BuildBackdropBlocks(Transform ext, ExteriorSkin skin, float facadeZ, float outSign)
        {
            // مباني الجهة المقابلة — كتل حقيقية بنوافذ مبنية (مش خامة مسطحة)
            float baseZ = facadeZ + outSign * 17f;
            var rnd = new System.Random(7);

            for (int i = 0; i < 5; i++)
            {
                float bw = 8f + (float)rnd.NextDouble() * 6f;
                float bh = 9f + (float)rnd.NextDouble() * 14f;
                float bx = -26f + i * 13f;
                float bz = baseZ + outSign * (float)rnd.NextDouble() * 5f;

                Box(ext, "Block" + i, new Vector3(bx, bh / 2f - 0.2f, bz),
                    new Vector3(bw, bh, 7f), "city_facade",
                    new Color(0.36f + (float)rnd.NextDouble() * 0.18f,
                              0.38f + (float)rnd.NextDouble() * 0.16f,
                              0.42f + (float)rnd.NextDouble() * 0.14f));

                // شبابيك مضيئة كبلوكات صغيرة بارزة
                int rows = Mathf.Clamp(Mathf.RoundToInt(bh / 3.2f), 2, 6);
                int cols = Mathf.Clamp(Mathf.RoundToInt(bw / 2.4f), 2, 4);
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        bool lit = ((r * 7 + c * 3 + i) % 3) != 0;
                        if (!lit) continue;
                        Box(ext, $"Block{i}_Win{r}{c}",
                            new Vector3(bx - bw / 2f + (bw / cols) * (c + 0.5f),
                                        1.6f + r * (bh - 3f) / rows,
                                        bz - outSign * 3.55f),
                            new Vector3(1.1f, 1.3f, 0.12f), "city_facade",
                            new Color(0.95f, 0.82f, 0.55f));
                    }
            }
        }

        // ────────────────────────── إضاءة الخارج ──────────────────────────

        static void StreetLamp(Transform ext, ExteriorSkin skin, Vector3 at, float outSign, float height)
        {
            Box(ext, "Lamp_Pole", new Vector3(at.x, height / 2f, at.z),
                new Vector3(0.14f, height, 0.14f), "metal_brushed", new Color(0.24f, 0.25f, 0.26f));
            Box(ext, "Lamp_Arm", new Vector3(at.x, height, at.z + outSign * 0.55f),
                new Vector3(0.1f, 0.1f, 1.2f), "metal_brushed", new Color(0.24f, 0.25f, 0.26f));
            Box(ext, "Lamp_Head", new Vector3(at.x, height - 0.12f, at.z + outSign * 1.15f),
                new Vector3(0.5f, 0.16f, 0.7f), "metal_brushed", new Color(0.92f, 0.86f, 0.66f));
            ExteriorLight(ext, skin, new Vector3(at.x, height - 0.3f, at.z + outSign * 1.15f), 2.1f, 17f, true);
        }

        static void ExteriorLight(Transform ext, ExteriorSkin skin, Vector3 at, float intensity,
                                  float range, bool shadows)
        {
            var go = new GameObject("ExtLight");
            go.transform.SetParent(ext, false);
            go.transform.position = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = skin.lightColor;
            l.intensity = LightUtil.Point(intensity);
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.6f;
        }
    }
}
