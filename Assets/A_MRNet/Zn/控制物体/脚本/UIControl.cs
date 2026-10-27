/****************************************************
	文件名：		UIControl.cs
	功能：		Nothing
*****************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UIControl : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other == null)
            return;
        if (other.tag.Equals("Player"))
        {
            // 计算指向目标的方向
            Vector3 directionToTarget = TexCore_Character.self.transform.position - transform.position;

            // 计算旋转角度
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // 让物体的负Z轴指向目标
            transform.rotation = targetRotation * Quaternion.Euler(0, 180, 0);
        }
    }
}
