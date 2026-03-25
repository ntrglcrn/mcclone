# Voxel Block Definition System - Integration Guide

## Overview

The MCClone voxel system has been refactored to use a flexible **Block Definition System** based on ScriptableObjects. This replaces the previous hardcoded block type constants with a scalable, asset-pack-friendly architecture.

### Key Benefits
✅ Support for unlimited block types  
✅ Per-block texture variations (top/side/bottom faces)  
✅ Easy integration of asset packs (KayKit BlockBits, etc.)  
✅ No changes to mesh-based rendering or chunk system  
✅ Backward compatible with existing worlds  

---

## Architecture

### Core Components

#### 1. **BlockDefinition.cs** (ScriptableObject)
Defines the visual properties of a single block type:

```csharp
public class BlockDefinition : ScriptableObject
{
    public string BlockName;                    // Display name
    public int BlockID;                         // Unique ID (0-255)
    public Vector2Int AtlasTop;                 // Top face texture coordinate
    public Vector2Int AtlasSide;                // Side faces texture coordinate
    public Vector2Int AtlasBottom;              // Bottom face texture coordinate
    public bool IsSolid;                        // Blocks movement?
    public bool IsTransparent;                  // Is visually transparent?
}
```

**Coordinate System:**
- Atlas coordinates (0-3) in a 4×4 grid
- (0,0) = bottom-left, (3,3) = top-right in texture space
- Atlas cell size = 1/4 of texture (0.25)

#### 2. **BlockDatabase.cs** (Singleton)
Manages all block definitions at runtime:

```csharp
public class BlockDatabase : MonoBehaviour
{
    // Get block definition by ID (returns Air if not found)
    BlockDefinition Get(int blockID);
    
    // Get block definition by name
    BlockDefinition GetByName(string name);
    
    // Get all definitions
    IEnumerable<BlockDefinition> GetAll();
    
    // Validate all definitions are valid
    void ValidateConsistency();
}
```

**Initialization:**
- Automatically created as singleton if missing
- Loads all BlockDefinitions from `Resources/BlockDefinitions/`
- Called on ChunkManager.Awake()

#### 3. **Updated Chunk.cs**
The mesh generation now uses BlockDatabase instead of hardcoded switch statements:

```csharp
// Old (hardcoded):
private static Vector2Int GetAtlasCell(byte blockType, int faceIndex)
{
    switch (blockType) { ... }
}

// New (database-driven):
private static Vector2Int GetAtlasCell(byte blockType, int faceIndex)
{
    BlockDatabase db = BlockDatabase.Instance;
    BlockDefinition def = db.Get(blockType);
    return def.GetFaceAtlasCoord(faceIndex);  // Returns atlasTop/Side/Bottom based on face
}
```

---

## Setup Instructions

### Step 1: Create Block Definitions (Automatic)

Run the editor setup tool:

1. Open Unity Editor
2. Go to **Tools > Voxel > Setup Block Definitions**
3. This creates:
   - Folder: `Assets/Resources/BlockDefinitions/`
   - Files: `Air.asset`, `Stone.asset`, `Regolith.asset`, `Ice.asset`, `Metal.asset`

**Result:**  
5 block definitions with the same texture mapping as the original system.

```
Assets/Resources/BlockDefinitions/
├── Air.asset           (ID=0, solid=false)
├── Stone.asset         (ID=1, all faces: 0,0)
├── Regolith.asset      (ID=2, top: 1,0, side: 0,1, bottom: 0,0)
├── Ice.asset           (ID=3, all faces: 2,0)
└── Metal.asset         (ID=4, all faces: 3,0)
```

### Step 2: Verify Initialization

Launch the game and check the Console for:

```
[BlockDatabase] Initialized with 5 block types.
  ID 0: Air [Top:(0, 0) Side:(0, 0) Bottom:(0, 0)] Solid=False Transparent=True
  ID 1: Stone [Top:(0, 0) Side:(0, 0) Bottom:(0, 0)] Solid=True Transparent=False
  ID 2: Regolith [Top:(1, 0) Side:(0, 1) Bottom:(0, 0)] Solid=True Transparent=False
  ID 3: Ice [Top:(2, 0) Side:(2, 0) Bottom:(2, 0)] Solid=True Transparent=False
  ID 4: Metal [Top:(3, 0) Side:(3, 0) Bottom:(3, 0)] Solid=True Transparent=False
```

---

## Adding New Blocks

### Method 1: Using the Editor UI (Manual)

1. Right-click in project folder: **Create > Block Definition**
2. Name it (e.g., "Sand.asset")
3. In Inspector, set:
   - **Block Name:** Sand
   - **Block ID:** 5 (must be unique and different from existing)
   - **Atlas Top:** (1, 1)    # Texture grid position
   - **Atlas Side:** (1, 1)
   - **Atlas Bottom:** (1, 1)
   - **Is Solid:** ✓
   - **Is Transparent:** ☐
4. Save
5. The database will automatically load it on next play

### Method 2: Automated Setup Script

Modify `BlockDefinitionSetup.cs` and add a new block:

```csharp
// In SetupBlockDefinitions() method, add:
CreateBlockDefinition(
    name: "Sand",
    blockID: 5,
    atlasTop: new Vector2Int(1, 1),
    atlasSide: new Vector2Int(1, 1),
    atlasBottom: new Vector2Int(1, 1),
    isSolid: true,
    isTransparent: false
);
```

Then run **Tools > Voxel > Setup Block Definitions** again.

### Method 3: Code Generation from Textures (Advanced)

To support full asset packs:

1. Import textures (PNG) into `Assets/Resources/Textures/blocks/`
2. Create a tool that:
   - Scans the folder
   - Builds a texture atlas automatically
   - Generates BlockDefinitions from metadata file

See **Optional: Asset Pack Integration** below.

---

## Modifying Existing Blocks

To change a block's appearance:

1. Open the `.asset` file in the Inspector
2. Modify **Atlas Top/Side/Bottom** coordinates
3. Save
4. Changes apply on next play (no recompilation needed)

**Example: Make Regolith grass-like**

- **Before:**  
  Top: (1,0) | Side: (0,1) | Bottom: (0,0)
- **After:**  
  Top: (2,1) | Side: (0,1) | Bottom: (0,0)

---

## Using Custom Asset Packs

### Example: KayKit BlockBits Textures

Assume you have imported BlockBits textures into a 16×16 atlas.

**Step 1: Update Atlas Settings**

In ChunkManager Inspector:
- Change **atlasSize** from 4 to 16
- Assign the new atlas texture

**Step 2: Create Block Definitions**

For each block in BlockBits, create a definition:

```csharp
// Example: KayKit Grass Block
CreateBlockDefinition(
    name: "KayKit_Grass",
    blockID: 10,
    atlasTop: new Vector2Int(5, 12),      // Grass top
    atlasSide: new Vector2Int(4, 12),     // Dirt side
    atlasBottom: new Vector2Int(4, 11),   // Dirt bottom
    isSolid: true,
    isTransparent: false
);
```

**Step 3: Update World Generation**

Modify `Chunk.GenerateBlocks()` to use new block IDs:

```csharp
// Old:
blockType = Regolith;  // ID=2

// New:
blockType = 10;  // KayKit_Grass (via BlockDatabase)
```

---

## Block ID Allocation Guide

| ID Range | Purpose | Example |
|----------|---------|---------|
| 0 | Air (reserved) | Always ID=0 |
| 1-9 | Core blocks | Stone, Wood, Sand |
| 10-99 | Asset pack blocks | KayKit, Voxel Art Packs |
| 100-199 | Custom blocks | Modded content |
| 200-255 | Reserved | Future use |

**Important:** Never change block ID of existing blocks in saves, as it breaks chunk data!

---

## Troubleshooting

### Issue: "Block ID not found" warnings

**Cause:** Block definitions not loaded  
**Solution:**
1. Check folder exists: `Assets/Resources/BlockDefinitions/`
2. Run **Tools > Voxel > Setup Block Definitions**
3. Restart Unity

### Issue: Black/invisible blocks

**Cause:** Atlas coordinates out of range  
**Solution:**
1. Open the block definition
2. Verify Atlas Top/Side/Bottom coordinates are in range [0, atlasSize-1]
3. For 4×4 grid: valid range is 0-3

### Issue: Texture bleeding (colors bleeding between blocks)

**Cause:** Atlas padding too small  
**Solution:**
1. In ChunkManager, increase **Atlas Padding** from 0.001 to 0.002
2. This adds more space between texture cells

### Issue: Block appears in one chunk but not another

**Cause:** Chunk regeneration or seam issue  
**Solution:**
1. Clear all chunks from memory
2. Re-enter play mode
3. Chunks will regenerate with correct block definitions

---

## Performance Notes

- **No performance impact:** BlockDatabase lookup is O(1) dictionary access
- **Memory:** Each BlockDefinition is small (~100 bytes)
- **Mesh generation:** Same speed as before (uses precomputed UV coordinates)

---

## Migration from Old System

### Backward Compatibility

The old block constants are preserved for compatibility:

```csharp
public const byte Air      = 0;
public const byte Stone    = 1;
public const byte Regolith = 2;
public const byte Ice      = 3;
public const byte Metal    = 4;
```

Existing chunk data (block IDs) works unchanged. Only the texture mapping has changed.

### Migrating Custom Block Code

If you have custom code referencing block types:

**Before:**
```csharp
if (blockType == Chunk.Stone) { ... }
```

**After:**
```csharp
BlockDefinition def = BlockDatabase.Instance.Get(blockType);
if (def.BlockName == "Stone") { ... }
```

---

## Advanced Topics

### Adding Transparency Support

To implement transparent blocks (glass, water):

1. Create block definition with `isTransparent = true`
2. Update mesh generation to:
   - Render transparent faces even if adjacent to solid blocks
   - Sort faces back-to-front
   - Use blended material

**Note:** Current implementation treats this as metadata only. Rendering support requires shader changes.

### Animated Blocks

To support animated textures:

1. Extend BlockDefinition with animation data:
   ```csharp
   public Vector2Int[] AnimatedFrames;
   public float FrameRate;
   ```

2. Update UV generation in Chunk.cs to cycle through frames

3. Material shader can handle animation as well

### Texture Atlas Builder

To automatically generate atlases from individual texture files:

1. Create `TextureAtlasBuilder.cs` in Editor
2. Scan folder of PNG textures
3. Combine into single atlas texture
4. Generate BlockDefinitions automatically

See `Optional: Asset Pack Integration` section in planning docs.

---

## File Locations

```
MCClone/
├── Assets/
│   ├── Resources/
│   │   └── BlockDefinitions/          ← Create here
│   │       ├── Air.asset
│   │       ├── Stone.asset
│   │       ├── Regolith.asset
│   │       ├── Ice.asset
│   │       └── Metal.asset
│   ├── Scripts/
│   │   ├── BlockDefinition.cs         ← New core class
│   │   ├── BlockDatabase.cs           ← New singleton manager
│   │   ├── Chunk.cs                   ← Updated (uses BlockDatabase)
│   │   ├── ChunkManager.cs            ← Updated (initializes BlockDatabase)
│   │   └── Editor/
│   │       └── BlockDefinitionSetup.cs ← New editor tool
│   └── Materials/
│       └── VoxelAtlas.mat             ← Unchanged
└── ...
```

---

## FAQ

**Q: Can I have different block IDs for the same visual?**  
A: Yes. Create multiple block definitions with the same atlas coordinates but different BlockIDs and names.

**Q: What happens if I delete a block definition?**  
A: Blocks with that ID will show as Air (ID=0) visually. Chunk data is preserved.

**Q: Can I change block IDs after creating a world?**  
A: No, block IDs are stored in chunk data. Changing IDs breaks existing saves.

**Q: How many blocks can I have?**  
A: Up to 256 (byte range 0-255). Practical limit depends on atlas size.

**Q: Does this work with existing chunks?**  
A: Yes! Chunk data (block IDs) is unchanged. Only the visual mapping changed.

---

## Support

For issues or feature requests:
1. Check the troubleshooting section above
2. Verify block definitions are in `Assets/Resources/BlockDefinitions/`
3. Check console for BlockDatabase initialization messages
4. Ensure ChunkManager.atlasSize matches your texture atlas

