    using UnityEngine;
using System.Collections;   
public class PlatformingSurface : MonoBehaviour
{
    // Oyuncu Tag'ı tekrar kontrol ediliyor
    private const string PlayerTag = "Player";

    // Oyuncu yüzeye girdiğinde
    private void OnCollisionEnter(Collision collision)
    {
        // Temas eden nesne oyuncu mu?
        if (collision.gameObject.CompareTag(PlayerTag))
        {
            // Oyuncuyu bu tren nesnesinin (PlatformingSurface'ın parent'ı) alt nesnesi yap.
            // Bu, oyuncunun pozisyonunu trenin pozisyonuna göre hizalar.
            collision.transform.SetParent(transform.parent);
        }
    }

    // Oyuncu yüzeyden ayrıldığında
    private void OnCollisionExit(Collision collision)
    {
        // Ayrılan nesne oyuncu mu?
        if (collision.gameObject.CompareTag(PlayerTag))
        {
            // Oyuncunun ebeveynliğini kaldır.
            // (Artık Dünya'ya göre hareket edecek.)
            collision.transform.SetParent(null);
        }
    }
}