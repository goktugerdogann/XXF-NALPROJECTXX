using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;
}

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    public int slotCount = 16;
    public List<InventorySlot> slots = new List<InventorySlot>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            slots.Clear();
            for (int i = 0; i < slotCount; i++)
                slots.Add(new InventorySlot());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool AddItem(ItemData data, int amount = 1)
    {
        if (data == null || amount <= 0)
            return false;

        if (data.isStackable)
        {
            for (int i = 0; i < slots.Count && amount > 0; i++)
            {
                var slot = slots[i];
                if (slot.item == data && slot.amount < data.maxStack)
                {
                    int canAdd = data.maxStack - slot.amount;
                    int add = Mathf.Min(canAdd, amount);
                    slot.amount += add;
                    amount -= add;
                }
            }
        }

        for (int i = 0; i < slots.Count && amount > 0; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty)
            {
                int add = data.isStackable
                    ? Mathf.Min(data.maxStack, amount)
                    : 1;

                slot.item = data;
                slot.amount = add;
                amount -= add;
            }
        }

        bool success = amount <= 0;
        if (success && SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        return success;
    }

    public void ClearAllSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].item = null;
            slots[i].amount = 0;
        }
    }

    public void RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return;

        var slot = slots[slotIndex];
        if (slot.IsEmpty) return;

        slot.amount -= amount;
        if (slot.amount <= 0)
        {
            slot.item = null;
            slot.amount = 0;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count) return null;
        return slots[index];
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count) return;
        if (indexB < 0 || indexB >= slots.Count) return;
        if (indexA == indexB) return;

        InventorySlot temp = new InventorySlot
        {
            item = slots[indexA].item,
            amount = slots[indexA].amount
        };

        slots[indexA].item = slots[indexB].item;
        slots[indexA].amount = slots[indexB].amount;

        slots[indexB].item = temp.item;
        slots[indexB].amount = temp.amount;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }
}
