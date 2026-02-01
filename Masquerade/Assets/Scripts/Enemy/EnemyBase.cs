using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour
{
    [Header("References to Other Objects")]
    [SerializeField] public Transform[] patrolPoints;
    [SerializeField] public Transform playerTransform;
    [Header("Patrol Settings")]
    [SerializeField] public float patrolWaitTime = 2f;
    [SerializeField] public float stopAtDistance = 0.5f;
    [SerializeField] public float detectionRange = 5f;
    [SerializeField] public float viewAngle = 90f;
    [SerializeField] public float disengageTime = 5f;
    [SerializeField] public float attackRange = 1.5f;
    [SerializeField] public bool canMoveWhileAttacking = false;
    [SerializeField] public float lurkingDuration = 3f;
    [Header("General Settings")]
    [SerializeField] public float movementSpeed = 3.5f;
    [SerializeField] public float health = 3f;
    [SerializeField] public float magicalBarrier = 0f;
    [SerializeField] public float staggeredTime = 3f;

//private fields
    public NavMeshAgent _agent;
    public Animator _animator;
    public int _currentPatrolIndex = 0;
    public bool _isWaiting;
    public EnemyState _currentState;
    public float _timeSinceLastSeenPlayer;
    public bool _isAttacking;
    public Vector3 _lastKnownPlayerPosition;
    public float _elapsedTimeStaggered;
//public fields
    public bool hasMagicalBarrier;

    public enum EnemyState
    {
        Patrolling,
        Chasing,
        Attacking, 
        searching,
        Dying
    }

    public virtual IEnumerator WaitAtPatrolPoint()
    {
        _isWaiting = true;
        _agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);
        _agent.isStopped = false;
        GoToNextPatrolPoint();
        _isWaiting = false;
    }

    public virtual void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        _agent.destination = patrolPoints[_currentPatrolIndex].position;
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }

    public virtual bool IsFacingPlayer()
    {
        var directionToPlayer = (playerTransform.position - transform.position).normalized;
        var angle = Vector3.Angle(transform.forward, directionToPlayer);
        return angle < viewAngle / 2f;
    }

    public virtual bool HasClearPathToPlayer()
    {
        var directionToPlayer = playerTransform.position - transform.position;
        if (Physics.Raycast(transform.position, directionToPlayer.normalized, out RaycastHit hit, directionToPlayer.magnitude))
        {
            return hit.transform == playerTransform;
        }

        return true;
    }

    public virtual bool CanSeePlayer()
    {
        Debug.Log("can see player");
        //return IsFacingPlayer() && HasClearPathToPlayer();
        return HasClearPathToPlayer();
    }
    

    public virtual void FollowPlayer()
    {
        _agent.SetDestination(playerTransform.position);
        _lastKnownPlayerPosition = playerTransform.position;
    }

    public virtual void LurkAtLastKnownPosition()
    {
        _agent.SetDestination(_lastKnownPlayerPosition);
    }


    public virtual void Patrol()
    {
        if (_isWaiting) return;

        if (!_agent.pathPending && _agent.remainingDistance < stopAtDistance)
        {
            StartCoroutine(WaitAtPatrolPoint());
            _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
            _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
            _agent.isStopped = false;
            _isWaiting = false;
        }
    }

    public virtual void StartAttack()
    {
        if (!canMoveWhileAttacking)
        {
            _agent.isStopped = true;
        }
        
        _isAttacking = true;
        _animator.SetTrigger("attack");
        //Don't forgor, once we get the animation event, call FinishAttack after that amount of time
        Invoke(nameof(FinishAttack), 4.667f); //Assuming attack animation lasts 1 second
    }

//This method can be invoked by
    public virtual void FinishAttack()
    {
        _isAttacking = false;
        _agent.isStopped = false;

        TakeDamage(1f); // Example damage value
    }

    public virtual void TakeDamage(float damage)
    {
        if (hasMagicalBarrier)
        {
            magicalBarrier -= damage;
            if (magicalBarrier <= 0f)
            {
                hasMagicalBarrier = false;
            }
        }
        else 
        {
            health -= damage;
            if (health <= 0)
            {
                _animator.SetTrigger("die");
                _currentState = EnemyState.Dying;
                Destroy(gameObject, 6.733f);
            }
            _agent.isStopped = true;
            _elapsedTimeStaggered = 0f;
            _agent.speed = 0f;
        }
    }

    public virtual void IncreaseSpeed()
    {
        _elapsedTimeStaggered += Time.deltaTime;
        float staggerProgress = _elapsedTimeStaggered / staggeredTime;
        _agent.speed = Mathf.Lerp(0f, movementSpeed, staggerProgress);
    }

    public IEnumerator WaitForATime(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public void UpdateAnimations()
    {
        bool isWalking = (_agent.velocity.sqrMagnitude > 0.1f);
        _animator.SetBool("isMoving", isWalking);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void Start()
    {
        GoToNextPatrolPoint();
    }

    // Update is called once per frame
    public virtual void Update()
    {
        var distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        Debug.Log("Current State: " + _agent.speed.ToString());


        //Add animation here
        _animator.SetFloat("WalkingSpeed", _agent.speed);



        switch (_currentState)
        {
            case EnemyState.Patrolling:
                Patrol();
                if (distanceToPlayer <= detectionRange && CanSeePlayer())
                {
                    _currentState = EnemyState.Chasing;
                }
                if (_agent.speed < movementSpeed)
                {
                    IncreaseSpeed();
                }
                break;
            
            case EnemyState.Chasing:
                FollowPlayer();
                if (distanceToPlayer <= attackRange)
                {
                    _currentState = EnemyState.Attacking;
                    StartAttack();
                }
                else
                if (!CanSeePlayer())
                {
                    _timeSinceLastSeenPlayer += Time.deltaTime;
                    if (_timeSinceLastSeenPlayer >= disengageTime)
                    {
                        _currentState = EnemyState.Patrolling;
                        _timeSinceLastSeenPlayer = 0f;
                        GoToNextPatrolPoint();
                    }
                }
                else
                {
                    _timeSinceLastSeenPlayer = 0f;
                }

                if (_agent.speed < movementSpeed)
                {
                    IncreaseSpeed();
                }
                break;
            case EnemyState.Attacking:
                if (!_isAttacking && distanceToPlayer > attackRange)
                {
                    _currentState = EnemyState.Chasing;
                    _agent.isStopped = false;
                }

                break;
            case EnemyState.searching:
                _timeSinceLastSeenPlayer += Time.deltaTime;
                if (_timeSinceLastSeenPlayer >= lurkingDuration)
                {
                    _currentState = EnemyState.Patrolling;
                    _timeSinceLastSeenPlayer = 0f;
                    GoToNextPatrolPoint();
                }
                else
                {
                    LurkAtLastKnownPosition();
                }
                break;
            case EnemyState.Dying:
                _agent.isStopped = true;
                _agent.speed = 0f;
                break;
        }

        Patrol();
        UpdateAnimations();
    }

    public virtual void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _currentState = EnemyState.Patrolling;
        _agent.speed = movementSpeed;
        if (magicalBarrier > 0f)
        {
            hasMagicalBarrier = true;
        }
        else
        {
            hasMagicalBarrier = false;
        }
    }

}
