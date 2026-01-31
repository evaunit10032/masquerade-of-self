using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player_Camera : MonoBehaviour
{
    [Header("Player Master")]
    [SerializeField] Player_Master p_master;

    [Space(20)]
    [Header("Cameras")]
    [SerializeField] Camera main_Camera;
    [SerializeField] Camera gun_Camera;

    [Space(20)]
    [Header("Transforms")]
    [SerializeField] Transform kickCamHolder;
    [SerializeField] Transform camLeanJoint;

    [Space(20)]
    [Header("KeyCodes")]
    [SerializeField] KeyCode leanLeftKey = KeyCode.LeftControl;
    [SerializeField] KeyCode leanRightKey = KeyCode.LeftControl;

    [Space(20)]
    [Header("Camera Aim")]
    [SerializeField, Range(0.5f, 10)] float lookSpeed = 2.0f;
    [SerializeField, Range(10, 120)] float lookXLimit = 80.0f;
    [HideInInspector] float rotationX = 0;
    [HideInInspector] public float vertical;
    [HideInInspector] public float horizontal;
    [HideInInspector] public float Lookvertical;
    [HideInInspector] public float Lookhorizontal;

    [Space(20)]
    [Header("FOV")]
    [SerializeField] float RunningFOV = 65.0f;
    [SerializeField] float AimingFOV = 65.0f;
    [SerializeField] float SpeedToFOV = 4.0f;
    [HideInInspector] float InstallFOV;
    [HideInInspector] private bool aiming;


    [Space(20)]
    [Header("Leaning")]
    [SerializeField, Range(0f, 50f)] float leanDegreeMax;
    [HideInInspector] private float currentLeanDegree = 0f;


    private void Start()
    {
        lookXLimit = 80f;
        InstallFOV = main_Camera.fieldOfView;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Camera bobs with animations
        camLeanJoint.transform.position = kickCamHolder.position;

        //Switch aiming
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (!aiming)
                UIEvents.SendCrosshairState(Crosshair_States.Shrink, 1);
            else
                UIEvents.SendCrosshairState(Crosshair_States.Shrink, 0);

            aiming = !aiming;
            
        }
           

        //Aim the camera
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Lookvertical = -Input.GetAxis("Mouse Y");
            Lookhorizontal = Input.GetAxis("Mouse X");

            rotationX += Lookvertical * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            main_Camera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Lookhorizontal * lookSpeed, 0);

            WalkRunSlideJump wrsj = p_master.Get_P_Controller().AmIWalkRunSlideJump();
            if (wrsj.walk && wrsj.run) main_Camera.fieldOfView = Mathf.Lerp(main_Camera.fieldOfView, RunningFOV, SpeedToFOV * Time.deltaTime);
            else if (aiming) main_Camera.fieldOfView = Mathf.Lerp(main_Camera.fieldOfView, AimingFOV, (SpeedToFOV * 2f) * Time.deltaTime);
            else main_Camera.fieldOfView = Mathf.Lerp(main_Camera.fieldOfView, InstallFOV, SpeedToFOV * Time.deltaTime);

            gun_Camera.fieldOfView = main_Camera.fieldOfView;
        }

        LeanUpdate();
    }

    //Get main camera
    public Camera GetMainCam()
    {
        return main_Camera;
    }

    //Stop aiming
    public void BreakAim()
    {
        aiming = false;
        UIEvents.SendCrosshairState(Crosshair_States.Shrink, 0);
    }

    //////////////////////////////////////////////
    /////     Smooth Transition to leaning   /////
    //////////////////////////////////////////////
    private void LeanUpdate()
    {
        float newLeanDegree = currentLeanDegree;
        if (Input.GetKey(leanLeftKey))
        {
            Debug.Log("LeanLeft");
            if (currentLeanDegree > -leanDegreeMax)
            {
                newLeanDegree = currentLeanDegree - ((leanDegreeMax * 2.5f) * Time.fixedDeltaTime);
                if (newLeanDegree < -leanDegreeMax)
                {
                    currentLeanDegree = -leanDegreeMax;
                }
                else
                {
                    currentLeanDegree = newLeanDegree;
                }
                camLeanJoint.localRotation = Quaternion.Euler(Vector3.forward * currentLeanDegree);
            }
        }
        else if (Input.GetKey(leanRightKey))
        {
            if (currentLeanDegree < leanDegreeMax)
            {
                newLeanDegree = currentLeanDegree + ((leanDegreeMax * 2.5f) * Time.fixedDeltaTime);
                if (newLeanDegree > leanDegreeMax)
                {
                    currentLeanDegree = leanDegreeMax;
                }
                else
                {
                    currentLeanDegree = newLeanDegree;
                }
                camLeanJoint.localRotation = Quaternion.Euler(Vector3.forward * currentLeanDegree);
            }
        }
        else if (currentLeanDegree != 0)
        {
            if (currentLeanDegree < 0)
            {
                newLeanDegree = currentLeanDegree + ((leanDegreeMax * 2.5f) * Time.fixedDeltaTime);
                if (newLeanDegree > 0)
                {
                    currentLeanDegree = 0;
                }
                else
                {
                    currentLeanDegree = newLeanDegree;
                }
            }
            else if (currentLeanDegree > 0)
            {
                newLeanDegree = currentLeanDegree - ((leanDegreeMax * 2.5f) * Time.fixedDeltaTime);
                if (newLeanDegree < 0)
                {
                    currentLeanDegree = 0;
                }
                else
                {
                    currentLeanDegree = newLeanDegree;
                }
            }
            camLeanJoint.localRotation = Quaternion.Euler(Vector3.forward * currentLeanDegree);
        }
    }

}
