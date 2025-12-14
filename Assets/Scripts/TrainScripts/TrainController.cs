using UnityEngine;

public class TrainController : MonoBehaviour
{
    [Header("Tren Fizik Değerleri")]
    [SerializeField] private float maxSpeed = 100.0f;
    [SerializeField] private float accelerationRate = 0.5f;
    [SerializeField] private float brakeForce = 2.0f;
    [SerializeField] private float rollingResistance = 0.1f;
    public Vector3 TrainVelocity { get; private set; }
    private Rigidbody rb;
    private Vector3 lastPos;
    
    [Header("VİTES AYARLARI")]
    [SerializeField] private int currentGear = 1;   // 1 ileri, -1 geri, 0 boş
    [SerializeField] private int maxGear = 1;
    [SerializeField] private int minGear = -1;

    [Header("Anlık Durum ve Kontroller")]
    public float currentSpeed = 0.0f;

    [Tooltip("Gaz BASINCI (0.0 - 1.0)")]
    private float throttleInput = 0.0f;

    [Tooltip("Fren (0.0 - 1.0)")]
    private float brakeInput = 0.0f;

    [Header("Kontrol Durumu")]
    public bool isControlledByPlayer = false;

    void Start()
    {

        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!isControlledByPlayer)
        {
            throttleInput = 0.0f;
            brakeInput = 0.0f;
            return;
        }

        // --------------------
        // GAZ BASINCI (0 - 1)
        // --------------------
        if (Input.GetKeyDown(KeyCode.U))
            throttleInput += 0.1f;

        if (Input.GetKeyDown(KeyCode.J))
            throttleInput -= 0.1f;

        throttleInput = Mathf.Clamp01(throttleInput); // ✅ -1 tamamen silindi

        // --------------------
        // FREN
        // --------------------
        if (Input.GetKeyDown(KeyCode.H))
            brakeInput += 0.1f;

        if (Input.GetKeyDown(KeyCode.K))
            brakeInput -= 0.1f;

        brakeInput = Mathf.Clamp01(brakeInput);

        // --------------------
        // VİTES SİSTEMİ
        // --------------------
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentGear = 1;   // İleri
        if (Input.GetKeyDown(KeyCode.Alpha0)) currentGear = 0;   // Boş
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentGear = -1;  // Geri

        currentGear = Mathf.Clamp(currentGear, minGear, maxGear);
    }

    private void FixedUpdate()
    {
        // ✅ Gaz artık SADECE 0–1
        // ✅ Yönü SADECE VİTES belirliyor
        currentSpeed += (throttleInput * accelerationRate * currentGear) * Time.fixedDeltaTime;

        // Fren
        if (brakeInput > 0f)
        {
            float brakeAmount = brakeInput * brakeForce * Time.fixedDeltaTime;

            if (Mathf.Abs(currentSpeed) <= brakeAmount)
                currentSpeed = 0f;
            else
                currentSpeed -= Mathf.Sign(currentSpeed) * brakeAmount;
        }

        // Sürtünme
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            0f,
            rollingResistance * Time.fixedDeltaTime
        );

        // Hız sınırı
        currentSpeed = Mathf.Clamp(currentSpeed, -maxSpeed, maxSpeed);

        if (Mathf.Abs(currentSpeed) < 0.01f)
            currentSpeed = 0f;

        // Hareket
        rb.MovePosition(rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime);

        TrainVelocity = (transform.position - lastPos) / Time.fixedDeltaTime;
        lastPos = transform.position;   
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }
}
