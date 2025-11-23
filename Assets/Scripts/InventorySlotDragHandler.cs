using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public int slotIndex;
    CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (InventoryUI.Instance == null) return;
        if (!InventoryUI.Instance.IsOpen) return;

        var slot = Inventory.Instance.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) return;

        InventoryUI.Instance.BeginSlotDrag(slotIndex);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (InventoryUI.Instance == null) return;
        InventoryUI.Instance.DragIconFollowMouse();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryUI.Instance == null) return;
        InventoryUI.Instance.EndSlotDrag();
        canvasGroup.blocksRaycasts = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (InventoryUI.Instance == null) return;
        InventoryUI.Instance.DropOnSlot(slotIndex);
    }
}

