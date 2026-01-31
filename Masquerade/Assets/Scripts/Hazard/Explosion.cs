using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] float explosionRadius = 15f;
    [SerializeField] float damage = 25f;
    // Start is called before the first frame update
    void Start()
    {
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, explosionRadius, Vector3.up);
        foreach (RaycastHit hit in hits)
        {
            //Decide what to do with object based on tag
            switch (hit.collider.gameObject.tag)
            {
                //Destroy fragile props like mugs or some glass. This also controls explosive barrels.
                case ("DestructibleProp"):
                    hit.collider.attachedRigidbody.AddExplosionForce(350f, transform.position, explosionRadius);
                    hit.collider.gameObject.GetComponent<InteractableObj>().Break_Obj(1);
                    break;

                case ("Prop"):
                    hit.collider.attachedRigidbody.AddExplosionForce(850f, transform.position, explosionRadius);
                    break;

                case ("Door"):
                    hit.transform.gameObject.GetComponent<Doors>().DoorKick(transform.position);
                    break;

                default:
                    break;
            }
        }
    }

  
}
