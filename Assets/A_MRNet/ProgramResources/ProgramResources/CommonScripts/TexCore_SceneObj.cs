using System.Collections;
using System.Collections.Generic;
using Unity;
using UnityEngine;

public class TexCore_SceneObj : MonoBehaviour
{
    public static TexCore_SceneObj self;
    public IProperties objs;

    private void Awake()
    {
        if (self != null) Destroy(self.gameObject);
        self = this;
    }

}
