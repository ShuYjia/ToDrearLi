using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetWork03_2.ExportRuntime
{
    public class UILine : MonoBehaviour
    {
        
        public GameObject go;
        public float ropeWidth = 0.1f;
        public float ropeendwidth = 0.5f;
        private LineRenderer lineRenderer;
        public Material material;

        private void Start()
        {
            gameObject.AddComponent<LineRenderer>();
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeendwidth;
            lineRenderer.material = material;
        }
        void Update()
        {
            if(gameObject.activeSelf)
            {
                lineRenderer.startWidth = ropeWidth;
                lineRenderer.endWidth = ropeendwidth;
                lineRenderer.material = material;
                lineRenderer.SetPosition(0, gameObject.transform.position);
                lineRenderer.SetPosition(1, go.transform.position);

            }
        }
    }
}
