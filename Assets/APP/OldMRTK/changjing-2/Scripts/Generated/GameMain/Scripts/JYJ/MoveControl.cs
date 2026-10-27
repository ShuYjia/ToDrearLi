using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_2.ExportRuntime
{
    public class MoveControl : MonoBehaviour
    {
        public float moveDistance = 1.0f; // the distance to move on collision  
        public Vector3 moveDirection = Vector3.up; // the direction to move on collision 
        private Rigidbody rb;
        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void OnCollisionEnter(Collision other)
        {
            // calculate the new position based on the distance and direction  
            Vector3 newPosition = rb.position + moveDistance * moveDirection;

            // set the Rigidbody's position to the new position  
            rb.MovePosition(newPosition);
        }

        // Update is called once per frame
    }
}
