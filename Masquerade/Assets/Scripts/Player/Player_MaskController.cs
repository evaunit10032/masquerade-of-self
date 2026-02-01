using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_MaskController : MonoBehaviour
{
    public List<Player_MaskBase> allMasks = new List<Player_MaskBase>();

    [SerializeField] private Player_MaskBase currentMask;

    int currentMaskIndex;  

    // Start is called before the first frame update
    void Start()
    {
        currentMask = allMasks[0];
        currentMaskIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // Get the value of the "Mouse ScrollWheel" axis
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput > 0)
        {
            Debug.Log("Scrolled Up: " + scrollInput);
            if(currentMaskIndex + 1 < allMasks.Count)
            {
                currentMaskIndex++;
                currentMask = allMasks[currentMaskIndex];
            } else
            {
                currentMask = allMasks[0];
                currentMaskIndex = 0;
            }
        }
        else if (scrollInput < 0)
        {
            Debug.Log("Scrolled Down: " + scrollInput);
            if (currentMaskIndex - 1 > 0)
            {
                currentMaskIndex--;
                currentMask = allMasks[currentMaskIndex];
            }
            else
            {
                currentMaskIndex = allMasks.Count - 1;
                currentMask = allMasks[currentMaskIndex];
            }
        }
    }
}