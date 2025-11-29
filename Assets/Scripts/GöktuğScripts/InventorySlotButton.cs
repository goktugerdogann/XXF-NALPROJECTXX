using UnityEngine;

public class InventorySlotButton : MonoBehaviour
{
    public int slotIndex;

    public void OnClick()
    {
        EquipManager equip = FindObjectOfType<EquipManager>();
        if (equip == null)
        {
            Debug.LogWarning("EquipManager not found");
            return;
        }

        equip.EquipFromSlot(slotIndex);
    }
}
