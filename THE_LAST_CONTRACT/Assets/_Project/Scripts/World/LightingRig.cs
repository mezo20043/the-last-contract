using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// إضاءة ليلية سينمائية + مطر + ضباب خفيف.
    /// الإضاءة مصمّمة للموبايل: ضوء اتجاهي واحد بظلال + نقطية قليلة بدون ظلال.
    /// </summary>
    public class LightingRig : MonoBehaviour
    {
        public Light MoonLight { get; private set; }
        public ParticleSystem Rain { get; private set; }

        public void Build()
        {
            SetupAmbient();
            CreateMoon();
            CreateInteriorLights();
            CreateRain();
            CreateReflectionProbe();
        }

        void SetupAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor    = new Color(0.10f, 0.11f, 0.15f);
            RenderSettings.ambientEquatorColor = new Color(0.075f, 0.078f, 0.09f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.045f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.045f, 0.052f, 0.070f);
        }

        void CreateMoon()
        {
            var go = new GameObject("Light_Moon");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.Euler(38f, -35f, 0f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.62f, 0.72f, 0.95f);
            light.intensity = LightUtil.Directional(0.42f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;

            MoonLight = light;
        }

        void CreateInteriorLights()
        {
            AddPoint("Light_LampLiving", new Vector3(-3.5f, 1.62f, -2.4f),
                     new Color(1f, 0.80f, 0.55f), 1.5f, 7f, true);

            AddPoint("Light_Entry", new Vector3(0f, 2.4f, -2.4f),
                     new Color(1f, 0.88f, 0.70f), 0.7f, 5f, false);

            AddPoint("Light_Kitchen", new Vector3(-6f, 2.3f, -1.8f),
                     new Color(0.95f, 0.95f, 0.88f), 0.8f, 5f, false);

            AddPoint("Light_Bedroom", new Vector3(7.6f, 1.2f, -0.35f),
                     new Color(1f, 0.78f, 0.52f), 0.9f, 5f, false);

            // توهّج المدينة من الشباك
            AddPoint("Light_CitySpill", new Vector3(0f, 1.7f, 4.2f),
                     new Color(0.35f, 0.55f, 0.85f), 1.1f, 9f, false);
        }

        void AddPoint(string name, Vector3 pos, Color color, float intensity, float range,
                      bool shadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;

            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = LightUtil.Point(intensity);
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
        }

        void CreateRain()
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            // قاعدة القصة: ممنوع المطر جوه الشقة. الشباك الكبير في الجدار الشمالي
            // (z = +3). فالمطر بيتولد **برّه** الشباك (z = +8.5) وعمقه ٩ م،
            // عشان يبان من الشباك بس وماينزلش على الأرضية جوه.
            go.transform.localPosition = new Vector3(0f, 6f, 8.5f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = 16f;
            main.startSize = 0.02f;
            main.startColor = new Color(0.72f, 0.80f, 0.92f, 0.35f);
            main.maxParticles = 2000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.35f;

            var emission = ps.emission;
            emission.rateOverTime = 400f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 1f, 9f);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 6f;
            renderer.velocityScale = 0.12f;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
                                   ?? Shader.Find("Particles/Standard Unlit")
                                   ?? Shader.Find("Standard"));
            mat.color = new Color(0.70f, 0.78f, 0.92f, 0.30f);
            renderer.sharedMaterial = mat;

            Rain = ps;
        }

        void CreateReflectionProbe()
        {
            var go = new GameObject("ReflectionProbe_Living");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            var probe = go.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(10f, 3f, 8f);
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Baked;
            probe.resolution = 128;
            probe.intensity = 0.6f;
        }
    }
}
