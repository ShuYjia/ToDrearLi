using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Bpf.Diagnostics;


public class TexCore_VirtualCircular : MonoBehaviour
{
    public bool autoAddClickLisen = true;
    public TexCore_ClickDriver _parent;

    public TexCore_LinearMapping myLinear;

    public Vector3 scale = Vector3.one * 1.5f;

    public float speed = 1f;

    public Material material;

    private void Start()
    {
        if(GetComponent<TexCore_LinearMapping>())
        {
            myLinear = GetComponent<TexCore_LinearMapping>();
        }
        if (autoAddClickLisen)
        {
            //给父物体小螺丝添加点击事件
            _parent.OnClick.AddListener(() => {
                //if (gameObject.activeSelf)
                //    Close();
                //else
                Open();
            });
        }
        

        myLinear.OnTrigger.AddListener(() =>
        {
            Close();
        });
        //将自身
        MeshRenderer[] meshes = GetComponentsInChildren<MeshRenderer>();
        foreach (var mesh in meshes)
        {
            for (int i = 0; i < mesh.materials.Length; i++)
            {
                mesh.materials[i].CopyPropertiesFromMaterial(material);
            }
        }

        gameObject.SetActive(false);
    }
    

    public void Open()
    {
        gameObject.SetActive(true);
        GetComponent<Collider>().enabled = true;
        transform.DOScale(scale, speed);
        //_parent.GetComponent<Collider>().enabled = false;
    }
    public void Close()
    {
        BpfLog.Info(BpfLogCategory.Network, "tex_core_virtual_circular.close", $"Close+{gameObject.name}");
        Tween tween= transform.DOScale(0.9f, speed);
        tween.onComplete += () => { 
            gameObject.SetActive(false);
            //_parent.GetComponent<Collider>().enabled = false;
        };
    }
}
