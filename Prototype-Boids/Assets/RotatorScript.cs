using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotatorScript : MonoBehaviour
{
    public float rotationSpeed;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!rb) Debug.LogError(gameObject + " has no Rigidbody2D");
    }

    void FixedUpdate()
    {
        rb.rotation += rotationSpeed * Time.fixedDeltaTime;
    }
}
