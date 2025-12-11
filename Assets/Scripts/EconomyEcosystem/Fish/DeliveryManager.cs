using UnityEngine;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance;

    [Header("Crate Settings")]
    public GameObject cratePrefab;       // Inspector: kasanin prefab'i
    public Transform deliveryCenter;     // Inspector: dagilacagi merkezin Transform'u
    public float spawnRadius = 2f;       // Kasanin dagilacagi dairenin yaricapi
    public float heightOffset = 0.1f;    // Zeminden ne kadar yukarida dursun
    public LayerMask groundMask;         // Zemin layer'i (raycast icin)

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Toplam kg icin kasalari hesapla ve spawn et
    public void SpawnFishCrates(int totalKg)
    {
        if (cratePrefab == null || deliveryCenter == null)
        {
            Debug.LogWarning("DeliveryManager: cratePrefab or deliveryCenter is not set.");
            return;
        }

        if (totalKg <= 0)
            return;

        int fullCrates = totalKg / 20;          // 20 kg'lik tam kasalar
        int remainder = totalKg % 20;           // Artan kg
        int totalCrates = fullCrates + (remainder > 0 ? 1 : 0);

        for (int i = 0; i < totalCrates; i++)
        {
            int crateKg = 20;
            if (i == totalCrates - 1 && remainder > 0)
                crateKg = remainder;           // Son kasa artan kg olsun

            Vector3 pos = GetRandomPositionOnGround();
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject crate = Instantiate(cratePrefab, pos, rot);

            FishCrate crateComp = crate.GetComponent<FishCrate>();
            if (crateComp != null)
            {
                crateComp.kgAmount = crateKg;
            }
        }
    }

    // DeliveryCenter etrafinda daire icinde rastgele nokta, zemine oturtulmus
    Vector3 GetRandomPositionOnGround()
    {
        Vector3 center = deliveryCenter.position;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

        RaycastHit hit;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out hit, 20f, groundMask))
        {
            pos = hit.point + Vector3.up * heightOffset;
        }

        return pos;
    }
}
