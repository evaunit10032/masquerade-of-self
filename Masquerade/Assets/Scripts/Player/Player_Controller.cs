using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class Player_Controller : MonoBehaviour
{
    [Header("Player Master")]
    [SerializeField] Player_Master playerMaster;

    [Space(20)]
    [Header("Input")]
    [SerializeField] KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] KeyCode leanLeftKey = KeyCode.LeftControl;
    [SerializeField] KeyCode leanRightKey = KeyCode.LeftControl;

    [Space(20)]
    [Header("PlayerController")]
    [SerializeField, Range(1, 20)] float walkingSpeed = 3.0f;
    [Range(0.1f, 10)] public float CroughSpeed = 1.0f;
    [SerializeField, Range(2, 40)] float RuningSpeed = 4.0f;
    [SerializeField, Range(0, 30)] float jumpSpeed = 6.0f;

    [Space(20)]
    [Header("Advance")]
    
    [SerializeField] float gravity = 20.0f;
    [SerializeField] float timeToRunning = 2.0f;

    [Space(20)]
    [Header("Crouching")]
    [SerializeField] float CroughHeight = 0.4f;
    [SerializeField] bool toggleCrouch; //Determines if the crouch button should be toggled or held
    [HideInInspector] bool isCrouch = false;
    [HideInInspector] float InstallCrouchHeight;

    [Space(20)]
    [Header("Sliding")]
    [Range(0f, 5f)]
    [SerializeField] float slideDuration;
    [HideInInspector] bool slideBroken;
    Vector3 slideStartForwards;
    float dashStamina = 100f;
    float maxDashStamina = 100f;
    bool isSlide;
    float slideFinishTime;
    Vector3 slideDir;
    
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
    [Header("SFX")]
    [SerializeField] AudioSource footStepPlayer;
    [SerializeField] List<AudioClip> footClipList;
    [SerializeField] List<AudioClip> move_sound_clips = new List<AudioClip>();

    [HideInInspector] public CharacterController characterController;
    [HideInInspector] public Vector3 moveDirection = Vector3.zero;

    [Space(20)]
    [Header("Jumping")]
    [SerializeField] int maxJumps = 1;
    int jumpCount;

    [HideInInspector] public bool isRunning = false;
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public bool CanRunning = true;
    [HideInInspector] float RunningValue;
    [HideInInspector] float installGravity;
    [HideInInspector] bool WallDistance;
    [HideInInspector] public float WalkingValue;
    [HideInInspector] public bool Moving;
    [HideInInspector] public float vertical;
    [HideInInspector] public float horizontal;


    // Start is called before the first frame update
    void Start()
    {
        footStepPlayer.clip = move_sound_clips[0];
        characterController = GetComponent<CharacterController>();
        //Cursor.lockState = CursorLockMode.Confined;
        //Cursor.visible = true;
        InstallCrouchHeight = characterController.height;
        //InstallCameraMovement = Camera.localPosition;
        RunningValue = RuningSpeed;
        installGravity = gravity;
        WalkingValue = walkingSpeed;
    }

    void Update()
    {
       // if (playerMaster.Get_P_Health().AmIDead())
         //   return;

        RaycastHit CroughCheck;
        RaycastHit ObjectCheck;

        if (!characterController.isGrounded && !isClimbing)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        isRunning = !isCrouch ? CanRunning ? Input.GetKey(KeyCode.LeftShift) : false : false;
        vertical = canMove ? (isRunning ? RunningValue : WalkingValue) * Input.GetAxis("Vertical") : 0;
        horizontal = canMove ? (isRunning ? RunningValue : WalkingValue) * Input.GetAxis("Horizontal") : 0;
        if (isRunning) RunningValue = Mathf.Lerp(RunningValue, RuningSpeed, timeToRunning * Time.deltaTime);
        else RunningValue = WalkingValue;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * vertical) + (right * horizontal);

        //Send movement values to animation script
        //playerMaster.Get_P_Anims().SetAnimFloat("VerticalInput", vertical);
        //playerMaster.Get_P_Anims().SetAnimFloat("HorizontalInput", horizontal);

        //Jump
        if (characterController.isGrounded && jumpCount != 0)
        {
            jumpCount = 0;
            playerMaster.Get_P_Anims().SetAnimTrigger("Jump-End");
        }
        if (Input.GetKeyDown(KeyCode.Space) && jumpCount < maxJumps)
        {
            //mainAudioPlayer.PlayOneShot(soundEffects[4]);
            playerMaster.Get_P_Anims().SetAnimTrigger("Jump");
            jumpCount++;
            if (isCrouch)
            {
                moveDirection.y = jumpSpeed * 4f;
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

            playerMaster.Get_P_Anims().SetAnimBool("Sliding", true);
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
                    playerMaster.Get_P_Anims().SetAnimBool("Sliding", false);
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

        playerMaster.Get_P_WeaponMaster().WalkRunAnims(Moving, isRunning);

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

        
    }

    public WalkRunSlideJump AmIWalkRunSlideJump()
    {
        //  Debug.Log(Moving + "+" + isRunning + "+" + isSlide);
        WalkRunSlideJump outputWRSJ = new WalkRunSlideJump(Moving, isRunning, isSlide, !characterController.isGrounded);
       
        return outputWRSJ;
    }
}

public class WalkRunSlideJump
{
    public bool walk;
    public bool run;
    public bool slide;
    public bool jump;
    public WalkRunSlideJump(bool inputWalk, bool inputRun, bool inputSlide, bool inputJump)
    {
        walk = inputWalk;
        run = inputRun;
        slide = inputSlide;
        jump = inputJump;
    }
}