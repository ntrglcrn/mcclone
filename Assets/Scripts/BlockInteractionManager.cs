using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class BlockInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private ChunkManager chunkManager;
    [SerializeField] private Inventory inventory;
    [SerializeField] private float reachDistance = 6f;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (inventory == null)
            inventory = GetComponent<Inventory>();

        if (playerCamera == null)
        {
            PlayerController pc = GetComponent<PlayerController>();
            if (pc != null) playerCamera = pc.PlayerCamera;
        }

        if (chunkManager == null)
            chunkManager = Object.FindObjectOfType<ChunkManager>();
    }

    private void Update()
    {
        if (playerCamera == null || chunkManager == null || inventory == null) return;

        if (Input.GetMouseButtonDown(0)) RemoveBlock();
        if (Input.GetMouseButtonDown(1)) PlaceBlock();
    }

    public void Configure(Camera targetCamera, ChunkManager manager, Inventory targetInventory)
    {
        playerCamera = targetCamera;
        chunkManager = manager;
        inventory    = targetInventory;
    }

    private void RemoveBlock()
    {
        if (!TryRaycast(-0.01f, out Vector3 hitPoint, out Chunk chunk)) return;

        Vector3Int local = ChunkManager.WorldToLocalBlock(hitPoint);
        byte removed = chunk.GetBlock(local.x, local.y, local.z);
        if (removed == Chunk.Air) return;

        chunk.SetBlock(local.x, local.y, local.z, Chunk.Air);
        chunk.BuildMesh();
        inventory.AddBlock(removed);
    }

    private void PlaceBlock()
    {
        byte blockType = inventory.SelectedBlockType;
        if (blockType == Chunk.Air) return;

        if (!TryRaycast(0.01f, out Vector3 hitPoint, out Chunk chunk)) return;

        Vector3Int local = ChunkManager.WorldToLocalBlock(hitPoint);
        if (chunk.GetBlock(local.x, local.y, local.z) != Chunk.Air) return;

        if (WouldOverlapPlayer(chunk, local)) return;
        if (!inventory.TryConsumeSelectedBlock()) return;

        chunk.SetBlock(local.x, local.y, local.z, blockType);
        chunk.BuildMesh();
    }

    private bool TryRaycast(float hitOffset, out Vector3 hitPoint, out Chunk hitChunk)
    {
        hitPoint = default;
        hitChunk = null;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, reachDistance)) return false;

        hitChunk = hit.collider.GetComponent<Chunk>();
        if (hitChunk == null) return false;

        hitPoint = hit.point + hit.normal * hitOffset;
        return true;
    }

    private bool WouldOverlapPlayer(Chunk chunk, Vector3Int localPos)
    {
        Vector3 blockCenter = chunk.transform.position +
                              new Vector3(localPos.x + 0.5f, localPos.y + 0.5f, localPos.z + 0.5f);
        Bounds blockBounds = new Bounds(blockCenter, Vector3.one * 0.98f);
        return characterController.bounds.Intersects(blockBounds);
    }
}
