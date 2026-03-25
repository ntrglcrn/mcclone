// Assets/Scripts/ChunkManager.cs
using System.Collections.Generic;
using UnityEngine;

public sealed class ChunkManager : MonoBehaviour
{
    private const string DefaultAtlasResourcePath = "Textures/block_atlas";

    // ── Inspector ─────────────────────────────────────────────────────────────
    [SerializeField] public Transform player;

    [Header("World")]
    [SerializeField] private int seed = 1337;
    [SerializeField] private float heightNoiseScale = 0.09f;
    [SerializeField] private float iceNoiseScale    = 0.18f;
    [SerializeField, Range(0.001f, 0.05f)] private float metalChance = 0.008f;

    [Header("Atlas")]
    [SerializeField] private int atlasSize = 4;
    [SerializeField, Range(0f, 0.01f)] private float atlasPadding = 0.001f;
    [SerializeField] private Texture2D atlasTexture;
    [SerializeField] private Material  atlasMaterial;

    [Header("Loading")]
    [Tooltip("Chunks loaded in each XZ direction (4 = 9×9 = 81 slots in XZ plane)")]
    [SerializeField] private int loadRadius = 4;
    [Tooltip("Chunks loaded above/below player (2 = 5 Y-levels total)")]
    [SerializeField] private int loadRadiusY = 2;
    [Tooltip("Max active chunks in memory (9×9×5 = 405 chunks max)")]
    [SerializeField] private int maxActiveChunks = 512;
    [Tooltip("Seconds between load/unload passes")]
    [SerializeField] private float updateInterval = 0.2f;

    // ── Public read-only properties used by Chunk ─────────────────────────────
    public int   Seed             => seed;
    public float HeightNoiseScale => heightNoiseScale;
    public float IceNoiseScale    => iceNoiseScale;
    public float MetalChance      => metalChance;
    public float TileSize         => 1f / Mathf.Max(1, atlasSize);
    public float AtlasPadding     => atlasPadding;
    public Material AtlasMaterial => atlasMaterial;

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly Dictionary<Vector3Int, Chunk> activeChunks = new Dictionary<Vector3Int, Chunk>();
    private float nextUpdateTime;
    private Material runtimeMaterial;

    // ── Unity ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Initialize BlockDatabase singleton first
        BlockDatabase.Instance.ValidateConsistency();
        
        EnsureMaterials();
    }

    private void Update()
    {
        if (player == null) return;
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + updateInterval;

        Vector3Int playerChunk = WorldToChunkCoord(player.position);
        LoadChunksAround(playerChunk);
        UnloadDistantChunks(playerChunk);

        Debug.Log($"[ChunkManager] Active chunks: {activeChunks.Count}");
    }

    // ── Chunk lookup ──────────────────────────────────────────────────────────

    /// <summary>Returns the Chunk at the given chunk-coordinate, or null.</summary>
    public Chunk GetChunk(Vector3Int chunkCoord)
    {
        activeChunks.TryGetValue(chunkCoord, out Chunk chunk);
        return chunk;
    }

    /// <summary>Returns the Chunk that contains the given world-space position, or null.</summary>
    public Chunk GetChunkAt(Vector3 worldPos)
    {
        return GetChunk(WorldToChunkCoord(worldPos));
    }

    // ── Coordinate helpers ────────────────────────────────────────────────────

    public static Vector3Int WorldToChunkCoord(Vector3 worldPos)
    {
        return new Vector3Int(
            FloorDiv(Mathf.FloorToInt(worldPos.x), Chunk.Size),
            FloorDiv(Mathf.FloorToInt(worldPos.y), Chunk.Size),
            FloorDiv(Mathf.FloorToInt(worldPos.z), Chunk.Size));
    }

    public static Vector3Int WorldToLocalBlock(Vector3 worldPos)
    {
        int wx = Mathf.FloorToInt(worldPos.x);
        int wy = Mathf.FloorToInt(worldPos.y);
        int wz = Mathf.FloorToInt(worldPos.z);
        return new Vector3Int(PosMod(wx, Chunk.Size), PosMod(wy, Chunk.Size), PosMod(wz, Chunk.Size));
    }

    // ── Loading / unloading ───────────────────────────────────────────────────

    private void LoadChunksAround(Vector3Int center)
    {
        // Load chunks in 3D: XZ radius + Y radius for proper vertical chunk support
        for (int dy = -loadRadiusY; dy <= loadRadiusY; dy++)
        for (int dx = -loadRadius; dx <= loadRadius; dx++)
        for (int dz = -loadRadius; dz <= loadRadius; dz++)
        {
            if (activeChunks.Count >= maxActiveChunks) return;

            var coord = new Vector3Int(center.x + dx, center.y + dy, center.z + dz);
            if (activeChunks.ContainsKey(coord)) continue;

            LoadChunk(coord);
        }
    }

    private void LoadChunk(Vector3Int coord)
    {
        var go = new GameObject($"Chunk {coord}");
        go.transform.parent   = transform;
        go.transform.position = new Vector3(coord.x * Chunk.Size, coord.y * Chunk.Size, coord.z * Chunk.Size);

        Chunk chunk = go.AddComponent<Chunk>();
        chunk.Init(coord, this);
        chunk.GenerateBlocks();
        chunk.BuildMesh();

        activeChunks[coord] = chunk;
    }

    private void UnloadDistantChunks(Vector3Int center)
    {
        var toRemove = new List<Vector3Int>();

        foreach (var kvp in activeChunks)
        {
            Vector3Int c = kvp.Key;
            int distX = Mathf.Abs(c.x - center.x);
            int distY = Mathf.Abs(c.y - center.y);
            int distZ = Mathf.Abs(c.z - center.z);
            if (distX > loadRadius + 1 || distY > loadRadiusY + 1 || distZ > loadRadius + 1)
                toRemove.Add(c);
        }

        foreach (var coord in toRemove)
        {
            Destroy(activeChunks[coord].gameObject);
            activeChunks.Remove(coord);
        }
    }

    // ── Material setup ────────────────────────────────────────────────────────

    private void EnsureMaterials()
    {
        if (atlasTexture == null)
        {
            atlasTexture = Resources.Load<Texture2D>(DefaultAtlasResourcePath);
            ApplyAtlasTextureSettings(atlasTexture);
        }

        if (atlasMaterial != null)
        {
            if (atlasTexture != null) atlasMaterial.mainTexture = atlasTexture;
            return;
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null) return;

        runtimeMaterial = new Material(shader)
        {
            name  = "RuntimeVoxelAtlasMaterial",
            color = Color.white
        };
        runtimeMaterial.mainTexture = atlasTexture;
        runtimeMaterial.SetFloat("_Glossiness", 0.05f);
        atlasMaterial = runtimeMaterial;
    }

    private static void ApplyAtlasTextureSettings(Texture2D tex)
    {
        if (tex == null) return;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode   = TextureWrapMode.Clamp;
    }

    // ── Math utils ────────────────────────────────────────────────────────────

    private static int FloorDiv(int a, int b) =>
        a >= 0 ? a / b : (a - b + 1) / b;

    private static int PosMod(int a, int b) =>
        ((a % b) + b) % b;

    // ── Gizmos ────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
        foreach (var kvp in activeChunks)
        {
            Vector3 center = kvp.Value.transform.position + Vector3.one * (Chunk.Size * 0.5f);
            Gizmos.DrawWireCube(center, Vector3.one * Chunk.Size);
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
