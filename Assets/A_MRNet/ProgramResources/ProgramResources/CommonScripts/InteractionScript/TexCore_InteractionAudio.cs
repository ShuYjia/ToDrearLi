using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexCore_InteractionAudio : MonoBehaviour
{
    
    public TexCore_AudioManager audioManager;

    public string audioPath = "audio/";
    public AudioClip btnHover;
    public AudioClip btnClick;

    private void OnEnable()
    {
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.交互音频, InteractionAudio);
    }
    private void OnDisable()
    {
        TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.交互音频, InteractionAudio);
    }

    private void Start()
    {
        btnHover = Resources.Load<AudioClip>(audioPath + "hover");
        btnClick = Resources.Load<AudioClip>(audioPath + "click");
    }

    void InteractionAudio(string _tag)
    {
        if ( audioManager == null) return;
        switch (_tag)
        {
            case "Hover": audioManager.PlayAudio(btnHover) ; break;
            case "Click": audioManager.PlayAudio(btnClick); break;
            default:
                break;
        }
       
    }
   
}
