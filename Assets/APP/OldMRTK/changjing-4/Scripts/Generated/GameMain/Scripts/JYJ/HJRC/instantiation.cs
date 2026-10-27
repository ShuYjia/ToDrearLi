using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Changjing_4.ExportRuntime
{
    public class instantiation : MonoBehaviour
    {
          
        public GameObject prefabA; // 物体A的预设 
        public Vector3 spawnPosition;
      

        // Start is called before the first frame update
        private void Start()
        {
            // 开始实例化第一个物体A  

            Instantiate(prefabA, spawnPosition, Quaternion.identity);
        }
       



    }
}
