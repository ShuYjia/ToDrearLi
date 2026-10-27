using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 退出控制逻辑
public class QuitCtrl : MonoBehaviour
{
    [SerializeField]
    private Transform _canvas; // UI画布
    [SerializeField]
    private Button _button; // 退出按钮

    void Awake()
    {
        Button _button = _canvas.Find("QuitBtn").GetComponent<Button>();
        _button.onClick.AddListener(Application.Quit); // 绑定退出事件
    }
    
}