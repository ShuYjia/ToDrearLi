using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TexCore_Talk : MonoBehaviour, IPointerClickHandler
{
    public string[] talks;
    public AudioClip[] audios;

    public int activeIdex;

    public TextMeshProUGUI text;

    public GameObject nexttip;

    public float waiteTime;

    IEnumerator t;

    public void OnEnable()
    {
        activeIdex =-1;
        ShowText();
    }
    public void ShowText()
    {
        nexttip.SetActive(false);
        activeIdex += 1;
        if (activeIdex < talks.Length) 
        {
            t = ShowTalk(talks[activeIdex]);
            if (TexCore_AudioManager.self != null)
            {
                TexCore_AudioManager.self.PlayAudio(audios[activeIdex]);
            }
            StartCoroutine(t);
        }
        else
        {
            
            TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name);
            gameObject.SetActive(false);
        }
    }


    IEnumerator ShowTalk(string str)
    {
        text.text = string.Empty;
        foreach (var item in str)
        {
            text.text += item;
            yield return new WaitForSeconds(waiteTime);
        }
        nexttip.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (t!=null)
        {
            StopCoroutine(t);
            text.text = talks[activeIdex];
            nexttip.SetActive(true);
            t = null;
        }
        else
        {
            ShowText();
        }
       
    }
}
