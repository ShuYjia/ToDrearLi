using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class SelectRolePanel : TexCore_PlaneBase
{
    public static SelectRolePanel instance;

    public Transform grop;

    public Transform sex;

    public Transform partform;
    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);

        switch (obj.name)
        {
            //开始实训
            case "Play":
                SetCharacter();
                break;
            //返回
            case "Return":
                TexCore_PlaneManager.self.ReturnPlane();
                break;
        }
    }
    public void Awake()
    {
        instance = this;
    }

    public void SetCharacter()
    {
        for (int i = 0; i < grop.childCount; i++)
        {
            if (grop.GetChild(i).GetComponent<TexCore_UISelect>().isSelect)
            {

                for (int j = 0; j < sex.childCount; j++)
                {
                    if (sex.GetChild(j).GetComponent<TexCore_UISelect>().isSelect)
                    {
                        JudgePartform();
                        TexCore_Const.character = (Character)Enum.Parse(typeof(Character), grop.GetChild(i).name);
                        
                        TexCore_WindowManager.self.CreatePromis("您选择的角色为：" + grop.GetChild(i).name+"\n角色性别为:"+sex.GetChild(j).name+"\n运行平台为："+ TexCore_Const.partfrom, () =>
                        {
                            TexCore_LoadScene.instance.OnlyLoad("03OperatePage");
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// 判断运行平台
    /// </summary>
    public void JudgePartform()
    {
        for (int i = 0; i < partform.childCount; i++)
        {
            if (partform.GetChild(i).GetComponent<TexCore_UISelect>().isSelect)
            {
                TexCore_Const.partfrom = (Partfrom)Enum.Parse(typeof(Partfrom), partform.GetChild(i).name); 
            }
        }
    }
}
