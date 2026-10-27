using Bpf.Diagnostics;
/****************************************************
	文件名：		BackgroundMusic.cs

	功能：		Nothing
*****************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic instance;

    public List<AudioClip> audioClipList;

    public AudioSource audioSource;

    public bool isAwake = true;

    private void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();
        if(isAwake)
        {
            audioSource.clip = audioClipList[Random.Range(0, audioClipList.Count)];
            PlayClip();
        }
    }

    public void OnClik(GameObject obj)
    {
        if(obj.GetComponent<TexCore_Interaction_UGUI>().isSelect)
        {
            PauseClip();
        }
        else
        {
            PlayClip();
        }
    }

    public void OnPlayClip(GameObject obj)
    {
        BpfLog.Info(BpfLogCategory.Asset, "background_music.on_play_clip", null, null, BpfLogField.Of("name", obj.name));
        foreach (AudioClip clip in audioClipList)
        {
            BpfLog.Info(BpfLogCategory.Asset, "background_music.on_play_clip", null, null, BpfLogField.Of("name", clip.name));
            if (clip.name.Equals(obj.name))
            {
                audioSource.clip = clip;
                PlayClip();
            }
        }
    }


    public void PlayClip()
    {
        audioSource.Play();
    }

    public void PauseClip()
    {
        audioSource.Pause();
    }
}
