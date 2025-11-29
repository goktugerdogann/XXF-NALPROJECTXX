using UnityEngine;

public class FootPlatformDetector : MonoBehaviour
{
    private Transform Player;

    private void Awake()
    {
        Player = transform.root; // Player'ın transformu
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MovingPlatform"))
        {
            Player.SetParent(other.transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MovingPlatform"))
        {
            Player.SetParent(null);
        }
    }
}
