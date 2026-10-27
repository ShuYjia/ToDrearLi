using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 进入指定题目时隐藏目标对象，离开该题目后恢复显示。
/// </summary>
/// <remarks>
/// 11 流程测试用它把 09 机器人收起来：机器人推到二维码之后这一题就交棒给 10
/// 机器人，两个机器人不该同时在场。判断依据是
/// <see cref="TexCore_EventType.更新题目"/>——题目面板每次启用都会广播自己的名字，
/// 而且该广播排在 <see cref="TexCore_EventType.设置物体状态"/> 之后，所以来回切题
/// 时隐藏结果不会被同一次进入的步骤状态覆盖。
/// </remarks>
[DisallowMultipleComponent]
public sealed class TexCore_TopicHideObjects : MonoBehaviour
{
    [SerializeField] private string hiddenTopicName = string.Empty;
    [SerializeField] private List<GameObject> hiddenObjects = new List<GameObject>();

    public void Configure(string topicName, IEnumerable<GameObject> objects)
    {
        hiddenTopicName = topicName ?? string.Empty;
        hiddenObjects = new List<GameObject>();
        if (objects == null)
        {
            return;
        }

        foreach (GameObject item in objects)
        {
            if (item != null && !hiddenObjects.Contains(item))
            {
                hiddenObjects.Add(item);
            }
        }
    }

    private void OnEnable()
    {
        TexCore_EventCenter.AddListener<string>(TexCore_EventType.更新题目, OnTopicShown);
    }

    private void OnDisable()
    {
        TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.更新题目, OnTopicShown);
    }

    private void OnTopicShown(string topicName)
    {
        if (string.IsNullOrEmpty(hiddenTopicName))
        {
            return;
        }

        bool hide = topicName == hiddenTopicName;
        foreach (GameObject item in hiddenObjects)
        {
            if (item != null)
            {
                item.SetActive(!hide);
            }
        }
    }
}
