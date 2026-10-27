using System.Collections;
using Bpf.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TexCore_SetNavigation : MonoBehaviour
{
    public NavMeshAgent nav;
    public Animator anim;
    public CharacterController character;

    public bool isRun = false;
    public Transform target;

    public GameObject nomalCamera;

    public GameObject nomalModel;
    public GameObject backModel;
    private void Start()
    {
        nav = GetComponent<NavMeshAgent>();
        anim = backModel.GetComponentInChildren<Animator>();
        character = GetComponent<CharacterController>();
    }

    public void SetNavTarget(Transform target)
    {
        nav.enabled = true;
        nav.Warp(transform.position);
        //nomalCamera.gameObject.SetActive(false);
        nomalModel.SetActive(false);
        backModel.SetActive(true);
        character.enabled = false;
        if(nav.isOnNavMesh)
        {
            
            if (nav.SetDestination(target.position))
            {
                BpfLog.Warning(BpfLogCategory.Scene, "tex_core_set_navigation.set_nav_target", "nav destination accepted", null, BpfLogField.Of("target", target.position));
                isRun = true;
                this.target = target;
                anim.SetBool("back", true);
                TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.控制角色移动, false);
            }
        }
        else
        {
            nav.Warp(transform.position);
        }
        

    }
    private void Update()
    {
        if (isRun)
        {
            if (Vector3.Distance(transform.position, target.position) < 0.8f)
            {
                TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.控制角色移动, true);
                character.enabled = true;
                anim.SetBool("back", false);
                nav.Warp(transform.position);
                nav.enabled = false;
                nomalModel.SetActive(true);
                backModel.SetActive(false);
                isRun = false;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="e"></param>
    public void HideNav()
    {
        nomalModel.SetActive(true);
        backModel.SetActive(false);
        character.enabled = true;
        TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.控制角色移动, true);
    }
}
