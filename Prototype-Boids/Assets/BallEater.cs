using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BallEater : MonoBehaviour
{
    public GameObject ballBoy;
    public float moveSpeed;

    Rigidbody2D Rb;
    Transform BBTForm;
    Vector2 MoveDir;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        BBTForm = ballBoy.transform;
    }

    private void FixedUpdate()
    {
        MoveDir = new Vector2(BBTForm.position.x - transform.position.x, BBTForm.position.y - transform.position.y );
        Rb.AddForce(MoveDir * moveSpeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
      

    }
}
