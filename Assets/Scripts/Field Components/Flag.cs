using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour
{
    void OnCollisionEnter(Collision c)
    {
        AudioSource speaker = GetComponent<AudioSource>();
        if (!SoundCatalog.PlayNamed(speaker, "target_hit_light"))
            speaker.Play();
    }
}
