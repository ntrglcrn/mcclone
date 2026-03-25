#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Editor utility to set up block definitions for the voxel system.
/// Run via menu: Tools > Voxel > Setup Block Definitions
/// </summary>
public static class BlockDefinitionSetup
{
    private const string BlockDefinitionsFolder = "Assets/Resources/BlockDefinitions";
    private const string BlockDefinitionsResourceFolder = "BlockDefinitions";

    [MenuItem("Tools/Voxel/Setup Block Definitions")]
    public static void SetupBlockDefinitions()
    {
        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(BlockDefinitionsFolder))
        {
            string guid = AssetDatabase.CreateFolder("Assets/Resources", "BlockDefinitions");
            Debug.Log($"[BlockDefinitionSetup] Created folder: {BlockDefinitionsFolder} (GUID: {guid})");
        }

        // Create Air block definition
        CreateBlockDefinition(
            name: "Air",
            blockID: 0,
            atlasTop: new Vector2Int(0, 0),
            atlasSide: new Vector2Int(0, 0),
            atlasBottom: new Vector2Int(0, 0),
            isSolid: false,
            isTransparent: true
        );

        // Create Stone block definition (all faces same texture at 0,0)
        CreateBlockDefinition(
            name: "Stone",
            blockID: 1,
            atlasTop: new Vector2Int(0, 0),
            atlasSide: new Vector2Int(0, 0),
            atlasBottom: new Vector2Int(0, 0),
            isSolid: true,
            isTransparent: false
        );

        // Create Regolith block definition (top: 1,0, bottom: 0,0, side: 0,1)
        CreateBlockDefinition(
            name: "Regolith",
            blockID: 2,
            atlasTop: new Vector2Int(1, 0),
            atlasSide: new Vector2Int(0, 1),
            atlasBottom: new Vector2Int(0, 0),
            isSolid: true,
            isTransparent: false
        );

        // Create Ice block definition
        CreateBlockDefinition(
            name: "Ice",
            blockID: 3,
            atlasTop: new Vector2Int(2, 0),
            atlasSide: new Vector2Int(2, 0),
            atlasBottom: new Vector2Int(2, 0),
            isSolid: true,
            isTransparent: false
        );

        // Create Metal block definition
        CreateBlockDefinition(
            name: "Metal",
            blockID: 4,
            atlasTop: new Vector2Int(3, 0),
            atlasSide: new Vector2Int(3, 0),
            atlasBottom: new Vector2Int(3, 0),
            isSolid: true,
            isTransparent: false
        );

        Debug.Log("[BlockDefinitionSetup] Block definitions created successfully!");
        AssetDatabase.Refresh();
    }

    private static void CreateBlockDefinition(
        string name, int blockID, Vector2Int atlasTop, Vector2Int atlasSide,
        Vector2Int atlasBottom, bool isSolid, bool isTransparent)
    {
        string path = $"{BlockDefinitionsFolder}/{name}.asset";

        // Check if already exists
        BlockDefinition existing = AssetDatabase.LoadAssetAtPath<BlockDefinition>(path);
        if (existing != null)
        {
            Debug.Log($"[BlockDefinitionSetup] {name} already exists at {path}, skipping.");
            return;
        }

        // Create new definition
        BlockDefinition definition = ScriptableObject.CreateInstance<BlockDefinition>();
        
        // Set private fields via reflection (since they're serialized but not public properties)
        var nameField = typeof(BlockDefinition).GetField("blockName", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var idField = typeof(BlockDefinition).GetField("blockID",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var atlasTopField = typeof(BlockDefinition).GetField("atlasTop",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var atlasSideField = typeof(BlockDefinition).GetField("atlasSide",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var atlasBottomField = typeof(BlockDefinition).GetField("atlasBottom",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isSolidField = typeof(BlockDefinition).GetField("isSolid",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var isTransparentField = typeof(BlockDefinition).GetField("isTransparent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        nameField?.SetValue(definition, name);
        idField?.SetValue(definition, blockID);
        atlasTopField?.SetValue(definition, atlasTop);
        atlasSideField?.SetValue(definition, atlasSide);
        atlasBottomField?.SetValue(definition, atlasBottom);
        isSolidField?.SetValue(definition, isSolid);
        isTransparentField?.SetValue(definition, isTransparent);

        // Save as asset
        AssetDatabase.CreateAsset(definition, path);
        Debug.Log($"[BlockDefinitionSetup] Created: {path}");
    }

    [MenuItem("Tools/Voxel/Clear Block Definitions")]
    public static void ClearBlockDefinitions()
    {
        if (!AssetDatabase.IsValidFolder(BlockDefinitionsFolder))
        {
            Debug.Log($"[BlockDefinitionSetup] Folder {BlockDefinitionsFolder} does not exist.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:BlockDefinition", new[] { BlockDefinitionsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
            Debug.Log($"[BlockDefinitionSetup] Deleted: {path}");
        }

        AssetDatabase.Refresh();
        Debug.Log("[BlockDefinitionSetup] Cleared all block definitions.");
    }
}
#endif
