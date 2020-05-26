using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    public float rotationSpeed;
    public Rigidbody2D rb;

    private void OnEnable()
    {
        rb.angularVelocity = rotationSpeed;
    }
}
