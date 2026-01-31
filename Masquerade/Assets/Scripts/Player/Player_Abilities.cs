using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Abilities : MonoBehaviour
{
    [SerializeField] KeyCode bulletTimeKey;

    bool bulletTime;
    float bulletTimeStamina = 100f;


    void Update()
    {
        //Activate bullet time
        if (Input.GetKeyDown(bulletTimeKey))
        {
            if (!bulletTime)
            {
                bulletTime = true;
                Time.timeScale = 0.40f;
            }
            else
            {
                Time.timeScale = 1f;
                bulletTime = false;
            }
        }

        if (bulletTime)
        {
            if (bulletTimeStamina > 0f)
            {
                bulletTimeStamina -= 25f * Time.deltaTime;
            }
            else
            {
                Time.timeScale = 1f;
                bulletTime = false;
            }
        }
        else
        {
            if (bulletTimeStamina < 100f)
            {
                bulletTimeStamina += 10f * Time.deltaTime;
            }

        }
    }
}
