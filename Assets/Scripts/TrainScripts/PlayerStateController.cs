using UnityEngine;
using System.Collections;
public enum PlayerState
{
    Free,
    TrainControl
}

public class PlayerStateController : MonoBehaviour
{
    public static PlayerStateController Instance;
    public PlayerState currentState = PlayerState.Free;

    private void Awake()
    {
        Instance = this;
    }

    public bool CanMove()
    {
        return currentState == PlayerState.Free;
    }
}