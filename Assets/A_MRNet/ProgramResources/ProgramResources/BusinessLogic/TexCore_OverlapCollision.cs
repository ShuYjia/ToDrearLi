using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexCore_OverlapCollision : MonoBehaviour
{
    public LayerMask targetLayer;
    public Transform sphere;
    public float radius;

    public Collider[] colliders;

    private void Start()
    {
        radius = sphere.localScale.x;
    }

    private void FixedUpdate()
    {
        colliders= Physics.OverlapSphere(sphere.position, radius, targetLayer);
    }
}
