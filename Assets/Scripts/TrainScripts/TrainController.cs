using UnityEngine;

public class TrainController : MonoBehaviour
{
    [Header("Tren Fizik Değerleri")]
    // Serileştirilmiş alanlar, Unity Inspector'dan ayarlanabilir.
    [SerializeField] private float maxSpeed = 100.0f;          // Maksimum hız (örneğin: km/sa veya m/s)
    [SerializeField] private float accelerationRate = 0.5f;    // Güç verildiğinde hızı artırma oranı
    [SerializeField] private float brakeForce = 2.0f;          // Fren uygulandığında hızı azaltma gücü
    [SerializeField] private float rollingResistance = 0.1f;   // Doğal yavaşlama/sürtünme kuvveti
    
    [Header("Anlık Durum ve Kontroller")]
    [Tooltip("Trenin anlık hızı")]
    public float currentSpeed = 0.0f;
    [Tooltip("Makinistin Gaz/Güç girişi (-1.0 Geri, 1.0 İleri)")]
    private float throttleInput = 0.0f;
    [Tooltip("Makinistin Fren girişi (0.0 Yok, 1.0 Tam Fren)")]
    private float brakeInput = 0.0f;
    [Header("Kontrol Durumu")]
    [Tooltip("Trenin oyuncu tarafından kontrol edilip edilmediği.")]
    public bool isControlledByPlayer = false;
    [Header("1860s Buhar Kontrolü")]
    [Tooltip("Reverser/Cut-Off kolu pozisyonu. -1.0 (Tam Geri) ile 1.0 (Tam İleri) arası.")]
    public float reverserInput = 1.0f;
    
    private Vector3 previousPosition;
    [HideInInspector] // Inspector'da gözükmesini istemiyorsak
    public Vector3 deltaPosition;
        
    // --- TEMEL GİRİŞ YÖNETİMİ ---
    void Update()
    {
        if (!isControlledByPlayer)
        {
            throttleInput = 0.0f;
            brakeInput = 0.0f;
            return; 
        }

        // --- KADEMELİ GÜÇ YÖNETİMİ ---
        // W'ye bir kez basmak gücü artırır, S'ye bir kez basmak gücü azaltır.
        if (Input.GetKeyDown(KeyCode.U))
        {
            // Gaz kolunu 0.1 kademe artır.
            throttleInput += 0.1f; 
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            // Gaz kolunu 0.1 kademe azaltır.
            throttleInput -= 0.1f; 
        }
    
        // throttleInput değerini -1.0 (Tam Geri) ile 1.0 (Tam İleri) arasında sınırla.
        throttleInput = Mathf.Clamp(throttleInput, -1.0f, 1.0f);
    

        // --- KADEMELİ FREN YÖNETİMİ ---
        // Freni Q ile artır, E ile azalt.
        if (Input.GetKeyDown(KeyCode.H))
        {
            // Fren gücünü artır.
            brakeInput += 0.1f; 
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            // Fren gücünü azalt/serbest bırak.
            brakeInput -= 0.1f; 
        }

        // brakeInput değerini 0.0 (Fren Yok) ile 1.0 (Tam Fren) arasında sınırla.
        brakeInput = Mathf.Clamp(brakeInput, 0.0f, 1.0f);

        
        
        
        // HATA AYIKLAMA KODLARI (Geçici olarak ekleyin)
        if (isControlledByPlayer)
        {
            // 1. Kontrolün sizde olduğunu teyit edin
            Debug.Log("Kontrol Aktif. ThrottleInput: " + throttleInput.ToString("F2") + 
                      " ReverserInput: " + reverserInput.ToString("F2"));
        }

        // 2. Tuşa basıldığında ThrottleInput'un değişip değişmediğini teyit edin
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("W/S tuşuna basıldı. Yeni ThrottleInput: " + throttleInput.ToString("F2"));
        }
        // Not: Artık Input.GetAxis("Vertical") kullanmıyoruz.
    }

    // --- FİZİK HESAPLAMALARI ---
    private void FixedUpdate()
    {
        
        if (!isControlledByPlayer)
        {
            // Kontrolsüzken sadece doğal sürtünme ile yavaşlamaya devam etmesini isterseniz
            // Burayı boş bırakıp sadece sürtünme hesaplamasını yürütebilirsiniz. 
            // Ancak en basit yöntem, girişleri sıfırlayarak sadece sürtünme ile durmasını sağlamaktır.
        
            // Bu kod parçacığı (sürtünme, hız sıfırlama, konum güncelleme) 
            // hâlâ çalışmaya devam etmelidir ki tren yavaşlayıp rayda ilerlesin.
        
            // Yani, FixedUpdate'i değiştirmeden bırakabilirsiniz, çünkü Update'te 
            // throttleInput ve brakeInput'ı zaten sıfırlıyoruz.
        }
        
        // Fizik hesaplamaları için sabit bir zaman adımı kullanırız.

        // 1. Güç Uygulama (İvmelenme)
        // Hızı, gaz girişine ve ivmelenme oranına göre artır.
        currentSpeed += throttleInput * accelerationRate * Time.fixedDeltaTime;

        // 2. Fren Uygulama
        // Fren girişine ve fren kuvvetine göre hızı azalt.
        currentSpeed -= brakeInput * brakeForce * Time.fixedDeltaTime;

        // 3. Ray Sürtünmesi/Doğal Direnç
        // Hızın yönüne göre sürtünmeyi uygula (hız pozitifse azalt, negatifse artır).
        // Mathf.Sign(currentSpeed) hızı +1 veya -1 olarak verir.
        currentSpeed -= Mathf.Sign(currentSpeed) * rollingResistance * Time.fixedDeltaTime;
        
        // --- HIZ SINIRLAMA VE KONUM GÜNCELLEME ---

        // 4. Maksimum Hız Sınırı
        // Hızı, tanımlanan maksimum hız ile eksi maksimum hız arasında tut.
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        // 5. Çok Yavaşsa Durdur
        // Hız sıfıra çok yakınsa, momentumu sıfırla (trenin sonsuza kadar yavaşça sürünmesini engeller).
        if (Mathf.Abs(currentSpeed) < 0.01f) 
        {
            currentSpeed = 0f;
        }

        // 6. Konum Güncelleme
        // Treni anlık hızı kadar ileri (forward) yönde hareket ettir.
        // **ÖNEMLİ:** Bu, basit düz bir hareket sağlar. Ray takibi için bu satır daha karmaşık olacaktır.
        transform.position += transform.forward * currentSpeed * Time.fixedDeltaTime;
    }
    
    void Start()
    {
        // Başlangıç pozisyonunu kaydet
        previousPosition = transform.position;
    }
    // Harici sistemlerin (örn. UI) hızı okuması için basit bir metod.
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
}