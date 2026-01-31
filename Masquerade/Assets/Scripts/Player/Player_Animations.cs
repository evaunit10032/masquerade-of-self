using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Animations : MonoBehaviour
{
    [SerializeField] Animator p_animator;
    public void SetAnimBool(string animVarName, bool inputBool)
    {
        //
        p_animator.SetBool(animVarName, inputBool);
    }

    public void SetAnimTrigger(string animVarName)
    {
        p_animator.SetTrigger(animVarName);
    }

    public void SetAnimFloat(string animVarName, float value)
    {
        p_animator.SetFloat(animVarName, value);
    }
}
