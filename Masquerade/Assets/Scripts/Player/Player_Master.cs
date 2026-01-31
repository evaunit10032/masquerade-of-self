using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Master : MonoBehaviour
{
    public static Player_Master instance;

    [SerializeField] Player_Controller playerController;
    [SerializeField] Player_WeaponMaster weaponMaster;
    [SerializeField] Player_Interaction interaction;
    [SerializeField] Player_Camera playerCamera;
    [SerializeField] Player_Health p_health;
    [SerializeField] Player_Animations p_anims;

    public bool canMove;
    [SerializeField] AudioSource mainAudioPlayer; 

    //[Header("Advance")]

    bool isDead;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        //mainAudioPlayer.pitch = Time.timeScale;
    }

    public Player_Camera Get_P_Cam()
    {
        return playerCamera;
    }

    public Player_Controller Get_P_Controller()
    {
        return playerController;
    }

    public Player_Health Get_P_Health()
    {
        return p_health;
    }

    public Player_Animations Get_P_Anims()
    {
        return p_anims;
    }

    public Player_WeaponMaster Get_P_WeaponMaster()
    {
        return weaponMaster;
    }



}
