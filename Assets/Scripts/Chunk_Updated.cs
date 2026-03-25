// Assets/Scripts/Chunk.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public sealed class Chunk : MonoBehaviour
{
    public const int Size = 16;

    // Block type IDs - managed by BlockDatabase now
    public const byte Air      = 0;
    public const byte Stone    = 1;
    public const byte Regolith = 2;
    public const byte Ice      = 3;
    public const byte Metal    = 4;

    // ── State ────────────────────────────────────────────────────────────────
    public Vector3Int ChunkPosition { get; private set; }   // in chunk coords
    private byte[,,] blocks;
    private ChunkManager manager;

    // ── Components ───────────────────────────────────────────────────────────
    private MeshFilter   meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh         chunkMesh;

    // ── Static geometry tables (same as VoxelWorld) ──────────────────────────
    private static readonly Vector3Int[] NeighborDirections =
    {
        new Vector3Int( 0,  1,  0),
        new Vector3Int( 0, -1,  0),
        new Vector3Int( 0,  0,  1),
        new Vector3Int( 0,  0, -1),
        new Vector3Int(-1,  0,  0),
        new Vector3Int( 1,  0,  0)
    };

    private static readonly Vector3[,] FaceVertices =
    {
        { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) },
        { new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1), new Vector3(0,0,1) },
        { new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,0,1) },
        { new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0), new Vector3(0,0,0) },
        { new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(0,0,1) },
        { new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(1,1,0), new Vector3(1,0,0) }
    };

    private static readonly Vector2[] FaceUVTemplate =
    {
        new Vector2(0,0), new Vector2(0,1),
        new Vector2(1,1), new Vector2(1,0)
    };

    // ── Init ─────────────────────────────────────────────────────────────────

    public void Init(Vector3Int chunkPos, ChunkManager chunkManager)
    {
        ChunkPosition = chunkPos;
        manager       = chunkManager;
        blocks        = new byte[Size, Size, Size];

        meshFilter   = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        meshRenderer.sharedMaterial = manager.AtlasMaterial;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public byte GetBlock(int x, int y, int z)
    {
        if (!InBounds(x, y, z)) return Air;
        return blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, byte blockType)
    {
        if (!InBounds(x, y, z)) return;
        blocks[x, y, z] = blockType;
    }

    // ── Generation ────────────────────────────────────────────────────────────

    public void GenerateBlocks()
    {
        int seed            = manager.Seed;
        float heightScale   = manager.HeightNoiseScale;
        float iceScale      = manager.IceNoiseScale;
        float metalChance   = manager.MetalChance;

        var random = new System.Random(seed ^ (ChunkPosition.x * 73856093) ^ (ChunkPosition.z * 19349663));

        int worldOffsetX = ChunkPosition.x * Size;
        int worldOffsetZ = ChunkPosition.z * Size;

        for (int x = 0; x < Size; x++)
        {
            for (int z = 0; z < Size; z++)
            {
                int wx = worldOffsetX + x;
                int wz = worldOffsetZ + z;

                float noiseX = (wx + seed) * heightScale;
                float noiseZ = (wz + seed) * heightScale;
                float terrainNoise = Mathf.PerlinNoise(noiseX, noiseZ);
                int surfaceHeight = Mathf.Clamp(Mathf.FloorToInt(terrainNoise * 8f) + 4, 2, Size - 1);

                float iceNoise = Mathf.PerlinNoise((wx + seed + 500) * iceScale, (wz + seed + 500) * iceScale);
                bool iceColumn = iceNoise > 0.73f;

                for (int y = 0; y < Size; y++)
                {
                    // Account for chunk Y offset
                    int wy = ChunkPosition.y * Size + y;

                    if (wy > surfaceHeight)
                    {
                        blocks[x, y, z] = Air;
                        continue;
                    }

                    byte blockType;
                    if (wy == surfaceHeight)
                    {
                        blockType = Regolith;
                    }
                    else if (iceColumn && wy >= surfaceHeight - 2)
                    {
                        blockType = Ice;
                    }
                    else if (wy > 1 && wy < surfaceHeight - 1 && random.NextDouble() < metalChance)
                    {
                        blockType = Metal;
                    }
                    else
                    {
                        blockType = Stone;
                    }

                    blocks[x, y, z] = blockType;
                }
            }
        }
    }

    // ── Mesh building ─────────────────────────────────────────────────────────

    public void BuildMesh()
    {
        if (chunkMesh == null)
        {
            chunkMesh = new Mesh
            {
                name         = $"Chunk {ChunkPosition}",
                indexFormat  = UnityEngine.Rendering.IndexFormat.UInt32
            };
        }
        else
        {
            chunkMesh.Clear();
        }

        var vertices  = new List<Vector3>(4096);
        var triangles = new List<int>(6144);
        var uvs       = new List<Vector2>(4096);

        for (int x = 0; x < Size; x++)
        for (int y = 0; y < Size; y++)
        for (int z = 0; z < Size; z++)
        {
            byte blockType = blocks[x, y, z];
            if (blockType == Air) continue;

            for (int face = 0; face < 6; face++)
            {
                Vector3Int dir = NeighborDirections[face];
                int nx = x + dir.x, ny = y + dir.y, nz = z + dir.z;

                byte neighbor = InBounds(nx, ny, nz)
                    ? blocks[nx, ny, nz]
                    : GetNeighborFromAdjacentChunk(nx, ny, nz);

                if (neighbor != Air) continue;

                AddFace(vertices, triangles, uvs, new Vector3(x, y, z), blockType, face);
            }
        }

        chunkMesh.SetVertices(vertices);
        chunkMesh.SetTriangles(triangles, 0, true);
        chunkMesh.SetUVs(0, uvs);
        chunkMesh.RecalculateNormals();
        chunkMesh.RecalculateBounds();

        meshFilter.sharedMesh = chunkMesh;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = chunkMesh;
    }

    // ── Seam handling ─────────────────────────────────────────────────────────

    private byte GetNeighborFromAdjacentChunk(int localX, int localY, int localZ)
    {
        // Convert local-out-of-bounds coord to neighbor chunk + local coord
        int cx = ChunkPosition.x + DivFloor(localX, Size);
        int cy = ChunkPosition.y + DivFloor(localY, Size);
        int cz = ChunkPosition.z + DivFloor(localZ, Size);

        Chunk neighbor = manager.GetChunk(new Vector3Int(cx, cy, cz));
        if (neighbor == null) return Air; // treat unloaded as air

        int bx = Mod(localX, Size);
        int by = Mod(localY, Size);
        int bz = Mod(localZ, Size);
        return neighbor.GetBlock(bx, by, bz);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddFace(List<Vector3> vertices, List<int> triangles,
                         List<Vector2> uvs, Vector3 pos, byte blockType, int faceIndex)
    {
        int start = vertices.Count;

        Vector2Int cell = GetAtlasCell(blockType, faceIndex);
        AddFaceUVs(uvs, cell);

        for (int i = 0; i < 4; i++)
            vertices.Add(pos + FaceVertices[faceIndex, i]);

        if (faceIndex >= 2)
        {
            triangles.Add(start);     triangles.Add(start + 2); triangles.Add(start + 1);
            triangles.Add(start);     triangles.Add(start + 3); triangles.Add(start + 2);
        }
        else
        {
            triangles.Add(start);     triangles.Add(start + 1); triangles.Add(start + 2);
            triangles.Add(start);     triangles.Add(start + 2); triangles.Add(start + 3);
        }
    }

    private void AddFaceUVs(List<Vector2> uvs, Vector2Int cell)
    {
        float ts      = manager.TileSize;
        float pad     = manager.AtlasPadding;
        float minU    = cell.x * ts + pad;
        float maxU    = (cell.x + 1) * ts - pad;
        float minV    = 1f - (cell.y + 1) * ts + pad;
        float maxV    = 1f - cell.y * ts - pad;

        foreach (Vector2 uv in FaceUVTemplate)
            uvs.Add(new Vector2(Mathf.Lerp(minU, maxU, uv.x), Mathf.Lerp(minV, maxV, uv.y)));
    }

    private static Vector2Int GetAtlasCell(byte blockType, int faceIndex)
    {
        BlockDatabase db = BlockDatabase.Instance;
        BlockDefinition def = db.Get(blockType);
        return def.GetFaceAtlasCoord(faceIndex);
    }

    private static bool InBounds(int x, int y, int z) =>
        x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;

    // Floor-division that works correctly for negatives
    private static int DivFloor(int a, int b) =>
        a >= 0 ? a / b : (a - b + 1) / b;

    // Positive modulo
    private static int Mod(int a, int b) =>
        ((a % b) + b) % b;

    private void OnDestroy()
    {
        if (chunkMesh != null) Destroy(chunkMesh);
    }
}
