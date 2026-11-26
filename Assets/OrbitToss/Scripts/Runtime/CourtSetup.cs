using UnityEngine;

namespace OrbitToss
{
    /// <summary>
    /// Generates the standard Boccia court (surface + tape markings + soft boundary rails) from
    /// <see cref="CourtDimensions"/>. Keep this GameObject at the world origin with no rotation/scale,
    /// because gameplay code measures positions in world space using the same coordinates.
    ///
    /// Use the component's context menu "Rebuild Court" after changing materials.
    /// Decorative scenery lives under the separate "Environment" group, never under the court.
    /// </summary>
    public class CourtSetup : MonoBehaviour
    {
        [Header("Materials (swap freely - geometry is unaffected)")]
        [SerializeField] private Material courtSurfaceMaterial;
        [SerializeField] private Material surroundFloorMaterial;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material railMaterial;

        [Header("Physics")]
        [Tooltip("Physics material on the court surface (rolling friction / bounce of balls).")]
        [SerializeField] private PhysicsMaterial courtPhysicsMaterial;

        [Header("Boundary rails (prototype: stops balls rolling away; not part of official rules)")]
        [SerializeField] private bool buildBoundaryRails = true;
        [SerializeField] private float railDistanceOutsideCourt = 0.6f;
        [SerializeField] private float railHeight = 0.12f;

        private const float LineHeight = 0.002f;   // tape thickness (visual only, no collider)
        private const float LineLift = 0.0015f;    // raise tape slightly to avoid z-fighting

        public Material CourtSurfaceMaterial => courtSurfaceMaterial;
        public Material LineMaterial => lineMaterial;

        public void Configure(Material surface, Material surround, Material lines, Material rails, PhysicsMaterial physics)
        {
            courtSurfaceMaterial = surface;
            surroundFloorMaterial = surround;
            lineMaterial = lines;
            railMaterial = rails;
            courtPhysicsMaterial = physics;
        }

        [ContextMenu("Rebuild Court")]
        public void Rebuild()
        {
            // Remove previously generated children.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            Transform surfaceGroup = CreateGroup("Surface");
            Transform linesGroup = CreateGroup("Markings");
            Transform railsGroup = CreateGroup("BoundaryRails");

            BuildSurface(surfaceGroup);
            BuildBoundaryLines(linesGroup);
            BuildThrowingArea(linesGroup);
            BuildVLine(linesGroup);
            BuildCross(linesGroup);
            if (buildBoundaryRails) BuildRails(railsGroup);
        }

        // ------------------------------------------------------------------ surface

        private void BuildSurface(Transform parent)
        {
            float L = CourtDimensions.CourtLength;
            float W = CourtDimensions.CourtWidth;

            // Court surface: a thick box whose top face is exactly Y = 0 (thick colliders stop tunnelling).
            GameObject court = Box("CourtSurface_12.5x6m", parent, new Vector3(0f, -0.1f, L * 0.5f), new Vector3(W, 0.2f, L),
                courtSurfaceMaterial, keepCollider: true);
            court.GetComponent<Collider>().sharedMaterial = courtPhysicsMaterial;

            // Surrounding hall floor (visually different from the court), slightly lower to avoid z-fighting.
            GameObject surround = Box("SurroundFloor", parent, new Vector3(0f, -0.1005f, L * 0.5f), new Vector3(W + 8f, 0.2f, L + 8f),
                surroundFloorMaterial, keepCollider: true);
            surround.GetComponent<Collider>().sharedMaterial = courtPhysicsMaterial;
        }

        // ------------------------------------------------------------------ markings

        private void BuildBoundaryLines(Transform parent)
        {
            float L = CourtDimensions.CourtLength;
            float hw = CourtDimensions.HalfWidth;
            float t = CourtDimensions.WideTape;

            // Measurements are to the inside of the boundary lines, so tape sits outside the rectangle.
            Line("BackLine", parent, new Vector3(0f, 0f, -t * 0.5f), new Vector3(2f * hw + 2f * t, 0f, t));
            Line("FrontLine", parent, new Vector3(0f, 0f, L + t * 0.5f), new Vector3(2f * hw + 2f * t, 0f, t));
            Line("SideLine_Left", parent, new Vector3(-hw - t * 0.5f, 0f, L * 0.5f), new Vector3(t, 0f, L));
            Line("SideLine_Right", parent, new Vector3(hw + t * 0.5f, 0f, L * 0.5f), new Vector3(t, 0f, L));
        }

        private void BuildThrowingArea(Transform parent)
        {
            float hw = CourtDimensions.HalfWidth;
            float zLine = CourtDimensions.ThrowingLineZ;
            float wide = CourtDimensions.WideTape;
            float narrow = CourtDimensions.NarrowTape;

            // Throwing line: its back edge is exactly 2.5 m from the back line; tape lies in the playing-area side
            // (rule 3.1: throwing line is inside the non-valid area for the Jack).
            Line("ThrowingLine", parent, new Vector3(0f, 0f, zLine + wide * 0.5f), new Vector3(2f * hw, 0f, wide));

            // Box dividers: centred on each metre mark (5 dividers for 6 boxes).
            for (int i = 1; i < CourtDimensions.ThrowingBoxCount; i++)
            {
                float x = -hw + i * CourtDimensions.ThrowingBoxWidth;
                Line($"ThrowingBoxDivider_{i}|{i + 1}", parent, new Vector3(x, 0f, zLine * 0.5f), new Vector3(narrow, 0f, zLine));
            }
        }

        private void BuildVLine(Transform parent)
        {
            float hw = CourtDimensions.HalfWidth;
            float t = CourtDimensions.WideTape;
            // Front edge of the V passes through (±3, 5.5) at the side lines and (0, 4.0) at the vertex.
            // The tape lies on the athlete side of that front edge (inside the non-valid area for the Jack).
            Vector2 apex = new Vector2(0f, CourtDimensions.VLineApexZ);
            BuildVLeg("VLine_Left", parent, apex, new Vector2(-hw, CourtDimensions.VLineSideZ), t);
            BuildVLeg("VLine_Right", parent, apex, new Vector2(hw, CourtDimensions.VLineSideZ), t);
        }

        private void BuildVLeg(string legName, Transform parent, Vector2 apex, Vector2 sideEnd, float tape)
        {
            Vector2 dir = (sideEnd - apex).normalized;
            float length = Vector2.Distance(apex, sideEnd);
            // Normal pointing back towards the throwing boxes (negative Z side).
            Vector2 normal = new Vector2(-dir.y, dir.x);
            if (normal.y > 0f) normal = -normal;
            Vector2 centre = (apex + sideEnd) * 0.5f + normal * (tape * 0.5f);
            float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

            GameObject leg = Line(legName, parent, new Vector3(centre.x, 0f, centre.y), new Vector3(tape, 0f, length));
            leg.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void BuildCross(Transform parent)
        {
            Vector3 c = CourtDimensions.CrossCentre;
            float n = CourtDimensions.NarrowTape;
            float leg = CourtDimensions.CrossLegLength;
            float inside = CourtDimensions.TargetBoxInsideSize;

            Transform crossGroup = new GameObject("TargetCross").transform;
            crossGroup.SetParent(parent, false);

            // The cross (lines measured to a pencil line with tape spread evenly either side).
            Line("Cross_AlongCourt", crossGroup, c, new Vector3(n, 0f, leg));
            Line("Cross_AcrossCourt", crossGroup, c, new Vector3(leg, 0f, n));

            // 35 cm x 35 cm target box: narrow tape placed on the OUTSIDE of the 35 cm square.
            float half = inside * 0.5f;
            float outer = inside + 2f * n;
            Line("TargetBox_Back", crossGroup, c + new Vector3(0f, 0f, -half - n * 0.5f), new Vector3(outer, 0f, n));
            Line("TargetBox_Front", crossGroup, c + new Vector3(0f, 0f, half + n * 0.5f), new Vector3(outer, 0f, n));
            Line("TargetBox_Left", crossGroup, c + new Vector3(-half - n * 0.5f, 0f, 0f), new Vector3(n, 0f, inside));
            Line("TargetBox_Right", crossGroup, c + new Vector3(half + n * 0.5f, 0f, 0f), new Vector3(n, 0f, inside));
        }

        // ------------------------------------------------------------------ rails

        private void BuildRails(Transform parent)
        {
            float L = CourtDimensions.CourtLength;
            float hw = CourtDimensions.HalfWidth;
            float d = railDistanceOutsideCourt;
            float h = railHeight;
            const float thick = 0.1f;

            // Sides and front only - the back (athlete side) stays open so nothing blocks a seated player.
            Rail("Rail_Left", parent, new Vector3(-hw - d - thick * 0.5f, h * 0.5f, L * 0.5f), new Vector3(thick, h, L + 2f * d));
            Rail("Rail_Right", parent, new Vector3(hw + d + thick * 0.5f, h * 0.5f, L * 0.5f), new Vector3(thick, h, L + 2f * d));
            Rail("Rail_Front", parent, new Vector3(0f, h * 0.5f, L + d + thick * 0.5f), new Vector3(2f * (hw + d) + 2f * thick, h, thick));
        }

        private void Rail(string railName, Transform parent, Vector3 centre, Vector3 size)
        {
            GameObject rail = Box(railName, parent, centre, size, railMaterial, keepCollider: true);
            rail.GetComponent<Collider>().sharedMaterial = courtPhysicsMaterial;
        }

        // ------------------------------------------------------------------ helpers

        private Transform CreateGroup(string groupName)
        {
            Transform group = new GameObject(groupName).transform;
            group.SetParent(transform, false);
            return group;
        }

        private GameObject Line(string lineName, Transform parent, Vector3 centreOnCourt, Vector3 footprint)
        {
            Vector3 size = new Vector3(footprint.x, LineHeight, footprint.z);
            return Box(lineName, parent, centreOnCourt + Vector3.up * LineLift, size, lineMaterial, keepCollider: false);
        }

        private static GameObject Box(string boxName, Transform parent, Vector3 localCentre, Vector3 size, Material material, bool keepCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = boxName;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localCentre;
            box.transform.localScale = size;
            if (material != null) box.GetComponent<Renderer>().sharedMaterial = material;
            box.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (!keepCollider)
            {
                Collider c = box.GetComponent<Collider>();
                if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
            }
            return box;
        }
    }
}
