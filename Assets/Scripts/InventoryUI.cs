using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("References")]
    public Inventory inventory;
    public GameObject panel;
    public TextMeshProUGUI[] slotTexts;
    public Button[] slotButtons;
    public Image[] slotIcons;

    // drag visuals are optional, you can leave them null
    public Canvas dragCanvas;
    public Image dragIconImage;
    public TextMeshProUGUI dragIconAmount;

    [Header("Control")]
    public KeyCode toggleKey = KeyCode.Tab;
    bool isOpen = false;
    public bool IsOpen => isOpen;

    [Header("Player Control")]
    public FPController playerController;

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
        isOpen = true;

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

        panel.SetActive(false);

        if (playerController != null)
            playerController.freezeMovement = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EndSlotDrag();
    }

    public void UpdateUI()
    {
        if (inventory == null) return;

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            var slot = inventory.slots[i];

            // text: only stack amount (like "3"), empty if 0 or 1
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

            // icon: show item icon
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

    public void BeginSlotDrag(int slotIndex)
    {
        if (inventory == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.slots.Count) return;

        var slot = inventory.slots[slotIndex];
        if (slot == null || slot.IsEmpty) return;

        draggingFromIndex = slotIndex;
        isDragging = true;

        // optional ghost icon, only if fields are assigned
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
