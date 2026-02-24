using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour
{
    AudioSource speaker;

    void Awake()
    {
        GetComponent<TrailRenderer>().enabled = false;
        speaker = GetComponent<AudioSource>();
    }

    void OnCollisionEnter(Collision c)
    {
        BoostObject bo = c.gameObject.GetComponent<BoostObject>();
        if (bo != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && c.contactCount > 0)
            {
                Vector3 dir = transform.position - c.contacts[0].point;
                float sqrMagnitude = dir.sqrMagnitude;
                if (sqrMagnitude > 0.000001f)
                {
                    dir /= Mathf.Sqrt(sqrMagnitude);
                    rb.AddForce(dir * bo.BoostForce);
                }
            }
        }

        if (speaker && !SoundCatalog.PlayRandom(speaker, "ball_hit_"))
            speaker.Play();

        ScoringObject so = c.gameObject.GetComponent<ScoringObject>();
        if (so != null) Player.instance.IncrementScore(so.IncrementValue);
    }

    void OnCollisionExit(Collision c) => Inventory.Equipped.OnCollision();
}
