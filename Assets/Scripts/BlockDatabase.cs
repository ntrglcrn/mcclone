using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for all block definitions in the world.
/// Loads block definitions from Resources/BlockDefinitions/ and provides lookup methods.
/// </summary>
public class BlockDatabase : MonoBehaviour
{
    private static BlockDatabase instance;
    
    [SerializeField] private int atlasSize = 4; // Must match ChunkManager.atlasSize

    private Dictionary<int, BlockDefinition> definitions = new Dictionary<int, BlockDefinition>();
    private BlockDefinition airDefinition;

    // ─ Singleton ───────────────────────────────────────────────────────────────

    public static BlockDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BlockDatabase>();
                if (instance == null)
                {
                    var go = new GameObject("BlockDatabase");
                    instance = go.AddComponent<BlockDatabase>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    // ─ Initialization ──────────────────────────────────────────────────────────

    private void Initialize()
    {
        definitions.Clear();
        
        // Load all block definitions from Resources/BlockDefinitions/
        BlockDefinition[] allDefinitions = Resources.LoadAll<BlockDefinition>("BlockDefinitions");
        
        foreach (var def in allDefinitions)
        {
            if (def == null) continue;
            
            if (!def.IsValid(atlasSize))
            {
                Debug.LogError($"BlockDatabase: Invalid definition {def.name}. Atlas coords out of range [0, {atlasSize-1}].");
                continue;
            }

            if (definitions.ContainsKey(def.BlockID))
            {
                Debug.LogWarning($"BlockDatabase: Duplicate block ID {def.BlockID}. Skipping {def.name}.");
                continue;
            }

            definitions[def.BlockID] = def;
            
            // Store air definition separately
            if (def.BlockID == 0)
                airDefinition = def;
        }

        if (airDefinition == null)
        {
            Debug.LogError("BlockDatabase: No Air block definition found (ID=0). Creating fallback.");
            airDefinition = CreateFallbackAirDefinition();
            definitions[0] = airDefinition;
        }

        Debug.Log($"[BlockDatabase] Initialized with {definitions.Count} block types.");
        LogDefinitions();
    }

    // ─ Lookup methods ──────────────────────────────────────────────────────────

    /// <summary>Get block definition by ID. Returns Air if not found.</summary>
    public BlockDefinition Get(int blockID)
    {
        if (definitions.TryGetValue(blockID, out var def))
            return def;

        Debug.LogWarning($"[BlockDatabase] Block ID {blockID} not found. Returning Air.");
        return airDefinition;
    }

    /// <summary>Get block definition by name. Returns null if not found.</summary>
    public BlockDefinition GetByName(string name)
    {
        foreach (var def in definitions.Values)
        {
            if (def.BlockName.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return def;
        }
        return null;
    }

    /// <summary>Get all registered block definitions.</summary>
    public IEnumerable<BlockDefinition> GetAll() => definitions.Values;

    public int Count => definitions.Count;
    public BlockDefinition Air => airDefinition;

    // ─ Validation ──────────────────────────────────────────────────────────────

    public void ValidateConsistency()
    {
        if (definitions.Count == 0)
        {
            Debug.LogError("[BlockDatabase] No block definitions loaded!");
            return;
        }

        if (airDefinition == null)
        {
            Debug.LogError("[BlockDatabase] Air block (ID=0) not found!");
            return;
        }

        if (!airDefinition.IsSolid)
        {
            Debug.LogWarning("[BlockDatabase] Air block should not be solid.");
        }

        // Check for ID gaps (optional - not required, but helpful for debugging)
        int maxID = 0;
        foreach (int id in definitions.Keys)
            if (id > maxID) maxID = id;

        if (definitions.Count != maxID + 1)
        {
            Debug.LogWarning($"[BlockDatabase] Block IDs have gaps. Count={definitions.Count}, MaxID={maxID}. This is OK but may waste memory.");
        }
    }

    // ─ Debugging ───────────────────────────────────────────────────────────────

    private void LogDefinitions()
    {
        foreach (var kvp in definitions)
        {
            var def = kvp.Value;
            Debug.Log($"  ID {def.BlockID}: {def.BlockName} " +
                      $"[Top:{def.AtlasTop} Side:{def.AtlasSide} Bottom:{def.AtlasBottom}] " +
                      $"Solid={def.IsSolid} Transparent={def.IsTransparent}");
        }
    }

    // ─ Fallback ────────────────────────────────────────────────────────────────

    private BlockDefinition CreateFallbackAirDefinition()
    {
        var def = CreateInstance<BlockDefinition>();
        def.name = "Air";
        return def;
    }
}
