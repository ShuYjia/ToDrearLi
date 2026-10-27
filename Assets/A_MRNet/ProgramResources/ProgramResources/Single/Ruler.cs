using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Ruler : MonoBehaviour
{
    public static Ruler self;

    public GameObject model;
    public LineRenderer line;
    public TextMeshProUGUI text;

    public Transform startPoint;
    public Transform endPoint;
    public bool isEnable = false;
    private void Awake()
    {
        self = this;
    }
    private void OnEnable()
    {
        model.SetActive(isEnable);
        if (text != null) 
            text.transform.parent.gameObject.SetActive(isEnable);
        line.gameObject.SetActive(isEnable);
    }
    private void OnDisable()
    {
        
    }

    public void Init(Transform start)
    {
        startPoint = start;
    }

    public void SetEnable(bool isEnable)
    {
        this.isEnable = isEnable;
        //text.transform.parent.gameObject.SetActive(isEnable);
        line.gameObject.SetActive(isEnable);
        model.SetActive(isEnable);
    }

    private void Update()
    {
        if (isEnable)
        {
            line.SetPosition(0, startPoint.position);
            line.SetPosition(1, endPoint.position);
            //string s = Vector3.Distance(startPoint.position, endPoint.position).ToString("F3");
            //text.text =s.Substring(0,s.Length-2)+ "m";
        }
    }
}
