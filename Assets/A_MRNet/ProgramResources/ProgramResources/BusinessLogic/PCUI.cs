using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCUI : MonoBehaviour
{
    public GameObject btn_Next;


    private void OnEnable()
    {
        btn_Next.SetActive(false);
        TexCore_EventCenter.AddListener(TexCore_EventType.显示下一步, () => { btn_Next.SetActive(true); });
    }
    private void OnDisable()
    {
        TexCore_EventCenter.RemoveListener(TexCore_EventType.显示下一步, () => { btn_Next.SetActive(true); });
    }

}
