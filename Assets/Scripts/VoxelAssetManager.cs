#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class VoxelAssetSetup
{
    private const string AtlasTexturePath = "Assets/Resources/Textures/block_atlas.png";
    private const string MaterialsFolderPath = "Assets/Materials";
    private const string MaterialPath = "Assets/Materials/VoxelAtlas.mat";

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += EnsureVoxelAssets;
    }

    [MenuItem("Tools/Voxel/Ensure Atlas Material")]
    private static void EnsureVoxelAssets()
    {
        EnsureFolder(MaterialsFolderPath);
        ConfigureAtlasTexture();
        CreateOrUpdateMaterial();
    }

    private static void ConfigureAtlasTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(AtlasTexturePath) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Default)   { importer.textureType = TextureImporterType.Default; changed = true; }
        if (importer.filterMode != FilterMode.Point)               { importer.filterMode = FilterMode.Point; changed = true; }
        if (importer.wrapMode != TextureWrapMode.Clamp)            { importer.wrapMode = TextureWrapMode.Clamp; changed = true; }
        if (importer.mipmapEnabled)                                { importer.mipmapEnabled = false; changed = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }

        if (changed)
            AssetDatabase.ImportAsset(AtlasTexturePath, ImportAssetOptions.ForceUpdate);
    }

    private static void CreateOrUpdateMaterial()
    {
        Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasTexturePath);
        if (atlasTexture == null) return;

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        bool created = false;

        if (material == null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) return;
            material = new Material(shader);
            created = true;
        }

        material.name = "VoxelAtlas";
        material.mainTexture = atlasTexture;
        material.color = Color.white;
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.05f);
        if (material.HasProperty("_Metallic"))   material.SetFloat("_Metallic", 0f);

        if (created)
            AssetDatabase.CreateAsset(material, MaterialPath);
        else
            EditorUtility.SetDirty(material);

        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string assetPath)
    {
        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
