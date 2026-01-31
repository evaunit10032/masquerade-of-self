using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class Player_Interaction : MonoBehaviour
{
    [SerializeField] Player_Master master;
    [SerializeField] Transform grabbingPoint;

    [Header("Pickup Settings")]
    [SerializeField] Transform grab_Point;
    private GameObject held_Obj;
    private Rigidbody held_Obj_RB;

    [Header("Physics Parameters")]
    [SerializeField] private float pickup_Range = 5f;
    [SerializeField] private float pickup_Force = 150f;
    [SerializeField] private float rotationSpeed = 5f;

    [SerializeField] bool heavy_Object;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
        
            if (held_Obj == null)
            {
                RaycastHit hit;
                if(Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, pickup_Range))
                {
                    switch(hit.transform.gameObject.tag)
                    {
                        case ("Door"):
                            Debug.Log("Hit a door");
                            hit.transform.gameObject.GetComponent<Doors>().DoorInteract(transform.position);
                            break;
                        case ("Prop"):
                        case("DestructibleProp"):
                            //Pickup object
                            PickUp_Object(hit.transform.gameObject);
                            break;
                        
                        default:
                            break;
                    }
                    Debug.Log(hit.transform.gameObject.name);
                    
                    
                }
            } else
            {
                //Drop object
                Drop_Object();
            }
        }
        if (held_Obj != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Throw_Object();
            }
            //Move object
            Move_Object();
        }
    }


    void Move_Object()
    {
        // Move object to player grabbing position
        if (Vector3.Distance(held_Obj.transform.position, grabbingPoint.position) > 0.1f)
        {
            Vector3 move_Direction = (grabbingPoint.position - held_Obj.transform.position);
            held_Obj_RB.AddForce(move_Direction * pickup_Force * held_Obj_RB.mass);
        }

        // Rotation handling
        Quaternion targetRotation = grabbingPoint.rotation;
        Quaternion rotationDifference = targetRotation * Quaternion.Inverse(held_Obj.transform.rotation);

        // Convert to axis-angle representation
        float angle;
        Vector3 axis;
        rotationDifference.ToAngleAxis(out angle, out axis);

        // Prevent rotation flipping by ensuring angle is between -180 and 180
        if (angle > 180)
        {
            angle -= 360;
        }

        //Reset angular velocity 
        held_Obj_RB.angularVelocity = Vector3.zero;

        // Apply torque if there's a significant rotation difference
        if (Mathf.Abs(angle) > 5.0f)
        {
            Vector3 torque = axis.normalized * angle * rotationSpeed;
            held_Obj_RB.AddTorque(torque, ForceMode.Force);
        }

        // For heavy objects, ensure y-axis points up while preserving rotation around y
        if (heavy_Object)
        {
            // Get the current rotation of the object
            Quaternion currentRotation = held_Obj.transform.rotation;

            // Extract the y-axis rotation (yaw) - the rotation around world up axis
            float yaw = currentRotation.eulerAngles.y;

            // Create a new rotation that keeps the y-axis rotation but zeroes out x and z
            Quaternion correctedRotation = Quaternion.Euler(0, yaw, 0);

            // Apply the corrected rotation
            held_Obj.transform.rotation = correctedRotation;
        }
    }


    void PickUp_Object(GameObject pickup_Obj)
    {
        if(pickup_Obj.GetComponent<InteractableObj>())
        {
            master.Get_P_WeaponMaster().SetCantFire(true);
            heavy_Object = pickup_Obj.GetComponent<InteractableObj>().heavy_Object;
            held_Obj_RB = pickup_Obj.GetComponent<Rigidbody>();
            held_Obj_RB.useGravity = false;
            held_Obj_RB.MoveRotation(grabbingPoint.transform.rotation);
            held_Obj_RB.drag = 10f;
            //held_Obj_RB.constraints = RigidbodyConstraints.FreezeRotation;

            held_Obj_RB.transform.parent = grabbingPoint;
            held_Obj = pickup_Obj;
            if (heavy_Object)
            {
                held_Obj_RB.constraints = RigidbodyConstraints.FreezeRotationX;
            }
            //  held_Obj.transform.localRotation = Quaternion.Euler(new Vector3(0,0, 0));
        }
    }

    void Drop_Object()
    {
        master.Get_P_WeaponMaster().SetCantFire(false);
        //held_Obj_RB.angularVelocity = Vector3.zero;
        held_Obj_RB.useGravity = true;
        held_Obj_RB.drag = 1f;
        held_Obj_RB.AddForce(grab_Point.transform.forward, ForceMode.Impulse);
        held_Obj_RB.constraints = RigidbodyConstraints.None;

        held_Obj_RB.transform.parent = null;
        held_Obj = null;
        held_Obj_RB = null;
    }

    void Throw_Object()
    {
        master.Get_P_WeaponMaster().SetCantFire(false);
        held_Obj_RB.useGravity = true;
        held_Obj_RB.drag = 1f;
        
        held_Obj_RB.constraints = RigidbodyConstraints.None;
        
        held_Obj_RB.transform.parent = null;
        if (heavy_Object)
        {
            Vector3 currentRotation = held_Obj.transform.forward;

            // Extract the y-axis rotation (yaw) - the rotation around world up axis
            float yaw = currentRotation.y;

            // Create a new rotation that keeps the y-axis rotation but zeroes out x and z
            Vector3 correctedRotation = new Vector3(0, yaw, 0);

            held_Obj_RB.AddForce(grab_Point.transform.forward * 70f, ForceMode.Impulse);
        } else
        {
            held_Obj_RB.AddForce(grab_Point.transform.forward * 70f, ForceMode.Impulse);
        }
        
        held_Obj = null;
        held_Obj_RB = null;
    }
}
