using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//see boids section of google doc for explanation on how boids work: https://docs.google.com/document/d/1EZdzAMjVQlY7s8be0RaXIs0J8D7uYIUL3dIqRx77Ouo/edit?usp=sharing 
public enum Collidables
{
    BOID,
    PLAYER,
    PREDATOR
    
}

// PD - 8/13/2020 - ordered by behavior priority
public enum BoidBehavior
{
    FLEE = 0,
    FOLLOWPLAYER = 1,
    AVOID = 2,
    ALIGN = 3,
    APPROACH = 4,
    WANDER = 5,
    UNDEFINED = 6
}


public class Boid : MonoBehaviour
{
#region VARS -----------------
    public List<Rigidbody2D> nearbyBoids = new List<Rigidbody2D>();
    private List<Predator> NearbyPredators = new List<Predator>();
    private BallBoy NearbyPlayer = null;
    private Collider2D myColl = null;
    private Rigidbody2D MyRb = null;

    private float MoveForce;
    private Vector2 MoveDir;
    private BoidBehavior Behavior = BoidBehavior.UNDEFINED;

    // wandering timer float
    private float WanderingTimer = 0f;
    private float WanderingResetTime = 3f;    
    private float AvoidTimer = 0f;
    private float SameBoidAvoidTimer = 0f;
    private int LastAvoidedBoid;
    private float AlignTimer = 0f;
    private float ApproachTimer = 0f;

    private float RotOnLastCalc = 0;
    private float RotAngle = 0;
    private float RotPerc = 0;


    // stores index for closest body (i.e. boid, predator) in its respective list
    private int ClosestBodyIndex = default;
    private float ClosestBodyDist = default;

    [Header("Base Speeds")]
    public float baseMoveSpeed = default;
    public float baseRotSpeed = default;
    [Header("Current Values")]
    public float rotationSpeed = default;
    [Header("Flee")]
    public float fleeSpeed = default;
    [Header("Wander")]
    public float wanderSpeed = default;
    [Header("Avoid")]
    public float avoidDistance = default;
    public float avoidSpeed = default, avoidRotSpeed = default;
    public float timeBetweenAvoids = 0.2f;
    public float timeBtwnSameBoidAvoid = 1.5f;
    [Header("Align")]
    public float alignDistance = default;
    public float alignSpeed = default, alignRotSpeed = default;
    public float timeBtwnAligns = default;
    [Header("Approach")]
    public float approachDistance = default;
    public float approachSpeed = default, approachRotSpeed = default;
    public float timeBtwnApproaches = default;



#endregion

    void Awake()
    {
        myColl = GetComponent<Collider2D>();
        if (myColl == null) Debug.LogError("No Collider2D on boid. ", gameObject);
        MyRb = GetComponent<Rigidbody2D>();
        if (MyRb == null) Debug.LogError("No Rigidbody2D on boid. ", gameObject ); 
        Time.timeScale = 0.2f;
    }

    private void FixedUpdate()
    {
        BoidBehavior LastFrameBehavior = Behavior;

        Behavior = BoidBehavior.UNDEFINED;
        AssignSoloBoidBehavior();
        if (Behavior == BoidBehavior.UNDEFINED) AssignFlockBoidBehavior();

        float ModdedSpeed = 0;
        bool ShouldCalc = false;

        // assigns movement vector and assigns 
        switch(Behavior)
        {

            case BoidBehavior.FLEE:
                print (gameObject.name + " FLEEING FROM " + NearbyPredators[ClosestBodyIndex].gameObject.name);

                // create vector going opposite direction from enemy. assign move speed value based on f
                MoveDir = new Vector2(transform.position.x - NearbyPredators[ClosestBodyIndex].transform.position.x,
                    transform.position.y - NearbyPredators[ClosestBodyIndex].transform.position.y);
                RotAngle = 0;

                break;
            case BoidBehavior.FOLLOWPLAYER:
                print (gameObject.name + " FOLLOWING PLAYER");

                ModdedSpeed = baseMoveSpeed;
                MoveDir = new Vector2(transform.position.x - NearbyPlayer.transform.position.x,
                    transform.position.y - NearbyPlayer.transform.position.y);
                // create vector toward player. make move speed = whatever value 
                RotAngle = 0;

                break;
            case BoidBehavior.WANDER:            
                print (gameObject.name + " WANDERING");
                WanderingTimer *= Time.deltaTime;
                ModdedSpeed = baseMoveSpeed * wanderSpeed;

                MoveDir = WanderingTimer == 0 ? new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)) : MoveDir;
                WanderingTimer = WanderingTimer > WanderingResetTime ? 0 : WanderingTimer;
                
                RotAngle = 0;

                break;
            case BoidBehavior.APPROACH:
                print (gameObject.name + " APPROACHING " + nearbyBoids[ClosestBodyIndex].gameObject.name);
                
                ModdedSpeed = baseMoveSpeed * approachSpeed;
                rotationSpeed = baseRotSpeed * approachRotSpeed;

                ShouldCalc = ShouldCalcNewApproach();
                if (ShouldCalc)
                    RotAngle = CalcApproachAngle();

                ApproachTimer = ApproachTimer > timeBtwnApproaches 
                    ? 0 
                    : ApproachTimer += Time.deltaTime;
                
                break;
            case BoidBehavior.AVOID:
                print (gameObject.name + " AVOIDING " + nearbyBoids[ClosestBodyIndex].gameObject.name);
                ModdedSpeed = baseMoveSpeed * avoidSpeed;
                rotationSpeed = baseRotSpeed * avoidRotSpeed;
                
                ShouldCalc = ShouldCalcNewAvoidAngle();
                if (ShouldCalc)
                {
                    RotAngle = CalcAvoidAngle();
                }

                // increment timers
                AvoidTimer = AvoidTimer > timeBetweenAvoids 
                    ? 0 
                    : AvoidTimer += Time.deltaTime;
                SameBoidAvoidTimer = SameBoidAvoidTimer > timeBtwnSameBoidAvoid || LastAvoidedBoid != ClosestBodyIndex
                    ? 0 
                    : SameBoidAvoidTimer += Time.deltaTime;

                LastAvoidedBoid = ClosestBodyIndex;

                // #TODO: MAKE BOIDS AVOID WALLS

                break;
            case BoidBehavior.ALIGN:
                print (gameObject.name + " ALIGNING " + nearbyBoids[ClosestBodyIndex].gameObject.name);

                ModdedSpeed = baseMoveSpeed * alignSpeed;
                rotationSpeed = baseRotSpeed * alignRotSpeed;

                ShouldCalc = ShouldCalcAlignAngle(LastFrameBehavior == BoidBehavior.ALIGN);
                if (ShouldCalc)
                {
                    RotAngle = CalcAlignAngle(); 
                    RotOnLastCalc = MyRb.rotation;                  
                }

                AlignTimer = AlignTimer > timeBetweenAvoids 
                    ? 0 
                    : AlignTimer += Time.deltaTime;

                break;
            
        }

        // if (ShouldCalc)
        // {
        //     MyRb.AddTorque((MyRb.rotation + RotAngle) * rotationSpeed * Time.deltaTime, ForceMode2D.Impulse);
        //     print(gameObject.name + " ADDED TORQUE");
        // }


        if (RotPerc < 1 
            &&  (Behavior == BoidBehavior.AVOID
            || (Behavior == BoidBehavior.ALIGN && !IsAligned() )
            || Behavior == BoidBehavior.APPROACH))
        {
            RotPerc += rotationSpeed - (1 / Mathf.Abs(RotAngle));
            MyRb.MoveRotation(MyRb.rotation + RotAngle * RotPerc * Time.fixedDeltaTime);
        }
        else if (RotPerc > 1)
        {
            RotPerc = 0;
        }
        
        MyRb.AddForce(transform.up * ModdedSpeed);


        // reset timers on exit state
        WanderingTimer = (LastFrameBehavior == BoidBehavior.WANDER && Behavior != BoidBehavior.WANDER) 
            ? 0 : WanderingTimer;
        AvoidTimer = (LastFrameBehavior == BoidBehavior.AVOID && Behavior != BoidBehavior.AVOID)
            ? 0 : AvoidTimer;
        if (SameBoidAvoidTimer > timeBtwnSameBoidAvoid) 
            SameBoidAvoidTimer = 0;
        AlignTimer = (LastFrameBehavior == BoidBehavior.ALIGN && Behavior != BoidBehavior.ALIGN)
            ? 0 : AlignTimer;
        
        
    }

#region BEHAVIOR_CHECKS -------------
    void AssignSoloBoidBehavior()
    {
        Behavior = nearbyBoids.Count == 0 ? BoidBehavior.WANDER : Behavior;      
        Behavior = NearbyPlayer != null ? BoidBehavior.FOLLOWPLAYER: Behavior;
        Behavior = NearbyPredators.Count > 0 ? BoidBehavior.FLEE : Behavior;  
    }

    // check distance to nearest boid
    // too close --> avoid: fly away from nearest boid
    // close --> align: fly along with nearest boid
    // just close enough to see another boid --> approach: fly towards nearest boid
    void AssignFlockBoidBehavior()
    {
        ClosestBodyIndex = 0;
        ClosestBodyDist = 1000000000;
        // finds nearest boid, it's index, and distance from this boid
        for (int i = 0; i < nearbyBoids.Count; i++)
        {
            float dist = Vector2.Distance(transform.position, nearbyBoids[i].transform.position);
            if (ClosestBodyDist > dist)
            {
                ClosestBodyDist = dist;
                ClosestBodyIndex = i;
            }              
        }
        Behavior = (ClosestBodyDist < avoidDistance )? BoidBehavior.AVOID : BoidBehavior.ALIGN ;
        Behavior = ClosestBodyDist > alignDistance ? BoidBehavior.APPROACH : Behavior;
    }
#endregion

#region BEHAVIOR_FUNCTIONS ------------


    private bool ShouldCalcNewAvoidAngle()
    {
        bool ShouldCalc = false;
        // if avoid timer isn't up, don't want to calculate new turn. 
        // if new boid, want to calculate new turn.
        // if same boid, just wanna keep turning.
        ShouldCalc = AvoidTimer  == 0 ? true : false ;
        ShouldCalc = SameBoidAvoidTimer == 0 ? true : false;

        return ShouldCalc;        
    }
    // find degrees to rotate away from nearest boid   
    private float CalcAvoidAngle()
    {
        print(gameObject.name + " avoiding : " + nearbyBoids[ClosestBodyIndex].gameObject.name);

        Vector3 MyDir = MyRb.velocity.normalized;
        Vector3 MeToThem = new Vector3(nearbyBoids[ClosestBodyIndex].position.x - MyRb.position.x,
                                        nearbyBoids[ClosestBodyIndex].position.y - MyRb.position.y,
                                        0)
                                        .normalized;

        float DotForward = (MyDir.x * MeToThem.x) + (MyDir.y * MeToThem.y);
        float Angl = Mathf.Acos(DotForward) * Mathf.Rad2Deg;

        // check which side other boid is on. If positive, other boid is to right of me
        float DotRightSide = (transform.right.normalized.x * MeToThem.x) + (transform.right.normalized.y * MeToThem.y);
        DotRightSide = ConvertToSimpleDot(DotRightSide);

        print(gameObject.name + "final angle = " + Angl * DotRightSide + 
            ": angl = " + Angl  + " dotRightSide: " + DotRightSide);

        return Angl * DotRightSide;
    }

    private bool ShouldCalcAlignAngle(bool HasBeenAligning)
    {
        bool ShouldCalc = false;
        ShouldCalc = !IsAligned() && AlignTimer == 0
            ? true 
            : false;

        return ShouldCalc;
    }

    private bool IsAligned()
    {
        Vector3 MyVel = MyRb.velocity.normalized;
        Vector3 TheirVel = nearbyBoids[ClosestBodyIndex].velocity.normalized;
        // is already aligned with boid
        float Dot = Mathf.Clamp((MyVel.x * TheirVel.x) + (MyVel.y * TheirVel.y), -1, 1);
        float Angl = Mathf.Acos(Dot) * Mathf.Rad2Deg;      

        return Angl > 5;  
    }

    private float CalcAlignAngle()
    {
        Vector3 MyVel = MyRb.velocity.normalized;
        Vector3 TheirVel = nearbyBoids[ClosestBodyIndex].velocity.normalized;
        Vector3 TheirPos = nearbyBoids[ClosestBodyIndex].position;
        Vector3 MeToThem = new Vector3(TheirPos.x - transform.position.x, 
                                        TheirPos.y - transform.position.y,
                                        0).normalized;

        float DotForward = Mathf.Clamp((MyVel.x * TheirVel.x) + (MyVel.y * TheirVel.y), -1, 1);
        float Angl = Mathf.Acos(DotForward) * Mathf.Rad2Deg;

        float DotRightSide = (transform.right.normalized.x * MeToThem.x) + (transform.right.normalized.y * MeToThem.y);
        DotRightSide = ConvertToSimpleDot(DotRightSide);
        
        print(gameObject.name + " ALIGN CALC: Final Angle = " + Angl * -DotRightSide + 
            ": Angl = " + Angl  + ", DotRightSide = " + DotRightSide 
            + ", dotForward = " + DotForward);

        return Angl * -DotRightSide ; 
    }

    private bool ShouldCalcNewApproach()
    {
        bool ShouldCalc = false;

        ShouldCalc = ApproachTimer == 0 
            ? true 
            : false;

        // if (Vector3.Angle(MyRb.velocity.normalized ,
        //      (nearbyBoids[ClosestBodyIndex].position - MyRb.position).normalized) < 3f)
        //         ShouldCalc = false;

        return ShouldCalc;
    }

    private float CalcApproachAngle()
    {
        Vector3 MyDir = MyRb.velocity.normalized;
        Vector3 MeToThem = new Vector3(nearbyBoids[ClosestBodyIndex].position.x - transform.position.x,
            nearbyBoids[ClosestBodyIndex].position.y - transform.position.y,
            0).normalized;

        float DotForward = MyDir.x * MeToThem.x + MyDir.y * MeToThem.y;
        float Angl = Mathf.Acos(Mathf.Clamp(DotForward, -1, 1)) * Mathf.Rad2Deg;

        // signed angle
        float DotRightSide = transform.right.normalized.x * MeToThem.x + transform.right.normalized.y * MeToThem.y;
        DotRightSide = ConvertToSimpleDot(DotRightSide);

        print(gameObject.name + "final angle = " + Angl * -DotRightSide + 
            ": angl = " + Angl  + ", dotRightSide = " + DotRightSide 
            + ", dotForward = " + DotForward);

        return Angl * -DotRightSide ;
    }

#endregion

#region OTHER_HELPFUL_FUNCS -----------

    // Constrains dot product to -1, 0, or 1. 
    //      Positive --> returns 1. Negative --> returns -1. Else --> returns 0
    private float ConvertToSimpleDot(float dot)
    {
        dot = 
            dot > 0 
                ? 1 : 
            dot < 0 
                ? -1 
                : 0;
        return dot; 
    }

#endregion


#region CHECK_FOR_NEARBY_BODIES ------------
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.GetComponent<Boid>())
        {
            nearbyBoids.Add(collider.GetComponent<Rigidbody2D>());
        }
        else if (collider.GetComponent<BallBoy>())
        {
            NearbyPlayer = collider.GetComponent<BallBoy>();
            // swims into player. Makes player bigger --> Zooms out camera
        }
        else if (collider.GetComponent<Predator>())
        {
            NearbyPredators.Add(collider.GetComponent<Predator>());
        }
    }
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.GetComponent<Boid>())
        {
            nearbyBoids.Remove(collider.GetComponent<Rigidbody2D>());
        }
        else if(collider.GetComponent<BallBoy>())
        {
            NearbyPlayer = null;
        }
        else if (collider.GetComponent<Predator>())
        {
            NearbyPredators.Add(collider.GetComponent<Predator>());
        }
    }
#endregion

}
