using UnityEngine;

public class CCTrainFollower : MonoBehaviour
{
    private CharacterController cc;
    private TrainController train;
    private bool onTrain;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Train"))
        {
            train = hit.collider.GetComponent<TrainController>();
            onTrain = true;
        }
    }

    void Update()
    {
        if (onTrain && train != null)
        {
            cc.Move(train.TrainVelocity * Time.deltaTime);
        }
    }

    void OnCollisionExit(Collision col)
    {
        if (col.collider.CompareTag("Train"))
        {
            onTrain = false;
            train = null;
        }
    }
}