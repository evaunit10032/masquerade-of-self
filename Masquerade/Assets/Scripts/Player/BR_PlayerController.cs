using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BR_PlayerController : MonoBehaviour
{
    Player_Master playerMaster;

    [Header("PlayerController")]
    [SerializeField] public Transform Camera;
    [SerializeField] public ItemChange Items;
    [SerializeField, Range(1, 20)] float walkingSpeed = 3.0f;
    [Range(0.1f, 10)] public float CroughSpeed = 1.0f;
    [SerializeField, Range(2, 40)] float RuningSpeed = 4.0f;
    [SerializeField, Range(0, 30)] float jumpSpeed = 6.0f;
    [SerializeField, Range(0.5f, 10)] float lookSpeed = 2.0f;
    [SerializeField, Range(10, 120)] float lookXLimit = 80.0f;
    [Space(20)]
    [Header("Advance")]
    [SerializeField] float RunningFOV = 65.0f;
    [SerializeField] float AimingFOV = 65.0f;
    [SerializeField] float SpeedToFOV = 4.0f;
    [SerializeField] float CroughHeight = 0.4f;
    [SerializeField] float gravity = 20.0f;
    [SerializeField] float timeToRunning = 2.0f;
    [SerializeField] bool toggleCrouch;
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public bool CanRunning = true;

    [Space(20)]
    [Header("Climbing")]
    [SerializeField] bool CanClimbing = true;
    [SerializeField, Range(1, 25)] float Speed = 2f;
    bool isClimbing = false;

    [Space(20)]
    [Header("HandsHide")]
    [SerializeField] bool CanHideDistanceWall = true;
    [SerializeField, Range(0.1f, 5)] float HideDistance = 1.5f;
    [SerializeField] int LayerMaskInt = 1;

    [Space(20)]
    [Header("Input")]
    [SerializeField] KeyCode CrouchKey = KeyCode.LeftControl;
    [SerializeField] KeyCode leanLeftKey = KeyCode.LeftControl;
    [SerializeField] KeyCode leanRightKey= KeyCode.LeftControl;


    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public Vector3 moveDirection = Vector3.zero;
    bool isCrouch = false;
    bool slideBroken;
    float InstallCrouchHeight;
    float rotationX = 0;
    [HideInInspector] public bool isRunning = false;

    Vector3 InstallCameraMovement;
    float InstallFOV;
    Camera cam;

    [HideInInspector] public bool Moving;
    [HideInInspector] public float vertical;
    [HideInInspector] public float horizontal;
    [HideInInspector] public float Lookvertical;
    [HideInInspector] public float Lookhorizontal;
    [HideInInspector] private bool aiming;
    [HideInInspector] private float currentLeanDegree = 0f;
    float RunningValue;
    float installGravity;
    bool WallDistance;
    [HideInInspector] public float WalkingValue;

    [SerializeField] GameObject bulletHitEffect;
    [SerializeField] Transform camLeanJoint;
    [SerializeField] Camera gunCam;
    [SerializeField, Range(0f, 50f)] float leanDegreeMax;
    [SerializeField] float health;
    [SerializeField] float max_health;
    [SerializeField] AudioSource mainAudioPlayer;
    [SerializeField] AudioSource footStepPlayer;
    [SerializeField] List<AudioClip> footClipList;
    [SerializeField] Animator player_animator;

    [SerializeField] List<AudioClip> move_sound_clips = new List<AudioClip>();



    Vector3 slideStartForwards;

    float dashStamina = 100f;

    float maxDashStamina = 100f;

    bool isSlide;

    float slideFinishTime;

    Vector3 slideDir;

    int jumpCount;

    int maxJumps = 1;

    bool bulletTime;

    float bulletTimeStamina = 100f;

    [SerializeField] float lampOil;

    [SerializeField] KeyCode crouchKey;

    [SerializeField] KeyCode bulletTimeKey;

    [SerializeField] float lampOilMax;

    [SerializeField] GameObject lampHand;

    [SerializeField] GameObject lampHolder;

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

    [SerializeField] List<AudioClip> soundEffects = new List<AudioClip>();

    [SerializeField] List<AudioClip> painGrunts = new List<AudioClip>();

    [SerializeField] Transform kickCamHolder;

    Vector3 install_cam_location;

    bool kicking;

    float kickEndTime = 0;

    [SerializeField] Camera litCamera;

    [Range(0f, 5f)]
    [SerializeField] float slideDuration;

    float armor = 0;

    float max_armor = 100f;

    OneBitCheckpoint respawn_anchor;

    bool dead;

    float timeUntilNextGrunt;

    public List<bool> GetActiveWeapons()
    {
        return usableWeapons;
    }

    public void MakeNotDead()
    {
        dead = false;
        canMove = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    void Start()
    {
        footStepPlayer.clip = move_sound_clips[0];
        health = 100f;
        characterController = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>();
        //Cursor.lockState = CursorLockMode.Confined;
        //Cursor.visible = true;
        InstallCrouchHeight = characterController.height;
        InstallCameraMovement = Camera.localPosition;
        InstallFOV = cam.fieldOfView;
        RunningValue = RuningSpeed;
        installGravity = gravity;
        WalkingValue = walkingSpeed;
        UpdateUI();
        GunHandle.transform.SetParent(currentWeapon.GetHandle(), true);
        GunHandle.transform.localPosition = Vector3.zero;
        MakeNotDead();
    }



    void Update()
    {
        if(playerMaster.Get_P_Health().AmIDead())
            return;

        //Yes I misspelled crouch and it would be too much work to change it now so shut up
        RaycastHit CroughCheck;
        RaycastHit ObjectCheck;

        //apply gravity
        if (!characterController.isGrounded && !isClimbing)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        //get forwards vectors
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        //check and apply movement state
        isRunning = !isCrouch ? CanRunning ? Input.GetKey(KeyCode.LeftShift) : false : false;
        vertical = canMove ? (isRunning ? RunningValue : WalkingValue) * Input.GetAxis("Vertical") : 0;
        horizontal = canMove ? (isRunning ? RunningValue : WalkingValue) * Input.GetAxis("Horizontal") : 0;
        if (isRunning) RunningValue = Mathf.Lerp(RunningValue, RuningSpeed, timeToRunning * Time.deltaTime);
        else RunningValue = WalkingValue;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * vertical) + (right * horizontal);

        //Send movement values to animation script
        playerMaster.Get_P_Anims().SetAnimFloat("VerticalInput", vertical);
        playerMaster.Get_P_Anims().SetAnimFloat("HorizontalInput", horizontal);

        //Jump
        if (characterController.isGrounded && jumpCount != 0)
        {
            jumpCount = 0;
            playerMaster.Get_P_Anims().SetAnimTrigger("Jump-End");
        }
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
        {
            //This code was meant for when the player could double jump but I just set maxJumps to 1 since that works too
            //mainAudioPlayer.PlayOneShot(soundEffects[4]); uh move this to the playerSoundManager script soon 
            playerMaster.Get_P_Anims().SetAnimTrigger("Jump");
            jumpCount++;
            if (isCrouch)
            {
               /// moveDirection.y = jumpSpeed * 4f; Crouch jumping was removed but I'll just keep this commented 
            }
            moveDirection.y = jumpSpeed;
        }
        else
        {
            
            moveDirection.y = movementDirectionY;
        }

        //If running then break aim
        if (isRunning)
            playerMaster.Get_P_Cam().BreakAim();

        if (Input.GetKeyDown(crouchKey) && !isCrouch && isRunning)
        {
            footStepPlayer.clip = move_sound_clips[1];
            footStepPlayer.Play();

            player_animator.SetBool("Sliding", true);
            slideStartForwards = forward;
            slideFinishTime = Time.fixedTime + slideDuration;
            Debug.Log(slideBroken);
            isSlide = true;
        }

        if (toggleCrouch && Input.GetKeyDown(crouchKey))
            isCrouch = !isCrouch;

        if ((Input.GetKey(crouchKey) && !slideBroken && !toggleCrouch) || (isCrouch && !slideBroken && toggleCrouch))
        {
            isCrouch = true;
            float Height = Mathf.Lerp(characterController.height, CroughHeight, 5 * Time.deltaTime);
            characterController.height = Height;
            if (isSlide)
            {
                if (Time.fixedTime < slideFinishTime)
                {
                    if (characterController.isGrounded)
                    {
                        moveDirection = (slideStartForwards * (RunningValue * 1.5f)) + ((right / 3f) * horizontal) + new Vector3(0, moveDirection.y, 0);
                    }
                    else
                    {
                        moveDirection = (slideStartForwards * (RunningValue)) + ((right / 3f) * horizontal) + new Vector3(0, moveDirection.y, 0);
                    }
                    float timeSlideStarted = slideFinishTime - slideDuration;
                    WalkingValue = Mathf.Lerp(CroughSpeed, RuningSpeed * 1.5f, (slideFinishTime - timeSlideStarted) - (Time.fixedTime - timeSlideStarted));
                }
                else
                {
                    Debug.Log("FinishedSlide");
                    slideBroken = true;
                }

            }
            else
            {
                WalkingValue = Mathf.Lerp(WalkingValue, CroughSpeed, 6 * Time.deltaTime);
            }
        }
        else if (!Physics.Raycast(GetComponentInChildren<Camera>().transform.position, transform.TransformDirection(Vector3.up), out CroughCheck, 0.8f, 1))
        {
            if (characterController.height != InstallCrouchHeight)
            {

                if (isCrouch)
                {
                    isCrouch = false;
                }
                if (slideBroken || isSlide)
                {
                    isCrouch = false;
                    isSlide = false;
                    footStepPlayer.clip = move_sound_clips[0];
                    footStepPlayer.Play();
                    player_animator.SetBool("Sliding", false);
                }

                float Height = Mathf.Lerp(characterController.height, InstallCrouchHeight, 6 * Time.deltaTime);
                characterController.height = Height;
                WalkingValue = Mathf.Lerp(WalkingValue, walkingSpeed, 4 * Time.deltaTime);
            }
        }
        if (slideBroken && ((Input.GetKeyUp(crouchKey) && !toggleCrouch) || (toggleCrouch && !isCrouch)))
        {
            slideBroken = false;
        }

        characterController.Move(moveDirection * Time.deltaTime);
        Moving = horizontal < 0 || vertical < 0 || horizontal > 0 || vertical > 0 ? true : false;

        currentWeapon.RunWalkAnim(Moving, isRunning);

        if (WallDistance != Physics.Raycast(GetComponentInChildren<Camera>().transform.position, transform.TransformDirection(Vector3.forward), out ObjectCheck, HideDistance, LayerMaskInt) && CanHideDistanceWall)
        {
            WallDistance = Physics.Raycast(GetComponentInChildren<Camera>().transform.position, transform.TransformDirection(Vector3.forward), out ObjectCheck, HideDistance, LayerMaskInt);
            //Items.ani.SetBool("Hide", WallDistance);
            //Items.DefiniteHide = WallDistance;
        }

        if (((vertical != 0 || horizontal != 0) && jumpCount <= 0) || (isCrouch && jumpCount <= 0))
        {
            footStepPlayer.volume = 0.3f;
            if (isCrouch)
            {
                footStepPlayer.volume = 0.6f;
            }
        }
        else
        {
            footStepPlayer.volume = 0f;
        }

        mainAudioPlayer.pitch = Time.timeScale;
    }

    public WalkRunSlideJump AmIWalkRunSlideJump()
    {
        WalkRunSlideJump outputWRSJ = new WalkRunSlideJump(Moving, isRunning, isSlide, !characterController.isGrounded);
        return outputWRSJ;
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

    public void take_damage(float damage)
    {
        if (Time.fixedTime >= timeUntilNextGrunt)
        {
            GameManager.Instance.PlayerHit();
            mainAudioPlayer.volume = 1;
            int pain_clip = Random.Range(0, painGrunts.Count - 1);
            mainAudioPlayer.PlayOneShot(painGrunts[pain_clip]);
            timeUntilNextGrunt = Time.fixedTime + painGrunts[pain_clip].length; //Time.fixedDeltaTime + Random.Range(0.1f, 0.3f);
            Debug.Log("Grunt " + painGrunts[pain_clip].length);
            //mainAudioPlayer.volume = 0.65f;
        }

        if (armor > 0)
        {
            if (health - (damage * 0.6f) > 0)
            {
                health -= (damage * 0.6f);
                armor -= Mathf.Round(damage - (damage * 0.6f));
                if (armor < 0)
                {
                    armor = 0;
                }
            }
            else if (!dead)
            {
                dead = true;
                health = 0;
                armor = 0;
                Cursor.lockState = CursorLockMode.None;
                UIEvents.SendPlayerDeath();
            }
        }
        else
        {
            if (health - damage > 0)
            {
                health -= damage;
            }
            else if (!dead)
            {
                dead = true;
                health = 0;
                Cursor.lockState = CursorLockMode.None;
                UIEvents.SendPlayerDeath();
            }
        }

        UpdateUI();
    }

    public void set_respawn_anchor(OneBitCheckpoint checkpoint)
    {
        respawn_anchor = checkpoint;
    }

    public void fill_lamp_oil()
    {
        if (lampOil + 50 * Time.fixedDeltaTime < lampOilMax)
        {
            lampOil += 50 * Time.fixedDeltaTime;
        }
        else
        {
            lampOil = lampOilMax;
        }
        if (health + 50 * Time.fixedDeltaTime <= 100)
        {
            health += 50 * Time.fixedDeltaTime;
        }
        else if (health < 100)
        {
            health = 100;
        }

        UpdateUI();
    }

    public void MakeMeDead()
    {
        dead = true;
    }

    public void PickupItem(float input_health, int ammoType, float input_ammo, float input_armor, float input_lamp_oil)
    {

        if (input_ammo != 0)
        {
            mainAudioPlayer.PlayOneShot(soundEffects[0]);
            if (storedBullets[ammoType] + input_ammo <= maxStoredBullets[ammoType])
            {
                storedBullets[ammoType] += (int)input_ammo;
                switch (ammoType)
                {
                    case 1:
                        UIEvents.SendBulletReadout("x" + input_ammo + " Small Caliber Bullets");
                        break;
                    case 2:
                        UIEvents.SendBulletReadout("x" + input_ammo + " Shotgun Shells");
                        break;
                }
            }
            else
            {
                storedBullets[ammoType] = maxStoredBullets[ammoType];
            }

        }
        else if (input_armor != 0)
        {
            mainAudioPlayer.PlayOneShot(soundEffects[1]);
            if (input_armor + armor <= max_armor)
            {
                armor += input_armor;
            }
            else
            {
                armor = max_armor;
            }
        }
        else if (input_lamp_oil != 0)
        {
            mainAudioPlayer.PlayOneShot(soundEffects[3]);
            if (input_lamp_oil + lampOil <= lampOilMax)
            {
                lampOil += input_lamp_oil;
            }
            else
            {
                lampOil = lampOilMax;
            }

        }
        else
        {
            if (input_health + health <= max_health)
            {
                health += input_health;
            }
            else
            {
                health = max_health;
            }
        }

        UpdateUI();
    }

    private void BulletRaycast()
    {
        RaycastHit hit;
        if(Physics.Raycast(cam.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0)), out hit, Mathf.Infinity)) {
            //Debug.Log(hit.transform.position);
            Instantiate(bulletHitEffect, hit.point, hit.transform.rotation);
        }
    }

    public bool FireBullet(int bulletType)
    {
        if (isRunning && !isSlide)
            return false;

        if (currentWeapon.loadedAmmo > 0)
        {
            currentWeapon.loadedAmmo--;
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
            }


            Debug.Log("Reloading");
            currentWeapon.Reload();
            UpdateUI();
            return true;
        } else
        {
            return false;
        }
    }

    private void UpdateUI()
    {
        //  UIEvents.SendUpdateUI((int)health, currentWeapon.loadedAmmo, storedBullets[currentWeapon.ammoType], (int)armor);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Ladder" && CanClimbing)
        {
            CanRunning = false;
            isClimbing = true;
            WalkingValue /= 2;
            Items.Hide(true);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "Ladder" && CanClimbing)
        {
            moveDirection = new Vector3(0, Input.GetAxis("Vertical") * Speed * (-Camera.localRotation.x / 1.7f), 0);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Ladder" && CanClimbing)
        {
            CanRunning = true;
            isClimbing = false;
            WalkingValue *= 2;

            Items.ani.SetBool("Hide", false);
            Items.Hide(false);
        }
    }

    public float GetBulletTimeStamina()
    {
        return bulletTimeStamina / 100f;
    }

    public float GetDashStamina()
    {
        return dashStamina / maxDashStamina;
    }

    public void Die()
    {

        Debug.Log("I'm back");
        characterController.enabled = false;
        transform.position = respawn_anchor.GetRespawnPoint().position;
        transform.rotation = respawn_anchor.GetRespawnPoint().rotation;
        characterController.enabled = true;
        dead = false;
        health = 100f;
        lampOil = lampOilMax;
        Cursor.lockState = CursorLockMode.Locked;
        if (storedBullets[1] < 50)
        {
            storedBullets[1] = 50;
        }
    }

    public void InputTransform (Vector3 inputPos, Quaternion inputRot)
    {
        characterController.enabled = false;
        transform.position = inputPos;
        transform.rotation = inputRot;
        characterController.enabled = true;
    }



    public Vector2 get_lamp_oil()
    {
        return new Vector2(lampOil, lampOilMax);
    }
}

