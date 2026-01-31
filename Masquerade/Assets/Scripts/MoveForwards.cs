using UnityEngine;

public class MoveForwards : MonoBehaviour
{
    private void Awake()
    {
        transform.position = transform.position + (transform.forward * 0.05f);
    }

}
