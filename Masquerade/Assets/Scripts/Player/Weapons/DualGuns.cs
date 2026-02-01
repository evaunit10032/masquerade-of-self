using System.Collections;
using UnityEngine;

public class DualGuns : OneBitGun
{
    [SerializeField] Animator gunAnimController2;


    [SerializeField] Transform barrelTransform2;

    public float shotCooldownEnd2 = 0f;

    [SerializeField] float powerShotCooldown;

    float storedFireRate;

    float timeToRapidShotEnd;

    [SerializeField] float rapidShotLength = 5f;



    bool rightGunFiredLast = false;


    private void Start()
    {
        storedFireRate = fireRate;

        BasicEvents.RecieveActivateAbility += ActivateAbility;

        //ActivateAbility(Abilities.RapidShot);
    }


    // Update is called once per frame
    void Update()
    {


        if (gameObject.activeSelf && Cursor.lockState == CursorLockMode.Locked)
        {
            gunAudio.pitch = Time.timeScale;

            if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && automatic))
            {
                if (Time.fixedTime >= shotCooldownEnd && weaponMaster.FireBullet(ammoType))
                {
                    if (abilityActive)
                    {
                        if (abilityType == Abilities.PowerShot)
                        {
                            gunAnimController.SetTrigger("Fire");
                            gunAudio.PlayOneShot(gunShotFX[0]);
                            for (int i = 0; i < pelletCount; i++)
                            {
                                float shotgunSpread = Random.Range(-bulletSpread, bulletSpread);
                                Instantiate(muzzleFlashPrefab, barrelTransform);
                                Instantiate(bulletPrefab, barrelTransform.position, barrelTransform.rotation);
                            }

                            abilityActive = false;
                            shotCooldownEnd = Time.fixedTime + fireRate + 1f;

                        } else if (abilityType == Abilities.RapidShot)
                        {
                            gunAnimController.SetTrigger("Fire");
                            gunAudio.PlayOneShot(gunShotFX[0]);
                            for (int i = 0; i < pelletCount; i++)
                            {
                                float shotgunSpread = Random.Range(-bulletSpread, bulletSpread);
                                Instantiate(muzzleFlashPrefab, barrelTransform);
                                Instantiate(bulletPrefab, barrelTransform.position, barrelTransform.rotation);

                            }
                            shotCooldownEnd = Time.fixedTime + fireRate;
                        }
                    } else
                    {
                        gunAnimController.SetTrigger("Fire");
                        gunAudio.PlayOneShot(gunShotFX[0]);
                        for (int i = 0; i < pelletCount; i++)
                        {
                           // float shotgunSpread = Random.Range(-bulletSpread, bulletSpread);
                            Instantiate(muzzleFlashPrefab, barrelTransform);
                            Instantiate(bulletPrefab, barrelTransform.position, barrelTransform.rotation);
                            
                        }
                        shotCooldownEnd = Time.fixedTime + fireRate;
                    }

                        //gunAudio.PlayOneShot(gunShotFX[Random.Range(0, gunShotFX.Count)]);
                        
                    /*if (!rightGunFiredLast)
                    {
                        rightGunFiredLast = true;
                        gunAnimController.SetTrigger("Fire");
                        gunAudio.PlayOneShot(gunShotFX[0]);
                        for (int i = 0; i < pelletCount; i++)
                        {
                            float shotgunSpread = Random.Range(-bulletSpread, bulletSpread);
                            Instantiate(muzzleFlashPrefab, barrelTransform);
                            Instantiate(bulletPrefab, barrelTransform.position, barrelTransform.rotation);
                        }
                    }
                    else
                    {
                        rightGunFiredLast = false;
                        gunAnimController2.SetTrigger("Fire");
                        gunAudio.PlayOneShot(gunShotFX[1]);
                        for (int i = 0; i < pelletCount; i++)
                        {
                            float shotgunSpread = Random.Range(-bulletSpread, bulletSpread);
                            Instantiate(muzzleFlashPrefab, barrelTransform2);
                            Instantiate(bulletPrefab, barrelTransform2.position, barrelTransform2.rotation);
                        }
                    }*/

                    //shotCooldownEnd = Time.fixedTime + fireRate;
                }







            }


        }

        if (Input.GetMouseButtonUp(0) && automatic)
        {
            gunAudio.Stop();
            autoShotsFired = 0;
        }

    }

    public override void RunWalkAnim(bool walk, bool run)
    {
        if (hasWalkAnim)
        {
            if (run && walk)
            {
                gunAnimController.SetBool("Running", true);
               // gunAnimController2.SetBool("Running", true);
            }
            else
            {
                gunAnimController.SetBool("Running", false);
               // gunAnimController2.SetBool("Running", false);
                
            }
        }
    }



    public override void Reload()
    {
        if (gameObject.activeSelf)
        {
            gunAudio.pitch = Time.timeScale;
            gunAudio.PlayOneShot(reloadFX);
            gunAnimController2.SetTrigger("Reload");
            gunAnimController.SetTrigger("Reload");

            shotCooldownEnd = Time.fixedTime + reloadTime;

            timeUntilInspect = Time.fixedTime + Random.Range(10f, 20f);
        }

    }


    public void ActivateAbility(Abilities inputAbility)
    {
        switch (inputAbility)
        {
            case Abilities.RapidShot:
                StartCoroutine(RapidFireDuration());
                break;
            case Abilities.PowerShot:
                StartCoroutine(PowerShotDuration()); 
                break;
        }
    }


    private IEnumerator RapidFireDuration()
    {
        abilityActive = true;
        abilityType = Abilities.RapidShot;
        automatic = true;
        fireRate = 0.08f;
        gunAnimController.speed = 2.5f;

        yield return new WaitForSeconds(5);

        abilityActive = false;
        abilityType = Abilities.RapidShot;
        automatic = false;
        fireRate = 0.2f;
        gunAnimController.speed = 1f;
    }


    private IEnumerator PowerShotDuration()
    {
        abilityActive = true;
        abilityType = Abilities.PowerShot;
        automatic = false;
        fireRate = 0.6f;
        gunAnimController.speed = 0.7f;

        yield return new WaitForSeconds(5);

        abilityActive = false;
        abilityType = Abilities.RapidShot;
        automatic = false;
        fireRate = 0.2f;
        gunAnimController.speed = 1f;
    }
}
