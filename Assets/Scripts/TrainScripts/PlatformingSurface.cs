using UnityEngine;

public class PlatformingSurface : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private TrainController trainController;
    private CharacterController playerCC; // Oyuncunun CC referansı

    private void Start()
    {
        // TrainController'a erişim sağlayın
        trainController = GetComponentInParent<TrainController>(); 
        
        if (trainController == null)
        {
            Debug.LogError("PlatformingSurface, TrainController'ı bulamadı!");
        }
    }

    // Oyuncu yüzeyin üzerinde durduğu sürece çalışır
    private void OnTriggerStay(Collider other) 
    {
        if (other.CompareTag(PlayerTag))
        {
            // Oyuncunun CC'sini bir kez al
            if (playerCC == null)
            {
                playerCC = other.GetComponent<CharacterController>();
            }

            if (playerCC != null)
            {
                // TREN HAREKETİNİ OYUNCUYA MANUEL UYGULA
                // CharacterController'ın kendi Move metodunu kullanmak zorundayız.
                playerCC.Move(trainController.deltaPosition);
            }
        }
    }

    // Oyuncu yüzeyden çıktığında (isteğe bağlı, ama temizlik için iyidir)
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PlayerTag))
        {
            playerCC = null;
        }
    }
}