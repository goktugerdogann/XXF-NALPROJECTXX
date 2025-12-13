using System.Collections.Generic;
using UnityEngine;

public class TrayCrateCollector : MonoBehaviour
{
    [Header("Refs")]
    public Transform cratesRoot; // kutular buraya child olur (tray objesi icinde bos bir empty)

    [Header("Filter")]
    public bool onlyFishCrate = true;

    private readonly HashSet<FishCrate> inside = new HashSet<FishCrate>();

    void Reset()
    {
        cratesRoot = transform;
    }

    public List<FishCrate> GetCurrentCrates()
    {
        List<FishCrate> list = new List<FishCrate>();
        foreach (var c in inside)
        {
            if (c == null) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            list.Add(c);
        }
        return list;
    }

    void OnTriggerEnter(Collider other)
    {
        FishCrate crate = other.GetComponentInParent<FishCrate>();
        if (onlyFishCrate && crate == null) return;
        if (crate == null) return;

        inside.Add(crate);

        // tepsiye yerlestirildi say: child yap
        if (cratesRoot != null)
        {
            crate.transform.SetParent(cratesRoot, true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        FishCrate crate = other.GetComponentInParent<FishCrate>();
        if (crate == null) return;

        inside.Remove(crate);

        // tepsiden ciktiysa parent'i serbest birak
        // (istersen tamamen null yap, istersen eski parent tutarsin)
        crate.transform.SetParent(null, true);
    }
}
