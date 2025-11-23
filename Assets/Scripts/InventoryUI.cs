using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance;

    [Header("References")]
    public Inventory inventory;
    public GameObject panel;
    public TextMeshProUGUI[] slotTexts;   // stack textleri (sadece miktar)
    public Button[] slotButtons;
    public Image[] slotIcons;             // slotlardaki item ikonlarý

    [Header("Drag & Drop (Ghost Icon)")]
    public Image dragIconImage;           // Canvas üzerindeki hayalet icon (Image)
    public TextMeshProUGUI dragIconAmount;// onun üzerindeki miktar yazýsý

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

        // Ghost icon baþlangýçta kapalý
        if (dragIconImage != null)
            dragIconImage.gameObject.SetActive(false);

        if (dragIconAmount != null)
            dragIconAmount.gameObject.SetActive(false);
    }

    void Update()
    {
        // Envanteri aç/kapat
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen)
                CloseInventory();
            else
                OpenInventory();
        }

        // Drag devam ederken ghost icon mouse'u takip etsin
        if (isDragging && dragIconImage != null)
        {
            dragIconImage.rectTransform.position = Input.mousePosition;
        }
    }

    public void OpenInventory()
    {
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

        EndSlotDrag(); // drag varsa kapat
    }

    public void UpdateUI()
    {
        if (inventory == null) return;

        for (int i = 0; i < inventory.slots.Count; i++)
        {
            var slot = inventory.slots[i];

            // Miktar text'i (ör: "3")
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

            // Icon resmi
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

    /// <summary>
    /// Drag baþlarken slot handler burayý çaðýrýyor
    /// </summary>
    public void BeginSlotDrag(int slotIndex)
    {
        if (inventory == null) return;
        if (slotIndex < 0 || slotIndex >= inventory.slots.Count) return;

        var slot = inventory.slots[slotIndex];
        if (slot == null || slot.IsEmpty) return;

        draggingFromIndex = slotIndex;
        isDragging = true;

        // GHOST ICON (eldeki)
        if (dragIconImage != null)
        {
            dragIconImage.sprite = slot.item.icon;
            dragIconImage.enabled = (slot.item.icon != null);
            dragIconImage.rectTransform.position = Input.mousePosition;
            dragIconImage.gameObject.SetActive(true);
        }

        if (dragIconAmount != null)
        {
            dragIconAmount.text = slot.amount > 1 ? slot.amount.ToString() : "";
            dragIconAmount.gameObject.SetActive(!string.IsNullOrEmpty(dragIconAmount.text));
        }

        // Kaynaktaki slotu görsel olarak boþalt
        if (slotIcons != null && slotIndex < slotIcons.Length && slotIcons[slotIndex] != null)
        {
            slotIcons[slotIndex].sprite = null;
            slotIcons[slotIndex].enabled = false;
        }

        if (slotTexts != null && slotIndex < slotTexts.Length && slotTexts[slotIndex] != null)
        {
            slotTexts[slotIndex].text = "";
        }
    }

    
    public void DragIconFollowMouse()
    {
        if (!isDragging || dragIconImage == null) return;
        dragIconImage.rectTransform.position = Input.mousePosition;
    }

    public void EndSlotDrag()
    {
        // Drag bitti
        isDragging = false;

        if (dragIconImage != null)
            dragIconImage.gameObject.SetActive(false);

        if (dragIconAmount != null)
            dragIconAmount.gameObject.SetActive(false);

        // draggingFromIndex'i sýfýrla
        draggingFromIndex = -1;

        UpdateUI();
    }


    public void DropOnSlot(int targetIndex)
    {
        if (!isDragging) return;
        if (inventory == null) return;
        if (draggingFromIndex < 0) return;

        if (targetIndex < 0 || targetIndex >= inventory.slots.Count)
            return;

        // Ayný slota býrakýyorsa envanterde deðiþiklik yok
        if (targetIndex == draggingFromIndex)
            return;

        
        Inventory.Instance.SwapSlots(draggingFromIndex, targetIndex);

        // UI yenilemeyi ve drag'i bitirmeyi OnEndDrag/EndSlotDrag yapacak
    }

}
