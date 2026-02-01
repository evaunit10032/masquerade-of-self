using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_MaskGun : Player_MaskBase
{

    public override void Ability1()
    {
        BasicEvents.SendActivateAbility(Abilities.PowerShot);
    }


    public override void Ability2()
    {
        BasicEvents.SendActivateAbility(Abilities.RapidShot);
    }

}
