using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HuaXian.ExportRuntime
{
    public class RotationSwitch : MonoBehaviour
    {
        public Transform SwitchTransform;
        public GameObject Ray;

        // Update is called once per frame
        void Update()
        {
            CheckRotationAndToggleRay();
        }

        public void CheckRotationAndToggleRay()
        {
            if (SwitchTransform.rotation.z >= 0.5f)
            {
                Ray.SetActive(true);
            }
            else if (SwitchTransform.rotation.z <= -0.2f)
            {
                Ray.SetActive(false);
            }
        }
    }
}
