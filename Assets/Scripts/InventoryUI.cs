using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("Drag & Drop")]
    public Image dragIcon;                // Ýstersen kullanýrsýn
    private RectTransform dragIconRect;

    [Header("References")]
    public Inventory inventory;
    public GameObject panel;
    public TextMeshProUGUI[] slotTexts;
    public Button[] slotButtons;
    public Image[] slotIcons;

    // drag visuals (ghost icon)
    public Canvas dragCanvas;
    public Image dragIconImage;
    public TextMeshProUGUI dragIconAmount;

    [Header("Control")]
    public KeyCode toggleKey = KeyCode.Tab;
    bool isOpen = false;
    public bool IsOpen => isOpen;

    [Header("Player Control")]
    public FPController playerController;

    //  VILLAGER KONUÞMASI VS. ÝÇÝN ENVANTER KÝLÝTÝ
    [HideInInspector]
    public bool blockInventory = false;

    int draggingFromIndex = -1;
    bool isDragging = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (inventory == null)
            inventory = Inventory.Instance;

        if (panel != null)
            panel.SetActive(false);

        if (dragIconImage != null)
            dragIconImage.gameObject.SetActive(false);

        if (dragIconAmount != null)
            dragIconAmount.gameObject.SetActive(false);
    }

    void Update()
    {
        //  Envanter kilitliyse hiçbir input çalýþmasýn
        if (blockInventory)
            return;

        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        if (isDragging && dragIconImage != null)
        {
            dragIconImage.rectTransform.position = Input.mousePosition;
        }
    }

    public void OpenInventory()
    {
        if (blockInventory) return; // güvenlik

        isOpen = true;

        if (panel != null)
            panel.SetActive(true);

        if (playerController != null)
            playerController.freezeMovement = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UpdateUI();
    }

    public void CloseInventory()
    {
        isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        if (playerController != null)
            playerController.freezeMovement = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EndSlotDrag();
    }

    /// <summary>
    /// Villager konuþmasýna girerken dýþarýdan sessizce kapatmak için:
    /// Hareket / cursor kilidini ELLEMEZ, sadece UI’ý gizler.
    /// </summary>
    public void ForceCloseFromConversation()
    {
        if (!isOpen) return;

        isOpen = false;

        if (panel != null)
            panel.SetActive(false);

        EndSlotDrag();
    }

    public void UpdateUI()
    {
        if (inventory == null) return;

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            var slot = inventory.slots[i];

            // amount text
            if (slotTexts != null && i < slotTexts.Length && slotTexts[i] != null)
            {
                if (slot.IsEmpty)
                {
                    slotTexts[i].text = "";
                }
                else
                {
                    slotTexts[i].text = slot.amount > 1 ? slot.amount.ToString() : "";
                }
            }

            // icon
            if (slotIcons != null && i < slotIcons.Length && slotIcons[i] != null)
            {
                if (slot.IsEmpty || slot.item == null || slot.item.icon == null)
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].enabled = false;
                }
                else
                {
                    slotIcons[i].sprite = slot.item.icon;
                    slotIcons[i].enabled = true;
                }
            }
        }
    }

    // ---------------- DRAG LOGIC ---------------- //

    public void BeginSlotDrag(int slotIndex)
    {
        if (inventory == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.slots.Count) return;

        var slot = inventory.slots[slotIndex];
        if (slot == null || slot.IsEmpty) return;

        draggingFromIndex = slotIndex;
        isDragging = true;

        if (dragIconImage != null)
        {
            dragIconImage.gameObject.SetActive(true);
            dragIconImage.sprite = slot.item.icon;
            dragIconImage.enabled = (slot.item.icon != null);
            dragIconImage.rectTransform.position = Input.mousePosition;
        }

        if (dragIconAmount != null)
        {
            dragIconAmount.gameObject.SetActive(true);
            dragIconAmount.text = slot.amount > 1 ? slot.amount.ToString() : "";
        }

        // slot içindeki görsel geçici kaybolsun
        if (slotIcons != null && draggingFromIndex < slotIcons.Length && slotIcons[draggingFromIndex] != null)
        {
            slotIcons[draggingFromIndex].enabled = false;
        }
    }

    public void DragIconFollowMouse()
    {
        if (!isDragging || dragIconImage == null) return;
        dragIconImage.rectTransform.position = Input.mousePosition;
    }

    public void EndSlotDrag()
    {
        isDragging = false;
        draggingFromIndex = -1;

        if (dragIconImage != null)
            dragIconImage.gameObject.SetActive(false);

        if (dragIconAmount != null)
            dragIconAmount.gameObject.SetActive(false);

        // bütün slotlarý güncelle, kaybolan iconlar geri gelsin
        UpdateUI();
    }

    public void DropOnSlot(int targetIndex)
    {
        if (!isDragging) return;
        if (inventory == null) return;
        if (draggingFromIndex < 0) return;

        if (targetIndex < 0 || targetIndex >= inventory.slots.Count)
        {
            EndSlotDrag();
            UpdateUI();
            return;
        }

        Inventory.Instance.SwapSlots(draggingFromIndex, targetIndex);

        EndSlotDrag();
        UpdateUI();
    }
}
