using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.AI;
using System.Linq;

public class EnemyShooter : EnemyBase
{
    [SerializeField] public int TotalAttackPoints = 10;
    [SerializeField] public float AttackPointRadius = 5f;

    
    Vector3 closestPoint;

    public override void FollowPlayer()
    {

        _agent.destination = closestPoint;
        _lastKnownPlayerPosition = playerTransform.position;
        
    }

    public void GetNewPoint()
    {
        float innerRange = 15; 
        float outerRange = 30;

        List<Vector3> storedValidPoints = new List<Vector3>();
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 25.0f;

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 25.0f, NavMesh.AllAreas))
                {
                    Vector3 result = hit.position;

                    Debug.Log("Distance: " + Vector3.Distance(result, transform.position));
                    if (Vector3.Distance(result, transform.position) < innerRange ||
                        Vector3.Distance(result, transform.position) > outerRange)
                    {
                        continue; // Point is out of desired range, try again
                    } else
                    {
                        storedValidPoints.Add(result);
                        break;
                    }
                    
                    
                }
        }

        Vector3 closestPoint = storedValidPoints[0];
        for (int i = 0; i < storedValidPoints.Count; i++)
        {
            if (Vector3.Distance(storedValidPoints[i], transform.position) <
                Vector3.Distance(closestPoint, transform.position))
            {
                closestPoint = storedValidPoints[i];
                Debug.Log(closestPoint);
            }
        }
    }

    public override void Update()
    {
        var distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        Debug.Log("Current State: " + _agent.speed.ToString());


        //Add animation here
        _animator.SetFloat("WalkingSpeed", _agent.speed);

        Debug.Log(_currentState);

        switch (_currentState)
        {
            case EnemyState.Patrolling:
             /*   if (storedValidPoints.Count > 0)
                {
                    storedValidPoints.Clear();
                }*/
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
               // if (storedValidPoints.Count <= TotalAttackPoints)
                {
                    GetNewPoint();
                }
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
                   GoToNextPatrolPoint();
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
}
