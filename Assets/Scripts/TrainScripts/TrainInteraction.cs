using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrainInteraction : MonoBehaviour
{
    // Hangi tuşa basılınca etkileşim gerçekleşecek (Örn: E tuşu)
    public KeyCode interactionKey = KeyCode.E; 
    
    // Etkileşim menziline giren oyuncu objesi
    [SerializeField] private GameObject playerObject;
    
    // Bağlantıyı kuracağımız TrainController
    [SerializeField] private TrainController trainController;

    private void Start()
    {
        // Aynı GameObject üzerindeki TrainController bileşenini al.
        trainController = GetComponent<TrainController>();

        if (trainController == null)
        {
            Debug.LogError("Bu objede TrainController bulunamadı!");
        }
    }

    // Oyuncu Trigger alanına girdiğinde
    private void OnTriggerEnter(Collider other)
    {
        // Oyuncuyu temsil eden bir etiketi (tag) kontrol etmeliyiz (Örn: "Player")
        if (other.CompareTag("Player"))
        {
            playerObject = other.gameObject;
            Debug.Log("Kontrol paneline yaklaşıldı. Devralmak için " + interactionKey.ToString() + " tuşuna basın.");
            // UI'da 'E' tuşuna bas' yazısını gösterebilirsiniz
        }
    }

    // Oyuncu Trigger alanından çıktığında
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerObject = null;
            // Oyuncu alandan çıktığında kontrolü otomatik bırakmasını isterseniz:
            if (trainController != null && trainController.isControlledByPlayer)
            {
                trainController.isControlledByPlayer = false;
                Debug.Log("Tren kontrolü bırakıldı.");
            }
            // UI'daki yazıyı kaldırın
        }
    }

    private void Update()
    {
        // Oyuncu menzildeyse (playerObject null değilse) VE E tuşuna basılıyorsa
        if (playerObject != null && Input.GetKeyDown(interactionKey))
        {
            // Kalan kod aynı.
            if (trainController != null)
            {
                trainController.isControlledByPlayer = !trainController.isControlledByPlayer;
            
                if (trainController.isControlledByPlayer)
                {
                    Debug.Log("Kontrol Devralındı!");
                }
                else
                {
                    Debug.Log("Kontrol Bırakıldı.");
                }
            }
        }
    }
}