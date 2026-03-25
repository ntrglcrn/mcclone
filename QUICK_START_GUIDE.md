# Quick Start Guide - Block Definition System

## 🚀 Get Started in 3 Steps

### Step 1: Prepare Files (1 minute)

**File to Replace:**
- Delete or backup: `Assets/Scripts/Chunk.cs`
- Rename: `Assets/Scripts/Chunk_Updated.cs` → `Chunk.cs`

**OR** manually update GetAtlasCell method in Chunk.cs with the new code from Chunk_Updated.cs.

### Step 2: Initialize System (30 seconds)

1. Open Unity Editor
2. Go to **Tools > Voxel > Setup Block Definitions**
3. Watch console for success message:
   ```
   [BlockDefinitionSetup] Block definitions created successfully!
   ```

This creates:
- Folder: `Assets/Resources/BlockDefinitions/`
- 5 block definition files (Air, Stone, Regolith, Ice, Metal)

### Step 3: Verify (1 minute)

1. Play the game
2. Check console for:
   ```
   [BlockDatabase] Initialized with 5 block types.
   ID 0: Air [...]
   ID 1: Stone [...]
   ...
   ```
3. Verify chunks render with correct textures

**✅ Done!** Your system is now using the block definition system.

---

## 📝 Adding Your First Custom Block

### Create a New Sand Block

1. Right-click in Project folder: **Create > Block Definition**
2. Name it: `Sand`
3. In Inspector, set:
   - **Block Name:** Sand
   - **Block ID:** 5
   - **Atlas Top:** (1, 1)
   - **Atlas Side:** (1, 1)
   - **Atlas Bottom:** (1, 1)
   - **Is Solid:** ✓ checked
   - **Is Transparent:** ☐ unchecked
4. Save and play
5. The Sand block is now available and will be loaded automatically

---

## 🎨 Using a Custom Asset Pack

Assume you have a 16×16 texture atlas from KayKit BlockBits.

1. **Import texture** as `Assets/Resources/Textures/blockbits_atlas.png`

2. **Update ChunkManager** (in Inspector):
   - Drag the new texture to the **atlasTexture** field
   - Change **atlasSize** from 4 to 16
   - Run **Tools > Voxel > Clear Block Definitions**

3. **Recreate block definitions** for the new atlas:
   - Edit `BlockDefinitionSetup.cs`
   - Add new CreateBlockDefinition calls with correct coordinates
   - Run **Tools > Voxel > Setup Block Definitions**

4. **Update terrain generation** in `Chunk.GenerateBlocks()`:
   ```csharp
   // Change from:
   blockType = Regolith;  // ID=2
   
   // To:
   blockType = 10;  // New KayKit_Grass block (ID=10)
   ```

5. Play and enjoy!

---

## 📊 Common Block Setups

### All Faces Same Texture
```
Top:    (2, 1)
Side:   (2, 1)
Bottom: (2, 1)
```
Example: Stone, Dirt, Concrete

### Directional Block (Top/Bottom Different)
```
Top:    (0, 0)
Side:   (1, 0)
Bottom: (2, 0)
```
Example: Grass (green top, dirt sides/bottom)

### Decorative (All Different)
```
Top:    (0, 2)
Side:   (1, 2)
Bottom: (2, 2)
```
Example: Signpost, mushroom

---

## 🔧 Troubleshooting

### Issue: "Block ID not found" in console
**Solution:** Run **Tools > Voxel > Setup Block Definitions** again

### Issue: Black blocks
**Solution:** Check atlas coordinates are valid (0-3 for 4×4 atlas)

### Issue: Changes don't appear
**Solution:** Restart Unity after modifying definitions

### Issue: "No BlockDatabase found" warning
**Solution:** The system auto-creates it. Just play and wait a moment.

---

## 📚 Learn More

- Full Guide: See `BLOCK_DEFINITION_GUIDE.md`
- Technical Details: See `IMPLEMENTATION_SUMMARY.md`
- Code Reference: See comments in `BlockDatabase.cs` and `BlockDefinition.cs`

---

## ⚡ Pro Tips

1. **Batch Changes:** Modify multiple block definitions before playing
2. **Copy & Paste:** Right-click existing block definitions to duplicate
3. **Organize by Pack:** Create subfolders in BlockDefinitions/ (e.g., `KayKit/`, `Custom/`)
4. **Debug Names:** Give blocks clear names for logging (e.g., "Stone_001")
5. **Backup Definitions:** Keep a copy before major changes

---

## 🎯 Next Level: Asset Pack Integration

Once comfortable with basics, you can:
1. Create a texture atlas builder (combine PNG files)
2. Auto-generate block definitions from metadata
3. Support multiple asset packs simultaneously
4. Create visual block editor UI in Unity

See `BLOCK_DEFINITION_GUIDE.md` section "Advanced Topics" for guidance.

---

**Happy building! 🎮**

