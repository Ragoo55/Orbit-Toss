using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

/// <summary>Turns on OpenXR for PC (Standalone) and enables the common controller profiles.</summary>
public static class BocceXRSetup
{
    static readonly string[] Profiles =
    {
        "OculusTouchControllerProfile",
        "MetaQuestTouchPlusControllerProfile",
        "MetaQuestTouchProControllerProfile",
        "KHRSimpleControllerProfile",
        "ValveIndexControllerProfile",
        "HTCViveControllerProfile",
    };

    public static void ConfigureOpenXR()
    {
        const BuildTargetGroup group = BuildTargetGroup.Standalone;

        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
        {
            Directory.CreateDirectory("Assets/XR");
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
        }

        if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
        if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

        var settings = perTarget.SettingsForBuildTarget(group);
        settings.InitManagerOnStart = true;
        bool assigned = XRPackageMetadataStore.AssignLoader(settings.Manager, typeof(OpenXRLoader).FullName, group);
        Debug.Log($"[Bocce] OpenXR loader assigned for Standalone: {assigned || settings.Manager.activeLoaders.Count > 0}");

        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if (openXR == null)
        {
            Debug.LogWarning("[Bocce] OpenXR settings not found yet. Run Tools > Bocce > 1. Set Up Project for 3D VR again after Unity finishes importing.");
            return;
        }

        // OpenXR creates its feature objects lazily (normally when the settings page is opened).
        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);

        foreach (var feature in openXR.GetFeatures<OpenXRInteractionFeature>())
        {
            if (System.Array.IndexOf(Profiles, feature.GetType().Name) >= 0)
            {
                feature.enabled = true;
                Debug.Log($"[Bocce] Enabled OpenXR profile: {feature.GetType().Name}");
            }
        }
        EditorUtility.SetDirty(openXR);
        EditorUtility.SetDirty(perTarget);
    }
}
