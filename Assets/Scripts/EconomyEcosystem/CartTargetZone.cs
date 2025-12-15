using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CartTargetZone : MonoBehaviour
{
    [Header("Refs")]
    public ShoppingCartFollowController cart;
    public Transform snapPoint;
    public Transform lookTarget;

    [Header("Filter")]
    public string playerTag = "Player";

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (cart == null) return;
        if (!other.CompareTag(playerTag)) return;

        cart.SetCurrentZone(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (cart == null) return;
        if (!other.CompareTag(playerTag)) return;

        cart.ClearZone(this);
    }
}
