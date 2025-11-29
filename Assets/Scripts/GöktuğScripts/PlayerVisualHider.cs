using UnityEngine;
using System.Collections.Generic;

public class PlayerVisualHider : MonoBehaviour
{
    [Header("HoldPoint Kapsam Dýþý")]
    public Transform excludeRoot;          // Buraya HoldPoint’i sürükle

    [Header("Ekstra Rendererlar (opsiyonel)")]
    public Renderer[] extraRenderers;

    Renderer[] allRenderers;

    void Awake()
    {
        RefreshRendererList();
    }

    void RefreshRendererList()
    {
        var list = new List<Renderer>();
        var found = GetComponentsInChildren<Renderer>(true);

        foreach (var r in found)
        {
            if (r == null) continue;

            // HoldPoint ve çocuklarýný listeye ekleme
            if (excludeRoot != null && r.transform.IsChildOf(excludeRoot))
                continue;

            list.Add(r);
        }

        if (extraRenderers != null)
        {
            foreach (var r in extraRenderers)
            {
                if (r == null) continue;
                if (excludeRoot != null && r.transform.IsChildOf(excludeRoot))
                    continue;
                if (!list.Contains(r)) list.Add(r);
            }
        }

        allRenderers = list.ToArray();
    }

    public void SetHidden(bool hidden)
    {
        RefreshRendererList();

        if (allRenderers == null) return;

        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            r.enabled = !hidden;
        }
    }
}
