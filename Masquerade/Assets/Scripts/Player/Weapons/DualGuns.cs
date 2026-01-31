using UnityEngine;

public class DualGuns : OneBitGun
{
    [SerializeField] Animator gunAnimController2;


    [SerializeField] Transform barrelTransform2;

    public float shotCooldownEnd2 = 0f;



    bool rightGunFiredLast = false;


    // Update is called once per frame
    void Update()
    {
        if (gameObject.activeSelf && Cursor.lockState == CursorLockMode.Locked)
        {
            gunAudio.pitch = Time.timeScale;
            if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && automatic))
            {
                if (Time.fixedTime > shotCooldownEnd && weaponMaster.FireBullet(ammoType))
                {


                    //gunAudio.PlayOneShot(gunShotFX[Random.Range(0, gunShotFX.Count)]);

                    if (!rightGunFiredLast)
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
                    }

                    shotCooldownEnd = Time.fixedTime + fireRate;
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
                gunAnimController2.SetBool("Running", true);
            }
            else
            {
                gunAnimController.SetBool("Running", false);
                gunAnimController2.SetBool("Running", false);
                
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
}
