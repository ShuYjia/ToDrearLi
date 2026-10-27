using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetChildButton : MonoBehaviour
{
    private void OnEnable()
    {
        for (int i = 0; i < this.gameObject.transform.childCount; i++)
        {
            TexCore_Interaction_UGUI obj = this.gameObject.transform.GetChild(i).GetComponentInChildren<TexCore_Interaction_UGUI>();
            obj.ResetBrother();
        }
    }
}
