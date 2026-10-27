using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Page02Navigation : TexCore_PlaneBase
{
    public static Page02Navigation instance;

    public string nextScene = "构造原理_01";

    private void Awake()
    {
        instance = this;
    }
    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        switch (obj.name)
        {
            case "认知":
                TexCore_LoadScene.instance.OnlyLoad(nextScene);
                break;
            case "实训":
                //TexCore_LoadScene.instance.OnlyLoad("");
                break;
            case "退出":
                Application.Quit();
                break;
            default:
                break;
        }
    }
}
