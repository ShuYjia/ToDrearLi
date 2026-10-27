using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TexCore_Interaction_Collider : MonoBehaviour
{
    public GameObject hint;
    public bool triggerCloseCollider = true;

    public GameObject show;
    public bool nomalShow = false;

    private void Start()
    {
        if(show!=null) show.SetActive(nomalShow);
    }
    private void OnMouseEnter()
    {

    }
    private void OnMouseDown()
    {
        
    }
    private void OnMouseOver()
    {
        
    }

    private void OnMouseUpAsButton()
    {
        if (hint != null) hint.SetActive(false);
        if (triggerCloseCollider) gameObject.GetComponentInChildren<Collider>().enabled = false;
        //if (show != null) show.SetActive(true);
        TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name);
    }

    private void OnMouseDrag()
    {

    }

    private void OnMouseUp()
    {
        
    }

    private void OnMouseExit()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag=="Player")
        {
            if (hint != null) hint.SetActive(false);
            if (triggerCloseCollider) gameObject.GetComponentInChildren<Collider>().enabled = false;
            //if (show != null) show.SetActive(true);
            TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name);
        }
        
    }

}
