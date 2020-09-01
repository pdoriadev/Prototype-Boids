using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Runs around. Controlled by player
public class BallBoy : MonoBehaviour
{
    public List<Ball> ballFollowers = new List<Ball>();
    public float moveForce;

    Rigidbody2D rb;
    Vector2 moveDir;
    float xDir;
    float yDir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Gets movement vector
        xDir = Input.GetAxisRaw("Horizontal");
        yDir = Input.GetAxisRaw("Vertical");
        moveDir = new Vector2(xDir, yDir);
    }

    private void FixedUpdate()
    {
        rb.AddForce(moveDir * moveForce);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Ball ball = collision.gameObject.GetComponent<Ball>();
        if (ball && !ballFollowers.Contains(ball))
        {
            ballFollowers.Add(ball);
            ball.AssignMyBallBoy(this);
            print("new ballboy");
        }
        
    }

    public int GetBallIndex(Ball ball)
    {
        int index = 0;
        if (ballFollowers.Contains(ball))
        {
            index = ballFollowers.IndexOf(ball);
        }
        else Debug.LogError("Lil ball isn't in this ballboy's list. Will return false index.");

        return index;
    }

    public Ball GetBall(int index)
    {
        Ball b = ballFollowers[index];
        if (!b)
            Debug.LogError("Error! No ball here!");

        return b;
    }
}
