using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavigationNPC : MonoBehaviour
{
    public NavMeshAgent nav;
    public Animator anim;

    public bool isRun = false;
    public Transform target;

    public Transform player;

    private void Start()
    {
        nav = GetComponent<NavMeshAgent>();
        anim = transform.GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    public void SetNavTarget(Transform target)
    {
        nav.enabled = true;
        nav.Warp(transform.position);
        if (nav.SetDestination(target.position))
        {
            isRun = true;
            this.target = target;
            anim.SetBool("run", true);
        }
        
    }
    private void FixedUpdate()
    {
        if (isRun)
        {
            if (Vector3.Distance(transform.position,target.position)<0.8f)
            {
                isRun = false;
                anim.SetBool("run", false);
                nav.Warp(transform.position);
                nav.enabled = false;
            }
        }
        else
        {
            transform.LookAt(player.position);
        }
    }


}
