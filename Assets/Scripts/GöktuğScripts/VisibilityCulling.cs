using UnityEngine;

public class VisibilityCulling : MonoBehaviour
{
    [Header("Görününce Aç / Görünmeyince Kapat")]
    public Renderer[] renderersToToggle;  // MeshRenderer, SkinnedMeshRenderer vs
    public Behaviour[] componentsToToggle; // Script, Animator vs

    bool _isVisible = true;

    void Reset()
    {
        // Otomatik kendine ait renderer’ý doldur
        var r = GetComponent<Renderer>();
        if (r != null)
            renderersToToggle = new Renderer[] { r };
    }

    void OnBecameVisible()
    {
        SetState(true);
    }

    void OnBecameInvisible()
    {
        SetState(false);
    }

    void SetState(bool state)
    {
        if (_isVisible == state) return;
        _isVisible = state;

        if (renderersToToggle != null)
        {
            foreach (var r in renderersToToggle)
                if (r != null) r.enabled = state;
        }

        if (componentsToToggle != null)
        {
            foreach (var c in componentsToToggle)
                if (c != null) c.enabled = state;
        }
    }
}
