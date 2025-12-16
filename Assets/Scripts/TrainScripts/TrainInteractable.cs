using UnityEngine;

public class TrainInteractable : MonoBehaviour
{
    public TrainController trainController;

    private bool playerInRange;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            UI_Train.Instance.ShowHint(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            UI_Train.Instance.ShowHint(false);
        }
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            EnterTrainControl();
        }
    }

    void EnterTrainControl()
    {
        PlayerStateController.Instance.currentState = PlayerState.TrainControl;
        trainController.isControlledByPlayer = true;

        UI_Train.Instance.ShowControlPanel(true);
    }
}