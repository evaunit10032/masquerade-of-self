using System;
using UnityEngine;

public class UIEvents : MonoBehaviour
{
    public static event Action<int, int, int, int> RecieveUpdateUI;

    public static event Action RecievePlayerDeath;

    public static event Action<OneBitInteractable> RecieveInteractableCall;

    public static event Action<string> RecieveBulletReadout;

    public static event Action<Crosshair_States, float> RecieveCrosshairState;


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

    public static void SendCrosshairState(Crosshair_States input_state, float fire_rate)
    {
        RecieveCrosshairState(input_state, fire_rate);
    }

}
