using System.Collections.Generic;
using UnityEngine;

public class OneBitGun : MonoBehaviour
{
    //public BR_PlayerController gunOwner;

    public Animator gunAnimController;

    public GameObject bulletPrefab;

    public GameObject muzzleFlashPrefab;

    public Transform barrelTransform;

    public AudioSource gunAudio;

    public List<AudioClip> gunShotFX;

    public AudioClip reloadFX;

    public float fireRate;

    public int pelletCount;

    public float bulletSpread;

    public bool automatic;

    public Player_WeaponMaster weaponMaster;

    public bool abilityActive = false;

    public Abilities abilityType;



    public GameObject handleLocation;

    public int ammoType;

    public int clipSize;

    public int loadedAmmo;

    public float reloadTime;

    public float timeUntilInspect;

    public bool hasWalkAnim;

    public float shotCooldownEnd = 0f;

    public int autoShotsFired = 0;

    public UnityEngine.Quaternion startRot;

    private void Start()
    {
        startRot = transform.localRotation;
    }

    public Transform GetHandle()
    {
        return handleLocation.transform;
    }

    public void StowWeapon(bool input)
    {
        gunAnimController.SetBool("Lower", input);
    }

    public virtual void RunWalkAnim(bool walk, bool run)
    {
        if (hasWalkAnim)
        {
            if (run && walk)
            {
                gunAnimController.SetBool("Running", true);     
            } else
            {
                gunAnimController.SetBool("Running", false);
                
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        

        if (gameObject.activeSelf && Cursor.lockState == CursorLockMode.Locked)
        {
            //gunAudio.pitch = Time.timeScale;
            if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && automatic))
            {
                if (Time.fixedTime > shotCooldownEnd && weaponMaster.FireBullet(ammoType))
                {


                    gunAudio.PlayOneShot(gunShotFX[Random.Range(0, gunShotFX.Count)]);


                    gunAnimController.SetTrigger("Fire");
                    for (int i = 0; i < pelletCount; i++)
                    {
                        float shotgunSpread = Random.Range(-bulletSpread, bulletSpread);
                        Instantiate(muzzleFlashPrefab, barrelTransform).transform.parent = barrelTransform;
                        Instantiate(bulletPrefab, barrelTransform.position, barrelTransform.rotation);
                    }

                    shotCooldownEnd = Time.fixedTime + fireRate;
                }



            }


        }

        if (Input.GetMouseButtonUp(0) && automatic)
        {
            //gunAudio.Stop();
            autoShotsFired = 0;
        }

    }

    public virtual void Shoot()
    {
        if (abilityActive)
        {
            if (abilityType == Abilities.PowerShot)
            {

            }
        }
    }

    public virtual void Reload()
    {
        if (gameObject.activeSelf)
        {
            //gunAudio.pitch = Time.timeScale;
            gunAudio.PlayOneShot(reloadFX);
            gunAnimController.SetTrigger("Reload");
            shotCooldownEnd = Time.fixedTime + reloadTime;

            timeUntilInspect = Time.fixedTime + Random.Range(10f, 20f);
        }

    }
}
