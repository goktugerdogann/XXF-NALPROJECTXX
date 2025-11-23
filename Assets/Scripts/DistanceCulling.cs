using UnityEngine;

public class DistanceCulling : MonoBehaviour
{
    public float deactivateDistance = 80f;
    public float reactivateDistance = 70f; // geri gelince biraz buffer olsun

    public Renderer[] renderersToToggle;
    public Collider[] collidersToToggle;
    public Behaviour[] componentsToToggle;  // AI, script vs

    Transform _cam;
    bool _isActive = true;

    void Start()
    {
        _cam = Camera.main != null ? Camera.main.transform : null;

        if (_cam == null)
        {
            Debug.LogWarning("DistanceCulling: Main Camera bulunamadý.");
        }

        if (renderersToToggle == null || renderersToToggle.Length == 0)
        {
            var r = GetComponent<Renderer>();
            if (r != null) renderersToToggle = new Renderer[] { r };
        }
    }

    void Update()
    {
        if (_cam == null) return;

        float sqrDist = (transform.position - _cam.position).sqrMagnitude;
        float deactivateSqr = deactivateDistance * deactivateDistance;
        float reactivateSqr = reactivateDistance * reactivateDistance;

        if (_isActive && sqrDist > deactivateSqr)
        {
            SetState(false);
        }
        else if (!_isActive && sqrDist < reactivateSqr)
        {
            SetState(true);
        }
    }

    void SetState(bool state)
    {
        _isActive = state;

        if (renderersToToggle != null)
            foreach (var r in renderersToToggle)
                if (r != null) r.enabled = state;

        if (collidersToToggle != null)
            foreach (var c in collidersToToggle)
                if (c != null) c.enabled = state;

        if (componentsToToggle != null)
            foreach (var b in componentsToToggle)
                if (b != null) b.enabled = state;
    }
}
