using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NewBehaviourScript : MonoBehaviour
{
    [Header("References to Other Objects")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform playerTransform;
    [Header("Patrol Settings")]
    [SerializeField] private float patrolWaitTime = 2f;
    [Header("Patrol Settings")]
    [SerializeField] private float stopAtDistance = 0.5f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private float disengageTime = 5f;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private bool canMoveWhileAttacking = false;

    private NavMeshAgent _agent;
    private Animator _animator;
    private int _currentPatrolIndex = 0;
    private bool _isWaiting;
    private EnemyState _currentState;
    private float _timeSinceLastSeenPlayer;
    private bool _isAttacking;

    private enum EnemyState
    {
        Patrolling,
        Chasing,
        Attacking
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        _isWaiting = true;
        _agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);
        _agent.isStopped = false;
        GoToNextPatrolPoint();
        _isWaiting = false;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        _agent.destination = patrolPoints[_currentPatrolIndex].position;
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private bool IsFacingPlayer()
    {
        var directionToPlayer = (playerTransform.position - transform.position).normalized;
        var angle = Vector3.Angle(transform.forward, directionToPlayer);
        return angle < viewAngle / 2f;
    }

    private bool HasClearPathToPlayer()
    {
        var directionToPlayer = playerTransform.position - transform.position;
        if (Physics.Raycast(transform.position, directionToPlayer.normalized, out RaycastHit hit, directionToPlayer.magnitude))
        {
            return hit.transform == playerTransform;
        }

        return true;
    }

    private bool CanSeePlayer()
    {
        return IsFacingPlayer() && HasClearPathToPlayer();
    }

    private void FollowPlayer()
    {
        _agent.SetDestination(playerTransform.position);
    }


    private void Patrol()
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

    private void StartAttack()
    {
        if (!canMoveWhileAttacking)
        {
            _agent.isStopped = true;
        }
        
        _isAttacking = true;
        _animator.SetTrigger("attack");
    }

//This method can be invoked by
    private void FinishAttack()
    {
        _isAttacking = false;
        if (!canMoveWhileAttacking)
        {
            _agent.isStopped = false;
        }
    }

    private void UpdateAnimations()
    {
        var isWalking = _agent.velocity.sqrMagnitude > 0.1f;
        _animator.SetBool("isMoving", isWalking);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GoToNextPatrolPoint();
    }

    // Update is called once per frame
    void Update()
    {
        var distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        switch (_currentState)
        {
            case EnemyState.Patrolling:
                Patrol();
                if (distanceToPlayer <= detectionRange && CanSeePlayer())
                {
                    _currentState = EnemyState.Chasing;
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
                break;
            case EnemyState.Attacking:
                if (!_isAttacking && distanceToPlayer > attackRange)
                {
                    _currentState = EnemyState.Chasing;
                    _agent.isStopped = false;
                }

                break;
        }

        Patrol();
        UpdateAnimations();
    }

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _currentState = EnemyState.Patrolling;
    }
}
