using UnityEngine;
using Economy;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance;

    [Header("Crate Settings")]
    public GameObject cratePrefab;
    public Transform deliveryCenter;
    public float spawnRadius = 2f;
    public float heightOffset = 0.1f;
    public LayerMask groundMask;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnFishCrates(FishDef fishDef, int totalKg)
    {
        if (cratePrefab == null || deliveryCenter == null)
        {
            Debug.LogWarning("DeliveryManager: cratePrefab or deliveryCenter is not set.");
            return;
        }

        if (totalKg <= 0) return;

        int fullCrates = totalKg / 20;
        int remainder = totalKg % 20;
        int totalCrates = fullCrates + (remainder > 0 ? 1 : 0);

        for (int i = 0; i < totalCrates; i++)
        {
            int crateKg = 20;
            if (i == totalCrates - 1 && remainder > 0)
                crateKg = remainder;

            Vector3 pos = GetRandomPositionOnGround();
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject crate = Instantiate(cratePrefab, pos, rot);

            FishCrate crateComp = crate.GetComponent<FishCrate>();
            if (crateComp == null)
                crateComp = crate.AddComponent<FishCrate>();

            crateComp.fishDef = fishDef;   // BALIGIN TÜRÜ
            crateComp.kgAmount = crateKg;   // KASADAKI KG
        }
    }


    // Eski kodu bozmasin diye istersen su overload’i birakabilirsin:
    public void SpawnFishCrates(int totalKg)
    {
        SpawnFishCrates(null, totalKg);
    }

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
