using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class OneBtntest : MonoBehaviour
    {
    public GameObject cube;
    public GameObject blue;
    public GameObject green;
    public GameObject[] otherImas; //声明数组otherImas

    public void OneBtn() // 实现IPointerClickHandler接口中的OnPointerClick方法，该方法在鼠标点击时会被调用
    {
    cube.SetActive(false);
    blue.SetActive(false);
    green.SetActive(false);
    // 使用循环将otherImas数组中的所有GameObject设置为非激活状态
    for (int i = 0; i < otherImas.Length; i++)
    {
    if (otherImas[i] != null) // 加上非空判断，避免空引用异常
    {
    otherImas[i].SetActive(true);
    }
    }
    }
    }
}
