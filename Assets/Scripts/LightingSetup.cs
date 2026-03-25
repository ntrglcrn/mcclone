using UnityEngine;
using UnityEngine.Rendering;

public sealed class LightingSetup : MonoBehaviour
{
    private static readonly Color MainLightColor = new Color(0.94f, 0.97f, 1f, 1f);
    private static readonly Color FillLightColor = new Color(0.52f, 0.58f, 0.68f, 1f);
    private static readonly Color RimLightColor = new Color(0.58f, 0.66f, 0.82f, 1f);
    private static readonly Color FlatAmbientColor = new Color(0.1f, 0.1f, 0.12f, 1f);

    [SerializeField, Range(0.8f, 1.2f)] private float mainLightIntensity = 1f;
    [SerializeField, Range(0.6f, 0.8f)] private float mainShadowStrength = 0.7f;
    [SerializeField, Range(0.2f, 0.4f)] private float fillLightIntensity = 0.3f;
    [SerializeField, Range(0f, 0.15f)] private float rimLightIntensity = 0.1f;
    [SerializeField] private bool enableRimLight = true;
    [SerializeField] private bool enableHdr = true;

    private Light mainLight;
    private Light fillLight;
    private Light rimLight;

    public static LightingSetup EnsureInScene()
    {
        LightingSetup setup = Object.FindObjectOfType<LightingSetup>();
        if (setup == null)
        {
            GameObject setupObject = new GameObject("Lighting");
            setup = setupObject.AddComponent<LightingSetup>();
        }

        return setup;
    }

    private void Awake()
    {
        Apply();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply(Camera targetCamera = null)
    {
        mainLight = ResolveMainLight();
        fillLight = GetOrCreateChildDirectionalLight("Fill Light");
        rimLight = GetOrCreateChildDirectionalLight("Rim Light");

        ConfigureMainLight(mainLight);
        ConfigureFillLight(fillLight, mainLight);
        ConfigureRimLight(rimLight, mainLight);
        ConfigureAmbient();
        ConfigureFog();
        ConfigureCamera(targetCamera);
        ConfigureVoxelMaterial();

        DynamicGI.UpdateEnvironment();
    }

    private Light ResolveMainLight()
    {
        if (mainLight != null)
        {
            return mainLight;
        }

        if (RenderSettings.sun != null)
        {
            mainLight = RenderSettings.sun;
            return mainLight;
        }

        Light[] lights = Object.FindObjectsOfType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type != LightType.Directional)
            {
                continue;
            }

            string lightName = lights[i].name.ToLowerInvariant();
            if (lightName.Contains("fill") || lightName.Contains("rim"))
            {
                continue;
            }

            mainLight = lights[i];
            RenderSettings.sun = mainLight;
            return mainLight;
        }

        GameObject lightObject = new GameObject("Sun Light");
        lightObject.transform.SetParent(transform, false);
        mainLight = lightObject.AddComponent<Light>();
        mainLight.type = LightType.Directional;
        RenderSettings.sun = mainLight;
        return mainLight;
    }

    private Light GetOrCreateChildDirectionalLight(string lightName)
    {
        Transform child = transform.Find(lightName);
        Light light = child != null ? child.GetComponent<Light>() : null;

        if (light == null)
        {
            GameObject lightObject = child != null ? child.gameObject : new GameObject(lightName);
            lightObject.transform.SetParent(transform, false);
            light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }
        }

        light.type = LightType.Directional;
        return light;
    }

    private void ConfigureMainLight(Light light)
    {
        light.name = "Sun Light";
        light.type = LightType.Directional;
        light.color = MainLightColor;
        light.intensity = mainLightIntensity;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = mainShadowStrength;
        light.transform.rotation = ResolveMainLightRotation();
    }

    private void ConfigureFillLight(Light light, Light sourceLight)
    {
        Vector3 fillForward = (-sourceLight.transform.forward + Vector3.up * 0.35f).normalized;
        light.name = "Fill Light";
        light.color = FillLightColor;
        light.intensity = fillLightIntensity;
        light.shadows = LightShadows.None;
        light.transform.rotation = Quaternion.LookRotation(fillForward, Vector3.up);
    }

    private void ConfigureRimLight(Light light, Light sourceLight)
    {
        if (!enableRimLight)
        {
            light.gameObject.SetActive(false);
            return;
        }

        Vector3 rimForward = (-sourceLight.transform.forward + sourceLight.transform.right * 0.2f + Vector3.up * 0.15f).normalized;
        light.gameObject.SetActive(true);
        light.name = "Rim Light";
        light.color = RimLightColor;
        light.intensity = rimLightIntensity;
        light.shadows = LightShadows.None;
        light.transform.rotation = Quaternion.LookRotation(rimForward, Vector3.up);
    }

    private void ConfigureAmbient()
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.reflectionIntensity = 0.35f;
            return;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = FlatAmbientColor;
    }

    private static void ConfigureFog()
    {
        RenderSettings.fog = false;
    }

    private void ConfigureCamera(Camera targetCamera)
    {
        if (targetCamera != null)
        {
            targetCamera.allowHDR = enableHdr;
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.allowHDR = enableHdr;
        }
    }

    private static void ConfigureVoxelMaterial()
    {
        Chunk chunk = Object.FindObjectOfType<Chunk>();
        if (chunk == null)
        {
            return;
        }

        Renderer renderer = chunk.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null)
        {
            return;
        }

        Material material = renderer.sharedMaterial;
        material.color = Color.white;

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.05f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }
    }

    private Quaternion ResolveMainLightRotation()
    {
        SkyboxSetup skyboxSetup = Object.FindObjectOfType<SkyboxSetup>();
        if (skyboxSetup != null && skyboxSetup.TryGetMoonDirection(out Vector3 moonDirection))
        {
            Vector3 lightForward = (-moonDirection + Vector3.down * 0.65f).normalized;
            Vector3 up = Mathf.Abs(Vector3.Dot(lightForward, Vector3.up)) > 0.98f ? Vector3.forward : Vector3.up;
            return Quaternion.LookRotation(lightForward, up);
        }

        return Quaternion.Euler(50f, -30f, 0f);
    }
}
