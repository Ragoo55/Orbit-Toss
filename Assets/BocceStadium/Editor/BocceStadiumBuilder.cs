using System.Collections.Generic;
using System.IO;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Editor tools for the VR Bocce game:
///   Tools > Bocce > 1. Set Up Project for 3D VR   (3D URP renderer + OpenXR)
///   Tools > Bocce > 2. Build Stadium Scene        (creates Assets/Scenes/BocceStadium.unity)
/// Re-running "Build Stadium Scene" regenerates the scene from scratch.
/// </summary>
public static class BocceStadiumBuilder
{
    const string Root = "Assets/BocceStadium";
    const string MatDir = Root + "/Materials";
    const string ScenePath = "Assets/Scenes/BocceStadium.unity";
    const string UrpAssetPath = "Assets/Settings/URP-VR.asset";
    const string UrpRendererPath = "Assets/Settings/URP-VR_Renderer.asset";

    // Court (metres). Length runs along Z, centred on the origin.
    const float CourtLength = 26.5f;
    const float CourtWidth = 4f;
    const float CourtTop = 0.05f;        // clay surface height
    const float BoardHeight = 0.3f;
    const float BoardThickness = 0.1f;
    const float FoulLineFromEnd = 3f;

    // Surroundings
    const float ApronHalfWidth = 6f;     // concrete walkway half-width (X)
    const float ApronHalfLength = 18f;   // concrete walkway half-length (Z)
    const int StandTiers = 6;
    const float TierDepth = 0.8f;
    const float TierRise = 0.45f;
    const float StandLength = 30f;

    // Balls
    const float BallRadius = 0.0535f;    // 107 mm bocce ball
    const float PallinoRadius = 0.02f;   // 40 mm pallino
    const float BallMass = 0.92f;

    // ---------------------------------------------------------------- menu items

    [MenuItem("Tools/Bocce/1. Set Up Project for 3D VR")]
    public static void SetUpProject()
    {
        SetUpRenderer();
        BocceXRSetup.ConfigureOpenXR();
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        AssetDatabase.SaveAssets();
        Debug.Log("[Bocce] Project set up for 3D VR.");
    }

    [MenuItem("Tools/Bocce/2. Build Stadium Scene")]
    public static void BuildScene()
    {
        Directory.CreateDirectory(MatDir);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var m = CreateMaterials();
        BuildLighting();

        var stadium = new GameObject("Stadium").transform;
        BuildGround(stadium, m);
        BuildCourt(stadium, m);
        BuildStands(stadium, m, +1);
        BuildStands(stadium, m, -1);
        BuildEndWalls(stadium, m);
        BuildFloodlights(stadium, m);
        BuildScoreboard(stadium, m);
        BuildBalls(m);
        BuildPlayerRig(m);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Bocce] Stadium scene saved to {ScenePath}");
    }

    /// <summary>Entry point for batch mode: -executeMethod BocceStadiumBuilder.BuildAll</summary>
    public static void BuildAll()
    {
        SetUpProject();
        BuildScene();
    }

    // ---------------------------------------------------------------- project setup

    static void SetUpRenderer()
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
        if (asset == null)
        {
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, UrpRendererPath);

            asset = UniversalRenderPipelineAsset.Create(rendererData);
            asset.msaaSampleCount = 4;     // smooth edges matter in VR
            asset.supportsHDR = false;     // cheaper for VR
            asset.shadowDistance = 60f;
            AssetDatabase.CreateAsset(asset, UrpAssetPath);
        }

        GraphicsSettings.defaultRenderPipeline = asset;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(current, false);
        Debug.Log("[Bocce] 3D URP renderer assigned (replaces the 2D renderer).");
    }

    // ---------------------------------------------------------------- materials

    class Mats
    {
        public Material grass, concrete, clay, wood, line, seat, metal, lamp, scoreboard, red, green, pallino, hand;
    }

    static Mats CreateMaterials() => new Mats
    {
        grass = Mat("Grass", new Color(0.25f, 0.48f, 0.22f)),
        concrete = Mat("Concrete", new Color(0.62f, 0.62f, 0.64f)),
        clay = Mat("CourtClay", new Color(0.78f, 0.60f, 0.40f), smooth: 0.1f),
        wood = Mat("BoardWood", new Color(0.45f, 0.30f, 0.17f)),
        line = Mat("LineWhite", Color.white),
        seat = Mat("SeatBlue", new Color(0.12f, 0.32f, 0.70f), smooth: 0.5f),
        metal = Mat("Metal", new Color(0.35f, 0.36f, 0.38f), 0.8f, 0.6f),
        lamp = Mat("FloodlightLamp", Color.white, emission: new Color(4f, 4f, 3.6f)),
        scoreboard = Mat("Scoreboard", new Color(0.05f, 0.05f, 0.06f)),
        red = Mat("BallRed", new Color(0.75f, 0.08f, 0.08f), 0.2f, 0.85f),
        green = Mat("BallGreen", new Color(0.08f, 0.55f, 0.18f), 0.2f, 0.85f),
        pallino = Mat("Pallino", new Color(0.95f, 0.95f, 0.90f), 0f, 0.8f),
        hand = Mat("Hand", new Color(0.85f, 0.85f, 0.88f), 0f, 0.5f),
    };

    static Material Mat(string name, Color color, float metallic = 0f, float smooth = 0.3f, Color? emission = null)
    {
        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smooth);
        if (emission.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission.Value);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------------------------------------------------------------- scene parts

    static void BuildLighting()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.sun = sun;
        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        RenderSettings.ambientMode = AmbientMode.Skybox;
    }

    static void BuildGround(Transform parent, Mats m)
    {
        Box("Ground (Grass)", parent, new Vector3(0f, -0.05f, 0f), new Vector3(80f, 0.1f, 90f), m.grass);
        Box("Walkway (Concrete)", parent, new Vector3(0f, 0.01f, 0f), new Vector3(ApronHalfWidth * 2f, 0.02f, ApronHalfLength * 2f), m.concrete);
    }

    static void BuildCourt(Transform parent, Mats m)
    {
        var court = new GameObject("Bocce Court").transform;
        court.SetParent(parent, false);

        Box("Court Surface (Clay)", court, new Vector3(0f, CourtTop / 2f, 0f), new Vector3(CourtWidth, CourtTop, CourtLength), m.clay);

        // Side and end boards
        float sideX = CourtWidth / 2f + BoardThickness / 2f;
        float endZ = CourtLength / 2f + BoardThickness / 2f;
        Box("Side Board L", court, new Vector3(-sideX, BoardHeight / 2f, 0f), new Vector3(BoardThickness, BoardHeight, CourtLength + 2f * BoardThickness), m.wood);
        Box("Side Board R", court, new Vector3(sideX, BoardHeight / 2f, 0f), new Vector3(BoardThickness, BoardHeight, CourtLength + 2f * BoardThickness), m.wood);
        Box("End Board Near", court, new Vector3(0f, BoardHeight / 2f, -endZ), new Vector3(CourtWidth, BoardHeight, BoardThickness), m.wood);
        Box("End Board Far", court, new Vector3(0f, BoardHeight / 2f, endZ), new Vector3(CourtWidth, BoardHeight, BoardThickness), m.wood);

        // Painted lines (no colliders so balls roll over them)
        var lineSize = new Vector3(CourtWidth, 0.003f, 0.05f);
        float lineY = CourtTop + 0.0015f;
        float foulZ = CourtLength / 2f - FoulLineFromEnd;
        Box("Center Line", court, new Vector3(0f, lineY, 0f), lineSize, m.line, false);
        Box("Foul Line Near", court, new Vector3(0f, lineY, -foulZ), lineSize, m.line, false);
        Box("Foul Line Far", court, new Vector3(0f, lineY, foulZ), lineSize, m.line, false);
    }

    /// <summary>Stepped concrete grandstand with bench seats along one long side (side = +1 right, -1 left).</summary>
    static void BuildStands(Transform parent, Mats m, int side)
    {
        var stand = new GameObject(side > 0 ? "Grandstand East" : "Grandstand West").transform;
        stand.SetParent(parent, false);

        float startX = ApronHalfWidth + 0.5f;
        for (int i = 0; i < StandTiers; i++)
        {
            float height = (i + 1) * TierRise;
            float x = side * (startX + i * TierDepth + TierDepth / 2f);
            Box($"Tier {i + 1}", stand, new Vector3(x, height / 2f, 0f), new Vector3(TierDepth, height, StandLength), m.concrete);

            // Bench along the front of the tier
            float benchX = x - side * (TierDepth / 2f - 0.25f);
            Box($"Bench {i + 1}", stand, new Vector3(benchX, height + 0.04f, 0f), new Vector3(0.35f, 0.08f, StandLength - 0.4f), m.seat, false);
        }

        // Back wall behind the top row
        float backX = side * (startX + StandTiers * TierDepth + 0.15f);
        float wallHeight = StandTiers * TierRise + 1.5f;
        Box("Back Wall", stand, new Vector3(backX, wallHeight / 2f, 0f), new Vector3(0.3f, wallHeight, StandLength), m.concrete);
    }

    static void BuildEndWalls(Transform parent, Mats m)
    {
        const float h = 1.0f;
        float width = ApronHalfWidth * 2f;
        Box("End Wall North", parent, new Vector3(0f, h / 2f, ApronHalfLength), new Vector3(width, h, 0.2f), m.concrete);
        Box("End Wall South", parent, new Vector3(0f, h / 2f, -ApronHalfLength), new Vector3(width, h, 0.2f), m.concrete);
    }

    static void BuildFloodlights(Transform parent, Mats m)
    {
        var lights = new GameObject("Floodlights").transform;
        lights.SetParent(parent, false);

        const float poleHeight = 14f;
        float px = ApronHalfWidth + 0.5f + StandTiers * TierDepth + 1.5f;
        float pz = 16f;
        int n = 1;
        foreach (var sx in new[] { -1f, 1f })
        foreach (var sz in new[] { -1f, 1f })
        {
            var pole = new GameObject($"Floodlight {n++}").transform;
            pole.SetParent(lights, false);
            pole.localPosition = new Vector3(sx * px, 0f, sz * pz);

            var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            column.name = "Pole";
            column.transform.SetParent(pole, false);
            column.transform.localPosition = new Vector3(0f, poleHeight / 2f, 0f);
            column.transform.localScale = new Vector3(0.3f, poleHeight / 2f, 0.3f);
            column.GetComponent<MeshRenderer>().sharedMaterial = m.metal;
            column.isStatic = true;

            // Lamp head aimed at the court centre
            var head = new GameObject("Lamp Head").transform;
            head.SetParent(pole, false);
            head.localPosition = new Vector3(0f, poleHeight, 0f);
            head.LookAt(new Vector3(0f, 0f, 0f));
            Box("Lamp Panel", head, Vector3.zero, new Vector3(1.6f, 1.0f, 0.2f), m.lamp, false);

            var spot = head.gameObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 45f;
            spot.spotAngle = 60f;
            spot.intensity = 8f;
            spot.color = new Color(1f, 0.97f, 0.9f);
            spot.shadows = LightShadows.None; // keep VR frame rate high
        }
    }

    static void BuildScoreboard(Transform parent, Mats m)
    {
        var board = new GameObject("Scoreboard").transform;
        board.SetParent(parent, false);
        board.localPosition = new Vector3(0f, 0f, ApronHalfLength + 1.5f);

        Box("Post L", board, new Vector3(-2.8f, 2.5f, 0f), new Vector3(0.25f, 5f, 0.25f), m.metal);
        Box("Post R", board, new Vector3(2.8f, 2.5f, 0f), new Vector3(0.25f, 5f, 0.25f), m.metal);
        Box("Panel", board, new Vector3(0f, 5f, 0f), new Vector3(6.5f, 2.6f, 0.3f), m.scoreboard);

        AddText(board, "Title", "BOCCE", new Vector3(0f, 5.8f, -0.17f), 0.09f, new Color(1f, 0.85f, 0.2f));
        AddText(board, "Score", "RED  0   -   0  GREEN", new Vector3(0f, 4.8f, -0.17f), 0.07f, Color.white);
    }

    static void AddText(Transform parent, string name, string text, Vector3 pos, float size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var tm = go.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tm.font = font;
        tm.text = text;
        tm.fontSize = 100;
        tm.characterSize = size;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    static void BuildBalls(Mats m)
    {
        var balls = new GameObject("Bocce Balls").transform;

        var physics = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>($"{Root}/BallPhysics.asset");
        if (physics == null)
        {
            physics = new PhysicsMaterial("BallPhysics") { bounciness = 0.15f, dynamicFriction = 0.5f, staticFriction = 0.6f };
            AssetDatabase.CreateAsset(physics, $"{Root}/BallPhysics.asset");
        }

        // Laid out on the clay at the near end, just in front of the player.
        float z = -CourtLength / 2f + 0.6f;
        Ball("Pallino", balls, new Vector3(0f, CourtTop + PallinoRadius, z + 0.3f), PallinoRadius, 0.06f, m.pallino, physics);
        for (int i = 0; i < 4; i++)
        {
            Ball($"Red Ball {i + 1}", balls, new Vector3(-0.6f - i * 0.25f, CourtTop + BallRadius, z), BallRadius, BallMass, m.red, physics);
            Ball($"Green Ball {i + 1}", balls, new Vector3(0.6f + i * 0.25f, CourtTop + BallRadius, z), BallRadius, BallMass, m.green, physics);
        }
    }

    static void Ball(string name, Transform parent, Vector3 pos, float radius, float mass, Material mat, PhysicsMaterial physics)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * radius * 2f;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        go.GetComponent<SphereCollider>().sharedMaterial = physics;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.angularDamping = 0.8f; // stops balls rolling forever on the clay
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    static void BuildPlayerRig(Mats m)
    {
        const float eyeHeight = 1.7f;

        var rig = new GameObject("XR Origin (First Person)");
        rig.transform.position = new Vector3(0f, CourtTop, -CourtLength / 2f + 1.5f); // behind the near foul line, facing down the court

        var controller = rig.AddComponent<CharacterController>();
        controller.height = 1.7f;
        controller.radius = 0.25f;
        controller.center = new Vector3(0f, controller.height / 2f + controller.skinWidth, 0f);

        var offset = new GameObject("Camera Offset").transform;
        offset.SetParent(rig.transform, false);

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.SetParent(offset, false);
        camGO.transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 500f;
        camGO.AddComponent<AudioListener>();
        AddPoseDriver(camGO, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");

        var origin = rig.AddComponent<XROrigin>();
        origin.Origin = rig;
        origin.CameraFloorOffsetObject = offset.gameObject;
        origin.Camera = cam;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        origin.CameraYOffset = eyeHeight;

        var left = Hand("Left Hand", "LeftHand", offset, m.hand);
        var right = Hand("Right Hand", "RightHand", offset, m.hand);

        var fp = rig.AddComponent<FirstPersonRig>();
        fp.cameraOffset = offset;
        fp.head = camGO.transform;
        fp.xrOnlyObjects = new[] { left, right };
    }

    static GameObject Hand(string name, string hand, Transform parent, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        AddPoseDriver(go, $"<XRController>{{{hand}}}/devicePosition", $"<XRController>{{{hand}}}/deviceRotation");

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Hand Visual";
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(0.08f, 0.08f, 0.12f);
        visual.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        return go;
    }

    static void AddPoseDriver(GameObject go, string positionPath, string rotationPath)
    {
        var tpd = go.AddComponent<TrackedPoseDriver>();
        tpd.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        tpd.positionInput = new InputActionProperty(new InputAction("Position", InputActionType.Value, positionPath, expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(new InputAction("Rotation", InputActionType.Value, rotationPath, expectedControlType: "Quaternion"));
    }

    // ---------------------------------------------------------------- helpers

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic = true;
        return go;
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
        foreach (var s in EditorBuildSettings.scenes)
            if (s.path != path) scenes.Add(s);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
