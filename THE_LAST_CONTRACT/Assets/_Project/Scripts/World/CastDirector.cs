using System.Collections.Generic;
using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// يوزّع باقي الشخصيات الواقعية في الشقة ويضبط أوضاعها:
    /// مراد قاعد، ليلى واقفة، ياسين عند الباب.
    /// بيوفّر العظام للكاميرا السينمائية عشان تطلع الزوايا القريبة من الوش.
    /// </summary>
    public class CastDirector : MonoBehaviour
    {
        class Member
        {
            public CharacterLibrary.Cast cast;
            public GameObject model;
            public HumanoidPuppet puppet;
        }

        readonly Dictionary<CharacterLibrary.Cast, Member> _cast =
            new Dictionary<CharacterLibrary.Cast, Member>();

        public int SpawnedCount => _cast.Count;
        public bool HasRealCharacters => _cast.Count > 0;

        public struct Placement
        {
            public CharacterLibrary.Cast cast;
            public Vector3 position;
            public float yaw;
            public bool seated;

            public Placement(CharacterLibrary.Cast c, Vector3 p, float y, bool sit)
            {
                cast = c; position = p; yaw = y; seated = sit;
            }
        }

        // توزيع في الصالة (8×6 متر مركزها الأصل)
        static readonly Placement[] Layout =
        {
            new Placement(CharacterLibrary.Cast.Murad,  new Vector3(-2.1f, 0f, 1.35f),  115f, true),
            new Placement(CharacterLibrary.Cast.Laila,  new Vector3( 2.4f, 0f, 0.90f), -125f, false),
            new Placement(CharacterLibrary.Cast.Yassin, new Vector3( 0.2f, 0f, -2.35f),  15f, false),
            new Placement(CharacterLibrary.Cast.Yusuf,  new Vector3( 2.9f, 0f,  1.6f), -140f, false),
        };

        public void SpawnAll()
        {
            foreach (var p in Layout) Spawn(p);
        }

        public void Spawn(Placement p) => Spawn(p.cast, p.position, p.yaw, p.seated);

        public void Spawn(CharacterLibrary.Cast cast, Vector3 position, float yaw, bool seated)
        {
            var prefab = CharacterLibrary.Load(cast);
            if (prefab == null)
            {
                Debug.LogWarning($"[TLC/Cast] {cast}: الموديل مش موجود — اتخطّى.");
                return;
            }

            var go = new GameObject(cast.ToString());
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var model = Instantiate(prefab, go.transform);
            model.name = cast + "_Model";
            ModelFit.DisableRootMotion(model);
            ModelFit.WrapInPivot(model, cast + "_Pivot");
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            foreach (var c in model.GetComponentsInChildren<Collider>())
                if (c != null) Destroy(c);

            var puppet = go.AddComponent<HumanoidPuppet>();
            puppet.Bind(model);

            if (!puppet.IsBound)
            {
                Debug.LogWarning($"[TLC/Cast] {cast}: العظام مش اتربطت — الكائن اتشال.");
                Destroy(go);
                return;
            }

            if (seated) puppet.SetSeated(true);

            _cast[cast] = new Member { cast = cast, model = model, puppet = puppet };
        }

        /// <summary>
        /// وضعية القعود بتغرق الشخصية في الأرض (المحور عند الحوض، والساقين بتتطبق
        /// لتحت). هنا بنقيس الموديل الفعلي ونرفعه لحد ما قاعدته تلمس الأرض بالظبط.
        /// </summary>
        static void ClampToGround(Transform root, GameObject model, Vector3 groundPos)
        {
            if (root == null || model == null) return;
            var pivot = model.transform.parent != null ? model.transform.parent : model.transform;
            ModelFit.DropToGround(pivot, groundPos.y);
        }

        /// <summary>يظهر/يخفي شخصية (بيفضل موجود في الكاست، بس مش في الكادر/المشهد).</summary>
        public void SetVisible(CharacterLibrary.Cast cast, bool visible)
        {
            if (_cast.TryGetValue(cast, out var m) && m.model != null)
            {
                var root = m.model.transform.parent != null ? m.model.transform.parent.gameObject : m.model;
                if (root != null && root.activeSelf != visible) root.SetActive(visible);
            }
        }

        public bool IsVisible(CharacterLibrary.Cast cast) =>
            _cast.TryGetValue(cast, out var m) && m.model != null &&
            (m.model.transform.parent == null || m.model.transform.parent.gameObject.activeSelf);

        /// <summary>يحطّ الشخصية في مكان معيّن ويخلّيها تبصّ ناحية نقطة.</summary>
        public void Place(CharacterLibrary.Cast cast, Vector3 position, Transform lookAt, bool seated = false)
        {
            if (!_cast.TryGetValue(cast, out var m) || m.model == null) return;
            var root = m.model.transform.parent != null ? m.model.transform.parent : m.model.transform;
            root.position = position;

            // (ضبط الأرضية بقى مسؤولية HumanoidPuppet.SettleFeet — بيتابع الوضعية نفسها)

            if (lookAt != null)
            {
                var dir = lookAt.position - position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f) root.rotation = Quaternion.LookRotation(dir);
            }

            var puppet = m.puppet;
            if (puppet != null) puppet.SetSeated(seated);
        }

        public HumanoidPuppet Puppet(CharacterLibrary.Cast cast) =>
            _cast.TryGetValue(cast, out var m) ? m.puppet : null;

        public GameObject Model(CharacterLibrary.Cast cast) =>
            _cast.TryGetValue(cast, out var m) ? m.model : null;

        public Transform HeadOf(CharacterLibrary.Cast cast)
        {
            var p = Puppet(cast);
            return p != null ? p.HeadBone : null;
        }

        /// <summary>يخلّي الشخصية تتكلّم (حركات إيماء) وتبصّ لحدّ.</summary>
        public void SetTalking(CharacterLibrary.Cast cast, bool talking, Transform facing = null)
        {
            var p = Puppet(cast);
            if (p == null) return;

            p.IsTalking = talking;

            if (talking && facing != null)
                p.LookTowards(facing.position);
        }

        public void StopAllTalking()
        {
            foreach (var m in _cast.Values)
                if (m.puppet != null) m.puppet.IsTalking = false;
        }

        /// <summary>يخلّي كل الموجودين يبصّوا على آدم أثناء الحوار.</summary>
        public void FaceAllTowards(Transform target)
        {
            if (target == null) return;
            foreach (var m in _cast.Values)
            {
                if (m.puppet == null) continue;
                var root = m.puppet.transform;
                Vector3 dir = target.position - root.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) continue;
                root.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }
    }
}
