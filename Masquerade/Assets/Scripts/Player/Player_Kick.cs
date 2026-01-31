using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Kick : MonoBehaviour
{
    [SerializeField] Player_Master p_master;

    [SerializeField] float kickRange = 4f;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            //If jumping then enter jump-kick
            if (p_master.Get_P_Controller().AmIWalkRunSlideJump().jump)
            {
                p_master.Get_P_Anims().SetAnimTrigger("Kick");
            }
            else
            {
                p_master.Get_P_Anims().SetAnimTrigger("Jump-Kick");
            }
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, kickRange))
            {
                switch (tag)
                {
                    case ("Prop"):
                    case ("DestructibleProp"):
                        //hit.collider.attachedRigidbody.hit.collider.attachedRigidbody.AddExplosionForce(850f, transform.position, explosionRadius);
                        break;
                    case ("Door"):
                        hit.transform.gameObject.GetComponent<Doors>().DoorKick(transform.position);
                        break;
                    default:
                        break;
                }
                Debug.Log(hit.transform.gameObject.name);


            }
        }
        
    }
}
