using System.Collections.Generic;
using UnityEngine;

public class CrateLoadingZone : MonoBehaviour
{
    [Header("Settings")]
    public bool debugLog = false;

    private readonly HashSet<FishCrate> crates = new HashSet<FishCrate>();

    // DIÞARI VERÝLEN TEMÝZ LÝSTE
    public List<FishCrate> GetCrates()
    {
        List<FishCrate> list = new List<FishCrate>();

        foreach (FishCrate c in crates)
        {
            if (c == null) continue;
            if (!c.gameObject.activeInHierarchy) continue;

            list.Add(c);
        }

        return list;
    }

    // =========================
    // TRIGGER EVENTS
    // =========================

    void OnTriggerEnter(Collider other)
    {
        FishCrate c = other.GetComponentInParent<FishCrate>();
        if (c == null) return;

        if (!crates.Contains(c))
        {
            crates.Add(c);
            if (debugLog)
                Debug.Log("[Zone] Crate ENTER: " + c.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        FishCrate c = other.GetComponentInParent<FishCrate>();
        if (c == null) return;

        if (crates.Contains(c))
        {
            crates.Remove(c);
            if (debugLog)
                Debug.Log("[Zone] Crate EXIT: " + c.name);
        }
    }

    // =========================
    // SAFETY NET (ÇOK ÖNEMLÝ)
    // =========================
    // Taþýrken / üst üste koyarken trigger kaçar diye
    // ZONE ÝÇÝNDEKÝ HER ÞEYÝ PERIODIC TEMÝZLER
    void LateUpdate()
    {
        if (crates.Count == 0) return;

        // NULL veya parent'ý deðiþmiþ olanlarý temizle
        crates.RemoveWhere(c =>
            c == null ||
            !c.gameObject.activeInHierarchy ||
            !IsCrateInsideZone(c)
        );
    }

    bool IsCrateInsideZone(FishCrate crate)
    {
        Collider zoneCol = GetComponent<Collider>();
        if (zoneCol == null) return false;

        Bounds b = zoneCol.bounds;
        return b.Contains(crate.transform.position);
    }

    // =========================
    // DIÞARDAN KONTROL ÝÇÝN
    // =========================

    public int CrateCount()
    {
        return crates.Count;
    }

    public void ClearZone()
    {
        crates.Clear();
    }
}
