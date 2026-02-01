using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShooter : EnemyBase
{
    [SerializeField] public int TotalAttackPoints = 10;
    public override void FollowPlayer()
    {
        List<Vector3> attackPoints = new List<Vector3>();

        for (int i = 0; i < TotalAttackPoints; i++)
        {
            //_agent.navMeshOwner.SamplePosition();
        }
    }

    private void SamplePosition()
    {
        throw new System.NotImplementedException();
    }
}
