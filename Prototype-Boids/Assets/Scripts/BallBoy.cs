using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Runs around. Controlled by player
public class BallBoy : MonoBehaviour
{
    [SerializeField]
    private float MoveForce = default;
    [SerializeField]
    private float RotSpeed = default;


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
        rb.AddForce(moveDir * MoveForce);
        
        float angl = Vector2.SignedAngle(transform.up, moveDir);
        float step = angl * RotSpeed * Time.fixedDeltaTime;
        
        if (angl > 0)
        {
            if (step > angl)
            {
                step = angl;
            }
        }
        else if (angl < 0)
        {
            if (-step < angl)
            {
                step = -angl;
            }
        }
        
        rb.rotation += step;
    }
}
