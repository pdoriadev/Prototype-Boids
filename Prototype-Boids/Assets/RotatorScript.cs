using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatorScript : MonoBehaviour
{
    public float rotationSpeed;

    void FixedUpdate()
    {
        transform.eulerAngles += new Vector3(0, 0, rotationSpeed);
    }
}
