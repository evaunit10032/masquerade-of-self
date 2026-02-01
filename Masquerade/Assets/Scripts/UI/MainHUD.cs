using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainHUD : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI ammoCounter;

    [SerializeField] Image maskIcon;

    [Header("Ability 1")]
    [SerializeField] Slider ability1Timer;
    [SerializeField] Image ability1TimerFill;
    [SerializeField] Image ability1TimerBack;

    [Header("Ability 2")]
    [SerializeField] Slider ability2Timer;
    [SerializeField] Image ability2TimerFill;
    [SerializeField] Image ability2TimerBack;

    [Header("Colors")]
    [SerializeField] Color activeColor;
    [SerializeField] Color activeBackColor;
    [SerializeField] Color chargingColor;
    [SerializeField] Color chargingBackColor;

    // Start is called before the first frame update
    void Start()
    {
        UIEvents.RecieveAmmoReadout += UpdateAmmoReadout;
        UIEvents.RecieveMaskIcon += UpdateMaskIcon;
        UIEvents.RecieveAbilityTimerInfo += SetAbilityTimerInfo;
    }

    private void OnDestroy()
    {
        UIEvents.RecieveAmmoReadout -= UpdateAmmoReadout;
        UIEvents.RecieveMaskIcon -= UpdateMaskIcon;
        UIEvents.RecieveAbilityTimerInfo -= SetAbilityTimerInfo;
    }


    private void UpdateMaskIcon(Sprite inputMaskIcon)
    {
        maskIcon.sprite = inputMaskIcon;
    }

    private void UpdateAmmoReadout(int newAmmoReadout)
    {
        ammoCounter.SetText("" + newAmmoReadout);
    }

    private void SetAbilityTimerInfo(int abilityNum, bool active, float remainingTime, float maxTime, float cooldown, float maxCooldown)
    {
        if (abilityNum == 1) 
        {
            if (active )
            {
                ability1TimerFill.color = activeColor;
                ability1TimerBack.color = activeBackColor;

                ability1Timer.value = remainingTime;
                ability1Timer.maxValue = maxTime;
            }
            else
            {
                if(cooldown >= maxCooldown)
                {
                    cooldown = maxCooldown;
                    ability1TimerFill.color = activeColor;
                    ability1TimerBack.color = activeBackColor;
                } else
                {
                    ability1TimerFill.color = chargingColor;
                    ability1TimerBack.color = chargingBackColor;
                }

                    

               /* if(cooldown > maxCooldown)
                    cooldown = maxCooldown;*/

                ability1Timer.value = cooldown;
                ability1Timer.maxValue = maxCooldown;
            }

            
        } else
        {
            if (active)
            {
                ability2TimerFill.color = activeColor;
                ability2TimerBack.color = activeBackColor;

                ability2Timer.value = remainingTime;
                ability2Timer.maxValue = maxTime;
            }
            else
            {
                if (cooldown >= maxCooldown)
                {
                    cooldown = maxCooldown;
                    ability2TimerFill.color = activeColor;
                    ability2TimerBack.color = activeBackColor;
                }
                else
                {
                    ability2TimerFill.color = chargingColor;
                    ability2TimerBack.color = chargingBackColor;
                }

                /*if (cooldown > maxCooldown)
                    cooldown = maxCooldown;*/

                ability2Timer.value = cooldown;
                ability2Timer.maxValue = maxCooldown;
            }
        }
        
    }

}
