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

    private float RotAngle = 0;


    // stores index for closest body (i.e. boid, predator) in its respective list
    private int ClosestBodyIndex = default;
    private float ClosestBodyDist = default;

    [Header("Defaults")]
    public float defaultMoveSpeed = default;
    public float defaultRotSpeed = default;
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
    public float TimeBtwnSameBoidAvoid = 1.5f;
    [Header("Align")]
    public float alignDistance = default;
    public float timeBtwnAligns = default;
    [Header("Approach")]
    public float approachDistance = default;
    public float approachSpeed = default;



#endregion

    void Awake()
    {
        myColl = GetComponent<Collider2D>();
        if (myColl == null) Debug.LogError("No Collider2D on boid. ", gameObject);
        MyRb = GetComponent<Rigidbody2D>();
        if (MyRb == null) Debug.LogError("No Rigidbody2D on boid. ", gameObject ); 
        
    }

    private void FixedUpdate()
    {
        BoidBehavior LastFrameBehavior = Behavior;

        Behavior = BoidBehavior.UNDEFINED;
        AssignSoloBoidBehavior();
        if (Behavior == BoidBehavior.UNDEFINED) AssignFlockBoidBehavior();

        float ModdedSpeed = 0;
 
        // assigns movement vector and assigns 
        switch(Behavior)
        {

            case BoidBehavior.FLEE:
                // create vector going opposite direction from enemy. assign move speed value based on f
                MoveDir = new Vector2(transform.position.x - NearbyPredators[ClosestBodyIndex].transform.position.x,
                    transform.position.y - NearbyPredators[ClosestBodyIndex].transform.position.y);
                RotAngle = 0;

                break;
            case BoidBehavior.FOLLOWPLAYER:
                ModdedSpeed = defaultMoveSpeed;
                MoveDir = new Vector2(transform.position.x - NearbyPlayer.transform.position.x,
                    transform.position.y - NearbyPlayer.transform.position.y);
                // create vector toward player. make move speed = whatever value 
                print ("follow player");
                RotAngle = 0;

                break;
            case BoidBehavior.WANDER:            
                WanderingTimer *= Time.deltaTime;
                ModdedSpeed = defaultMoveSpeed * wanderSpeed;

                MoveDir = WanderingTimer == 0 ? new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)) : MoveDir;
                WanderingTimer = WanderingTimer > WanderingResetTime ? 0 : WanderingTimer;
                print("wander");
                
                RotAngle = 0;

                break;
            case BoidBehavior.APPROACH:
                 ModdedSpeed = defaultMoveSpeed * approachSpeed;
                 MoveDir = new Vector2(nearbyBoids[ClosestBodyIndex].transform.position.x - transform.position.x,
                    nearbyBoids[ClosestBodyIndex].transform.position.y - transform.position.y);

                print("approaching");
                // create v
                RotAngle = 0;
                
                break;
            case BoidBehavior.AVOID:
                print ("avoiding");
                ModdedSpeed = defaultMoveSpeed * avoidSpeed;
                rotationSpeed *= avoidRotSpeed;

                if (ShouldCalcAvoidAngle())
                    RotAngle = CalcAvoidAngle();

                // increment timers
                AvoidTimer = AvoidTimer > timeBetweenAvoids 
                    ? 0 
                    : AvoidTimer += Time.deltaTime;
                SameBoidAvoidTimer = SameBoidAvoidTimer > TimeBtwnSameBoidAvoid || LastAvoidedBoid != ClosestBodyIndex
                    ? 0 
                    : SameBoidAvoidTimer += Time.deltaTime;

                print("rotAngle: " + RotAngle);

                LastAvoidedBoid = ClosestBodyIndex;

                // #TODO: MAKE BOIDS AVOID WALLS

                break;
            case BoidBehavior.ALIGN:
                print ("Align");

                ModdedSpeed = defaultMoveSpeed;
                rotationSpeed = defaultRotSpeed;
                if (ShouldCalcAlignAngle())
                    RotAngle = CalcAlignAngle();

                AlignTimer = AlignTimer > timeBetweenAvoids ? 0 : AlignTimer += Time.deltaTime;

                break;
            
        }

 
        MyRb.MoveRotation(MyRb.rotation + RotAngle * rotationSpeed * Time.fixedDeltaTime);

        MyRb.AddForce(transform.up * ModdedSpeed);

        // reset timers on exit state
        WanderingTimer = (LastFrameBehavior == BoidBehavior.WANDER && Behavior != BoidBehavior.WANDER) 
            ? 0 : WanderingTimer;
        AvoidTimer = (LastFrameBehavior == BoidBehavior.AVOID && Behavior != BoidBehavior.AVOID)
            ? 0 : AvoidTimer;
        if (SameBoidAvoidTimer > TimeBtwnSameBoidAvoid) 
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


    private bool ShouldCalcAvoidAngle()
    {
        bool ShouldCalc = false;
        // if avoid timer isn't up, don't want to calculate new turn. 
        // if new boid, want to calculate new turn.
        // if same boid, just wanna keep turning.
        ShouldCalc = AvoidTimer > timeBetweenAvoids ? true : false ;
        ShouldCalc = SameBoidAvoidTimer == 0 ? true : false;

        return ShouldCalc;        
    }
    // find degrees to rotate away from nearest boid   
    private float CalcAvoidAngle()
    {
        print(gameObject.name + " avoiding : " + nearbyBoids[ClosestBodyIndex].gameObject.name);

        Vector3 myDir = MyRb.velocity.normalized;
        Vector3 meToThem = new Vector3(nearbyBoids[ClosestBodyIndex].position.x - MyRb.position.x,
                                        nearbyBoids[ClosestBodyIndex].position.y - MyRb.position.y,
                                        0)
                                        .normalized;

        float dot = (myDir.x * meToThem.x) + (myDir.y * meToThem.y);
        float angl = Mathf.Acos(dot) * Mathf.Rad2Deg;

        // check which side other boid is on. If positive, other boid is to right of me
        float dotRightSide = (transform.right.normalized.x * meToThem.x) + (transform.right.normalized.y * meToThem.y);
        dotRightSide = dotRightSide > 0 ? 1 
            : dotRightSide < 0 ? -1 
            : 0;

        print(gameObject.name + "final angle = " + angl * dotRightSide + 
            ": angl = " + angl  + " dotRightSide: " + dotRightSide);

        return angl * dotRightSide;
    }

    private bool ShouldCalcAlignAngle()
    {
        bool ShouldCalc = false;

        ShouldCalc = AlignTimer == 0 
            ? true 
            : false;

        // is already aligned with boid
        ShouldCalc = Vector3.Angle(MyRb.velocity.normalized ,nearbyBoids[ClosestBodyIndex].velocity.normalized) > 1 
            ? true
            : false;


        return ShouldCalc;
    }

    private float CalcAlignAngle()
    {
        print(gameObject.name + " aligning with : " + nearbyBoids[ClosestBodyIndex].gameObject.name);

        Vector3 myVel = MyRb.velocity.normalized;
        Vector3 theirVel = nearbyBoids[ClosestBodyIndex].velocity.normalized;
        Vector3 theirPos = nearbyBoids[ClosestBodyIndex].position;
        Vector3 meToThem = new Vector3(theirPos.x - transform.position.x, 
                                        theirPos.y - transform.position.y,
                                        0).normalized;

        float dotForward = (myVel.x * theirVel.x) + (myVel.y * theirVel.y);
        float angl = Mathf.Acos(dotForward) * Mathf.Rad2Deg;

        float dotRightSide = (transform.right.normalized.x * meToThem.x) + (transform.right.normalized.y * meToThem.y);
        dotRightSide = dotRightSide > 0 ? 1 
            : dotRightSide < 0 ? -1 
            : 0;
        
        print(gameObject.name + "final angle = " + angl * -dotRightSide + 
            ": angl = " + angl  + ", dotRightSide = " + dotRightSide 
            + ", dotForward = " + dotForward);

        return angl * -dotRightSide ;
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
