using UnityEngine;
using UnityEngine.UI;

public class InventoryDragManager : MonoBehaviour
{
    public static InventoryDragManager Instance;

    [Header("Drag Icon")]
    public Image dragIcon;              // Canvas'taki DragIcon image
    private RectTransform dragRect;

    [HideInInspector] public int draggedSlotIndex = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (dragIcon != null)
        {
            dragRect = dragIcon.GetComponent<RectTransform>();
            dragIcon.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Drag baþlarken çaðýr: hangi sprite ve hangi slot'tan geldi?
    /// </summary>
    public void BeginDrag(Sprite sprite, int slotIndex)
    {
        if (dragIcon == null || sprite == null) return;

        draggedSlotIndex = slotIndex;
        dragIcon.sprite = sprite;
        dragIcon.SetNativeSize(); // istersen yorum satýrý yap
        dragIcon.gameObject.SetActive(true);
    }

    /// <summary>
    /// Drag sýrasýnda mouse pozisyonuna göre icon'u taþý.
    /// </summary>
    public void UpdateDragPosition(Vector2 screenPosition)
    {
        if (dragIcon == null || dragRect == null) return;
        if (!dragIcon.gameObject.activeSelf) return;

        dragRect.position = screenPosition;
    }

    /// <summary>
    /// Drag bitince icon'u gizle.
    /// </summary>
    public void EndDrag()
    {
        if (dragIcon == null) return;

        dragIcon.gameObject.SetActive(false);
        draggedSlotIndex = -1;
    }
}
