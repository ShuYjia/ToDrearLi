//using HighlightingSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectToggle : MonoBehaviour
{
    public TexCore_ObjSelect[] obj;

    public RulerAnim ruler;

    public Transform parentObj;

    public bool isOn;

    // Start is called before the first frame update
    void Start()
    {
        obj = transform.GetComponentsInChildren<TexCore_ObjSelect>();
        parentObj = transform.parent;
        ruler = transform.GetChild(2).GetComponent<RulerAnim>();
    }
    void Update()
    {
        TriggerObj();//确定起始点和终止点
    }

    

    void TriggerObj()
    {
        for (int i = 0; i < obj.Length-1; i++)
        {
            if (obj[i].isSelect)
            {
                //obj[i].GetComponent<Collider>().enabled = false;
                if (obj[0].isSelect && obj[1].isSelect)
                {
                    ruler.gameObject.SetActive(true);
                    //Destroy(obj[i].GetComponent<Highlighter>());
                }
                else
                {
                    ruler.gameObject.SetActive(false);
                }
            }
            else
            {
                ruler.gameObject.SetActive(false);
            }
        }
    }

}
