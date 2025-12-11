using UnityEngine;
using Economy;

public class BuyerInteraction : MonoBehaviour
{
    public BuyerData buyerData;
    public BuyerDeliveryZone deliveryZone;

    public void OpenBuyer()
    {
        if (SellManager.Instance == null)
        {
            Debug.LogWarning("BuyerInteraction: SellManager.Instance is null.");
            return;
        }

        if (buyerData == null || deliveryZone == null)
        {
            Debug.LogWarning("BuyerInteraction: buyerData or deliveryZone is not set.");
            return;
        }

        SellManager.Instance.EnterBuyer(buyerData, deliveryZone);
    }

    // Example: you can call this from a trigger or an interaction system
    void OnMouseDown()
    {
        // For simple test: click on NPC opens sell UI
        OpenBuyer();
    }
}
