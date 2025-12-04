using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string id;              // "key_basement", "gun_pistol" vs.
    public string displayName;     // UI name

    [Header("Visuals")]
    public Sprite icon;            // Inventory icon
    public GameObject worldPrefab; // World prefab

    [Header("Stack Settings")]
    public bool isStackable = false;
    public int maxStack = 1;

    [Header("Usage")]
    public bool canEquip = true;   // can be held in hand
    public bool canDrop = true;    // can be dropped on ground

    [Header("Equip Prefab")]
    public GameObject equippedPrefab;  // first person model (if null -> worldPrefab)

    [Header("Placement")]
    public bool isPlaceable = false;

    [Header("Type Flags")]
    public bool isWeapon = false;

    // NEW: this item is the repair tool (the thing you hold in hand)
    public bool isRepairTool = false;

    // NEW: if true, this item (when in world as pickup) requires
    // player to hold the repair tool to be picked up
    public bool requiresRepairToolForPickup = false;
}
