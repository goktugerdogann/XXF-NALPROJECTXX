using UnityEngine;
using UnityEngine.UI;

public class InteractionRaycaster : MonoBehaviour
{
    [Header("General")]
    public Camera playerCamera;
    public float interactDistance = 3f;
    public LayerMask interactionLayer;
    public InteractionUIManager uiManager;

    [Header("Place Settings")]
    public float placeDistance = 4f;
    public LayerMask groundLayer;

    [Header("Snap Settings")]
    public bool useGridSnap = true;
    public float gridSize = 0.5f;
    public bool freezeOnPlace = true;

    [Header("Preview Settings")]
    public Material previewMaterial;
    public float placeForwardOffset = 0f;
    public float rotationSpeed = 200f;

    [Header("Preview Colors")]
    public Color validColor = new Color(0.2f, 1f, 0.2f, 0.4f);
    public Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.4f);

    [Header("Overlap Check")]
    public LayerMask overlapCheckMask;

    [Header("Pickup Settings")]
    public KeyCode pickupKey = KeyCode.Q;
    public float pickupHoldTime = 1f;

    [Header("Pickup Progress UI")]
    public Image pickupProgressImage;
    public bool usePickupProgress = true;

    [Header("Crosshair")]
    public GameObject crosshairObject;
    public bool hideCrosshairWhileCharging = true;

    [Header("Conversation Block")]
    public bool blockPlacementFromConversation = false;
    bool previewWasActiveBeforeConversation = false;

    float pickupTimer = 0f;
    bool isHoldingPickup = false;
    [Header("FishCrate Stacking")]
    public float crateStackEpsilon = 0.02f; // 2 cm tolerans

    GameObject heldObject;
    Rigidbody heldRigidbody;

    float heldHeightOffset;
    Quaternion baseRotation;
    Quaternion currentRotation;
    float rotationAngleY = 0f;

    GameObject previewObject;
    Renderer[] previewRenderers;
    Collider[] previewColliders;

    bool isPlacementMode = false;

    bool isPlacingFromInventory = false;
    ItemData placingFromInventoryItem = null;
    bool inventoryWasOpen = false;

    public bool HasActiveInventoryPreview
    {
        get { return isPlacementMode && isPlacingFromInventory && heldObject != null; }
    }

    public bool HasHeldWorldPreview()
    {
        // inventoryden place edilen preview degil, world preview (FishCrate vs)
        return isPlacementMode && !isPlacingFromInventory && heldObject != null;
    }

    void Start()
    {
        Debug.Log(Application.persistentDataPath);

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (uiManager == null)
            uiManager = FindObjectOfType<InteractionUIManager>();

        if (pickupProgressImage != null)
        {
            pickupProgressImage.fillAmount = 0f;
            pickupProgressImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
       

        if (blockPlacementFromConversation)
        {
            if (previewObject != null && previewObject.activeSelf)
                previewObject.SetActive(false);

            if (uiManager != null)
                uiManager.HideInteractText();

            return;
        }

        bool invOpen = (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen);

        if (invOpen)
        {
            if (!inventoryWasOpen)
            {
                inventoryWasOpen = true;

                if (isHoldingPickup)
                {
                    isHoldingPickup = false;
                    pickupTimer = 0f;
                    UpdatePickupProgressUI(0f, true);
                }

                if (previewObject != null && previewObject.activeSelf)
                    previewObject.SetActive(false);
            }

            if (uiManager != null)
                uiManager.HideInteractText();

            return;
        }
        else
        {
            if (inventoryWasOpen)
            {
                inventoryWasOpen = false;

                if (previewObject != null &&
                    !previewObject.activeSelf &&
                    heldObject != null &&
                    isPlacementMode)
                {
                    previewObject.SetActive(true);
                }
            }
        }

        if (heldObject == null)
            HandlePickup();
        else
            HandlePlacementMode();
    }

    void HandlePickup()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactionLayer))
        {
            PickupItem pickup = hit.collider.GetComponentInParent<PickupItem>();
            if (pickup == null)
            {
                if (uiManager != null) uiManager.HideInteractText();
                return;
            }

            GameObject target = pickup.gameObject;

            if (!CanPickupThis(pickup))
            {
                if (isHoldingPickup)
                {
                    isHoldingPickup = false;
                    pickupTimer = 0f;
                    UpdatePickupProgressUI(0f, true);
                }

                if (uiManager != null)
                    uiManager.ShowInteractText("Need repair tool");

                return;
            }

            if (Input.GetKey(pickupKey))
            {
                if (!isHoldingPickup)
                {
                    isHoldingPickup = true;
                    pickupTimer = 0f;
                }

                pickupTimer += Time.deltaTime;
                if (uiManager != null)
                    uiManager.ShowInteractText("Hold Q to pick up");

                UpdatePickupProgressUI(pickupTimer / pickupHoldTime);

                if (pickupTimer >= pickupHoldTime)
                {
                    if (pickup != null && pickup.goesToInventory && pickup.itemData != null)
                    {
                        bool added = (Inventory.Instance != null) &&
                                     Inventory.Instance.AddItem(pickup.itemData, pickup.amount);

                        if (added)
                        {
                            target.SetActive(false);

                            if (SaveManager.Instance != null)
                                SaveManager.Instance.SaveGame();

                            Object.Destroy(target);

                            if (uiManager != null)
                                uiManager.ShowInteractText(pickup.itemData.displayName + " added");
                        }
                        else
                        {
                            if (uiManager != null)
                                uiManager.ShowInteractText("Inventory full");
                        }
                    }
                    else
                    {
                        isPlacingFromInventory = false;
                        placingFromInventoryItem = null;
                        PickUpObject(target);
                    }

                    isHoldingPickup = false;
                    pickupTimer = 0f;
                    UpdatePickupProgressUI(0f, true);
                }
            }
            else
            {
                if (isHoldingPickup)
                {
                    isHoldingPickup = false;
                    pickupTimer = 0f;
                    UpdatePickupProgressUI(0f, true);
                }

                if (uiManager != null)
                    uiManager.ShowInteractText("Press Q to pick up");
            }
        }
        else
        {
            if (isHoldingPickup)
            {
                isHoldingPickup = false;
                pickupTimer = 0f;
            }

            UpdatePickupProgressUI(0f, true);
            if (uiManager != null)
                uiManager.HideInteractText();
        }
    }

    public void BeginPlaceFromInventory(ItemData item)
    {
        if (item == null || item.worldPrefab == null)
        {
            Debug.LogWarning("BeginPlaceFromInventory: item or prefab missing");
            return;
        }

        CancelPlacementPreview(false);

        isPlacingFromInventory = true;
        placingFromInventoryItem = item;

        GameObject obj = Object.Instantiate(item.worldPrefab);
        obj.name = "PLACE_FROM_INV_" + item.displayName;

        PickUpObject(obj);
    }

    void PickUpObject(GameObject obj)
    {
        // only one thing in hand: if we pick a world object, clear equipped visual
        if (EquipManager.Instance != null)
        {
            EquipManager.Instance.ForceUnequipInstant(false);
        }

        heldObject = obj;
        heldRigidbody = heldObject.GetComponent<Rigidbody>();

        baseRotation = heldObject.transform.rotation;
        rotationAngleY = 0f;
        currentRotation = baseRotation;

        Collider col = heldObject.GetComponent<Collider>();
        if (col != null)
        {
            float bottomY = col.bounds.center.y - col.bounds.extents.y;
            heldHeightOffset = heldObject.transform.position.y - bottomY;
        }
        else
        {
            heldHeightOffset = 0.5f;
        }

        if (heldRigidbody != null)
        {
            heldRigidbody.velocity = Vector3.zero;
            heldRigidbody.angularVelocity = Vector3.zero;
        }

        if (previewObject != null)
            Object.Destroy(previewObject);

        previewObject = Object.Instantiate(heldObject);
        previewObject.name = heldObject.name + "_Preview";

        PickupItem previewPickup = previewObject.GetComponent<PickupItem>();
        if (previewPickup != null)
            Object.Destroy(previewPickup);

        FishCrate previewCrate = previewObject.GetComponent<FishCrate>();
        if (previewCrate != null)
            Object.Destroy(previewCrate);

        previewColliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider c in previewColliders)
            c.isTrigger = true;

        Rigidbody prb = previewObject.GetComponent<Rigidbody>();
        if (prb != null)
            Object.Destroy(prb);

        previewRenderers = previewObject.GetComponentsInChildren<Renderer>();

        if (previewMaterial != null)
        {
            foreach (Renderer r in previewRenderers)
                r.material = previewMaterial;
        }

        previewObject.SetActive(true);
        SetPreviewColor(false);

        heldObject.SetActive(false);
        isPlacementMode = true;

        UpdatePickupProgressUI(0f, true);
        if (uiManager != null)
            uiManager.HideInteractText();

        ForceUpdatePreviewPosition();
    }

    void ForceUpdatePreviewPosition()
    {
        if (previewObject == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // Her seye ray at (ground disinda crate de yakalayalim)
        bool hasHit = Physics.Raycast(ray, out hit, placeDistance, ~0, QueryTriggerInteraction.Ignore);

        bool validSurface = false;

        if (hasHit)
        {
            bool isGround = ((groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0);
            bool isCrateSurface = IsHoldingFishCratePreview() && hit.collider.GetComponentInParent<FishCrate>() != null;

            validSurface = isGround || isCrateSurface;
        }

        Vector3 pos;
        if (validSurface) pos = CalculatePlacementPosition(hit);
        else pos = playerCamera.transform.position + playerCamera.transform.forward * (placeDistance * 0.5f);

        previewObject.transform.position = pos;
        previewObject.transform.rotation = currentRotation;
    }


    void HandlePlacementMode()
    {
        if (!isPlacementMode)
            isPlacementMode = true;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            rotationAngleY += scroll * rotationSpeed;
            currentRotation = Quaternion.Euler(0f, rotationAngleY, 0f) * baseRotation;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        bool hasHit = Physics.Raycast(ray, out hit, placeDistance, ~0, QueryTriggerInteraction.Ignore);

        bool hasGround = false;
        if (hasHit)
        {
            bool isGround = ((groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0);
            bool isCrateSurface = IsHoldingFishCratePreview() && hit.collider.GetComponentInParent<FishCrate>() != null;

            hasGround = isGround || isCrateSurface;
        }


        Vector3 previewPos;
        if (hasGround)
            previewPos = CalculatePlacementPosition(hit);
        else
            previewPos = playerCamera.transform.position +
                         playerCamera.transform.forward * (placeDistance * 0.5f);

        if (previewObject != null)
        {
            previewObject.SetActive(true);
            previewObject.transform.position = previewPos;
            previewObject.transform.rotation = currentRotation;
        }

        bool hasOverlap = hasGround && CheckOverlapAtPreview();
        bool isValidSpot = hasGround && !hasOverlap;

        if (uiManager != null)
        {
            if (!hasGround)
                uiManager.ShowInteractText("Look at ground to place");
            else if (hasOverlap)
                uiManager.ShowInteractText("Cannot place here");
            else
                uiManager.ShowInteractText("Left click to place");
        }

        SetPreviewColor(isValidSpot);

        if (Input.GetMouseButtonDown(0) && isValidSpot)
            PlaceObject(previewPos);

        if (Input.GetMouseButtonDown(1))
            CancelPlacementPreview(true);
    }

    Vector3 CalculatePlacementPosition(RaycastHit hit)
    {
        Vector3 placePos = hit.point;

        if (placeForwardOffset != 0f)
        {
            Vector3 flatForward = playerCamera.transform.forward;
            flatForward.y = 0f;
            flatForward.Normalize();
            placePos += flatForward * placeForwardOffset;
        }

        if (useGridSnap && gridSize > 0.0001f)
        {
            placePos.x = Mathf.Round(placePos.x / gridSize) * gridSize;
            placePos.z = Mathf.Round(placePos.z / gridSize) * gridSize;
        }

        placePos.y += heldHeightOffset;
        return placePos;
    }

    void PlaceObject(Vector3 placePos)
    {
        if (heldObject == null)
            return;

        heldObject.transform.position = placePos;
        heldObject.transform.rotation = currentRotation;
        heldObject.SetActive(true);

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = false;
            heldRigidbody.useGravity = true;
            if (freezeOnPlace)
                heldRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (previewObject != null)
        {
            Object.Destroy(previewObject);
            previewObject = null;
            previewRenderers = null;
            previewColliders = null;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        if (isPlacingFromInventory &&
            placingFromInventoryItem != null &&
            EquipManager.Instance != null)
        {
            EquipManager.Instance.ConsumeEquippedItemOne();
        }

        heldObject = null;
        heldRigidbody = null;
        isPlacementMode = false;
        isPlacingFromInventory = false;
        placingFromInventoryItem = null;

        if (uiManager != null)
            uiManager.HideInteractText();
    }

    public void CancelPlacementPreview(bool returnToInventory)
    {
        if (heldObject == null && previewObject == null)
        {
            isPlacementMode = false;
            isPlacingFromInventory = false;
            placingFromInventoryItem = null;
            return;
        }

        if (previewObject != null)
        {
            Object.Destroy(previewObject);
            previewObject = null;
            previewRenderers = null;
            previewColliders = null;
        }

        if (heldObject != null)
        {
            if (isPlacingFromInventory && returnToInventory)
            {
                Object.Destroy(heldObject);
                heldObject = null;
            }
            else
            {
                heldObject.SetActive(true);
                heldObject = null;
            }
        }

        heldRigidbody = null;
        isPlacementMode = false;
        isPlacingFromInventory = false;
        placingFromInventoryItem = null;

        if (uiManager != null)
            uiManager.HideInteractText();
    }

    public void BlockPlacementForConversation(bool block)
    {
        blockPlacementFromConversation = block;

        if (block)
        {
            if (previewObject != null && previewObject.activeSelf)
            {
                previewWasActiveBeforeConversation = true;
                previewObject.SetActive(false);
            }
            else
            {
                previewWasActiveBeforeConversation = false;
            }
        }
        else
        {
            if (previewWasActiveBeforeConversation &&
                previewObject != null &&
                isPlacementMode &&
                heldObject != null)
            {
                previewObject.SetActive(true);
            }

            previewWasActiveBeforeConversation = false;
        }
    }

    void SetPreviewColor(bool isValid)
    {
        if (previewRenderers == null) return;

        Color targetColor = isValid ? validColor : invalidColor;

        foreach (Renderer r in previewRenderers)
        {
            if (r.material.HasProperty("_Color"))
                r.material.color = targetColor;
        }
    }

    bool CheckOverlapAtPreview()
    {
        if (previewColliders == null || previewColliders.Length == 0)
            return false;

        bool holdingCrate = IsHoldingFishCratePreview();

        foreach (Collider c in previewColliders)
        {
            if (!c.enabled) continue;

            Vector3 center = c.bounds.center;
            Vector3 halfExtents = c.bounds.extents;

            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                c.transform.rotation,
                overlapCheckMask,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                if (hit == null) continue;

                // preview'in kendi colliderlari
                if (hit.transform.root == previewObject.transform.root)
                    continue;

                // --- SADECE FishCrate icin: crate'e temas serbest, ic ice girmek yasak ---
                if (holdingCrate)
                {
                    FishCrate otherCrate = hit.GetComponentInParent<FishCrate>();
                    if (otherCrate != null)
                    {
                        // preview collider alt seviyesi
                        float previewBottomY = c.bounds.min.y;
                        // diger crate ust seviyesi
                        float otherTopY = hit.bounds.max.y;

                        // Eger preview alt seviyesi, diger crate ustunden daha asagidaysa -> ic ice
                        if (previewBottomY < otherTopY - crateStackEpsilon)
                            return true;

                        // aksi halde (ustu uste) bu hit'i engel sayma
                        continue;
                    }
                }

                // crate degilse veya crate degil item ise normal engel
                return true;
            }
        }

        return false;
    }

    bool IsHoldingFishCratePreview()
    {
        if (!HasHeldWorldPreview()) return false;
        if (heldObject == null) return false;
        return heldObject.GetComponent<FishCrate>() != null;
    }

    void UpdatePickupProgressUI(float normalized, bool hide = false)
    {
        if (!usePickupProgress || pickupProgressImage == null)
            return;

        if (hide)
        {
            pickupProgressImage.fillAmount = 0f;
            pickupProgressImage.gameObject.SetActive(false);

            if (crosshairObject != null && hideCrosshairWhileCharging)
                crosshairObject.SetActive(true);
        }
        else
        {
            pickupProgressImage.gameObject.SetActive(true);
            pickupProgressImage.fillAmount = Mathf.Clamp01(normalized);

            if (crosshairObject != null && hideCrosshairWhileCharging)
                crosshairObject.SetActive(false);
        }
    }

    bool CanPickupThis(PickupItem pickup)
    {
        if (pickup == null || pickup.itemData == null)
            return true;

        ItemData data = pickup.itemData;

        if (!data.requiresRepairToolForPickup)
            return true;

        if (EquipManager.Instance == null)
            return false;

        return EquipManager.Instance.IsHoldingRepairTool();
    }
}
