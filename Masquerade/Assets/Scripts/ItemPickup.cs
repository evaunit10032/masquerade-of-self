using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] int myitemID;

    [SerializeField] GameObject itemModel;

    [SerializeField] Rigidbody myRigidBody;

    [SerializeField] AudioClip myAudioClip;

    [SerializeField] bool ammo;

    [SerializeField] int ammoType;

    [SerializeField] bool world;

    [SerializeField] bool armor;

    [SerializeField] bool lampOil;







    // Start is called before the first frame update
    void Start()
    {
        if (!world)
        {
            myRigidBody.AddForce(transform.up * 5, ForceMode.Impulse);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            BR_PlayerController PC = other.GetComponent<BR_PlayerController>();
            if (ammo)
            {
                switch (ammoType)
                {
                    case 1:
                        if (world)
                        {
                            PC.PickupItem(0, 1, 50, 0, 0);
                        }
                        else
                        {
                            PC.PickupItem(0, 1, 15, 0, 0);
                        }
                        break;
                    case 2:
                        if (world)
                        {
                            PC.PickupItem(0, 2, 20, 0, 0);
                        }
                        else
                        {
                            PC.PickupItem(0, 2, 10, 0, 0);
                        }
                        break;
                    case 3:
                        PC.PickupItem(0, 1, 50, 0, 0);
                        break;
                }
            }
            else if (armor)
            {
                PC.PickupItem(0, 0, 0, 25f, 0);
            }
            else if (lampOil)
            {
                PC.PickupItem(0, 0, 0, 0, 50f);
            }
            else
            {
                PC.PickupItem(25f, 0, 0, 0, 0);
            }
            //PC.PlayAudioFile(myAudioClip);
            Destroy(gameObject);

        }
    }

    private void Update()
    {
        itemModel.transform.Rotate(0, 0.25f, 0);
    }
}
