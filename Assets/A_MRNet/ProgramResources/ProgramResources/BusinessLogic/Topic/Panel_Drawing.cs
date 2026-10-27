/****************************************************
	文件名：		Panel_Drawing.cs

	功能：		Nothing
*****************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class DataClass_Drawing : SelfDataBase
{
    //public List<SessionInformation> sessions;
}

[Serializable]
public class DetectionData
{
    public string colliderName;
    public GameObject obj;
    public bool isTrigger;
}

public class Panel_Drawing : PanelTopicBase, IGetData
{
    [Space()]
    [Header("子类")]
    [Space()]

    public DataClass_Drawing dataClass_Dialogue;

    [Space()]

    public UIDrawLine uiDrawLine;

    public GameObject btn_Confirm;

    public List<DetectionData> detectionDataLists;


    public override void OnEnable()
    {
        base.OnEnable();
        DrawingInit();
    }


    public override void Start()
    {
        base.Start();
        if (!isHeadline)
        {
            TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.开关标题, true);
            TexCore_EventCenter.Broadcast<string>(TexCore_EventType.显示标题, topicData.hintTexts[stepIndex]);
        }
        else
        {
            TexCore_EventCenter.Broadcast<bool>(TexCore_EventType.开关标题, false);
            if (topicTitle != null)
            {
                topicTitle.text = topicData.hintTexts[stepIndex];
            }
        }
    }

    private void Update()
    {
        // 1. 首先将所有物体的布尔值重置为 false
        foreach (DetectionData detection in detectionDataLists)
        {
            detection.isTrigger = false;
        }

        // 2. 遍历所有线条并检测每条线段
        foreach (var linePositions in uiDrawLine.AllLinePositions)
        {
            for (int i = 0; i < linePositions.Count - 1; i++)
            {
                Vector3 lineStart = linePositions[i];
                Vector3 lineEnd = linePositions[i + 1];
                Vector3 direction = lineEnd - lineStart;

                // 检测每一段线段是否与物体发生碰撞
                if (Physics.Raycast(lineStart, direction, out RaycastHit hit, direction.magnitude))
                {
                    //Debug.Log("Line intersects with object: " + hit.collider.name);

                    // 获取对应物体的 DetectionData 并设置 isTrigger
                    DetectionData detection = ReturnsObject(hit.collider.name);
                    if (detection != null)
                    {
                        detection.isTrigger = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 根据名称返回游戏对象的 DetectionData
    /// </summary>
    /// <param name="objName"></param>
    /// <returns></returns>
    public DetectionData ReturnsObject(string objName)
    {
        foreach (DetectionData detion in detectionDataLists)
        {
            if (objName.Equals(detion.colliderName))
            {
                return detion;
            }
        }
        return null;
    }

    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        switch (obj.name)
        {
            case "确定":
                CalculateScore();
                TexCore_EventCenter.Broadcast(TexCore_EventType.触发物体, nodeIndex, topicIndex, 0);
                break;
        }
    }

    public override void CalculateScore()
    {
        int scoreIndex = 0;

        for (int i = 0; i < detectionDataLists.Count; i++)
        {
            if (detectionDataLists[i].isTrigger)
            {
                scoreIndex++;
            }
        }

        switch (TexCore_Const.mode)
        {
            case Mode.练习://需要提示
                if (scoreIndex == detectionDataLists.Count)
                {
                    TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, "<color=green>恭喜，画线成功</color>", 1.5f);
                    haveScore = topicData.score;
                }
                else
                {
                    if (errorNum < 2)
                    {
                        errorNum++;
                        TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"<color=red>画线错误</color>\n<color=green>正确提示：</color>{mistakeText}\n错误<color=red>{errorNum}</color>次", 1.5f);
                        return;
                    }
                    if (errorNum == 2)
                    {
                        errorNum++;
                        TexCore_EventCenter.Broadcast(TexCore_EventType.显示提示, $"已经画线错误<color=red>3</color>次\n正确答案已显示", 1.5f);
                        haveScore = 0;
                    }
                }
                break;
            case Mode.考核://不需要提示
                if (scoreIndex == detectionDataLists.Count)
                {
                    haveScore = topicData.score;
                }
                else
                {
                    haveScore = 0;
                }
                break;
        }
        Submit();
    }

    public override void Submit()
    {
        base.Submit();
        btn_Confirm.SetActive(false);
        btn_Next.SetActive(true);
    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase>();
        datas.selfDatas.Add(topicData);
        //datas.selfDatas.Add(selfData);
        return datas;
    }

    public void DrawingInit()
    {
        uiDrawLine.canvas = GetComponentInParent<Canvas>();
        uiDrawLine.camera = uiDrawLine.canvas.transform.parent.GetComponentInChildren<Camera>();
    }
}
