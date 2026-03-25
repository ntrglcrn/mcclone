using UnityEngine;

public sealed class SkyboxSetup : MonoBehaviour
{
    private const string DefaultSkyboxResourcePath = "Skyboxes/SpaceSkybox";
    private static readonly Color DefaultSkyTint = new Color(0.015f, 0.018f, 0.03f, 1f);
    private static readonly Color DefaultGroundColor = Color.black;

    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Texture panoramaTexture;
    [SerializeField] private Texture frontTexture;
    [SerializeField] private Texture backTexture;
    [SerializeField] private Texture leftTexture;
    [SerializeField] private Texture rightTexture;
    [SerializeField] private Texture topTexture;
    [SerializeField] private Texture bottomTexture;

    private Material runtimeSkyboxMaterial;

    public static SkyboxSetup EnsureInScene()
    {
        SkyboxSetup setup = Object.FindObjectOfType<SkyboxSetup>();
        if (setup == null)
        {
            GameObject setupObject = new GameObject("SkyboxSetup");
            setup = setupObject.AddComponent<SkyboxSetup>();
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
        Material resolvedSkybox = ResolveSkyboxMaterial();
        if (resolvedSkybox != null)
        {
            RenderSettings.skybox = resolvedSkybox;
        }

        ApplyCameraSettings(targetCamera);
        DynamicGI.UpdateEnvironment();
    }

    private Material ResolveSkyboxMaterial()
    {
        if (skyboxMaterial != null)
        {
            return skyboxMaterial;
        }

        if (runtimeSkyboxMaterial != null)
        {
            return runtimeSkyboxMaterial;
        }

        Material resourceMaterial = Resources.Load<Material>(DefaultSkyboxResourcePath);
        if (resourceMaterial != null)
        {
            skyboxMaterial = resourceMaterial;
            return skyboxMaterial;
        }

        SixSidedTextures sixSidedTextures = GetSixSidedTextures();
        if (sixSidedTextures.IsComplete)
        {
            runtimeSkyboxMaterial = CreateSixSidedSkybox(sixSidedTextures);
            return runtimeSkyboxMaterial;
        }

        Texture sourcePanorama = panoramaTexture != null ? panoramaTexture : FindPanoramaTexture();

        if (sourcePanorama != null)
        {
            runtimeSkyboxMaterial = CreatePanoramicSkybox(sourcePanorama);
            return runtimeSkyboxMaterial;
        }

        runtimeSkyboxMaterial = CreateProceduralSkybox();
        return runtimeSkyboxMaterial;
    }

    private void ApplyCameraSettings(Camera targetCamera)
    {
        if (targetCamera != null)
        {
            targetCamera.clearFlags = CameraClearFlags.Skybox;
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.Skybox;
        }
    }

    private SixSidedTextures GetSixSidedTextures()
    {
        if (frontTexture != null &&
            backTexture != null &&
            leftTexture != null &&
            rightTexture != null &&
            topTexture != null &&
            bottomTexture != null)
        {
            return new SixSidedTextures(frontTexture, backTexture, leftTexture, rightTexture, topTexture, bottomTexture);
        }

        Texture[] textures = Resources.LoadAll<Texture>("Textures/Skyboxes");
        SixSidedTextures result = default;

        for (int i = 0; i < textures.Length; i++)
        {
            Texture texture = textures[i];
            string textureName = texture.name.ToLowerInvariant();

            if (textureName.Contains("front"))
            {
                result.Front = texture;
            }
            else if (textureName.Contains("back"))
            {
                result.Back = texture;
            }
            else if (textureName.Contains("left"))
            {
                result.Left = texture;
            }
            else if (textureName.Contains("right"))
            {
                result.Right = texture;
            }
            else if (textureName.Contains("top") || textureName.Contains("up"))
            {
                result.Top = texture;
            }
            else if (textureName.Contains("bottom") || textureName.Contains("down"))
            {
                result.Bottom = texture;
            }
        }

        return result;
    }

    public bool TryGetMoonDirection(out Vector3 direction)
    {
        SixSidedTextures textures = GetSixSidedTextures();
        if (!textures.IsComplete)
        {
            direction = default;
            return false;
        }

        direction = EstimateMoonDirection(textures);
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = default;
            return false;
        }

        return true;
    }

    private static Vector3 EstimateMoonDirection(SixSidedTextures textures)
    {
        FaceScore bestFace = default;
        EvaluateFace(textures.Front, Vector3.forward, ref bestFace);
        EvaluateFace(textures.Back, Vector3.back, ref bestFace);
        EvaluateFace(textures.Left, Vector3.left, ref bestFace);
        EvaluateFace(textures.Right, Vector3.right, ref bestFace);
        EvaluateFace(textures.Top, Vector3.up, ref bestFace);
        EvaluateFace(textures.Bottom, Vector3.down, ref bestFace);
        return bestFace.Direction;
    }

    private static void EvaluateFace(Texture texture, Vector3 faceDirection, ref FaceScore bestFace)
    {
        float score = CalculateFaceBrightnessScore(texture);
        if (score > bestFace.Score)
        {
            bestFace = new FaceScore
            {
                Direction = faceDirection,
                Score = score
            };
        }
    }

    private static float CalculateFaceBrightnessScore(Texture texture)
    {
        if (texture == null)
        {
            return 0f;
        }

        const int sampleSize = 32;
        RenderTexture renderTexture = RenderTexture.GetTemporary(sampleSize, sampleSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;
        Texture2D sampleTexture = new Texture2D(sampleSize, sampleSize, TextureFormat.RGBA32, false, true);

        try
        {
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;
            sampleTexture.ReadPixels(new Rect(0f, 0f, sampleSize, sampleSize), 0, 0);
            sampleTexture.Apply(false, false);

            Color[] pixels = sampleTexture.GetPixels();
            float score = 0f;

            for (int i = 0; i < pixels.Length; i++)
            {
                float luminance = pixels[i].grayscale;
                score += luminance * luminance;
            }

            return score;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            Object.Destroy(sampleTexture);
        }
    }

    private static Texture FindPanoramaTexture()
    {
        Texture[] textures = Resources.LoadAll<Texture>("Textures/Skyboxes");
        for (int i = 0; i < textures.Length; i++)
        {
            string textureName = textures[i].name.ToLowerInvariant();
            if (textureName.Contains("panorama") || textureName.Contains("equirect") || textureName.Contains("latlong"))
            {
                return textures[i];
            }
        }

        return null;
    }

    private static Material CreateProceduralSkybox()
    {
        Shader shader = Shader.Find("Skybox/Procedural");
        if (shader == null)
        {
            return null;
        }

        Material material = new Material(shader)
        {
            name = "RuntimeSpaceSkybox"
        };

        SetColorIfPresent(material, "_SkyTint", DefaultSkyTint);
        SetColorIfPresent(material, "_GroundColor", DefaultGroundColor);
        SetFloatIfPresent(material, "_AtmosphereThickness", 0f);
        SetFloatIfPresent(material, "_Exposure", 1f);
        SetFloatIfPresent(material, "_SunDisk", 0f);
        SetFloatIfPresent(material, "_SunSize", 0.01f);
        return material;
    }

    private static Material CreateSixSidedSkybox(SixSidedTextures textures)
    {
        Shader shader = Shader.Find("Skybox/6 Sided");
        if (shader == null)
        {
            return CreateProceduralSkybox();
        }

        Material material = new Material(shader)
        {
            name = "RuntimeSpaceSkybox"
        };

        SetTextureIfPresent(material, "_FrontTex", textures.Front);
        SetTextureIfPresent(material, "_BackTex", textures.Back);
        SetTextureIfPresent(material, "_LeftTex", textures.Left);
        SetTextureIfPresent(material, "_RightTex", textures.Right);
        SetTextureIfPresent(material, "_UpTex", textures.Top);
        SetTextureIfPresent(material, "_DownTex", textures.Bottom);
        SetColorIfPresent(material, "_Tint", Color.white);
        SetFloatIfPresent(material, "_Exposure", 1f);
        SetFloatIfPresent(material, "_Rotation", 0f);
        return material;
    }

    private static Material CreatePanoramicSkybox(Texture sourcePanorama)
    {
        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null)
        {
            return CreateProceduralSkybox();
        }

        Material material = new Material(shader)
        {
            name = "RuntimeSpaceSkybox"
        };

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", sourcePanorama);
        }

        SetColorIfPresent(material, "_Tint", Color.white);
        SetFloatIfPresent(material, "_Exposure", 1f);
        SetFloatIfPresent(material, "_Rotation", 0f);
        return material;
    }

    private static void SetTextureIfPresent(Material material, string propertyName, Texture value)
    {
        if (value != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, value);
        }
    }

    private static void SetColorIfPresent(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private void OnDestroy()
    {
        if (runtimeSkyboxMaterial != null)
        {
            Destroy(runtimeSkyboxMaterial);
        }
    }

    private struct SixSidedTextures
    {
        public Texture Front;
        public Texture Back;
        public Texture Left;
        public Texture Right;
        public Texture Top;
        public Texture Bottom;

        public bool IsComplete =>
            Front != null &&
            Back != null &&
            Left != null &&
            Right != null &&
            Top != null &&
            Bottom != null;

        public SixSidedTextures(Texture front, Texture back, Texture left, Texture right, Texture top, Texture bottom)
        {
            Front = front;
            Back = back;
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }
    private struct FaceScore
    {
        public Vector3 Direction;
        public float Score;
    }
}
