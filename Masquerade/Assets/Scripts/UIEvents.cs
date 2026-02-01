using System;
using Unity.VisualScripting;
using UnityEngine;

public class UIEvents : MonoBehaviour
{
    public static event Action<int, int, int, int> RecieveUpdateUI;

    public static event Action RecievePlayerDeath;

    public static event Action<OneBitInteractable> RecieveInteractableCall;

    public static event Action<string> RecieveBulletReadout;

    public static event Action<int> RecieveAmmoReadout;

    public static event Action<Crosshair_States, float> RecieveCrosshairState;

    public static event Action<Sprite> RecieveMaskIcon;

    public static event System.Action<int, bool, float, float, float, float> RecieveAbilityTimerInfo;


    public static void SendUpdateUI(int health, int loadedAmmo, int storedAmmo, int armor)
    {
        RecieveUpdateUI(health, loadedAmmo, storedAmmo, armor);
    }

    public static void SendPlayerDeath()
    {
        RecievePlayerDeath();
    }

    public static void SendInteractableCall(OneBitInteractable interactable)
    {
        RecieveInteractableCall(interactable);
    }

    public static void SendBulletReadout(string bulletReadout)
    {
        RecieveBulletReadout(bulletReadout);
    }

    public static void SendAmmoReadout(int ammoReadout) 
    {
        RecieveAmmoReadout(ammoReadout);
    }

    public static void SendCrosshairState(Crosshair_States input_state, float fire_rate)
    {
        RecieveCrosshairState(input_state, fire_rate);
    }

    public static void SendMaskIcon(Sprite maskIcon)
    {
        RecieveMaskIcon(maskIcon);
    }

    public static void SendAbilityTimerInfo(int abilityNum, bool active, float remainingTime, float maxTime, float cooldown, float maxCooldown)
    {
        RecieveAbilityTimerInfo(abilityNum, active, remainingTime, maxTime, cooldown, maxCooldown);

    }

}
