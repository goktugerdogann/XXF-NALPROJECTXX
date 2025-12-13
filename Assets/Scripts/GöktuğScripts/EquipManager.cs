using System.Collections;
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

    [Header("Anim")]
    public HeldItemAnimator heldAnimator;

    [Header("State")]
    public int currentSlotIndex = -1;
    public ItemData currentItem;
    GameObject equippedInstance;
    bool currentFromInventory = false;

    bool isSwitching = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (inventory == null) inventory = Inventory.Instance;
        if (playerCamera == null) playerCamera = Camera.main;

        if (heldAnimator == null && handAnchor != null)
            heldAnimator = handAnchor.GetComponent<HeldItemAnimator>();
        if (heldAnimator == null)
            heldAnimator = FindObjectOfType<HeldItemAnimator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            DropEquipped();

        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            return;

        if (VillagerInteractionCam.Current != null)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipFromSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipFromSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipFromSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipFromSlot(3);
    }

    public bool IsHoldingWeapon()
    {
        if (currentItem == null) return false;
        return currentItem.isWeapon;
    }

    public bool IsHoldingRepairTool()
    {
        if (currentItem == null) return false;
        return currentItem.isRepairTool;
    }

    public void EquipFromSlot(int slotIndex)
    {
        if (isSwitching) return;

        // BLOCK: if holding world preview (FishCrate etc), do not allow equipping anything
        InteractionRaycaster ray = FindObjectOfType<InteractionRaycaster>();
        if (ray != null && ray.HasHeldWorldPreview())
        {
            if (ray.uiManager != null)
                ray.uiManager.ShowInteractText("Place the crate first");
            return;
        }

        var ui = InventoryUI.Instance;
        if (ui != null && ui.IsOpen)
        {
            ui.CloseInventory();
        }

        if (inventory != null &&
            slotIndex >= 0 && slotIndex < inventory.slots.Count)
        {
            var slot = inventory.slots[slotIndex];

            if (!slot.IsEmpty &&
                slotIndex == currentSlotIndex &&
                currentItem == slot.item &&
                equippedInstance != null &&
                (currentItem == null || !currentItem.isPlaceable))
            {
                return;
            }
        }

        StartCoroutine(EquipFromSlotRoutine(slotIndex));
    }

    public void ForceUnequipInstant(bool save = false)
    {
        ClearEquippedVisualImmediate();

        currentItem = null;
        currentFromInventory = false;
        currentSlotIndex = -1;

        if (save && SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    IEnumerator EquipFromSlotRoutine(int slotIndex)
    {
        isSwitching = true;

        InteractionRaycaster ray = FindObjectOfType<InteractionRaycaster>();
        if (ray != null)
            ray.CancelPlacementPreview(true);

        if (inventory == null)
        {
            isSwitching = false;
            yield break;
        }
        if (slotIndex < 0 || slotIndex >= inventory.slots.Count)
        {
            isSwitching = false;
            yield break;
        }

        var slot = inventory.slots[slotIndex];

        if (slot.IsEmpty)
        {
            yield return HideCurrentIfAny();

            currentItem = null;
            currentFromInventory = false;
            currentSlotIndex = -1;

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();

            isSwitching = false;
            yield break;
        }

        ItemData data = slot.item;
        if (data == null)
        {
            isSwitching = false;
            yield break;
        }

        yield return HideCurrentIfAny();

        currentItem = data;
        currentSlotIndex = slotIndex;
        currentFromInventory = true;

        if (currentItem.isPlaceable)
        {
            if (ray != null)
                ray.BeginPlaceFromInventory(currentItem);

            isSwitching = false;
            yield break;
        }

        GameObject prefabToUse = currentItem.equippedPrefab != null
            ? currentItem.equippedPrefab
            : currentItem.worldPrefab;

        if (prefabToUse == null)
        {
            Debug.LogError("EquipManager: prefabToUse is null for " + currentItem.displayName);
            isSwitching = false;
            yield break;
        }

        equippedInstance = Instantiate(prefabToUse, handAnchor);
        equippedInstance.name = "EQUIPPED_" + prefabToUse.name;
        equippedInstance.transform.localPosition = Vector3.zero;
        equippedInstance.transform.localRotation = Quaternion.identity;

        PickupItem pickupOnEquipped = equippedInstance.GetComponent<PickupItem>();
        if (pickupOnEquipped != null)
            Destroy(pickupOnEquipped);

        var rb = equippedInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in equippedInstance.GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (heldAnimator != null)
        {
            heldAnimator.SetCurrentItem(equippedInstance.transform);
            heldAnimator.PlayShow();
        }

        isSwitching = false;
    }

    IEnumerator HideCurrentIfAny()
    {
        if (equippedInstance == null)
        {
            ClearEquippedVisualImmediate();
            yield break;
        }

        if (heldAnimator != null)
        {
            heldAnimator.SetCurrentItem(equippedInstance.transform);

            bool done = false;
            heldAnimator.PlayHideAnimated(() =>
            {
                done = true;
            });

            while (!done)
                yield return null;
        }

        ClearEquippedVisualImmediate();
    }

    void ClearEquippedVisualImmediate()
    {
        if (heldAnimator != null)
        {
            heldAnimator.OnEquippedDestroyed();
        }

        if (equippedInstance != null)
        {
            Destroy(equippedInstance);
            equippedInstance = null;
        }
    }

    public void ClearEquippedVisual()
    {
        ClearEquippedVisualImmediate();
    }

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

        ConsumeEquippedItemOne();

        ClearEquippedVisualImmediate();
        currentItem = null;
        currentFromInventory = false;
        currentSlotIndex = -1;
    }
}
