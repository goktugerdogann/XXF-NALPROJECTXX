using System.Collections.Generic;
using UnityEngine;
using Economy;

public class BuyerDeliveryZone : MonoBehaviour
{
    public BuyerData buyerData;

    [Header("Scan settings")]
    public float scanRadius = 3f;
    public LayerMask scanMask = ~0;

    public List<SellFishSummary> GetCurrentFishSummary()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, scanMask);

        Dictionary<FishDef, int> dict = new Dictionary<FishDef, int>();

        foreach (Collider hit in hits)
        {
            FishCrate crate = hit.GetComponent<FishCrate>();
            if (crate == null) continue;
            if (crate.kgAmount <= 0) continue;
            if (crate.fishDef == null) continue;

            int current;
            if (!dict.TryGetValue(crate.fishDef, out current))
                current = 0;

            current += crate.kgAmount;
            dict[crate.fishDef] = current;
        }

        List<SellFishSummary> list = new List<SellFishSummary>();

        foreach (var kv in dict)
        {
            SellFishSummary s;
            s.fish = kv.Key;
            s.totalKg = kv.Value;
            list.Add(s);
        }

        return list;
    }

    public void RemoveKgFromCrates(FishDef fish, int kgToRemove)
    {
        if (fish == null || kgToRemove <= 0) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius, scanMask);

        foreach (Collider hit in hits)
        {
            if (kgToRemove <= 0) break;

            FishCrate crate = hit.GetComponent<FishCrate>();
            if (crate == null) continue;
            if (crate.fishDef != fish) continue;
            if (crate.kgAmount <= 0) continue;

            int take = Mathf.Min(crate.kgAmount, kgToRemove);
            crate.kgAmount -= take;
            kgToRemove -= take;

            if (crate.kgAmount <= 0)
                Destroy(crate.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}

public struct SellFishSummary
{
    public FishDef fish;
    public int totalKg;
}
