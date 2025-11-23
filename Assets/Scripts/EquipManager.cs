using UnityEngine;

public class EquipManager : MonoBehaviour
{
    public static EquipManager Instance;

    [Header("Refs")]
    public Inventory inventory;
    public Transform handAnchor;
    public Camera playerCamera;
    public LayerMask groundMask;

    [Header("Drop Settings")]
    public float dropForward = 1.2f;
    public float dropRayHeight = 1.0f;
    public float maxDropDownDistance = 3f;

    [Header("State")]
    public int currentSlotIndex = -1;
    public ItemData currentItem;
    GameObject equippedInstance;
    bool currentFromInventory = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (inventory == null) inventory = Inventory.Instance;
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Update()
    {
        // drop key
        if (Input.GetKeyDown(KeyCode.G))
            DropEquipped();

        // if inventory is open, do not process hotbar keys
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            return;

        // hotbar 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1))
            EquipFromSlot(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            EquipFromSlot(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            EquipFromSlot(2);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            EquipFromSlot(3);
    }

    public void EquipFromSlot(int slotIndex)
    {
        InteractionRaycaster ray = FindObjectOfType<InteractionRaycaster>();
        if (ray != null) ray.CancelPlacementPreview(true);

        if (inventory == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.slots.Count) return;

        var slot = inventory.slots[slotIndex];

        // clicked empty slot
        if (slot.IsEmpty)
        {
            ClearEquippedVisual();
            currentItem = null;
            currentFromInventory = false;
            currentSlotIndex = -1;

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();

            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                InventoryUI.Instance.CloseInventory();

            return;
        }

        // slot has item
        ItemData data = slot.item;
        if (data == null) return;

        // clear old equipped visual
        ClearEquippedVisual();

        currentItem = data;
        currentSlotIndex = slotIndex;
        currentFromInventory = true;

        // placeable item -> start placement preview, do not remove from inventory
        if (currentItem.isPlaceable)
        {
            if (ray != null) ray.BeginPlaceFromInventory(currentItem);

            var ui = InventoryUI.Instance != null ? InventoryUI.Instance : FindObjectOfType<InventoryUI>();
            if (ui != null && ui.IsOpen) ui.CloseInventory();

            return;
        }

        // normal equip (weapon, tool etc.)
        GameObject prefabToUse = currentItem.equippedPrefab != null
            ? currentItem.equippedPrefab
            : currentItem.worldPrefab;

        if (prefabToUse == null)
        {
            Debug.LogError("EquipManager: prefabToUse is null for " + currentItem.displayName);
            return;
        }

        equippedInstance = Instantiate(prefabToUse, handAnchor);
        equippedInstance.name = "EQUIPPED_" + prefabToUse.name;
        equippedInstance.transform.localPosition = Vector3.zero;
        equippedInstance.transform.localRotation = Quaternion.identity;

        PickupItem pickupOnEquipped = equippedInstance.GetComponent<PickupItem>();
        if (pickupOnEquipped != null) Destroy(pickupOnEquipped);

        var rb = equippedInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in equippedInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            InventoryUI.Instance.CloseInventory();
    }

    public void ClearEquippedVisual()
    {
        if (equippedInstance != null)
        {
            Destroy(equippedInstance);
            equippedInstance = null;
        }
    }

    // called when player places a placeable item or drops an equipped item
    public void ConsumeEquippedItemOne()
    {
        if (!currentFromInventory) return;
        if (currentItem == null) return;
        if (inventory == null) return;

        int index = currentSlotIndex;

        bool valid =
            index >= 0 &&
            index < inventory.slots.Count &&
            inventory.slots[index].item == currentItem &&
            inventory.slots[index].amount > 0;

        if (!valid)
        {
            index = -1;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                var s = inventory.slots[i];
                if (s.item == currentItem && s.amount > 0)
                {
                    index = i;
                    break;
                }
            }
        }

        if (index < 0) return;

        inventory.RemoveItem(index, 1);
        InventoryUI.Instance?.UpdateUI();
    }

    public void DropEquipped()
    {
        if (currentItem == null) return;
        if (!currentItem.canDrop) return;

        Vector3 origin = playerCamera.transform.position;
        Vector3 forward = playerCamera.transform.forward;
        Vector3 dropPos = origin + forward * 1.5f;

        RaycastHit hit;
        if (Physics.Raycast(dropPos + Vector3.up * 2f, Vector3.down, out hit, 5f, groundMask))
            dropPos = hit.point + Vector3.up * 0.05f;

        GameObject worldPrefab = currentItem.worldPrefab != null
            ? currentItem.worldPrefab
            : currentItem.equippedPrefab;

        if (worldPrefab != null)
        {
            GameObject obj = Instantiate(worldPrefab, dropPos, Quaternion.identity);
            obj.name = "DROPPED_" + worldPrefab.name;

            var rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // now consume one from inventory
        ConsumeEquippedItemOne();

        ClearEquippedVisual();
        currentItem = null;
        currentFromInventory = false;
        currentSlotIndex = -1;
    }
}
