using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Behavior")]
    public bool goesToInventory = true; // true = to inventory, false = placement system

    [Header("Saving")]
    public bool saveToWorld = true;     // true = SaveManager will save and respawn this object
}
