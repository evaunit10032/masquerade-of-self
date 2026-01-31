using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosive_Obj : InteractableObj
{
    [SerializeField] ParticleSystem flamingParticles;

    [SerializeField] GameObject explosion;

    [SerializeField] int hitTolerance = 1;

    [SerializeField] float timeTillExplode = 3f;

    [SerializeField] float centerOffset = 0;

    [SerializeField] private int timesHit = 0;


    
    public override void Break_Obj(int dmg)
    {
        if(timesHit + dmg < hitTolerance)
        {
            timesHit+= dmg;
            if (!flamingParticles.isPlaying)
            {
                flamingParticles.Play();
            }
            StartCoroutine(TimedExplode());
        } else
        {
            Explode();
            
        }
    }


    private void Explode()
    {
        Instantiate(explosion, transform.position + new Vector3(0, centerOffset, 0), transform.rotation);
        base.Break_Obj(0);
    }

    private IEnumerator TimedExplode()
    {
        yield return new WaitForSeconds(timeTillExplode);
        Explode();
    }
}
