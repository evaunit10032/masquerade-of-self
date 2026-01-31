using UnityEngine;

public class OneBitCheckpoint : MonoBehaviour
{
    BR_PlayerController playerController;

    [SerializeField] Transform RespawnPoint;

    // Start is called before the first frame update
    void Start()
    {

    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if (playerController != null)
        {
            playerController.fill_lamp_oil();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("GameController"))
        {
            playerController = other.gameObject.GetComponent<BR_PlayerController>();
            playerController.set_respawn_anchor(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("GameController"))
        {
            playerController = null;
        }
    }

    public Transform GetRespawnPoint()
    {
        return RespawnPoint;
    }
}
