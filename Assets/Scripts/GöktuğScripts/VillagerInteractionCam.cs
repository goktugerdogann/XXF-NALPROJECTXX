using System.Collections;
using UnityEngine;
using Cinemachine;
using Economy;

public class VillagerInteractionCam : MonoBehaviour
{
    // global flag: any villager conversation active
    public static bool AnyConversationActive = false;

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
    public ShopData shopData;

    bool inConversation = false;
    bool playerInRange = false;

    Coroutine enterRoutine;
    Coroutine exitRoutine;

    void Update()
    {
        if (!playerInRange) return;

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

        if (inConversation)
        {
            ExitConversation();
        }
    }

    public void EnterConversation()
    {
        if (inConversation) return;
        inConversation = true;
        AnyConversationActive = true;

        if (exitRoutine != null)
        {
            StopCoroutine(exitRoutine);
            exitRoutine = null;
        }

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.blockInventory = true;
            InventoryUI.Instance.ForceCloseFromConversation();
        }

        if (heldItemAnimator != null)
            heldItemAnimator.PlayHide();

        if (interactionRaycaster != null)
            interactionRaycaster.BlockPlacementForConversation(true);

        if (playerVisualHider != null)
            playerVisualHider.SetHidden(true);

        if (playerController != null)
            playerController.freezeMovement = true;

        if (villagerVCam != null && mainVCam != null)
        {
            villagerVCam.Priority = 30;
            mainVCam.Priority = 10;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        enterRoutine = StartCoroutine(EnterMouseUnlockCoroutine());
    }

    public void ExitConversation()
    {
        if (!inConversation) return;
        inConversation = false;
        AnyConversationActive = false;

        if (enterRoutine != null)
        {
            StopCoroutine(enterRoutine);
            enterRoutine = null;
        }

        if (villagerVCam != null && mainVCam != null)
        {
            mainVCam.Priority = 30;
            villagerVCam.Priority = 10;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        exitRoutine = StartCoroutine(ExitMovementUnlockCoroutine());
    }

    IEnumerator EnterMouseUnlockCoroutine()
    {
        yield return new WaitForSeconds(enterMouseUnlockDelay);

        if (!inConversation)
        {
            enterRoutine = null;
            yield break;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (TradeManager.Instance != null && shopData != null)
        {
            TradeManager.Instance.EnterShop(shopData);
        }

        enterRoutine = null;
    }

    IEnumerator ExitMovementUnlockCoroutine()
    {
        yield return new WaitForSeconds(exitMovementUnlockDelay);

        if (playerController != null)
            playerController.freezeMovement = false;

        if (playerVisualHider != null)
            playerVisualHider.SetHidden(false);

        if (heldItemAnimator != null)
            heldItemAnimator.PlayShow();

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.blockInventory = false;

        if (interactionRaycaster != null)
            interactionRaycaster.BlockPlacementForConversation(false);

        exitRoutine = null;
    }
}
