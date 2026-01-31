
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
/*

.enemy
Will be world if not currently angry at anyone.

.movetarget
The next path spot to walk toward.  If .enemy, ignore .movetarget.
When an enemy is killed, the enemy will try to return to it's path.

.huntt_ime
Set to time + something when the player is in sight, but movement straight for
him is blocked.  This causes the enemy to use wall following code for
movement direction instead of sighting on the player.

.pausetime
An enemy will leave it's stand state and head towards it's .movetarget when
time > .pausetime.

.walkRadius
The range around the enemy for them to pick a point to walk to randomly

.view_ofs
The distance between the enemy's transform origin and eye position

.playerview_ofs
The distance between the player's transform origin and eye position


*/
public class BasicAI : Client
{
    [SerializeField] EnemyType enemyType;

    [SerializeField] Client enemy;

    [SerializeField] Vector3 moveTarget;

    float hunt_time;

    [SerializeField] float pausetime;

    NavMeshAgent enemy_agent;

    [SerializeField] float walkRadius;

    //Enemy and player's eye height from their origin point

    [SerializeField] float view_ofs;

    [SerializeField] float playerview_ofs;

    [SerializeField] string think;

    [SerializeField] Animator enemyAnimator;

    float next_think;

    AudioClip sight_sound;

    AudioSource enemy_Audio;

    float showHostile;

    float searchTime;

    float next_attack_time;

    [SerializeField] float fire_rate;

    [SerializeField] GameObject bulletPrefab;

    [SerializeField] float bullet_spread;

    [SerializeField] Transform gun_barrel;

    [SerializeField] GameObject gun;

    [SerializeField] GameObject axe;

    [SerializeField] Rig grip_rig;

    [SerializeField] MultiAimConstraint aimAtPlayer;

    [SerializeField] GameObject aim_holder;

    [SerializeField] GameObject aim_point;

    [SerializeField] bool on_alert;

    [SerializeField] float shot_cooldown;

    [SerializeField] List<GameObject> drop_list = new List<GameObject>();

    [SerializeField] AudioSource enemy_audio;

    [SerializeField] AudioClip gun_shot;

    [SerializeField] bool standing;

    //   [SerializeField] SkinnedMeshRenderer skinnedMeshRenderer;

    // [SerializeField] List<Material> materials = new List<Material>();

    [SerializeField] Vector2 bullet_count_range;

    bool spawning;

    float timeSinceHit;

    public bool dead;


    // GLOBALS
    // when a monster becomes angry at a player, that monster will be used
    // as the sight target the next frame so that monsters near that one
    // will wake up even if they wouldn't have noticed the player
    //

    static BasicAI sight_entity;

    static float sight_entity_time;



    private void Start()
    {

        enemy_agent = GetComponent<NavMeshAgent>();
        //enemy_agent.destination = Vector3.zero;
        StartCoroutine(spawn_enemy());

        InvokeRepeating("ai_think", 0.1f, 0.1f);
    }

    public IEnumerator spawn_enemy()
    {
        on_alert = true;
        //skinnedMeshRenderer.materials[1] = materials[0];
        //skinnedMeshRenderer.materials[5] = materials[0];
        think = "ai_dead";
        grip_rig.weight = 0;
        aimAtPlayer.weight = 0;
        enemyAnimator.SetLayerWeight(1, 0);
        enemyAnimator.SetLayerWeight(2, 0);
        enemyAnimator.SetLayerWeight(3, 0);
        spawning = true;
        float startingHeight = transform.localScale.y;
        for (int i = 0; i <= 100f; i++)
        {
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Lerp(0.1f, startingHeight, i / 100f), transform.localScale.z);
            yield return new WaitForSeconds(0.01f);
        }

        yield return new WaitForSeconds(3.2f);
        grip_rig.weight = 1;
        aimAtPlayer.weight = 1;
        enemyAnimator.SetLayerWeight(1, 1);
        enemyAnimator.SetLayerWeight(2, 1);
        enemyAnimator.SetLayerWeight(3, 1);
        if (standing)
        {
            think = "ai_stand";
        }
        else
        {
            think = "ai_walk";
        }
        // skinnedMeshRenderer.materials[1] = materials[1];
        // skinnedMeshRenderer.materials[5] = materials[1];
        spawning = false;
    }

    public IEnumerator despawn_enemy()
    {
        yield return new WaitForSeconds(3f);
        float startingHeight = transform.localScale.y;
        for (int i = 0; i <= 100f; i++)
        {
            transform.localScale = new Vector3(transform.localScale.x, Mathf.Lerp(startingHeight, 0.1f, i / 100f), transform.localScale.z);
            yield return new WaitForSeconds(0.01f);
        }
        Destroy(aim_point);

        Destroy(transform.parent.gameObject);
    }

    private void ai_think()
    {
        Invoke(think, 0f);
    }

    private void Update()
    {
        if (enemy != null && think != "ai_dead")
        {
            Vector3 lookRot = Quaternion.LookRotation(enemy.transform.position - transform.position).eulerAngles;


            float turnAmount = Mathf.Round(Vector3.SignedAngle((enemy.transform.position - transform.position), transform.forward, Vector3.up));
            if (Mathf.Abs(turnAmount) <= 20f || Vector3.Distance(transform.position, enemy.transform.position) < 5f)
            {
                turnAmount = 0;
            }

            enemyAnimator.SetFloat("TurnOffset", turnAmount);
            if (think == "ai_walk")
            {
                transform.rotation = Quaternion.LookRotation((enemy_agent.destination - transform.position), transform.up);
                enemyAnimator.SetFloat("TargetOffsetX", Mathf.Round(Vector3.SignedAngle((enemy_agent.destination - transform.position), transform.forward, Vector3.up)));
            }

            //this.transform.rotation = Quaternion.Euler(0, lookRot.y, 0);


            //enemyAnimator.SetFloat("TargetOffsetY", (enemy.transform.position - this.transform.position).normalized.z); //enemy_agent.destination
            //Debug.Log((this.transform.InverseTransformPoint(enemy.transform.position)).normalized.z + " , " + (this.transform.InverseTransformPoint(enemy.transform.position)).normalized.x);
        }



    }

    private void ai_dead()
    {

    }



    /*
    ==============================================================================

    MOVETARGET CODE

    targetname
    must be present.  The name of this movetarget.

    target
    the next spot to move to.  If not present, stop here for good.

    pausetime
    The number of seconds to spend standing

    Movetarget_w
    Move to a random place in the world

    Movetarget_t
    Move to a random point around a target

    ==============================================================================
    */

    private void Movetarget_w(float dist)
    {

        if (moveTarget == Vector3.zero || Vector3.Distance(this.transform.position, moveTarget) <= 13f)
        {
            Vector3 randomDirectionWorld = Random.insideUnitSphere * 100f;
            randomDirectionWorld += transform.position;
            NavMeshHit hitWorld;
            NavMesh.SamplePosition(randomDirectionWorld, out hitWorld, 100f, 1);
            Vector3 finalPositionWorld = hitWorld.position;
            moveTarget = finalPositionWorld;
        }

        Vector3 finalHit = Vector3.zero;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * dist;
            randomDirection += transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, dist, 1);

            if (Vector3.Distance(hit.position, moveTarget) < Vector3.Distance(finalHit, moveTarget))
            {
                finalHit = hit.position;
            }
        }

        Vector3 finalPosition = finalHit;

        enemy_agent.speed = 0.01f;//3.5f;

        enemy_agent.destination = finalPosition;

        float turnAmount = Mathf.Round(Vector3.SignedAngle((enemy_agent.destination - transform.position), transform.forward, Vector3.up));
        if (Mathf.Abs(turnAmount) <= 20f)
        {
            turnAmount = 0;
        }
        else
        {
            enemyAnimator.SetFloat("TurnOffset", turnAmount);
        }

        enemyAnimator.SetBool("Walking", true);

    }

    private void Movetarget_t(float dist, Transform target)
    {
        Vector3 finalHit = Vector3.zero;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * dist;
            randomDirection += transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, dist, 1);

            if (Vector3.Distance(hit.position, target.position) < Vector3.Distance(finalHit, target.position))
            {
                finalHit = hit.position;
            }
        }

        Vector3 finalPosition = finalHit;

        //Vector3 finalPosition = target.position;

        enemy_agent.speed = 4f;

        enemy_agent.destination = finalPosition;

        enemyAnimator.SetBool("Running", true);
        /*Vector3 randomDirection = Random.insideUnitSphere * dist;
        randomDirection += transform.position;
        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, dist, 1);
        Vector3 finalPosition = hit.position;

        enemy_agent.destination = finalPosition;*/
    }

    //============================================================================

    /*
    =============
    range

    returns the range catagorization of an entity reletive to self
    0	melee range, will become hostile even if back is turned
    1	visibility and infront, or visibility and show hostile
    2	infront and show hostile
    3	only triggered by damage
    =============
    */

    private float Range(Transform targ)
    {
        Vector3 spot1 = this.transform.position + (Vector3.up * view_ofs);

        Vector3 spot2 = targ.position + (Vector3.up * playerview_ofs);

        float r = (spot1 - spot2).magnitude;

        if (r < 10f)
            return 0;
        if (r < 25f)
            return 1;
        if (r < 35f)
            return 2;
        return 3;
    }

    /*
    =============
    visible

    returns 1 if the entity is visible to self, even if not infront ()
    =============
    */

    private bool Visible(Transform targ, bool player)
    {
        Vector3 spot1 = this.transform.position + (Vector3.up * (view_ofs * 2.5f));

        Vector3 spot2 = targ.position + (Vector3.up * playerview_ofs);

        Vector3 raycastDir = spot2 - spot1;

        RaycastHit hit;

        Debug.DrawRay(spot1, raycastDir, Color.green, 0.1f, true);



        if (player)
        {
            //Check if the player is visible

            string[] newMask = new string[] { "Default", "GameController" };

            Physics.Raycast(spot1, raycastDir, out hit, Mathf.Infinity, LayerMask.GetMask(newMask)); //See through other enemies

            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("GameController"))
                {
                    return true;
                }
            }
        }
        else
        {
            //Check if other enemy who saw player is visible

            string[] newMask2 = new string[] { "Default", "Enemy" };

            RaycastHit hit2;

            Physics.Raycast(spot1, raycastDir, out hit2, Mathf.Infinity, LayerMask.GetMask(newMask2), QueryTriggerInteraction.Collide); //See through other enemies

            if (hit2.collider != null)
            {
                if (hit2.collider.CompareTag("Enemy") && hit2.collider.gameObject.transform != this.gameObject)
                {
                    Debug.Log(hit2.collider.gameObject.name);
                    return true;
                }

            }
        }

        return false;
    }

    /*
    =============
    infront

    returns 1 if the entity is in front (in sight) of self
    =============
    */

    private bool In_front(Transform targ)
    {
        Vector3 vec = (targ.transform.position - this.transform.position).normalized;

        //float dot = Vector3.Dot(vec, this.transform.position);

        float dot = Vector3.Dot(transform.forward, transform.InverseTransformPoint(targ.transform.position));

        if (dot >= 7.5f) //(dot >= 7.5f) 
            return true;

        return false;

    }

    //============================================================================

    private void HuntTarget()
    {
        this.think = "ai_run";
        this.next_think = Time.fixedTime + 1f;
        //SUB_AttackFinished (1); Add something here to wait a second before attacking // wait a while before first attack
    }

    private void FoundTarget()
    {
        Debug.Log("Found Target");
        if (enemy.gameObject.CompareTag("GameController"))
        {   // let other monsters see this monster for a while
            aim_point.transform.SetParent(enemy.transform, false);
            aim_point.transform.localPosition = Vector3.zero;

            sight_entity = this;
            sight_entity_time = Time.fixedTime;
            enemy_agent.destination = Vector3.zero;
        }
        this.show_hostile = Time.fixedTime + 1f;		// wake up other monsters

        //enemy_Audio.PlayOneShot(sight_sound);
        HuntTarget();

    }

    /*
    ===========
    FindTarget

    Self is currently not attacking anything, so try to find a target

    Returns TRUE if an enemy was sighted

    When a player fires a missile, the point of impact becomes a fakeplayer so
    that monsters that see the impact will respond as if they had seen the
    player.

    To avoid spending too much time, only a single client (or fakeclient) is
    checked each frame.
    ============
    */

    private bool FindTarget()
    {
        if (standing)
        {
            on_alert = true;
        }


        Client client;

        if (spotted_clients.Count <= 0)
            return false;



        client = spotted_clients[Random.Range(0, spotted_clients.Count)];

        float r;

        if (sight_entity_time >= Time.fixedTime - 0.1f)
        {
            client = sight_entity;
            //return (client.transform == this.enemy);

        }




        r = Range(client.transform);

        //Debug.Log(r);

        //
        Debug.Log(r);

        if (r == 3 && client.show_hostile < Time.fixedTime && !In_front(client.transform) && !on_alert)
            return false;
        if (!Visible(client.transform, client.is_player) && !on_alert)
            return false;

        if (r == 2)
        {
            if (client.show_hostile < Time.fixedTime && !In_front(client.transform) && !on_alert)
                return false;
        }
        else if (r == 1)
        {
            if (!In_front(client.transform) && !on_alert)
                return false;
        }

        //Debug.Log("dont need hostile");


        if (!client.gameObject.CompareTag("GameController"))
        {
            if (client.show_hostile < Time.fixedTime)
            {
                return false;
            }
        }


        //
        // got one
        //

        this.enemy = client;
        if (!client.gameObject.CompareTag("GameController"))
        {
            Client new_enemy = client.GetComponent<BasicAI>().enemy;
            if (new_enemy != null)
            {
                if (!new_enemy.gameObject.CompareTag("GameController"))
                {
                    this.enemy = null;
                    return false;
                }
                else
                {
                    this.enemy = new_enemy;
                }
            }
            else
            {
                return false;
            }

        }
        else
        {
            Debug.Log("spotted");
            this.enemy = client;

            aimAtPlayer.weight = 1f;
            //aim_point.transform.parent = enemy.gameObject.transform;
            //aim_point.transform.position = enemy.gameObject.transform.position;

            //enemyAnimator.SetBool("EnterCombat", true);
        }

        FoundTarget();

        return true;

    }

    private void ai_forward(float dist)
    {
        enemy_agent.SetDestination(this.transform.position + (this.transform.forward * dist));
    }

    private void ai_back(float dist)
    {
        enemy_agent.SetDestination(this.transform.position + (this.transform.forward * -(dist)));
    }

    /*
    =============
    ai_pain

    stagger back a bit
    =============
    */

    public void ai_pain(float dist)
    {
        ai_back(dist);
        /*
	    local float	away;
	
	    away = anglemod (vectoyaw (self.origin - self.enemy.origin) 
	    + 180*(random()- 0.5) );
	
	    walkmove (away, dist);
        */
    }

    /*
    =============
    entity_search

    Look for all entities in the nearby area
    =============
    */
    private void entity_search()
    {
        string[] mask = new string[] { "Enemy", "GameController" };
        Collider[] hitColliders = Physics.OverlapSphere(this.transform.position, 50f, LayerMask.GetMask(mask), QueryTriggerInteraction.Collide);
        spotted_clients.Clear();
        for (int i = 0; i < hitColliders.Length; i++)
        {
            if (hitColliders[i].gameObject != this.gameObject)
            {
                if (hitColliders[i].GetComponent<Client>() != null)
                {
                    spotted_clients.Add(hitColliders[i].GetComponent<Client>());
                }
            }

        }
    }


    /*
    =============
    ai_walk

    The robot is walking it's beat
    =============
    */

    private void ai_walk()
    {


        if (enemy != null)
        {
            if (Vector3.Distance(transform.position, enemy_agent.destination) <= 5f || next_attack_time + shot_cooldown < Time.fixedTime)
            {
                enemy_agent.speed = 0;
                enemyAnimator.SetBool("Running", false);
                think = "ai_run";


            }
            /*if(enemyType == EnemyType.Shotgunner && Range(enemy.transform) <= 1 && Time.fixedTime >= next_attack_time)
            {
                ai_run_missile();
            next_attack_time = Time.fixedTime + shot_cooldown;
            }*/
        }
        else
        {
            if (enemy_agent.speed == 0 && think == "ai_walk")
                Movetarget_w(walkRadius);

            if (Vector3.Distance(transform.position, enemy_agent.destination) <= 1f || pausetime + 3f < Time.fixedTime)
            {
                enemy_agent.speed = 0;
                //Stop enemy until a new destination is found
                //enemy_agent.destination = Vector3.zero;


                pausetime = Time.fixedTime + Random.Range(0f, 1f);
                entity_search();
                if (FindTarget())
                    return;

                enemyAnimator.SetBool("Walking", false);

                think = "ai_stand";


            }
        }




    }

    /*
    =============
    ai_stand
    
    The monster is staying in one place for a while, with slight angle turns
    =============
    */

    private void ai_stand()
    {
        /*
        RaycastHit hit;
        if (Physics.SphereCast(this.transform.position, walkRadius, transform.forward, out hit))
        {
            if (hit.collider.GetComponent<Client>() != null)
            {
                if (FindTarget(hit.collider.GetComponent<Client>()))
                    return;
            }

            if(Time.time > pausetime)
            {
                this.think = "ai_walk";
            }
        }
        */

        enemyAnimator.SetBool("Walking", false);



        /*float turnAmount = Mathf.Round(Vector3.SignedAngle((enemy_agent.destination - transform.position), transform.forward, Vector3.up));
        if (Mathf.Abs(turnAmount) <= 20f)
        {
            turnAmount = 0;
        } else
        {
            enemyAnimator.SetFloat("TurnOffset", turnAmount);
        }*/



        entity_search();
        if (FindTarget())
            return;

        if (Time.fixedTime > pausetime) //Mathf.Abs(turnAmount) <= 20f
        {
            if (!standing)
            {
                this.think = "ai_walk";
            }

        }
        // change angle slightly
    }

    /*
    =============
    ai_run_melee

    Turn and close until within an angle to launch a melee attack
    =============
    */
    private void ai_run_melee()
    {

        if (enemyAnimator.GetBool("Melee") == false)
        {
            enemyAnimator.SetBool("Melee", true);
            gun.SetActive(false);
            grip_rig.weight = 0;
            axe.SetActive(true);

        }
        else
        {
            //transform.rotation = Quaternion.LookRotation((enemy_agent.destination - transform.position), transform.up);
            //Movetarget_t(3f, enemy.transform);
            enemyAnimator.SetFloat("SwingState", Mathf.Round(Random.Range(1f, 4.4f)));
            enemyAnimator.SetTrigger("Swing");

        }
        /*if(Vector3.Distance(transform.position, enemy.transform.position) > 3.5f && enemy_agent.speed == 0)
        {
            transform.rotation = Quaternion.LookRotation((enemy_agent.destination - transform.position), transform.up);
            Movetarget_t(10f, enemy.transform);
        } else
        {
            transform.rotation = Quaternion.LookRotation((enemy_agent.destination - transform.position), transform.up);
            Movetarget_t(3f, enemy.transform);
            enemyAnimator.SetFloat("SwingState", Mathf.Round(Random.Range(1f, 4.4f)));
            enemyAnimator.SetTrigger("Swing");
        }*/
        //move towards a lerp point right in front of the enemy then check if in_front(), if so then melee
    }

    /*
    =============
    ai_run_missile

    Turn in place until within an angle to launch a projectile attack
    =============
    */
    private void ai_run_missile()
    {
        float turnAmount = Mathf.Round(Vector3.SignedAngle((new Vector3(enemy.transform.position.x, transform.position.y, enemy.transform.position.x) - transform.position), transform.forward, Vector3.up));
        Debug.Log(turnAmount);
        if (Mathf.Abs(turnAmount) <= 20f)
        {
            turnAmount = 0;
        }
        else
        {
            enemyAnimator.SetFloat("TurnOffset", turnAmount);
        }
        if (turnAmount < 40f || standing)
        {
            next_attack_time = Time.fixedTime + shot_cooldown;
            switch (enemyType)
            {
                case EnemyType.MachineGunGuard:
                    enemy_audio.PlayOneShot(gun_shot);
                    StartCoroutine(Rapid_fire(Random.Range((int)bullet_count_range.x, (int)bullet_count_range.x)));
                    break;
                case EnemyType.Shotgunner:
                    enemyAnimator.SetTrigger("Shoot");
                    enemy_audio.PlayOneShot(gun_shot);
                    for (int i = 0; i < 8; i++)
                    {
                        GameObject newBullet = Instantiate(bulletPrefab, gun_barrel.position, Quaternion.Euler(gun_barrel.rotation.eulerAngles + new Vector3(Random.Range(-bullet_spread, bullet_spread), Random.Range(-bullet_spread, bullet_spread), Random.Range(-bullet_spread, bullet_spread))));
                        //newBullet.transform.LookAt(enemy.transform.position + new Vector3(Random.Range(-bullet_spread, bullet_spread), Random.Range(-bullet_spread, bullet_spread), Random.Range(-bullet_spread, bullet_spread)));

                    }

                    break;
            }
        }


    }

    public void take_damage(float damage)
    {
        if (!spawning)
        {
            health -= damage;
            if (health <= 0 && think != "ai_dead")
            {
                dead = true;
                think = "ai_dead";
                grip_rig.weight = 0;
                aimAtPlayer.weight = 0;
                Instantiate(drop_list[Random.Range(0, drop_list.Count)], transform.position, Quaternion.identity);
                enemyAnimator.SetLayerWeight(1, 0);
                enemyAnimator.SetLayerWeight(2, 0);
                enemyAnimator.SetLayerWeight(3, 0);
                float turnAmount = Mathf.Round(Vector3.SignedAngle((enemy_agent.destination - transform.position), transform.forward, Vector3.up));
                if (turnAmount <= -135f)
                {
                    turnAmount = -180f;
                }
                else if (turnAmount <= -45f)
                {
                    turnAmount = -90;
                }
                else if (turnAmount <= 45f)
                {
                    turnAmount = 0;
                }
                else if (turnAmount <= 135f)
                {
                    turnAmount = 90;
                }
                else if (turnAmount <= 180f)
                {
                    turnAmount = 180;
                }
                enemyAnimator.SetFloat("TurnOffset", turnAmount);

                enemyAnimator.SetTrigger("Die");

                StartCoroutine(despawn_enemy());

            }
            else
            {
                if (!dead)
                {
                    on_alert = true;
                    if (enemy == null)
                    {
                        FindTarget();
                    }
                    enemyAnimator.SetTrigger("Hit");

                    enemy_agent.speed = 0;
                    StopAllCoroutines();

                    timeSinceHit = Time.fixedTime + 0.5f;
                    next_attack_time += 0.5f;
                }

            }
        }




    }

    private IEnumerator Rapid_fire(int numShots)
    {
        for (int i = 0; i < numShots; i++)
        {

            GameObject newBullet = Instantiate(bulletPrefab, gun_barrel.position, Quaternion.Euler(gun_barrel.rotation.eulerAngles + new Vector3(bullet_spread, bullet_spread, bullet_spread)));
            //newBullet.transform.LookAt(enemy.transform.position + new Vector3(Random.Range(-bullet_spread, bullet_spread), Random.Range(-bullet_spread, bullet_spread), Random.Range(-bullet_spread, bullet_spread)));
            enemyAnimator.SetTrigger("Shoot");
            yield return new WaitForSeconds(fire_rate);
        }
    }

    /*
    =============
    ai_run_slide

    Strafe sideways, but stay at aproximately the same range
    =============
    */
    private void ai_run_slide()
    {

    }

    /*
    =============
    ai_run

    The monster has an enemy it is trying to kill
    =============
    */
    private void ai_run()
    {
        enemyAnimator.SetBool("InCombat", true);

        if (next_think > Time.fixedTime)
            return;

        this.showHostile = Time.fixedTime + 1f;

        bool enemy_vis = Visible(this.enemy.transform, enemy.is_player);
        if (enemy_vis)
            searchTime = Time.fixedTime + 5f;

        float enemy_range = Range(enemy.transform);

        if (next_attack_time < Time.fixedTime)
        {
            switch (enemy_range)
            {
                case 0:
                    ai_run_missile();
                    //ai_run_melee(); 
                    break;
                case 1:
                    ai_run_missile();
                    break;

                case 2:
                    ai_run_missile();
                    break;
                case 3:
                    ai_run_missile();
                    break;
            }

        }

        //if time > self.enemy.attack finished, ai_run_slide
        if (!standing)
        {
            if (enemy_agent.speed == 0 && timeSinceHit <= Time.fixedTime)
            {
                if (enemyType == EnemyType.Shotgunner)
                {
                    if (enemy_range > 1) //health >= 55f && 
                    {
                        Movetarget_t(25f, enemy.transform);
                        this.think = "ai_walk";
                    }
                }
                else
                {
                    if (enemy_range > 2) //health >= 55f && 
                    {
                        Movetarget_t(25f, enemy.transform);
                        this.think = "ai_walk";
                    }
                }

                /*else if (health < 25f && enemy_range < 2)
                {
                    ai_back(walkRadius);
                }*/
            }
        }



    }



}

public enum EnemyType
{
    MachineGunGuard,
    Shotgunner
}
