using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Player_WeaponMaster : MonoBehaviour
{
    [SerializeField] Player_Master playerMaster;

    [SerializeField] List<int> maxStoredBullets = new List<int>();

    [SerializeField] List<int> storedBullets = new List<int>();

    [SerializeField] List<int> loadedBullets = new List<int>();

    [SerializeField] OneBitGun currentWeapon;

    [SerializeField] List<OneBitGun> weaponList = new List<OneBitGun>();

    [SerializeField] GameObject aimTarget;

    [SerializeField] GameObject GunHandle;

    [SerializeField] GameObject default_aim;

    [SerializeField] AudioClip item_pickup_sfx;

    [SerializeField] List<bool> usableWeapons = new List<bool>();

    [SerializeField] GameObject bulletHitEffect;

    public event Action triggerGun;

    private bool cantFire = false;

    private void Update()
    {
        //Reload and break aim
        if (Input.GetKeyDown(KeyCode.R) && !cantFire)
            if (Reload())
                playerMaster.Get_P_Cam().BreakAim();

        ///              ///
        // Switch Weapons //
        ///              ///
        ///              
        /*
        if ((Input.GetKeyDown(KeyCode.Alpha1) || (currentWeapon == weaponList[3] && Input.mouseScrollDelta.y > 0) || (currentWeapon == weaponList[2] && Input.mouseScrollDelta.y < 0)) && usableWeapons[1])
        {
            if (currentWeapon != weaponList[1])
            {
                GunHandle.transform.SetParent(transform, true);
                currentWeapon.gameObject.SetActive(false);
                weaponList[1].gameObject.SetActive(true);
                currentWeapon = weaponList[1];
                GunHandle.transform.SetParent(currentWeapon.GetHandle(), true);
                GunHandle.transform.position = currentWeapon.GetHandle().position;
                GunHandle.transform.rotation = currentWeapon.GetHandle().rotation;
                UpdateUI();
            }
        }
        else if ((Input.GetKeyDown(KeyCode.Alpha2) || (currentWeapon == weaponList[1] && Input.mouseScrollDelta.y > 0) || (currentWeapon == weaponList[3] && Input.mouseScrollDelta.y < 0)) && usableWeapons[2])
        {
            if (currentWeapon != weaponList[2])
            {
                GunHandle.transform.SetParent(transform, true);
                currentWeapon.gameObject.SetActive(false);
                weaponList[2].gameObject.SetActive(true);
                currentWeapon = weaponList[2];
                GunHandle.transform.SetParent(currentWeapon.GetHandle(), true);
                GunHandle.transform.position = currentWeapon.GetHandle().position;
                GunHandle.transform.rotation = currentWeapon.GetHandle().rotation;
                UpdateUI();
            }
        }
        else if ((Input.GetKeyDown(KeyCode.Alpha3) || (currentWeapon == weaponList[2] && Input.mouseScrollDelta.y > 0) || (currentWeapon == weaponList[1] && Input.mouseScrollDelta.y < 0)) && usableWeapons[3])
        {
            if (currentWeapon != weaponList[3])
            {
                GunHandle.transform.SetParent(transform, true);
                currentWeapon.gameObject.SetActive(false);
                weaponList[3].gameObject.SetActive(true);
                currentWeapon = weaponList[3];
                GunHandle.transform.SetParent(currentWeapon.GetHandle(), true);
                GunHandle.transform.position = currentWeapon.GetHandle().position;
                GunHandle.transform.rotation = currentWeapon.GetHandle().rotation;
                UpdateUI();
            }
        }*/
    }

    public void SetCantFire(bool input)
    {
        cantFire = input;
        currentWeapon.StowWeapon(input);
    }

    public void WalkRunAnims(bool Moving, bool isRunning)
    {
        currentWeapon.RunWalkAnim(Moving, isRunning);
    }

    private void UpdateUI()
    {
        //  UIEvents.SendUpdateUI((int)health, currentWeapon.loadedAmmo, storedBullets[currentWeapon.ammoType], (int)armor);
    }

    private void BulletRaycast()
    {
        if (playerMaster == null)
        {
            Debug.Log("Master is null!");
        }
        Camera cam = playerMaster.Get_P_Cam().GetMainCam();
        if (cam == null)
        {
            Debug.Log("Cam is null!");
        }
        RaycastHit hit;
        if (Physics.Raycast(cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)), out hit, Mathf.Infinity))
        {
            //Decide what to do with object based on tag
            switch(hit.collider.gameObject.tag)
            {
                //Destroy fragile props like mugs or some glass. This also controls explosive barrels.
                case ("DestructibleProp"):
                    hit.collider.attachedRigidbody.AddForceAtPosition(Camera.main.transform.forward * 5f, hit.point, ForceMode.Impulse);
                    hit.collider.gameObject.GetComponent<InteractableObj>().Break_Obj(1);
                    break;
                case ("Prop"):
                    hit.collider.attachedRigidbody.AddForceAtPosition(Camera.main.transform.forward * 100f, hit.point, ForceMode.Impulse);
                    break;
                //If no special tag than it is likely enviroment, so spawn bullet decal and hit effect
                default:
                    Instantiate(bulletHitEffect, hit.point, hit.transform.rotation);
                    break;
            }
            
        }
    }

    public bool FireBullet(int bulletType)
    {
        WalkRunSlideJump wrsj = playerMaster.Get_P_Controller().AmIWalkRunSlideJump();
        if (wrsj.run && !wrsj.slide)
            return false;

        if(cantFire)
            return false;

        if (currentWeapon.loadedAmmo > 0)
        {
            UIEvents.SendCrosshairState(Crosshair_States.Expand, currentWeapon.fireRate);

            if(!currentWeapon.automatic)
            {
                currentWeapon.loadedAmmo--;
            }
            
            UpdateUI();
            BulletRaycast();
            return true;
        }
        else if (storedBullets[bulletType] > 0)
        {
            Reload();
            return false;
        }
        else
        {
            return false;
        }
    }

    private bool Reload()
    {
        int currentWeaponAmmoType = currentWeapon.ammoType;
        if (storedBullets[currentWeaponAmmoType] > 0 && currentWeapon.loadedAmmo != currentWeapon.clipSize)
        {
            int freshBullets = currentWeapon.clipSize - currentWeapon.loadedAmmo;

            currentWeapon.loadedAmmo += freshBullets;

            /*
            if (storedBullets[currentWeaponAmmoType] - freshBullets < 0)
            {
                freshBullets = freshBullets - storedBullets[currentWeaponAmmoType];
                currentWeapon.loadedAmmo = freshBullets;
                storedBullets[currentWeaponAmmoType] = 0;
            }
            else
            {
                currentWeapon.loadedAmmo += freshBullets;
                storedBullets[currentWeaponAmmoType] -= freshBullets;
            }*/


            Debug.Log("Reloading");
            UIEvents.SendCrosshairState(Crosshair_States.Expand, currentWeapon.reloadTime);
            //playerMaster.Get_P_Cam().BreakAim();
            currentWeapon.Reload();
            UpdateUI();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void UnlockWeapon(int weapon)
    {
        usableWeapons[weapon] = true;
        GunHandle.transform.SetParent(transform, true);
        currentWeapon.gameObject.SetActive(false);
        weaponList[weapon].gameObject.SetActive(true);
        currentWeapon = weaponList[weapon];
        GunHandle.transform.SetParent(currentWeapon.GetHandle(), true);
        GunHandle.transform.localPosition = Vector3.zero;
        UpdateUI();
    }


}
