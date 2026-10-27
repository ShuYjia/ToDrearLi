using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bpf.Diagnostics;

public class StepTrigger : MonoBehaviour
{


    private void OnTriggerEnter(Collider other)
    {
        if (other != null) {
            BpfLog.Error(BpfLogCategory.Scene, "step.trigger_entered", null, null, BpfLogField.Of("transform", other.transform));
            TexCore_MouseCameraTarget.self.targetTransform = other.transform;
            TexCore_MouseCameraTarget.self.TargetingRaycast();
        }
    }

    private void OnTriggerExit(Collider other)
    {
          TexCore_MouseCameraTarget.self.targetTransform = null;
    }


}
