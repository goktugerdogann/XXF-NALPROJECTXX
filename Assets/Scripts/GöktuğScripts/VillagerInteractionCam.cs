using System.Collections;
using UnityEngine;
using Cinemachine;
using Economy;

public class VillagerInteractionCam : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineVirtualCamera mainVCam;
    public CinemachineVirtualCamera villagerVCam;

    [Header("Player")]
    public FPController playerController;
    public PlayerVisualHider playerVisualHider;
    public HeldItemAnimator heldItemAnimator;
    public InteractionRaycaster interactionRaycaster;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Blend Timing")]
    public float enterMouseUnlockDelay = 0.9f;
    public float exitMovementUnlockDelay = 0.9f;

    [Header("Shop")]
    public ShopData shopData; // Bu koye bagli dukan datasini inspector'dan ver

    bool inConversation = false;
    bool playerInRange = false;

    Coroutine enterRoutine;
    Coroutine exitRoutine;

    void Update()
    {
        if (!playerInRange) return;

        // Sadece giris, cikis yok
        if (Input.GetKeyDown(interactKey) && !inConversation)
        {
            EnterConversation();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        // Oyuncu alanin disina cikarsa ve henuz cikis yapilmadiysa
        if (inConversation)
        {
            ExitConversation();
        }
    }

    public void EnterConversation()
    {
        if (inConversation) return;
        inConversation = true;

        if (exitRoutine != null)
        {
            StopCoroutine(exitRoutine);
            exitRoutine = null;
        }

        // Envanteri kilitle
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.blockInventory = true;
            InventoryUI.Instance.ForceCloseFromConversation();
        }

        // Elde tutulan item'i sakla
        if (heldItemAnimator != null)
            heldItemAnimator.PlayHide();

        // Placement ghost'u kapat
        if (interactionRaycaster != null)
            interactionRaycaster.BlockPlacementForConversation(true);

        // Player modelini gizle
        if (playerVisualHider != null)
            playerVisualHider.SetHidden(true);

        // Hareketi dondur
        if (playerController != null)
            playerController.freezeMovement = true;

        // Kameralari degistir
        if (villagerVCam != null && mainVCam != null)
        {
            villagerVCam.Priority = 30;
            mainVCam.Priority = 10;
        }

        // Blend boyunca mouse kilitli
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        enterRoutine = StartCoroutine(EnterMouseUnlockCoroutine());
    }

    public void ExitConversation()
    {
        if (!inConversation) return;
        inConversation = false;

        if (enterRoutine != null)
        {
            StopCoroutine(enterRoutine);
            enterRoutine = null;
        }

        // Ana kameraya don
        if (villagerVCam != null && mainVCam != null)
        {
            mainVCam.Priority = 30;
            villagerVCam.Priority = 10;
        }

        // Mouse'u tekrar kilitliyoruz;
        // coroutine sonunda hareket acilacak.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        exitRoutine = StartCoroutine(ExitMovementUnlockCoroutine());
    }

    IEnumerator EnterMouseUnlockCoroutine()
    {
        // Kamera blend suresi
        yield return new WaitForSeconds(enterMouseUnlockDelay);

        if (!inConversation)
        {
            enterRoutine = null;
            yield break;
        }

        // Artik mouse serbest, UI acilabilir
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tam bu noktada shop panelini aciyoruz
        if (TradeManager.Instance != null && shopData != null)
        {
            TradeManager.Instance.EnterShop(shopData);
        }

        enterRoutine = null;
    }

    IEnumerator ExitMovementUnlockCoroutine()
    {
        yield return new WaitForSeconds(exitMovementUnlockDelay);

        // Character hareketi ac
        if (playerController != null)
            playerController.freezeMovement = false;

        // Player modelini tekrar goster
        if (playerVisualHider != null)
            playerVisualHider.SetHidden(false);

        // Elde tutulan item'i geri getir
        if (heldItemAnimator != null)
            heldItemAnimator.PlayShow();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.blockInventory = false;

        if (interactionRaycaster != null)
            interactionRaycaster.BlockPlacementForConversation(false);

        exitRoutine = null;
    }
}
