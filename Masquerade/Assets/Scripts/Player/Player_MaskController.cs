using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_MaskController : MonoBehaviour
{
    public List<Player_MaskBase> allMasks = new List<Player_MaskBase>();

    [SerializeField] private Player_MaskBase currentMask;

    [SerializeField] private Player_MaskBase activeMask;

    [SerializeField] private Animator maskAnimator;

    [SerializeField] private GameObject maskArm;

    [SerializeField] private GameObject maskHolder;

    

    int currentMaskIndex;

    bool animating = false;

    // Start is called before the first frame update
    void Start()
    {
        //currentMask = allMasks[0];
        currentMaskIndex = 0;
        currentMask.amActiveMask = true;
        activeMask = currentMask;
        maskArm.transform.localPosition = new Vector3(-0.809000015f, -1.34800005f, 0.337000012f);
    }

    // Update is called once per frame
    void Update()
    {
        if (animating)
            return;

        // Get the value of the "Mouse ScrollWheel" axis
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if(Input.GetMouseButtonDown(2))
        {
            animating = true;
            StartCoroutine(EquipMask());
        } else if (scrollInput > 0)
        {
            Debug.Log("Scrolled Up: " + scrollInput);
            if (currentMaskIndex + 1 < allMasks.Count)
            {
                currentMaskIndex++;
                //   currentMask = allMasks[currentMaskIndex];
            }
            else
            {
                //   currentMask = allMasks[0];
                currentMaskIndex = 0;
            }
            animating = true;
            StartCoroutine(SwitchMask());
            //maskAnimator.SetTrigger("SwitchMask");
        }
        else if (scrollInput < 0)
        {
            Debug.Log("Scrolled Down: " + scrollInput);
            if (currentMaskIndex - 1 >= 0)
            {
                currentMaskIndex--;
                //  currentMask = allMasks[currentMaskIndex];
            }
            else
            {
                currentMaskIndex = allMasks.Count - 1;
                //  currentMask = allMasks[currentMaskIndex];
            }
            animating = true;
            StartCoroutine(SwitchMask());
            //  maskAnimator.SetTrigger("SwitchMask");
        }
    }

    private IEnumerator SwitchMask()
    {
        //maskAnimator.SetTrigger("WearMask");
        maskAnimator.SetTrigger("SwitchMask");
        yield return new WaitForSeconds(0.5f);

        Transform maskTransform = currentMask.transform;
        //GameObject maskParent = currentMask.GetComponentInParent<GameObject>();

        currentMask.GetComponent<MeshRenderer>().enabled = false;
        currentMask = allMasks[currentMaskIndex];
        currentMask.GetComponent<MeshRenderer>().enabled = true;


        yield return new WaitForSeconds(0.5f);

        animating = false;
    }


    private IEnumerator EquipMask()
    {
        maskArm.transform.localPosition = new Vector3(-0.486999989f, -1.029f, 0.05f);
        maskAnimator.SetTrigger("WearMask");
        yield return new WaitForSeconds(0.334f);
        maskArm.transform.localPosition = new Vector3(-0.809000015f, -1.34800005f, 0.337000012f);
        activeMask.amActiveMask = false;
        activeMask = currentMask;
        activeMask.amActiveMask = true;
        UIEvents.SendMaskIcon(activeMask.GetMaskIcon());
        animating = false;
    }
}