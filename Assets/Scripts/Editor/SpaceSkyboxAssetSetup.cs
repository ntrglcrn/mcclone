#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SpaceSkyboxAssetSetup
{
    private const string SkyboxFolderPath = "Assets/Resources/Skyboxes";
    private const string TextureFolderPath = "Assets/Resources/Textures/Skyboxes";
    private const string SkyboxMaterialPath = "Assets/Resources/Skyboxes/SpaceSkybox.mat";

    private static readonly Color DefaultSkyTint = new Color(0.015f, 0.018f, 0.03f, 1f);

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += EnsureSpaceSkyboxAssets;
    }

    [MenuItem("Tools/Voxel/Ensure Space Skybox")]
    private static void EnsureSpaceSkyboxAssets()
    {
        EnsureFolder(SkyboxFolderPath);
        EnsureFolder(TextureFolderPath);

        SixSidedTextures sixSidedTextures = FindSixSidedTextures();
        if (sixSidedTextures.IsComplete)
        {
            ConfigureTexture(sixSidedTextures.Front);
            ConfigureTexture(sixSidedTextures.Back);
            ConfigureTexture(sixSidedTextures.Left);
            ConfigureTexture(sixSidedTextures.Right);
            ConfigureTexture(sixSidedTextures.Top);
            ConfigureTexture(sixSidedTextures.Bottom);
            CreateOrUpdateSixSidedMaterial(sixSidedTextures);
            return;
        }

        Texture2D panoramaTexture = FindPanoramaTexture();
        if (panoramaTexture != null)
        {
            ConfigureTexture(panoramaTexture);
        }

        CreateOrUpdateSkyboxMaterial(panoramaTexture);
    }

    private static SixSidedTextures FindSixSidedTextures()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolderPath });
        SixSidedTextures result = default;

        for (int i = 0; i < textureGuids.Length; i++)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                continue;
            }

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

    private static Texture2D FindPanoramaTexture()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolderPath });
        for (int i = 0; i < textureGuids.Length; i++)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                continue;
            }

            string textureName = texture.name.ToLowerInvariant();
            if (textureName.Contains("panorama") || textureName.Contains("equirect") || textureName.Contains("latlong"))
            {
                return texture;
            }
        }

        return null;
    }

    private static void ConfigureTexture(Texture2D texture)
    {
        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool changed = false;

        if (importer.textureType != TextureImporterType.Default)
        {
            importer.textureType = TextureImporterType.Default;
            changed = true;
        }

        if (importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.wrapMode = TextureWrapMode.Clamp;
            changed = true;
        }

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        }
    }

    private static void CreateOrUpdateSixSidedMaterial(SixSidedTextures textures)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        bool created = false;
        Shader shader = Shader.Find("Skybox/6 Sided");

        if (shader == null)
        {
            return;
        }

        if (material == null)
        {
            material = new Material(shader);
            created = true;
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.name = "SpaceSkybox";
        SetTextureIfPresent(material, "_FrontTex", textures.Front);
        SetTextureIfPresent(material, "_BackTex", textures.Back);
        SetTextureIfPresent(material, "_LeftTex", textures.Left);
        SetTextureIfPresent(material, "_RightTex", textures.Right);
        SetTextureIfPresent(material, "_UpTex", textures.Top);
        SetTextureIfPresent(material, "_DownTex", textures.Bottom);
        SetFloatIfPresent(material, "_Exposure", 1f);
        SetFloatIfPresent(material, "_Rotation", 0f);

        SaveMaterial(material, created);
    }

    private static void CreateOrUpdateSkyboxMaterial(Texture2D panoramaTexture)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        bool created = false;

        Shader shader = panoramaTexture != null
            ? Shader.Find("Skybox/Panoramic")
            : Shader.Find("Skybox/Procedural");

        if (shader == null)
        {
            return;
        }

        if (material == null)
        {
            material = new Material(shader);
            created = true;
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.name = "SpaceSkybox";

        if (panoramaTexture != null)
        {
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", panoramaTexture);
            }

            SetColorIfPresent(material, "_Tint", Color.white);
            SetFloatIfPresent(material, "_Exposure", 1f);
            SetFloatIfPresent(material, "_Rotation", 0f);
        }
        else
        {
            SetColorIfPresent(material, "_SkyTint", DefaultSkyTint);
            SetColorIfPresent(material, "_GroundColor", Color.black);
            SetFloatIfPresent(material, "_AtmosphereThickness", 0f);
            SetFloatIfPresent(material, "_Exposure", 1f);
            SetFloatIfPresent(material, "_SunDisk", 0f);
            SetFloatIfPresent(material, "_SunSize", 0.01f);
        }

        SaveMaterial(material, created);
    }

    private static void SaveMaterial(Material material, bool created)
    {
        if (created)
        {
            AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }

        AssetDatabase.SaveAssets();
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

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private struct SixSidedTextures
    {
        public Texture2D Front;
        public Texture2D Back;
        public Texture2D Left;
        public Texture2D Right;
        public Texture2D Top;
        public Texture2D Bottom;

        public bool IsComplete =>
            Front != null &&
            Back != null &&
            Left != null &&
            Right != null &&
            Top != null &&
            Bottom != null;
    }
}
#endif
