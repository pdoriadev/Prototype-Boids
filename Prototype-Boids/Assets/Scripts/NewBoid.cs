using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class NewBoid : MonoBehaviour
{

    public struct BoidData
    {
        public NewBoid boid;
        public NewBoid aligningWithBoidDat;
        public bool isAlphaDat;

        public BoidData(NewBoid b)
        {
            boid = b;
            aligningWithBoidDat = b.BoidImAligningWith ? b.BoidImAligningWith.GetComponent<NewBoid>() : null;
            isAlphaDat = b.IsAlpha;
        }
    }

    private CircleCollider2D MyTrig;
    private Collider2D ClosestObstacle = null;
    private Rigidbody2D MyRb;
    private Rigidbody2D ClosestBoidRb;
    private NewBoid ClosestBoid;
    private NewBoid BoidImAligningWith;
    private float MaxAngleCutOff = 7.5f;

    [Header("See child game objects")]
    [SerializeField]
    private TriggerCheck TrigChecker = default;
    [SerializeField]
    private SpriteRenderer Sr = default;

    [Space]

    [SerializeField]
    private List<NewBoid> NearBoids = new List<NewBoid>();
    [SerializeField]
    private List<Collider2D> NearObstacles = new List<Collider2D>();

    [Space]
    [SerializeField]
    private float moveSpeed = default;
    [SerializeField]
    public float rotSpeed = default;
    [SerializeField]
    public float alignDistance = default, avoidDistance = default, approachDistance = default;

    [Header("Generated on Start()")]
    [SerializeField]
    private float AngleCutoff = default;
    [SerializeField]
    private bool IsAlpha = false; 


    void Awake()
    {
        MyRb = GetComponent<Rigidbody2D>();
        if (!MyRb) Debug.LogError(gameObject + " has no Rigidbody2D.");
        if (!TrigChecker) Debug.LogError(gameObject + " has no Collider2D");
        Sr = GetComponentInChildren<SpriteRenderer>();
        if (!Sr) Debug.LogError(gameObject + " has no sprite renderer among child game objects");

        TrigChecker.EnteredEvent += TriggerEnter2DCheck;
        TrigChecker.ExitedEvent += TriggerExit2DCheck;
    }

    void Start()
    {
        IsAlpha = Random.Range(0f, 1f) < 0.05f ? true : false;

        if (IsAlpha)
        {
            Color cy = Color.cyan;
            Sr.color = cy;
        }
        else 
        {
            Color orange = Color.Lerp(Color.red, Color.yellow, 0.5f);
            orange = new Color (orange.r, orange.g, orange.b, 1);
            Sr.color = orange;
        }

        AngleCutoff = Random.Range(0, MaxAngleCutOff);
    }
    void Destroy()
    {
        TrigChecker.EnteredEvent -= TriggerEnter2DCheck;
        TrigChecker.ExitedEvent -= TriggerExit2DCheck;
    }


    void FixedUpdate()
    {
        
        // Boid Dist Checks
        NewBoid alphaBoid = null;
        float closestAlphaDist = float.MaxValue;
        float closestBoidDist = float.MaxValue;
        // Check for near boids
        for (int i = 0; i < NearBoids.Count; i++)
        {
            float dist = (NearBoids[i].transform.position - transform.position).sqrMagnitude;
            if (dist < closestBoidDist)
            {
                closestBoidDist = dist;
                ClosestBoid = NearBoids[i];
            }
            ClosestBoidRb = ClosestBoid != null ? ClosestBoid.GetRigidbody2D() : null;

            BoidData bD = new BoidData(ClosestBoid);
            if (bD.isAlphaDat)
            {
                alphaBoid = ClosestBoid;
                closestAlphaDist = dist < closestAlphaDist ? dist : closestAlphaDist;
            }
        }

        // Obstacle Dist Checks
        Vector2 myPos = new Vector2 (transform.position.x, transform.position.y);
        Vector2 closestObsPoint = Vector2.zero;
        float closestObstacleDist = float.MaxValue;
        for(int i = 0; i < NearObstacles.Count; i++)
        {
            Vector2 point = NearObstacles[i].ClosestPoint(transform.position);
            float dist = (point - myPos).sqrMagnitude;
            if (dist < closestObstacleDist)
            {
                closestObstacleDist = dist;
                closestObsPoint = point;
                ClosestObstacle = NearObstacles[i];
            }
        }

        // avoiding obstacles takes priority
        if (NearObstacles.Count > 0 && ShouldAvoidObstacle(closestObstacleDist, closestObsPoint))
        {
            HandleAvoidObstacle(closestObsPoint);
        }
        else if (ClosestBoid)
        {
            if (ShouldIAvoid(closestBoidDist))
            {
                HandleAvoidingBoid();
            }
            else if (alphaBoid && ShouldIAlign(closestAlphaDist, alphaBoid))
            {
                BoidImAligningWith = alphaBoid;
                HandleAligning();   
            }
            else if (ShouldIAlign(closestBoidDist, ClosestBoid))
            {
                BoidImAligningWith = ClosestBoid;
                HandleAligning();
            }
            else if (ShouldIApproach(closestBoidDist))
            {
                HandleApproaching();
            }
        }

        MyRb.AddForce(transform.up * moveSpeed * Time.fixedDeltaTime);
    }

#region Handle Behavior Methods
    private void HandleAvoidObstacle(Vector2 point)
    {
        Vector2 myPos = transform.position;
        Vector2 myDir = transform.up;
        Vector2 meToThem = (point - myPos).normalized;
        
        float dot = (myDir.x * meToThem.x ) + (myDir.y * meToThem.y);
        float cutoff = Random.Range(-0.8f, -0.95f);

        if (dot > cutoff)
        {
            float angl = Vector2.SignedAngle(meToThem, myDir);

            float step = angl * rotSpeed * moveSpeed * 0.02f * Time.fixedDeltaTime;

            if (angl > 0)
            {
                if (step > angl)
                    step = angl;
                MyRb.rotation = MyRb.rotation + step;
            }
            else if (angl < 0)
            {
                if (-step < angl)
                    step = -angl;
                MyRb.rotation = MyRb.rotation + step;
            }   

        }
    }
    private void HandleAvoidingBoid()
    {
        Vector2 myDir = transform.up;
        Vector2 meToThem = new Vector2(ClosestBoid.transform.position.x - transform.position.x, 
                                        ClosestBoid.transform.position.y - transform.position.y);
        
        float angl = Vector2.SignedAngle(meToThem, myDir);

        float step = angl * rotSpeed * Time.deltaTime;

        
        if ((step > 0 && step + AngleCutoff > angl) || (step < 0 && step - AngleCutoff < angl ))
        {
            MyRb.rotation += angl;
        }
        else
        {
            MyRb.rotation += step;
        }

        // if (Mathf.Abs(angl) > Mathf.Abs(AngleCutoff))
        // {
        //     // if next step will exceed target angle + cutoff
        //     if (Mathf.Abs(AngleCutoff) + Mathf.Abs(step) > Mathf.Abs(angl))
        //     {
        //         MyRb.rotation += angl + AngleCutoff;
        //     }
        //     else 
        //     {
        //         MyRb.rotation += step;
        //     }
        // }
    }


    private void HandleAligning()
    {
        Vector2 myDir = transform.up;
        Vector2 theirDir = BoidImAligningWith.transform.up;

        float angl = Vector2.SignedAngle(myDir, theirDir); 
        float step = angl * rotSpeed * moveSpeed * 0.02f * Time.fixedDeltaTime;

        if ((step > 0 && step + AngleCutoff > angl) || (step < 0 && step - AngleCutoff < angl ))
        {
            MyRb.rotation += angl;
        }
        else
        {
            MyRb.rotation += step;
        } 
    }

    private void HandleApproaching()
    {
        Vector2 myDir = transform.up;
        Vector2 meToThem = new Vector2(ClosestBoid.transform.position.x - transform.position.x, 
                                        ClosestBoid.transform.position.y - transform.position.y);

        float angl = Vector2.SignedAngle (myDir, meToThem);

        float step = angl * rotSpeed * Time.fixedDeltaTime;

        if ((step > 0 && step + AngleCutoff > angl) || (step < 0 && step - AngleCutoff < angl ))
        {
            MyRb.rotation += angl;
        }
        else
        {
            MyRb.rotation += step;
        }

    }
#endregion

#region BOOL METHODS
    
    private bool ShouldAvoidObstacle(float obsDist, Vector2 point)
    {
        bool shouldAvoid = true;

        Vector2 myDir = transform.up;
        Vector2 myPos = transform.position;
        Vector2 meToThem = (point - myPos).normalized;

        shouldAvoid = shouldAvoid &&
                    obsDist < moveSpeed * 0.01f
                    ? true : false;

        return shouldAvoid;
    }

    private bool ShouldIAvoid(float closestDist)
    {
        bool shouldIAvoid = false;

        shouldIAvoid = closestDist < avoidDistance * avoidDistance;

        return shouldIAvoid;
    }

    private bool ShouldIAlign(float dist, NewBoid boidAligningWith)
    {
        bool shouldIAlign = false;

        BoidData bD = boidAligningWith.GetBoidData();
        
        // if it's not already aligning with me, I want to align. If it's an alpha, I want to align. 
        shouldIAlign = (!bD.aligningWithBoidDat == MyRb || bD.isAlphaDat ? true : false);
        shouldIAlign = dist < alignDistance * alignDistance;
        shouldIAlign = IsAlpha ? false : true;

        return shouldIAlign;
    }

    private bool ShouldIApproach(float closestDist)
    {
        bool shouldIApproach = false;

        shouldIApproach = closestDist < approachDistance * approachDistance ? true : false;

        return shouldIApproach;
    }
#endregion

    private void TriggerEnter2DCheck(Collider2D coll)
    {
        NewBoid b = coll.GetComponent<NewBoid>();
        if (b && !NearBoids.Contains(b))
        {
            NearBoids.Add(b);
        }
        else if (coll.gameObject.layer == 8)
        {
            NearObstacles.Add(coll.gameObject.GetComponent<Collider2D>());
        }
    }

    private void TriggerExit2DCheck(Collider2D coll)
    {
        NewBoid b = coll.GetComponent<NewBoid>();
        NearBoids.Remove(b);
        if (!b && coll.gameObject.layer == 8)
        {
            NearObstacles.Remove(coll.GetComponent<Collider2D>());
        }
    }


    // Getter for other boids to get the state of boids around them. 
    public BoidData GetBoidData()
    {
        BoidData dat = new BoidData(this);
        return dat;
    }

    public Rigidbody2D GetRigidbody2D()
    {
        return MyRb;
    }
}

   