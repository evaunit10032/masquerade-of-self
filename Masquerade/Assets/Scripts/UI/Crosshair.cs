using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [SerializeField] Animator crosshair_animator;

    float shrinkSpeed = 1f;

    float expandSpeed = 1f;

    private void Expand(float fire_rate)
    {
        crosshair_animator.SetTrigger("Expand");
        //0.15
        

        if (crosshair_animator.GetBool("Shrink"))
        {
            crosshair_animator.SetFloat("FiringSpeed", (fire_rate / shrinkSpeed));
            //fire_rate = (fire_rate / shrinkSpeed);
        }
        else if (!crosshair_animator.GetBool("Shrink"))
        {
            Debug.Log(shrinkSpeed);
    
            crosshair_animator.SetFloat("FiringSpeed", (expandSpeed / fire_rate ));
        }
        //crosshair_animator.SetFloat("FiringSpeed", fire_rate);
    }

    private void Shrink(float aim_state)
    {
        if (aim_state == 0f)
        {
            crosshair_animator.SetBool("Shrink", false);
        } else if (aim_state == 1f)
        {
            crosshair_animator.SetBool("Shrink", true);
        }
    }

    private void StateManager(Crosshair_States input_state, float fire_rate)
    {
        switch (input_state)
        {
            case(Crosshair_States.Expand):
                Expand(fire_rate);
                break;
            case(Crosshair_States.Shrink):
                Shrink(fire_rate); 
                break;
        }
    }

    private void FindSpeeds()
    {
        foreach (AnimationClip clip in crosshair_animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.Contains("Expand"))
            {
                expandSpeed = clip.length;
            } else if (clip.name.Contains("Shrink") && !clip.name.Contains("Idle"))
            {
                shrinkSpeed = clip.length;
            }
        }
    }

    private void Awake()
    {
        UIEvents.RecieveCrosshairState += StateManager;
        FindSpeeds();
    }

    private void OnDestroy()
    {
        
    }
}

public enum Crosshair_States
{
   Expand,
   Shrink
}