using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Installs the shared HDR glow profile once for the entire game session and
/// enables URP post processing on every camera that renders during the session.
/// </summary>
public static class GlobalGlowBootstrap
{
    private const string ProfileResourcePath = "Rendering/GlobalGlowProfile";
    private const string VolumeObjectName = "Global Glow Post Processing";

    private static bool _installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (_installed)
        {
            return;
        }

        _installed = true;

        VolumeProfile profile = Resources.Load<VolumeProfile>(ProfileResourcePath);
        if (profile == null)
        {
            Debug.LogError($"Missing global glow profile at Resources/{ProfileResourcePath}.");
            return;
        }

        GameObject volumeObject = new(VolumeObjectName);
        Object.DontDestroyOnLoad(volumeObject);

        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.sharedProfile = profile;

        Camera.onPreCull += EnablePostProcessing;
    }

    private static void EnablePostProcessing(Camera camera)
    {
        // Bloom needs an HDR camera so values above 1 can reach the shared
        // post-processing volume. This also covers cameras in future scenes.
        camera.allowHDR = true;

        UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }

        cameraData.renderPostProcessing = true;
    }
}
