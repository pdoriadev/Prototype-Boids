using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class Ball : MonoBehaviour
{
    enum BallState
    {
        STILL,
        WANDER,
        FOLLOW,
        DEAD
    }

    public float moveSpeed;
    public float followOffset;
    
    BallState State = BallState.STILL;
    Rigidbody2D rb;
    Vector2 moveDir;
    BallBoy myBallBoy;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (State == BallState.FOLLOW)
        {

            int i = myBallBoy.GetBallIndex(this);

            if (i - 1 >= 0)
            {
                Ball followBall = null;
                followBall = myBallBoy.GetBall(i);
                moveDir = new Vector2(followBall.transform.position.x - transform.position.x , followBall.transform.position.y - transform.position.y);
            }
            else if (i == 0)
            {
                moveDir = new Vector2(myBallBoy.transform.position.x - transform.position.x, myBallBoy.transform.position.y - transform.position.y);
            }
            else
                Debug.LogError("index is not equal to or greater than 0");

///TODO: GET DISTANCE FROM BALL TO BALLBOY AND DO THING THAT SLOWS MOVE SPEED CLOSER TO BALLBOY THE BALL IS.!-- 
            
            rb.AddForce(moveDir * moveSpeed);
        }


    }

    public void AssignMyBallBoy(BallBoy boy)
    {
        myBallBoy = boy;
        State = BallState.FOLLOW;
        print(gameObject.name + "is following ");
    }


}
