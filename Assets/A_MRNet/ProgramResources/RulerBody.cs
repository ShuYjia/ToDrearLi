using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RulerBody : MonoBehaviour
{
    public TextMeshPro text;

    public Transform head;

    [SerializeField]
    public LineRenderer line;

    public bool realDis = false;

    private void OnEnable()
    {
        line = GetComponentInChildren<LineRenderer>();
        line.positionCount = 2;
    }

    private void Update()
    {
        if (realDis)
        {
            Distance();
        }
        if(line.positionCount==2)
        {
            line.SetPosition(1, transform.position);
            line.SetPosition(0, head.transform.position);
        }
    }

    public void Distance()
    {
        if (text!=null&&head!=null)
        {
            text.text = Vector3.Distance(transform.position, head.transform.position).ToString("0.00");
            //Ruler.self.text.transform.parent.gameObject.SetActive(true);
            //Ruler.self.text.text= Vector3.Distance(transform.position, head.transform.position).ToString("0.00") + "m";
        }
    }

    private void OnDisable()
    {
        line.positionCount = 0;
    }
}
