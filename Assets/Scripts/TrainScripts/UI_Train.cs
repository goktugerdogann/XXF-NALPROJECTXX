using UnityEngine;

public class UI_Train : MonoBehaviour
{
    public static UI_Train Instance;

    [Header("UI Elements")]
    public GameObject hintText;
    public GameObject controlPanel;

    private void Awake()
    {
        Instance = this;
        ShowHint(false);
        ShowControlPanel(false);
    }

    public void ShowHint(bool value)
    {
        hintText.SetActive(value);
    }

    public void ShowControlPanel(bool value)
    {
        controlPanel.SetActive(value);
    }

    private void Update()
    {
        if (PlayerStateController.Instance.currentState == PlayerState.TrainControl)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitTrainControl();
            }
        }
    }

    void ExitTrainControl()
    {
        PlayerStateController.Instance.currentState = PlayerState.Free;

        FindObjectOfType<TrainController>().isControlledByPlayer = false;
        ShowControlPanel(false);
    }
}