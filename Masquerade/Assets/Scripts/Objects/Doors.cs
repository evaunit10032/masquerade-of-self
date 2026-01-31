using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Doors : MonoBehaviour
{
    [SerializeField]
    private Animator doorAnimator;

    [SerializeField]
    private Transform doorCenterPoint;

    [SerializeField]
    List<AudioClip> doorSounds = new List<AudioClip>();

    [SerializeField]
    AudioSource doorAudio;

    [SerializeField]
    bool doorOpen;

    public void DoorInteract(Vector3 openedFromPos)
    {
        doorAudio.PlayOneShot(doorSounds[0]);
        if(!doorOpen)
        {
            doorOpen = true;
            if (FrontTest(openedFromPos))
            {
                doorAnimator.SetBool("Front", true);
            }
            else
            {
                doorAnimator.SetBool("Front", false);
            }
            doorAnimator.SetTrigger("Open");
        } else
        {
            doorOpen = false;
            doorAnimator.SetTrigger("Close");
        }
        
    }

    public void DoorKick(Vector3 openedFromPos)
    {
        if (!doorOpen)
        {
            doorAudio.PlayOneShot(doorSounds[1]);
            doorOpen = true;
            if (FrontTest(openedFromPos))
            {
                doorAnimator.SetBool("Front", true);
            }
            else
            {
                doorAnimator.SetBool("Front", false);
            }
            doorAnimator.SetTrigger("Kick-open");
            
        } else
        {
            if (FrontTest(openedFromPos) != doorAnimator.GetBool("Front"))
            {
                doorAudio.PlayOneShot(doorSounds[1]);
                doorOpen =false;
                doorAnimator.SetTrigger("Kick-close");
                doorAnimator.SetBool("Front", false);
            }
        }
        
        

    }

    bool FrontTest(Vector3 otherTransform)
    {
        Vector3 fwd = doorCenterPoint.forward;
        Vector3 directionToOther = otherTransform - doorCenterPoint.position;

        // Check if the other object is too close (to avoid normalization issues with very small vectors)
        float distanceSqr = directionToOther.sqrMagnitude;
        if (distanceSqr < 0.0001f) // Adjust this threshold as needed
        {
            // If objects are practically at the same position, we can consider it "not in front"
            // Or you might want to return true here depending on your specific requirements
            return false;
        }

        // Normalize the vector for the angle calculation
        directionToOther.Normalize();

        // Use dot product to check if the object is within 45 degrees of forward direction
        float dotProduct = Vector3.Dot(fwd, directionToOther);

        // An object is in front if dot product > cos(45°)
        return dotProduct > Mathf.Cos(43);
    }

}
