using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_MaskBase : MonoBehaviour
{
    [SerializeField] Sprite maskIcon;

    [SerializeField] GameObject maskModel;

    [SerializeField] float Ability1MaxCooldown;

    [SerializeField] float Ability1MaxChargeTime;

    [SerializeField] float Ability2MaxCooldown;

    [SerializeField] float Ability2MaxChargeTime;

    bool ability1Active = false;

    bool ability2Active = false;

    float ability1CooldownTime;

    float ability1ChargeTime;

    float ability2CooldownTime;

    float ability2ChargeTime;

    public bool amActiveMask;


    





    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1) && ability1CooldownTime >= Ability1MaxCooldown && !ability1Active && amActiveMask)
        {
            ability1Active = true;
            ability1ChargeTime = Ability1MaxChargeTime;
            ability1CooldownTime = 0;
            Ability1();

        } else if (Input.GetKeyDown(KeyCode.Alpha2) && ability2CooldownTime >= Ability2MaxCooldown && !ability2Active && amActiveMask) {
            ability2Active = true;
            ability2ChargeTime = Ability1MaxChargeTime;
            ability2CooldownTime = 0;
            Ability2();

        }

        //Is ability 1 active?
        if(ability1Active)
        {
            //Reduce the ability active charge
            ability1ChargeTime -= Time.deltaTime;
            if (ability1ChargeTime <= 0)
            {
                ability1Active = false;
                ability1CooldownTime = 0;
            }
        } else
        {
            //Incriment the cooldown countdown
            ability1CooldownTime += Time.deltaTime;
        }
        //Is ability 2 active?
        if (ability2Active)
        {
            //Reduce the ability active charge
            ability2ChargeTime -= Time.deltaTime;
            if (ability2ChargeTime <= 0)
            {
                ability2Active = false;
                ability2CooldownTime = 0;
            }
        } else
        {
            //Incriment the cooldown countdown
            ability2CooldownTime += Time.deltaTime;
        }


        if(amActiveMask)
        {
            UIEvents.SendAbilityTimerInfo(1, ability1Active, ability1ChargeTime, Ability1MaxChargeTime, ability1CooldownTime, Ability1MaxCooldown);
            UIEvents.SendAbilityTimerInfo(2, ability2Active, ability2ChargeTime, Ability2MaxChargeTime, ability2CooldownTime, Ability2MaxCooldown);
        }
        
    }


    public virtual void Ability1()
    {

    }


    public virtual void Ability2()
    {

    }

    public Sprite GetMaskIcon() { return maskIcon; }

}


public enum Abilities{
    PowerShot,
    RapidShot
}
