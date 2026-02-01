using System;
using UnityEngine;

public class BasicEvents : MonoBehaviour
{
    public static event Action<Abilities> RecieveActivateAbility;


    public static void SendActivateAbility(Abilities ability)
    {
        RecieveActivateAbility(ability);
    }
}
