using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // CharacterController çarpınca tetiklenir
        if (hit.collider.CompareTag("Player"))
        {
            // Oyuncuyu platforma child et
            hit.collider.transform.SetParent(this.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Platformdan ayrılınca parent'ı sıfırla
            other.transform.SetParent(null);
        }
    }
}