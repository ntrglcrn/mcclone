using UnityEngine;

public sealed class Inventory : MonoBehaviour
{
    [SerializeField] private int startingStone = 128;
    [SerializeField] private int startingRegolith = 128;
    [SerializeField] private int startingIce = 64;
    [SerializeField] private int startingMetal = 32;
    [SerializeField, Range(0, Chunk.BlockTypeCount - 1)] private byte selectedBlockType = Chunk.Regolith;

    private int[] blockCounts;

    public byte SelectedBlockType => selectedBlockType;
    public int SelectedIndex => Mathf.Clamp(selectedBlockType, 0, Chunk.BlockTypeCount - 1);

    private void Awake()
    {
        blockCounts = new int[Chunk.BlockTypeCount];
        blockCounts[Chunk.Stone] = startingStone;
        blockCounts[Chunk.Regolith] = startingRegolith;
        blockCounts[Chunk.Ice] = startingIce;
        blockCounts[Chunk.Metal] = startingMetal;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedBlockType = Chunk.Air;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedBlockType = Chunk.Stone;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            selectedBlockType = Chunk.Regolith;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            selectedBlockType = Chunk.Ice;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            selectedBlockType = Chunk.Metal;
        }
    }

    public void AddBlock(byte blockType, int amount = 1)
    {
        if (!IsValidBlock(blockType) || blockType == Chunk.Air)
        {
            return;
        }

        blockCounts[blockType] += amount;
    }

    public bool TryConsumeSelectedBlock()
    {
        if (!IsValidBlock(selectedBlockType) || selectedBlockType == Chunk.Air)
        {
            return false;
        }

        if (blockCounts[selectedBlockType] <= 0)
        {
            return false;
        }

        blockCounts[selectedBlockType]--;
        return true;
    }

    public int GetCount(byte blockType)
    {
        if (!IsValidBlock(blockType))
        {
            return 0;
        }

        return blockCounts[blockType];
    }

    public int GetHotbarCount(int slotIndex)
    {
        if (slotIndex <= Chunk.Air || slotIndex >= Chunk.BlockTypeCount)
        {
            return 0;
        }

        return blockCounts[slotIndex];
    }

    private static bool IsValidBlock(byte blockType)
    {
        return blockType < Chunk.BlockTypeCount;
    }
}
