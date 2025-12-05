using System;
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
        // ilk açýlýþta bir þey varsa al
        if (currentItem == null)
        {
            CacheFromChildren();
        }
    }

    // Eski auto sistem: sadece fallback için dursun
    void CacheFromChildren()
    {
        if (transform.childCount == 0)
        {
            currentItem = null;
            return;
        }

        currentItem = transform.GetChild(0);
        originalLocalPos = currentItem.localPosition;
        originalLocalScale = currentItem.localScale;
    }

    // ÖNEMLÝ: EquipManager buraya yeni item transform'unu gönderiyor
    public void SetCurrentItem(Transform itemTransform)
    {
        currentItem = itemTransform;

        if (currentItem != null)
        {
            originalLocalPos = currentItem.localPosition;
            originalLocalScale = currentItem.localScale;
        }
    }

    // EquipManager eldeki modeli yok ettiðinde çaðýrýyor
    public void OnEquippedDestroyed()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentItem = null;
    }

    // ---------------- HIDE DIRECT (instant) ---------------- //
    public void PlayHide()
    {
        if (currentItem == null)
        {
            CacheFromChildren();
        }
        if (currentItem == null) return;

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentItem.gameObject.SetActive(false);
    }

    // ---------------- HIDE ANIM (reverse) ---------------- //
    public void PlayHideAnimated(Action onComplete = null)
    {
        if (currentItem == null)
        {
            CacheFromChildren();
        }
        if (currentItem == null)
        {
            if (onComplete != null) onComplete();
            return;
        }

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(HideRoutine(onComplete));
    }

    IEnumerator HideRoutine(Action onComplete)
    {
        Transform t = currentItem;
        if (t == null)
        {
            currentRoutine = null;
            if (onComplete != null) onComplete();
            yield break;
        }

        t.gameObject.SetActive(true);

        Vector3 startPos = originalLocalPos;
        Vector3 endPos = originalLocalPos + hideOffset;

        Vector3 startScale = originalLocalScale;
        Vector3 endScale = Vector3.zero;

        float tNorm = 0f;
        while (tNorm < 1f)
        {
            if (t == null)
            {
                currentRoutine = null;
                if (onComplete != null) onComplete();
                yield break;
            }

            tNorm += Time.deltaTime / animDuration;
            float k = curve != null ? curve.Evaluate(tNorm) : tNorm;

            t.localPosition = Vector3.Lerp(startPos, endPos, k);
            t.localScale = Vector3.Lerp(startScale, endScale, k);

            yield return null;
        }

        if (t != null)
        {
            t.localPosition = endPos;
            t.localScale = endScale;
            t.gameObject.SetActive(false);
        }

        currentRoutine = null;

        if (onComplete != null)
            onComplete();
    }

    // ---------------- SHOW ANIM ---------------- //
    public void PlayShow()
    {
        if (currentItem == null)
        {
            CacheFromChildren();
        }
        if (currentItem == null) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    IEnumerator ShowRoutine()
    {
        Transform t = currentItem;
        if (t == null)
        {
            currentRoutine = null;
            yield break;
        }

        t.gameObject.SetActive(true);

        Vector3 startPos = originalLocalPos + hideOffset;
        Vector3 endPos = originalLocalPos;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = originalLocalScale;

        float tNorm = 0f;
        while (tNorm < 1f)
        {
            if (t == null)
            {
                currentRoutine = null;
                yield break;
            }

            tNorm += Time.deltaTime / animDuration;
            float k = curve != null ? curve.Evaluate(tNorm) : tNorm;

            t.localPosition = Vector3.Lerp(startPos, endPos, k);
            t.localScale = Vector3.Lerp(startScale, endScale, k);

            yield return null;
        }

        if (t != null)
        {
            t.localPosition = endPos;
            t.localScale = endScale;
        }

        currentRoutine = null;
    }
}
