using UnityEngine;

/// <summary>
/// Defines the visual properties of a block type.
/// Each block type maps to a texture cell in the atlas and can have different textures per face.
/// Instances of this are stored as ScriptableObjects in Resources/BlockDefinitions/.
/// </summary>
public class BlockDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string blockName = "Block";
    [SerializeField] private int blockID = 1;

    [Header("Visual - Atlas Coordinates")]
    [SerializeField] private Vector2Int atlasTop = Vector2Int.zero;
    [SerializeField] private Vector2Int atlasSide = Vector2Int.zero;
    [SerializeField] private Vector2Int atlasBottom = Vector2Int.zero;

    [Header("Properties")]
    [SerializeField] private bool isSolid = true;
    [SerializeField] private bool isTransparent = false;

    // ─ Properties ─────────────────────────────────────────────────────────────

    public string BlockName => blockName;
    public int BlockID => blockID;
    
    public Vector2Int AtlasTop => atlasTop;
    public Vector2Int AtlasSide => atlasSide;
    public Vector2Int AtlasBottom => atlasBottom;
    
    public bool IsSolid => isSolid;
    public bool IsTransparent => isTransparent;

    // ─ Validation ──────────────────────────────────────────────────────────────

    public bool IsValid(int maxAtlasCoord)
    {
        if (blockID < 0 || blockID > 255)
            return false;

        if (!IsValidAtlasCoord(atlasTop, maxAtlasCoord)) return false;
        if (!IsValidAtlasCoord(atlasSide, maxAtlasCoord)) return false;
        if (!IsValidAtlasCoord(atlasBottom, maxAtlasCoord)) return false;

        return true;
    }

    private bool IsValidAtlasCoord(Vector2Int coord, int maxCoord)
    {
        return coord.x >= 0 && coord.x < maxCoord &&
               coord.y >= 0 && coord.y < maxCoord;
    }

    public Vector2Int GetFaceAtlasCoord(int faceIndex)
    {
        return faceIndex switch
        {
            0 => atlasTop,      // +Y (top)
            1 => atlasBottom,   // -Y (bottom)
            _ => atlasSide      // All sides (2-5)
        };
    }

    public override string ToString() => $"BlockDef({blockID}: {blockName})";
}
