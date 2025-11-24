using System.Collections;
using UnityEngine;
using Cinemachine;

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

    bool inConversation = false;
    bool playerInRange = false;

    Coroutine enterRoutine;
    Coroutine exitRoutine;

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!inConversation)
                EnterConversation();
            else
                ExitConversation();
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
            ExitConversation();
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

        // Lock inventory if you have that logic
        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.blockInventory = true;
            InventoryUI.Instance.ForceCloseFromConversation();
        }

        // Hide held item instantly
        if (heldItemAnimator != null)
            heldItemAnimator.PlayHide();

        // Hide placement preview ghost
        if (interactionRaycaster != null)
            interactionRaycaster.BlockPlacementForConversation(true);

        // Hide player body
        if (playerVisualHider != null)
            playerVisualHider.SetHidden(true);

        // Freeze movement
        if (playerController != null)
            playerController.freezeMovement = true;

        // Switch cameras
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

        if (!inConversation) yield break;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
