using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Health : MonoBehaviour
{
    bool isDead = false;

    public bool AmIDead()
    {
        return isDead;
    }
}
