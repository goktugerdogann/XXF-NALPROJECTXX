using System.Collections;
using UnityEngine;

public class HeldItemAnimator : MonoBehaviour
{
    [Header("Anim")]
    public float animDuration = 0.25f;
    public Vector3 hideOffset = new Vector3(0f, -0.1f, -0.3f);
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    Transform currentItem;
    Vector3 originalLocalPos;
    Vector3 originalLocalScale;

    Coroutine currentRoutine;

    void Start()
    {
        CacheCurrentItem();
    }

    public void CacheCurrentItem()
    {
        if (transform.childCount == 0) return;

        currentItem = transform.GetChild(0);
        originalLocalPos = currentItem.localPosition;
        originalLocalScale = currentItem.localScale;
    }

    // ---------------- HIDE DIRECT ---------------- //
    public void PlayHide()
    {
        if (currentItem == null) CacheCurrentItem();
        if (currentItem == null) return;

        // ANINDA KAPAT — animasyon yok
        currentItem.gameObject.SetActive(false);
    }

    // ---------------- SHOW ANIM ---------------- //
    public void PlayShow()
    {
        if (currentItem == null) CacheCurrentItem();
        if (currentItem == null) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        Transform t = currentItem;
        t.gameObject.SetActive(true);

        Vector3 startPos = originalLocalPos + hideOffset;
        Vector3 endPos = originalLocalPos;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = originalLocalScale;

        float tNorm = 0f;
        while (tNorm < 1f)
        {
            tNorm += Time.deltaTime / animDuration;
            float k = curve != null ? curve.Evaluate(tNorm) : tNorm;

            t.localPosition = Vector3.Lerp(startPos, endPos, k);
            t.localScale = Vector3.Lerp(startScale, endScale, k);

            yield return null;
        }

        t.localPosition = endPos;
        t.localScale = endScale;

        currentRoutine = null;
    }
}
