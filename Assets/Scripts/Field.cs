using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field : MonoBehaviour
{
    // Same-scene "singleton" pattern
    private static Field _instance;
    public static Field instance
    {
        get
        {
            if (!_instance)
                _instance = FindObjectOfType<Field>();
            return _instance;
        }
    }

    public Transform Spawnpoint;

    [Header("Sounds")]
    [SerializeField]
    AudioClip Death;

    [SerializeField]
    AudioClip Boost;

    [SerializeField]
    AudioClip Tilt;

    [SerializeField]
    AudioClip Powerup;

    AudioSource speaker;

    void Awake()
    {
        speaker = GetComponent<AudioSource>();
        OverrideClipIfFound(ref Death, "drain_loss");
        OverrideClipIfFound(ref Boost, "nudge_success");
        OverrideClipIfFound(ref Tilt, "tilt_alarm_short");
        OverrideClipIfFound(ref Powerup, "powerup_activate_arcade");
    }

    void OverrideClipIfFound(ref AudioClip slot, string clipName)
    {
        AudioClip clip = SoundCatalog.Get(clipName);
        if (clip)
            slot = clip;
    }

    public bool HasBall
    {
        get => BallsInField.Count > 0;
    }

    public bool StationaryBalls
    {
        get
        {
            foreach(Rigidbody ball in BallsInField)
                if(ball.linearVelocity.magnitude > 0.1)
                    return false;
            
            return true;
        }
    }

    [HideInInspector]
    public List<Rigidbody> BallsInField = new List<Rigidbody>();

    public void PowerupSound() => 
        speaker?.PlayOneShot(Powerup);

    public void TiltSound() => 
        speaker?.PlayOneShot(Tilt);

    public void BoostSound() => 
        speaker?.PlayOneShot(Boost);

    public void EliminateBalls()
    {
        for (int i = BallsInField.Count - 1; i >= 0; i--)
        {
            Rigidbody rb = BallsInField[i];
            BallsInField.RemoveAt(i);
            GameObject.Destroy(rb.gameObject);
        }

        Plunger.instance.ObjectsInSpring = new List<Rigidbody>();
    }

    void OnTriggerEnter(Collider c)
    {
        if (c.tag == "Ball")
            BallsInField.Add(c.GetComponent<Rigidbody>());
    }

    void OnTriggerExit(Collider c)
    {
        if (c.tag == "Ball")
        {
            BallsInField.Remove(c.GetComponent<Rigidbody>());
            Destroy(c.gameObject);

            // Plays death sound
            speaker?.PlayOneShot(Death);
        }
    }
}
