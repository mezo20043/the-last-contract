using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using TLC.Player;
using TLC.CameraRig;

namespace TLCSim
{
    /// <summary>
    /// محاكاة رأسية لجلسة لعب — بتشغّل **كود اللعبة الحقيقي** من غير Unity.
    ///
    /// مهم جداً إيه اللي بيحصل هنا وإيه اللي مابيحصلش:
    /// • بنستخدم <c>PlayerInputReader</c> و<c>ThirdPersonController</c> و
    ///   <c>ThirdPersonCameraRig</c> الحقيقيين — نفس الملفات اللي بتشتغل في اللعبة.
    /// • بننده دوال Unity بالانعكاس (Update / LateUpdate / Awake) بنفس الترتيب
    ///   اللي المحرك بينده بيه.
    /// • الستاب بتاع <c>CharacterController.Move</c> بتحرّك الترانسفورم بنفس إزاحة
    ///   Unity، فالإزاحة اللي بتطلع رقم حقيقي من معادلات المتحكم.
    ///
    /// • اللي **مش** بيحصل: مفيش رندرة، مفيش فيزياء حقيقية (Raycast بيرجّع false)،
    ///   مفيش Animator بيتشغّل، مفيش تصادم مع الحيطان.
    ///   يعني المحاكاة بتثبت **مسار الحركة والحسابات**، مش شكل اللعبة.
    /// </summary>
    public static class PlaySimulation
    {
        const float Dt = 1f / 60f;

        public class Sample
        {
            public float t;
            public Vector3 pos;
            public float speed;
            public bool running;
            public Vector2 move;
            public string phase;
        }

        public static List<Sample> Run(out string report, out List<Sample> cameraSamples)
        {
            var samples = new List<Sample>();
            cameraSamples = new List<Sample>();
            var sb = new StringBuilder();

            // ── تركيب آدم بنفس مكوّنات المشهد ──
            var playerGo = new GameObject("ADAM_MANSOUR");
            var playerTr = new Transform();
            playerGo.transform = playerTr;
            playerTr.position = new Vector3(-0.6f, 0f, -1.4f);   // نفس نقطة الظهور في Play.unity

            var cc = new CharacterController { transform = playerTr };

            var input = new PlayerInputReader();
            input.transform = playerTr;

            var controller = new ThirdPersonController();
            controller.transform = playerTr;

            // مراجع داخلية (في Unity بتتظبط من المشهد/Bootstrap)
            SetField(controller, "input", input);
            SetField(controller, "cameraTransform", (Transform)null);   // مفيش كاميرا → الحركة على محور العالم
            SetField(controller, "animator", (Animator)null);
            SetField(controller, "rig", (ProceduralRig)null);

            Invoke(controller, "Awake");
            // Awake بيعمل GetComponent<CharacterController>() — الستاب بيرجّع null فالبنحطه يدوي
            SetField(controller, "_cc", cc);

            sb.AppendLine("════════ محاكاة جلسة لعب (كود المشروع الحقيقي) ════════");
            sb.AppendLine("الملفات المستخدمة: PlayerInputReader · ThirdPersonController · ThirdPersonCameraRig");
            sb.AppendLine("الستاب: CharacterController.Move بتحرّك الترانسفورم · Time.deltaTime = 1/60");
            sb.AppendLine("مفيش رندرة ولا فيزياء — القياس على معادلات الحركة نفسها.");
            sb.AppendLine();
            sb.AppendLine("| الثانية | الطور | Joystick | السرعة (م/ث) | الموضع (x, z) | المسافة من البداية |");
            sb.AppendLine("|---|---|---|---|---|---|");

            Vector3 start = playerTr.position;
            float nextLog = 0f;

            // ── الأطوار: زي ما اللاعب بيعمل بإيده ──
            // 0.0–2.0  سكون            (مفيش لمس)
            // 2.0–5.0  دفع العصا لقدام (مشي)
            // 5.0–8.0  نفس الدفع + زر الجري
            // 8.0–9.0  سيب كل حاجة
            // 9.0–10.0 الحوار مفتوح (GameplayEnabled = false) والعصا مازالت مدفوعة
            float t = 0f;
            string phase = "سكون";

            while (t < 10.0f)
            {
                // المدخلات
                if (t >= 2.0f && t < 5.0f)
                {
                    phase = "مشي";
                    input.SetMobileMove(new Vector2(0f, 1f));
                    input.SetMobileRun(false);
                }
                else if (t >= 5.0f && t < 8.0f)
                {
                    phase = "جري";
                    input.SetMobileMove(new Vector2(0f, 1f));
                    input.SetMobileRun(true);
                }
                else if (t >= 8.0f && t < 9.0f)
                {
                    phase = "وقف";
                    input.SetMobileMove(Vector2.zero);
                    input.SetMobileRun(false);
                }
                else if (t >= 9.0f)
                {
                    phase = "حوار مفتوح";
                    input.GameplayEnabled = false;
                    input.SetMobileMove(new Vector2(0f, 1f));   // العصا لسه مدفوعة
                }
                else
                {
                    phase = "سكون";
                    input.SetMobileMove(Vector2.zero);
                    input.SetMobileRun(false);
                }

                Time.deltaTime = Dt;

                // ترتيب نداء Unity: Update للكل، وبعدهم LateUpdate
                Invoke(input, "Update");
                Invoke(controller, "Update");

                // الستاب مفيش فيه كولايدر أرض، فبنحل التصادم يدوياً زي ما Unity بيعمله:
                // الكائن ما ينزلش تحت مستوى الأرض.
                var p = playerTr.position;
                if (p.y < 0f) playerTr.position = new Vector3(p.x, 0f, p.z);

                var s = new Sample
                {
                    t = t,
                    pos = playerTr.position,
                    speed = controller.CurrentSpeed,
                    running = controller.IsRunning,
                    move = input.Move,
                    phase = phase
                };
                samples.Add(s);

                if (t >= nextLog - 0.001f)
                {
                    float dist = Vector3.Distance(start, playerTr.position);
                    sb.AppendLine($"| {t:0.0} | {phase} | ({input.Move.x:0.0}, {input.Move.y:0.0}) | " +
                                  $"{controller.CurrentSpeed:0.00} | ({playerTr.position.x:0.00}, {playerTr.position.z:0.00}) | " +
                                  $"{dist:0.00} م |");
                    nextLog += 0.5f;
                }

                t += Dt;
            }

            // ── الكاميرا: مدار كامل بمدخلات لمس ──
            sb.AppendLine();
            sb.AppendLine("── الكاميرا (ThirdPersonCameraRig الحقيقي) ──");

            var rig = new ThirdPersonCameraRig();
            var rigTr = new Transform();
            rig.transform = rigTr;
            rigTr.position = new Vector3(0f, 1.66f, 0f);

            SetField(rig, "target", playerTr);
            SetField(rig, "input", input);
            SetField(rig, "_cam", new Camera());

            Invoke(rig, "Awake");
            Invoke(rig, "Start");

            float startYaw = rigTr.eulerAngles.y;
            float startDist = Vector3.Distance(rigTr.position, playerTr.position);

            input.SetMobileLook(new Vector2(4f, 0f));   // سحب لليمين
            Invoke(input, "Update");
            Invoke(rig, "LateUpdate");

            float endYaw = rigTr.eulerAngles.y;
            float endDist = Vector3.Distance(rigTr.position, playerTr.position);

            sb.AppendLine($"  yaw: {startYaw:0.0}° → {endYaw:0.0}°  (تغيّر {MathfExt.DeltaAngleSafe(startYaw, endYaw):0.0}°)");
            sb.AppendLine($"  المسافة للاعب: {startDist:0.00} م → {endDist:0.00} م");

            // ── الاستنتاجات ──
            float maxSpeed = 0f, walkSpeed = 0f, idleSpeed = 0f;
            float distance = 0f;
            foreach (var s in samples)
            {
                maxSpeed = Mathf.Max(maxSpeed, s.speed);
                if (s.phase == "سكون" && s.t > 1.5f) idleSpeed = Mathf.Max(idleSpeed, s.speed);
                if (s.phase == "مشي" && s.t > 4.5f) walkSpeed = Mathf.Max(walkSpeed, s.speed);
            }
            distance = Vector3.Distance(start, playerTr.position);

            sb.AppendLine();
            sb.AppendLine("── الاستنتاجات ──");
            sb.AppendLine($"  أعلى سرعة عند الجري : {maxSpeed:0.00} م/ث");
            sb.AppendLine($"  سرعة المشي المستقرة : {walkSpeed:0.00} م/ث");
            sb.AppendLine($"  سرعة السكون        : {idleSpeed:0.00} م/ث");
            sb.AppendLine($"  إجمالي الإزاحة     : {distance:0.00} م");

            sb.AppendLine();
            sb.AppendLine("  ملاحظة: الأرقام دي من معادلات ThirdPersonController نفسها");
            sb.AppendLine("  (walkSpeed 1.55 · runSpeed 4.1 · تسريع 11 · تباطؤ 15).");

            report = sb.ToString();
            return samples;
        }

        // ─── أدوات انعكاس ───

        static void SetField(object target, string name, object value)
        {
            var f = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            f?.SetValue(target, value);
        }

        static void Invoke(object target, string method)
        {
            var m = target.GetType().GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            m?.Invoke(target, null);
        }

        // ─── مخرجات ───

        public static void WriteCsv(List<Sample> samples, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("t,phase,x,y,z,speed,isRunning,moveX,moveY");
            foreach (var s in samples)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0:0.000},{1},{2:0.000},{3:0.000},{4:0.000},{5:0.000},{6},{7:0.00},{8:0.00}",
                    s.t, s.phase, s.pos.x, s.pos.y, s.pos.z, s.speed,
                    s.running ? 1 : 0, s.move.x, s.move.y));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }

    /// <summary>مساعد صغير — Mathf في Unity مفيهوش DeltaAngle مباشر للزوايا الشمال.</summary>
    public static class MathfExt
    {
        public static float DeltaAngleSafe(float from, float to)
        {
            float d = to - from;
            while (d > 180f) d -= 360f;
            while (d < -180f) d += 360f;
            return d;
        }
    }
}
