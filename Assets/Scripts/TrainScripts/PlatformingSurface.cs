using UnityEngine;

public class PlatformingSurface : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private TrainController trainController;
    private CharacterController playerCC; 
    
    // YENİ EK: Player'ı tutmak yerine direkt CC'yi tutalım.
    private bool playerIsOnPlatform = false; 

    private void Start()
    {
        // TrainController'ı güvenli bir şekilde al.
        trainController = GetComponentInParent<TrainController>(); 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            playerCC = other.GetComponent<CharacterController>();
            if (playerCC != null)
            {
                playerIsOnPlatform = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            playerIsOnPlatform = false;
            playerCC = null;
        }
    }
    
    // TREN HAREKETİNİ UYGULAMA (Tüm hareketler bittikten sonra)
    private void LateUpdate()
    {
        if (playerIsOnPlatform && playerCC != null && trainController != null)
        {
            Vector3 movement = trainController.deltaPosition;
            
            // CC'ye hareketi uygula
            // Bu, oyuncunun kendi hareketine ek olarak uygulanır.
            playerCC.Move(movement); 
        }
    }
}