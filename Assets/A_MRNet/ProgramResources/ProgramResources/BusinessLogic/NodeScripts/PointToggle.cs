//using HighlightingSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointToggle : MonoBehaviour
{
    public TexCore_ObjSelect[] obj;
    // Start is called before the first frame update
    void Start()
    {
        obj = transform.GetComponentsInChildren<TexCore_ObjSelect>();
    }
    void Update()
    {
        TriggerObj();//确定起始点和终止点
    }
    void TriggerObj()
    {
        int count = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (obj[i].isSelect == true)
            {
                count++;
                obj[i].GetComponent<BoxCollider>().enabled = false;
                
            }
        }
        if (count == transform.childCount - 1)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                //transform.GetComponentInParent<NodePanelBase>().selfObjs.GetProperty("切换点位").SetActive(true);
                //Destroy(obj[i].GetComponent<Highlighter>());
            }

        }
    }

}
