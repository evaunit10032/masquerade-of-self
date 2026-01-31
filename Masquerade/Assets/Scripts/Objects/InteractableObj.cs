using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class InteractableObj : MonoBehaviour
{
    [SerializeField] Transform grabPoint;

    [SerializeField] Rigidbody rb;

    [SerializeField] GameObject brokenEffect;

    [SerializeField] bool breakable;

    private bool grabbed = false;

    public bool heavy_Object;

    bool thrown;

    private void Update()
    {
        //Debug.Log(rb.velocity.magnitude);
        
    }

    public void KillVelocity()
    {
        rb.velocity = Vector3.zero;
    }

    public void MoveToPos(Vector3 inputPos)
    {
        Vector3 direction = (inputPos - transform.position).normalized;
        rb.velocity = Vector3.zero;
        rb.AddForce(direction * 5f, ForceMode.VelocityChange); 
        //transform.position = inputPos;
    }

    public void TurnToRot(Quaternion inputRot)
    {
        transform.rotation = Quaternion.Euler(new Vector3(0,inputRot.eulerAngles.y,0));
    }

    public virtual void Break_Obj(int i)
    {
        if(breakable)
        {
            Instantiate(brokenEffect, transform.position, transform.rotation);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPos = transform.position;
        if(rb.velocity.magnitude > 40f && breakable && rb.useGravity)
        {
            Break_Obj(2);
            
        } else if (rb.velocity.magnitude > 20f && breakable && rb.useGravity)
        {
            Break_Obj(1);
        }
    }
}
