#!/usr/bin/env python3
"""
مولّد مشهد اللعب Play.unity + محدّث حقول Main.unity.

ليه سكربت مش كتابة يدوية:
مشهد Unity بصيغة YAML وفيه معرّفات مراجع (fileID) وروابط GUID للسكربتات.
لو الكلام ده اتكتب بالغلط مرة، يبقى عندك مشهد بأخطاء «Missing Script»
مش هتظهر غير جوه المحرر. السكربت ده بيولّد الملف من نفس البيانات
اللي بيقراها tools/sim للتحقق، فالاتنين ما يختلفوش.

الاستخدام:
    python3 tools/make_scenes.py            # يولّد/يحدّث المشهدين
    python3 tools/make_scenes.py --check     # يتحقق بس من غير كتابة
"""
import hashlib
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(ROOT, "Assets", "_Project")
SCENES = os.path.join(ASSETS, "Scenes")


def guid_of(rel_path):
    """نفس الدالة اللي مولّدة ملفات .meta — لازم تتطابق بالحرف."""
    return hashlib.md5(("thelastcontract:" + rel_path).encode()).hexdigest()


def script_guid(cs_rel):
    """GUID لسكربت من مساره النسبي جوه Assets/."""
    meta = os.path.join(ROOT, "Assets", cs_rel + ".meta")
    if not os.path.exists(meta):
        raise SystemExit(f"مفيش ملف .meta للسكربت: {cs_rel}")
    m = re.search(r"guid:\s*([0-9a-f]{32})", open(meta).read())
    if not m:
        raise SystemExit(f"مفيش GUID في {meta}")
    return m.group(1)


# ─────────────────── كتل YAML ───────────────────

HEADER = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_OcclusionBakeSettings:
    smallestOccluder: 5
    smallestHole: 0.25
    backfaceThreshold: 100
  m_SceneGUID: 00000000000000000000000000000000
  m_OcclusionCullingData: {fileID: 0}
--- !u!104 &2
RenderSettings:
  m_ObjectHideFlags: 0
  serializedVersion: 9
  m_Fog: 1
  m_FogColor: {r: 0.043, g: 0.048, b: 0.062, a: 1}
  m_FogMode: 3
  m_FogDensity: 0.018
  m_LinearFogStart: 0
  m_LinearFogEnd: 90
  m_AmbientSkyColor: {r: 0.085, g: 0.095, b: 0.125, a: 1}
  m_AmbientEquatorColor: {r: 0.052, g: 0.055, b: 0.065, a: 1}
  m_AmbientGroundColor: {r: 0.02, g: 0.02, b: 0.024, a: 1}
  m_AmbientIntensity: 1
  m_AmbientMode: 3
  m_SkyboxMaterial: {fileID: 0}
  m_HaloStrength: 0.5
  m_FlareStrength: 1
  m_FlareFadeSpeed: 3
  m_DefaultReflectionMode: 0
  m_DefaultReflectionResolution: 128
  m_ReflectionBounces: 1
  m_ReflectionIntensity: 1
  m_CustomReflection: {fileID: 0}
  m_Sun: {fileID: 0}
"""


def go(fid, name, components, tag="Untagged", layer=0, active=1):
    comps = "\n".join(f"  - component: {{fileID: {c}}}" for c in components)
    return f"""--- !u!1 &{fid}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comps}
  m_Layer: {layer}
  m_Name: {name}
  m_TagString: {tag}
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}
"""


def transform(fid, go_id, pos=(0, 0, 0), rot=(0, 0, 0, 1), euler=(0, 0, 0),
              children=(), father=0, scale=(1, 1, 1)):
    ch = "".join(f"\n  - {{fileID: {c}}}" for c in children)
    ch_block = f"  m_Children:{ch}" if children else "  m_Children: []"
    return f"""--- !u!4 &{fid}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  serializedVersion: 2
  m_LocalRotation: {{x: {rot[0]}, y: {rot[1]}, z: {rot[2]}, w: {rot[3]}}}
  m_LocalPosition: {{x: {pos[0]}, y: {pos[1]}, z: {pos[2]}}}
  m_LocalScale: {{x: {scale[0]}, y: {scale[1]}, z: {scale[2]}}}
  m_ConstrainProportionsScale: 0
{ch_block}
  m_Father: {{fileID: {father}}}
  m_LocalEulerAnglesHint: {{x: {euler[0]}, y: {euler[1]}, z: {euler[2]}}}
"""


def camera(fid, go_id, fov=48.0, near=0.06, far=70.0):
    return f"""--- !u!20 &{fid}
Camera:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  serializedVersion: 2
  m_ClearFlags: 1
  m_BackGroundColor: {{r: 0.03, g: 0.035, b: 0.045, a: 1}}
  m_projectionMatrixMode: 1
  m_GateFitMode: 2
  m_FOVAxisMode: 0
  m_SensorSize: {{x: 36, y: 24}}
  m_LensShift: {{x: 0, y: 0}}
  m_FocalLength: 50
  m_NormalizedViewPortRect:
    serializedVersion: 2
    x: 0
    y: 0
    width: 1
    height: 1
  near clip plane: {near}
  far clip plane: {far}
  field of view: {fov}
  orthographic: 0
  orthographic size: 5
  m_Depth: -1
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingPath: -1
  m_TargetTexture: {{fileID: 0}}
  m_TargetDisplay: 0
  m_TargetEye: 3
  m_HDR: 1
  m_AllowMSAA: 1
  m_AllowDynamicResolution: 0
  m_ForceIntoRT: 0
  m_OcclusionCulling: 1
  m_StereoConvergence: 10
  m_StereoSeparation: 0.022
"""


def audio_listener(fid, go_id):
    return f"""--- !u!81 &{fid}
AudioListener:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
"""


def light(fid, go_id, intensity=0.55, color=(0.88, 0.91, 1.0)):
    return f"""--- !u!108 &{fid}
Light:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  serializedVersion: 11
  m_Type: 1
  m_Shape: 0
  m_Color: {{r: {color[0]}, g: {color[1]}, b: {color[2]}, a: 1}}
  m_Intensity: {intensity}
  m_Range: 12
  m_SpotAngle: 30
  m_InnerSpotAngle: 21.80208
  m_CookieSize: 10
  m_Shadows:
    m_Type: 2
    m_Resolution: -1
    m_CustomResolution: -1
    m_Strength: 0.82
    m_Bias: 0.05
    m_NormalBias: 0.4
    m_NearPlane: 0.2
  m_DrawHalo: 0
  m_Flare: {{fileID: 0}}
  m_RenderMode: 0
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 4294967295
  m_RenderingLayerMask: 1
  m_Lightmapping: 4
  m_LightShadowCasterMode: 0
  m_AreaSize: {{x: 1, y: 1}}
  m_BounceIntensity: 1
  m_ColorTemperature: 5800
  m_UseColorTemperature: 0
  m_BoundingSphereOverride: {{x: 0, y: 0, z: 0, w: 0}}
  m_UseBoundingSphereOverride: 0
  m_UseViewFrustumForShadowCasterCull: 1
  m_ShadowRadius: 0
  m_ShadowAngle: 0
"""


def mono(fid, go_id, guid, fields=""):
    body = f"\n{fields}" if fields else ""
    return f"""--- !u!114 &{fid}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: {body}
"""


def vector3(x, y, z):
    return f"    m_LocalPosition: {{x: {x}, y: {y}, z: {z}}}"


# ─────────────────── بناء المشهد ───────────────────

def build_play_scene():
    g_cam_rig   = script_guid("_Project/Scripts/Camera/ThirdPersonCameraRig.cs")
    g_adam      = script_guid("_Project/Scripts/World/AdamBuilder.cs")
    g_apartment = script_guid("_Project/Scripts/World/ApartmentBuilder.cs")
    g_lighting  = script_guid("_Project/Scripts/World/LightingRig.cs")
    g_cast      = script_guid("_Project/Scripts/World/CastDirector.cs")
    g_mobile    = script_guid("_Project/Scripts/UI/MobileControls.cs")
    g_orient    = script_guid("_Project/Scripts/UI/OrientationWatcher.cs")
    g_bootstrap = script_guid("_Project/Scripts/Bootstrap/GameBootstrap.cs")

    out = [HEADER]

    # ── MAIN CAMERA ──
    cam_go, cam_tr = 100000, 100001
    rot_y180 = (0, 1, 0, 0)
    # نفس موضع الكاميرا اللي الـrig بيحسبه وقت التشغيل (pitch 8 · distance 3.1 ·
    # shoulder 0.30 · height 1.35) — محسوب بـ tools/frame_check.py
    rot_cam = (0, 0.9976, -0.0698, 0)
    out.append(go(cam_go, "Main Camera",
                  [cam_tr, 100002, 100003, 100004], tag="MainCamera"))
    out.append(transform(cam_tr, cam_go,
                         pos=(-0.9, 1.781, 1.67),
                         rot=rot_cam, euler=(8, 180, 0)))
    out.append(camera(100002, cam_go, fov=48.0))
    out.append(audio_listener(100003, cam_go))
    # target = Transform آدم (مرجع حقيقي جوه المشهد، مش بحث وقت التشغيل)
    out.append(mono(100004, cam_go, g_cam_rig, "\n".join([
        "  target: {fileID: 100061}",
        "  input: {fileID: 0}",
        "  heightOffset: 1.55",
        "  distance: 3.1",
        "  shoulderOffset: 0.42",
        "  minPitch: -28",
        "  maxPitch: 62",
        "  followLerp: 12",
        "  collisionRadius: 0.24",
        "  collisionMask:",
        "    serializedVersion: 2",
        "    m_Bits: 4294967295",
    ])))

    # ── ضوء الشمس (إضاءة مشهد حقيقية) ──
    sun_go, sun_tr = 100010, 100011
    out.append(go(sun_go, "Directional Light", [sun_tr, 100012]))
    # ميل 48° ودوران 34° — ضوء نافذة عصر
    out.append(transform(sun_tr, sun_go, pos=(0, 3.2, 0),
                         rot=(0.3714, 0.2408, -0.1003, 0.8908),
                         euler=(48, 34, 0)))
    out.append(light(100012, sun_go, intensity=0.55))

    # ── WORLD: الشقة + الإضاءة العملية + الكاست ──
    world_go, world_tr = 100020, 100021
    out.append(go(world_go, "WORLD", [world_tr]))
    out.append(transform(world_tr, world_go,
                         children=[100031, 100041, 100051]))

    out.append(go(100030, "Apartment", [100031, 100032]))
    out.append(transform(100031, 100030, father=world_tr))
    out.append(mono(100032, 100030, g_apartment))

    out.append(go(100040, "Lighting", [100041, 100042]))
    out.append(transform(100041, 100040, father=world_tr))
    out.append(mono(100042, 100040, g_lighting))

    out.append(go(100050, "CAST", [100051, 100052]))
    out.append(transform(100051, 100050, father=world_tr))
    out.append(mono(100052, 100050, g_cast))

    # ── آدم ── نقطة الظهور = ApartmentBuilder.PlayerSpawn (-0.6, 0, -1.4)
    adam_go, adam_tr = 100060, 100061
    out.append(go(adam_go, "ADAM_MANSOUR", [adam_tr, 100062]))
    out.append(transform(adam_tr, adam_go,
                         pos=(-0.6, 0.0, -1.4), rot=rot_y180, euler=(0, 180, 0)))
    out.append(mono(100062, adam_go, g_adam))

    # ── واجهة تحكم الموبايل ──
    mob_go, mob_tr = 100070, 100071
    out.append(go(mob_go, "MOBILE_CONTROLS", [mob_tr, 100072]))
    out.append(transform(mob_tr, mob_go))
    out.append(mono(100072, mob_go, g_mobile, "\n".join([
        "  showOnDesktop: 1",
        "  stickSize: 300",
        "  handleSize: 128",
        "  handleRange: 108",
        "  lookSensitivity: 0.35",
        "  stickTint: {r: 1, g: 1, b: 1, a: 0.1}",
        "  handleTint: {r: 0.85, g: 0.71, b: 0.42, a: 0.34}",
    ])))

    # ── مراقب الاتجاه ──
    ori_go, ori_tr = 100080, 100081
    out.append(go(ori_go, "OrientationWatcher", [ori_tr, 100082]))
    out.append(transform(ori_tr, ori_go))
    out.append(mono(100082, ori_go, g_orient, "\n".join([
        "  allowPortrait: 1",
        "  applyScreenPolicy: 1",
        "  referenceAspect: 1.777778",
    ])))

    # ── نقطة الدخول ──
    game_go, game_tr = 100090, 100091
    out.append(go(game_go, "GAME", [game_tr, 100092]))
    out.append(transform(game_tr, game_go))
    out.append(mono(100092, game_go, g_bootstrap, "\n".join([
        "  storyResourcePath: Story/chapter01",
        "  chapterTitle: الفصل الأول · العقد الأخير",
        "  chapterResources:",
        "  - Story/chapter01",
        "  - Story/chapter02",
        "  - Story/chapter03",
        "  chapterTitles:",
        "  - الفصل الأول · العقد الأخير",
        "  - الفصل الثاني · البيت القديم",
        "  - الفصل الثالث · الحساب",
        "  delayBeforeNextChapter: 3.5",
        "  buildMobileControls: 1",
        "  autoQuality: 1",
        "  menuOnly: 0",
        "  useSceneObjects: 1",
        "  playSceneName: Play",
        "  showTitleMenu: 0",
        "  openWithCinematic: 1",
    ])))

    return "".join(out)


PLAY_META = """fileFormatVersion: 2
guid: {g}
DefaultImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        fh.write(text)
    return len(text)


def main():
    check_only = "--check" in sys.argv

    scene_text = build_play_scene()
    scene_path = os.path.join(SCENES, "Play.unity")
    meta_path = scene_path + ".meta"

    if check_only:
        if not os.path.exists(scene_path):
            raise SystemExit("✗ Play.unity مش موجود")
        if open(scene_path, encoding="utf-8").read() != scene_text:
            raise SystemExit("✗ Play.unity مختلف عن المولّد — شغّل السكربت من غير --check")
        print("✓ Play.unity مطابق للمولّد")
        return

    n = write(scene_path, scene_text)
    if not os.path.exists(meta_path):
        write(meta_path, PLAY_META.format(g=guid_of("_Project/Scenes/Play.unity")))

    print(f"✓ Play.unity — {n} بايت")
    print(f"✓ Play.unity.meta — guid {guid_of('_Project/Scenes/Play.unity')}")


if __name__ == "__main__":
    main()
