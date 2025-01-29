using UnityEngine;
using System;

public class HitLogic : MonoBehaviour
{
    public GameObject smokePrefab;
    private new ParticleSystem particleSystem;
    private ParticleCollisionEvent[] collisionEvents;

    private void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        collisionEvents = new ParticleCollisionEvent[particleSystem.GetSafeCollisionEventSize()];
    }

    [Obsolete]
    private void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = particleSystem.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 pos = collisionEvents[i].intersection;

            if (smokePrefab != null)
            {
                Quaternion rot = Quaternion.LookRotation(transform.position - pos);
                Instantiate(smokePrefab, pos, rot);
            }
        }
    }
}
