using UnityEngine;

public class OneBitBullet : MonoBehaviour
{
    [SerializeField] bool isPlayerBullet;

    [SerializeField] float myDamage;

    Vector3 lastPos;

    [SerializeField] bool killBarrier;





    float lifeTime = 1f;

    float startTime;

    private void Awake()
    {

    }
    // Start is called before the first frame update
    void Start()
    {
        lastPos = transform.position;
        startTime = Time.fixedTime;

    }

    // Update is called once per frame
    void Update()
    {
        if (startTime + lifeTime < Time.fixedTime && !killBarrier)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        // if (Time.fixedTime > startTime + Time.fixedDeltaTime)
        // {
        if (!killBarrier)
        {
            RaycastHit hit;

            if (Physics.Linecast(lastPos, transform.position, out hit))
            {
                deal_dmg(hit.collider);
            }

            lastPos = transform.position;
        }



        // }



    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Killlayer entered");

        deal_dmg(other);


    }

    private void deal_dmg(Collider other)
    {
        Debug.Log("Killlayer activated");
        if (!other.isTrigger && !killBarrier)
        {
            Destroy(gameObject);
        }
        if (isPlayerBullet)
        {
            BasicAI enemyHit = other.gameObject.GetComponent<BasicAI>();
            if (enemyHit != null)
            {
                enemyHit.take_damage(myDamage);
                if (!killBarrier)
                    Destroy(gameObject);
            }
            if (!other.CompareTag("GameController") && !other.CompareTag("Bullet"))
            {
                Debug.Log(other.name);
                if (!killBarrier)
                    Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log("hit");
            BR_PlayerController enemyHit = other.gameObject.GetComponent<BR_PlayerController>();
            if (enemyHit != null)
            {
                Debug.Log("hit2");
                enemyHit.take_damage(myDamage);
            }
            if (!other.CompareTag("Enemy") && !other.CompareTag("Bullet"))
            {
                if (!killBarrier)
                    Destroy(gameObject);
            }
        }

    }


}
