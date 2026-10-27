using Bpf.Diagnostics;
/****************************************************
	文件名：		Panel_Dialogue.cs

	功能：		对话类型题目
    功能需求：
        两种方式：
            一、主动触发对话
                1.鼠标点击某物体，对方接话
                2.靠近某物体，对方接话
            二、被动触发对话
                1.靠近某物体对方自动说话，等待接话
                2.对方自动说话，等待接话
*****************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 对话类型
/// </summary>
public enum DialogueEnum
{
    主动,
    被动
}

[Serializable]
public class DataClass_Dialogue : SelfDataBase
{
    public List<SessionInformation> sessions;
}

[Serializable]
public class SessionInformation
{
    /// <summary>
    /// 对话人姓名方便区分
    /// </summary>
    public string name;
    /// <summary>
    /// 对话人说话内容
    /// </summary>
    public string word;
    /// <summary>
    /// 对话人说话音频地址
    /// </summary>
    public string wordPath;
}

public class Panel_Dialogue : PanelTopicBase, IGetData
{
    [Space()]
    [Header("子类")]
    [Space()]

    public DataClass_Dialogue dataClass_Dialogue;

    [Space()]

    public DialogueEnum dialogueEnum;

    public GameObject btn_Confirm;

    public Transform itemParent;

    public GameObject dialogBox;

    public int activeIdex;

    public float waiteTime;

    public bool isPlay;

    /// <summary>
    /// 对话预制体
    /// </summary>
    public GameObject textTemplate;
    /// <summary>
    /// 点击继续
    /// </summary>
    public GameObject clickToContinue;
    /// <summary>
    /// 点击结束
    /// </summary>
    public GameObject clickToEnd;
    /// <summary>
    /// 对话根节点
    /// </summary>
    public Transform wordRoot;
    /// <summary>
    /// 当前显示的对话文本
    /// </summary>
    public TextMeshProUGUI textObj;
    private IEnumerator t;

    public override void Start()
    {
        base.Start();
        topicData = (DataClass_Topic)datas.selfDatas[0];
    }

    public override void OnEnable()
    {
        base.OnEnable();
        activeIdex = -1;
        if (isPlay) PlayShowWord();


        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
        //TexCore_EventCenter.Broadcast<string>(TexCore_EventType.播放音频, datas.GetUrl() + topicData.hintAudios[stepIndex]);
    }

    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        BpfLog.Info(BpfLogCategory.UI, "panel_dialogue.on_click", "点击：" + datas.name + obj.name);
        switch (obj.name)
        {
            case "确定":
                CalculateScore();
                break;
            case "对话框":
                if (t != null)
                {
                    StopCoroutine(t);
                    textObj.text = dataClass_Dialogue.sessions[activeIdex].name + ":" + dataClass_Dialogue.sessions[activeIdex].word;
                    t = null;
                    TexCore_AudioManager.self.wordSourcce.Stop();
                    if (activeIdex < dataClass_Dialogue.sessions.Count - 1)
                    {
                        clickToContinue.SetActive(true);
                        //TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "请继续聊天", 1.5f);
                    }
                    else
                    {

                        clickToEnd.SetActive(true);
                        TexCore_EventCenter.Broadcast(TexCore_EventType.触发物体,nodeIndex,topicIndex,0);
                        //TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "请结束聊天", 1.5f);
                    }
                }
                else
                {
                    PlayShowWord();
                }
                RectTransform rect = wordRoot.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                break;
            //case "下一步":
            //    base.Next();
            //    break;
            default:
                if (stepIndex >= topicData.tasks.Count) return;

                string[] task = topicData.tasks[stepIndex].Split('/');
                string[] operate = obj.name.Split("_");
                //不带工具步骤
                if (operate[0].Equals(topicData.tasks[stepIndex]))//如果点击的是当前步骤需要操作的物体
                {
                    operater = operate[operate.Length - 1];
                    trigger = true;
                    TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "操作正确", 1.5f);
                    steps.Add(topicData.tasks[stepIndex]);
                    stepIndex += 1;
                    NotifyStepVoiceChanged();
                    if (stepIndex < topicData.hintTexts.Count) //更新下一步的提示
                    {
                        TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
                    }
                }
                break;

        }
    }


    public override void CalculateScore()
    {
        Submit();
    }

    public override void Submit()
    {
        base.Submit();
        btn_Next.SetActive(true);
    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase>();
        datas.selfDatas.Add(topicData);
        //datas.selfDatas.Add(selfData);
        return datas;
    }

    /// <summary>
    /// 开始显示对话
    /// </summary>
    public void PlayShowWord()
    {
        dialogBox.SetActive(true);
        clickToContinue.SetActive(false);
        clickToEnd.SetActive(false);

        activeIdex += 1;
        if (activeIdex < dataClass_Dialogue.sessions.Count)
        {
            t = ShowWord(dataClass_Dialogue.sessions[activeIdex]);
            StartCoroutine(t);
        }
        else
        {
            //TexCore_BusinessManager.OnClick(clickToEnd, SceneManager.GetActiveScene().name);
            btn_Next.GetComponent<TexCore_Interaction_UGUI>().OnClick();
            gameObject.SetActive(false);
            
        }

    }

    IEnumerator ShowWord(SessionInformation session)
    {
        //音频赋值
        AudioClip audioClip = Resources.Load<AudioClip>("AuidoClip/" + session.wordPath);
        if (TexCore_AudioManager.self != null && audioClip != null) 
        {
            TexCore_AudioManager.self.PlayWordAudio(audioClip);
        }
        //文字生成
        if (textObj != null)
        {
            Destroy(textObj.gameObject);
        }
        textObj = Instantiate(textTemplate, wordRoot).GetComponent<TextMeshProUGUI>();
        textObj.text = string.Empty;
        textObj.text = $"{session.name}:";
        foreach (var item in session.word) 
        {
            textObj.text += item;
            yield return new WaitForSeconds(waiteTime);
        }
        if (activeIdex < dataClass_Dialogue.sessions.Count-1) 
        {
            clickToContinue.SetActive(true);
            //TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "请继续聊天", 1.5f);
        }
        else
        {
            clickToEnd.SetActive(true);
            //TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "请结束聊天", 1.5f);
        }
    }

    private new void OnDisable()
    {
        TexCore_AudioManager.self.StopWordAudio();
    }
}
