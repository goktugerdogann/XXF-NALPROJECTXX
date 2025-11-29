using System;
using UnityEngine;
public class PlatformingSurface : MonoBehaviour
{
    private const string PlayerTag = "Player";
    private TrainController trainController;
    private FPController playerFPController;
    private Vector3 lastTrainPos;

    private void Start()
    {
        trainController = GetComponentInParent<TrainController>();
        if (trainController != null)
            lastTrainPos = trainController.transform.position;
    }

    private void Update()
    {
        if (trainController == null) return;

        // Frame-bazlı delta (world-space)
        Vector3 current = trainController.transform.position;
        Vector3 deltaThisFrame = current - lastTrainPos;
        lastTrainPos = current;

        if (playerFPController != null && playerFPController.isOnMovingPlatform)
        {
            // DOĞRU: frame delta ver (NOT velocity)
            playerFPController.platformMoveDelta = deltaThisFrame;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(PlayerTag))
            playerFPController = other.GetComponent<FPController>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(PlayerTag) && playerFPController != null)
        {
            playerFPController.isOnMovingPlatform = false;
            playerFPController.platformMoveDelta = Vector3.zero;
            playerFPController = null;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(PlayerTag)) return;

        var fp = other.GetComponent<FPController>();
        if (fp != null)
        {
            fp.isOnMovingPlatform = true;
            // platformMoveDelta artık Update tarafından set edilecek
            playerFPController = fp;
        }
    }
}