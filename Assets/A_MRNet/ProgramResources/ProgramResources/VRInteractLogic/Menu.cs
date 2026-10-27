using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using UnityEngine.XR;


public class Menu : MonoBehaviour
{
    //public Valve.VR.InteractionSystem.Hand hand;

    public GameObject[] child;
    public bool isActive = false;

    private void OnEnable()
    {
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新题目, UpdateActive);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.完成题目, UpdateActive);
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.点击目录, UpdateActive);
    }
    private void OnDisable()
    {
        TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.更新题目, UpdateActive);
        TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.完成题目, UpdateActive);
        TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.点击目录, UpdateActive);
    }

    public void UpdateActive(string _s)
    {
        SetActive(isActive);
    }
    public void StartSet()
    {
        SetActive(isActive);
    }
    public bool isHodel=false;
    //public IEnumerator GripDoubleClick(Valve.VR.InteractionSystem.Hand hand)
    //{
    //    isHodel = true;
    //    yield return 0;
    //    for (float i = 0; i < 0.7f; i+=Time.deltaTime)
    //    {
    //        if (hand.grabGripAction.GetStateDown(hand.handType))
    //        {
    //            isActive = !isActive;
    //            SetActive(isActive);
    //            break;
    //        }
    //        yield return 0;
    //    }
    //    yield return 0;
    //    isHodel = false;
    //}


    private void Update()
    {
        //if (hand!=null)
        //{
        //    if (hand.grabGripAction.GetStateDown(hand.handType)&&isHodel==false)//握持
        //    {
        //        StartCoroutine(GripDoubleClick(hand));
        //    }

        //    //if (hand.grabPinchAction.GetState(hand.handType) && hand.grabGripAction.GetStateDown(hand.handType))//按住扳机，再按握持
        //    //{
        //    //    isActive = !isActive;
        //    //    SetActive(isActive);
        //    //}
        //    //if (hand.grabGripAction.GetState(hand.handType)&&hand.grabPinchAction.GetStateDown(hand.handType))//按住握持，再按扳机
        //    //{
        //    //    isActive = !isActive;
        //    //    SetActive(isActive);
        //    //}

        //}
        //else
        //{
            if (Input.GetKeyDown(KeyCode.M))
            {
                isActive = !isActive;
                SetActive(isActive);
            }
        //}
        
    }

    public void SetActive(bool isActive)
    {
        for (int i = 0; i < child.Length; i++)
        {
            //child[i].SetActive(isActive);

            Image[] ims = child[i].GetComponentsInChildren<Image>();
            foreach (var item in ims)
            {
                item.enabled = isActive;
            }

            MeshRenderer[] meshs = child[i].GetComponentsInChildren<MeshRenderer>();
            foreach (var item in meshs)
            {
                item.enabled = isActive;
            }

            TextMeshProUGUI[] texts = child[i].GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var item in texts)
            {
                item.enabled = isActive;
            }

            Collider[] colls = child[i].GetComponentsInChildren<Collider>();
            foreach (var item in colls)
            {
                item.enabled = isActive;
            }
        }
    }
}
