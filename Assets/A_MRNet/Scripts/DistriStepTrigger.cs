using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistriStepTrigger : MonoBehaviour
{
    public  TexCore_InteracterFunction texCore_InteracterFunction;

    private void Start()
    {
        texCore_InteracterFunction = GetComponent<TexCore_InteracterFunction>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other != null)
        {
            texCore_InteracterFunction.OnTriggerFunc.transformTarget = other.transform;
            texCore_InteracterFunction.OnFinishFunc.transformTarget = other.transform;
            TexCore_MouseCameraTarget.self.targetTransform = other.transform;
            TexCore_MouseCameraTarget.self.TargetingRaycast();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TexCore_MouseCameraTarget.self.targetTransform = null;
        texCore_InteracterFunction.OnTriggerFunc.transformTarget = null;
        texCore_InteracterFunction.OnFinishFunc.transformTarget = null;
    }



}
