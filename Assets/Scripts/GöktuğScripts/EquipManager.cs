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

    bool isSwitching = false; // slot animasyonu çalýþýrken tekrar basýlmasýn

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
        // drop key
        if (Input.GetKeyDown(KeyCode.G))
            DropEquipped();

        // if inventory is open, do not process hotbar keys
        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            return;

        // hotbar 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipFromSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipFromSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipFromSlot(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipFromSlot(3);
    }

    // check if we are holding a weapon
    public bool IsHoldingWeapon()
    {
        if (currentItem == null) return false;
        return currentItem.isWeapon;
    }

    // check if we are holding the repair tool
    public bool IsHoldingRepairTool()
    {
        if (currentItem == null) return false;
        return currentItem.isRepairTool;
    }

    // BURASI GÜNCELLENDÝ
    public void EquipFromSlot(int slotIndex)
    {
        if (isSwitching) return;

        // zaten ayný slotun ayni itemini elde tutuyorsak HÝÇBÝR ÞEY YAPMA
        if (inventory != null &&
            slotIndex >= 0 && slotIndex < inventory.slots.Count)
        {
            var slot = inventory.slots[slotIndex];

            if (!slot.IsEmpty &&
                slotIndex == currentSlotIndex &&
                currentItem == slot.item &&
                equippedInstance != null &&
                (currentItem == null || !currentItem.isPlaceable)) // normal eldeki item (preview deðil)
            {
                return; // aynýsýný zaten tutuyoruz, animasyon çalýþmasýn
            }
        }

        StartCoroutine(EquipFromSlotRoutine(slotIndex));
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

        // ---------------- EMPTY SLOT  ELDEKINI HIDE ANIM ÝLE KAPAT ----------------
        if (slot.IsEmpty)
        {
            // Eldeki item varsa önce içeri girme animasyonu
            yield return HideCurrentIfAny();

            currentItem = null;
            currentFromInventory = false;
            currentSlotIndex = -1;

            if (SaveManager.Instance != null)
                SaveManager.Instance.SaveGame();

            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
                InventoryUI.Instance.CloseInventory();

            isSwitching = false;
            yield break;
        }

        // ---------------- SLOTTA ITEM VAR ----------------
        ItemData data = slot.item;
        if (data == null)
        {
            isSwitching = false;
            yield break;
        }

        // önce ESKI eldeki item içeri girsin (hide anim)
        yield return HideCurrentIfAny();

        currentItem = data;
        currentSlotIndex = slotIndex;
        currentFromInventory = true;

        // ---------------- PLACEABLE (PREVIEW) LOGIC ----------------
        if (currentItem.isPlaceable)
        {
            // Normal eldeki item çoktan hide animle saklandý
            if (ray != null)
                ray.BeginPlaceFromInventory(currentItem); // preview objesi için ANIM YOK

            var ui = InventoryUI.Instance != null ? InventoryUI.Instance : FindObjectOfType<InventoryUI>();
            if (ui != null && ui.IsOpen)
                ui.CloseInventory();

            isSwitching = false;
            yield break;
        }

        // ---------------- NORMAL EQUIP (WEAPON / TOOL VS.) ----------------
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

        // ELÝME GELME ANÝMASYONU (her zaman doðru objeyi animle)
        if (heldAnimator != null)
        {
            heldAnimator.SetCurrentItem(equippedInstance.transform);
            heldAnimator.PlayShow();
        }

        if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen)
            InventoryUI.Instance.CloseInventory();

        isSwitching = false;
    }
        // Elde bir þey varsa hide anim oynatýp sonra destroy eder
        IEnumerator HideCurrentIfAny()
    {
        if (equippedInstance == null)
        {
            ClearEquippedVisualImmediate();
            yield break;
        }

        if (heldAnimator != null)
        {
            // hangi objeyi saklayacaðýmýzý net söyle
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


    // Eskiden ClearEquippedVisual ne yapýyorsa bu onu direkt yapýyor (anim yok)
    void ClearEquippedVisualImmediate()
    {
        // notify animator that equipped instance is destroyed
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


    // Dýþarýdan çaðýrýlan eski isim kalsýn, yapý bozulmasýn
    public void ClearEquippedVisual()
    {
        ClearEquippedVisualImmediate();
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

        // Drop'ta eldeki model direkt kaybolsun (istersen bunu da animli yaparýz sonra)
        ClearEquippedVisualImmediate();
        currentItem = null;
        currentFromInventory = false;
        currentSlotIndex = -1;
    }
}
