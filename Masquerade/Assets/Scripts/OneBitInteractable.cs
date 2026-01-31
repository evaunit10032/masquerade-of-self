using System.Collections.Generic;
using UnityEngine;

public class OneBitInteractable : MonoBehaviour
{
    [SerializeField] List<AudioClip> voices = new List<AudioClip>();
    [SerializeField] List<string> script = new List<string>();

    [SerializeField] List<GameObject> doors = new List<GameObject>();
    [SerializeField] int questStep;

    [SerializeField] bool weaponPickup;

    [SerializeField] int weaponID;

    [SerializeField] GameObject spawnDoor;



    // Start is called before the first frame update
    void Start()
    {

    }

    public void Interact()
    {
        foreach (GameObject door in doors)
        {
            door.SetActive(false);

        }
        if (spawnDoor != null)
        {
            spawnDoor.SetActive(true);
        }
        //GameManager.Instance.questStep = questStep;
        GameManager.Instance.play_dialouge(voices, script);
        Destroy(gameObject);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (questStep == GameManager.Instance.questStep + 1 && other.CompareTag("GameController"))
        {
            Debug.Log("Popup");
            UIEvents.SendInteractableCall(this);
        }
        else if (weaponPickup && other.CompareTag("GameController"))
        {
            other.GetComponent<BR_PlayerController>().UnlockWeapon(weaponID);
            Destroy(gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            UIEvents.SendInteractableCall(null);
        }
    }
}
