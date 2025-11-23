
using UnityEngine;

public class InventoryDropHandler : MonoBehaviour
{
    public Camera playerCamera;
    public float dropForward = 1.2f;
    public float dropUp = 0.2f;
    public LayerMask groundMask;

    public void DropItem(int slotIndex)
    {
        InventorySlot slot = Inventory.Instance.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty || slot.item.worldPrefab == null)
            return;

        Vector3 origin = playerCamera.transform.position +
                         playerCamera.transform.forward * dropForward +
                         Vector3.up * 1.0f;

        RaycastHit hit;
        Vector3 spawnPos;

        if (Physics.Raycast(origin, Vector3.down, out hit, 3f, groundMask))
        {
            spawnPos = hit.point;
        }
        else
        {
            spawnPos = origin + Vector3.down * 1.0f;
        }

        GameObject obj = Instantiate(slot.item.worldPrefab, spawnPos, Quaternion.identity);

        obj.transform.position += Vector3.up * 0.05f;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Inventory.Instance.RemoveItem(slotIndex, 1);
        FindObjectOfType<InventoryUI>()?.UpdateUI();
    }
}
