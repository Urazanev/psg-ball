using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Movement : MonoBehaviour
{
    // Same-scene "singleton" pattern 
    private static Movement _instance;
    public static Movement instance
    {
        get
        {
            if (!_instance)
                _instance = FindObjectOfType<Movement>();
            return _instance;
        }
    }
    
    [Header("Flippers")]
    [SerializeField]
    HingeJoint LeftFlipper;
    
    [SerializeField]
    HingeJoint RightFlipper;

    public int FlipperMotorVelocity;
    public int FlipperMotorForce;

    [Header("Shaking/Tilting")]
    [SerializeField]
    float ShakingForce;

    [Header("Plunger")]
    [SerializeField, Range(0, 50)]
    byte MaxForce;
    
    [SerializeField, Range(0, 50)]
    byte MinForce;

    [SerializeField]
    float IncreasingFactor;

    [HideInInspector]
    public int TiltChance = 5;

    float force;
    bool activated;

    void Update()
    {
        if(Player.instance.Lives < 0) return;

        // Launching mechanism
        if (InputAdapter.PlungerPressedThisFrame())
            Plunger.instance.Retract();

        if (InputAdapter.PlungerHeld())
            AccumulateForce();

        if (InputAdapter.PlungerReleasedThisFrame())
            ReleaseForce();

        if(Player.Tilt) return;

        // Right flipper
        if (InputAdapter.RightFlipperPressedThisFrame())
            RightFlipper.GetComponent<AudioSource>().Play();

        if (InputAdapter.RightFlipperHeld())
            RightFlipper.motor = RotateFlipper(FlipperMotorVelocity, FlipperMotorForce);

        if (InputAdapter.RightFlipperReleasedThisFrame())
            RightFlipper.motor = RotateFlipper(-FlipperMotorVelocity, FlipperMotorForce);
        
        // Left flipper
        if (InputAdapter.LeftFlipperPressedThisFrame())
            LeftFlipper.GetComponent<AudioSource>().Play();

        if (InputAdapter.LeftFlipperHeld())
            LeftFlipper.motor = RotateFlipper(-FlipperMotorVelocity, FlipperMotorForce);

        if (InputAdapter.LeftFlipperReleasedThisFrame())
            LeftFlipper.motor = RotateFlipper(FlipperMotorVelocity, FlipperMotorForce);

        // Tilting activation/Shaking mechanism
        if (InputAdapter.NudgeLeftPressedThisFrame())
            Boost(Vector3.left, ShakingForce);

        if (InputAdapter.NudgeRightPressedThisFrame())
            Boost(Vector3.right, ShakingForce);

        // Powerup
        if (InputAdapter.UseItemPressedThisFrame())
            Inventory.UseItem();
    }

    JointMotor RotateFlipper(float velocity, float force)
    {
        JointMotor jointMotor = new JointMotor();
        jointMotor.force = force;
        jointMotor.targetVelocity = velocity;
        return jointMotor;
    }

    void AccumulateForce()
    {
        if(!activated)
        {
            force += IncreasingFactor;

            if(force >= MaxForce)
            {
                Plunger.instance.Fail();
                activated = true;
                force *= Random.Range(0.7f, 0.5f);
            }
        }
    }

    void Boost(Vector3 direction, float force)
    {
        // Applies the force to the ball(s)
        foreach(Rigidbody rb in Field.instance.BallsInField)
            rb.AddForce(force*direction);

        // Randomly (20%) decides to activate the machine TILT
        if(Random.Range(0, TiltChance) == 0)
        {
            Player.Tilt = true;

            // Plays tilt sound
            Field.instance.TiltSound();
        }
        else
        {
            // Plays successful boost sound
            Field.instance.BoostSound();
        }
    }

    void ReleaseForce()
    {
        Plunger.instance.Release();

        foreach(Rigidbody rb in Plunger.instance.ObjectsInSpring)
            rb.AddForce(force*Vector3.forward);

        force = MinForce;
        activated = false;
    }
}
