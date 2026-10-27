using UnityEngine;

public class Page01Start : TexCore_PlaneBase
{
    public static Page01Start instance;

    public string nextScene = TexCore_LoadScene.BusinessSceneName;

    private void Awake()
    {
        instance = this;
    }

    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);

        if (obj == null)
        {
            return;
        }

        string objectName = obj.name;
        if (string.IsNullOrEmpty(objectName))
        {
            return;
        }

        if (ShouldQuit(objectName))
        {
            Application.Quit();
            return;
        }

        if (ShouldEnterBusinessScene(objectName))
        {
            TexCore_LoadScene.instance?.OnlyLoad(GetTargetScene());
        }
    }

    private string GetTargetScene()
    {
        if (string.IsNullOrEmpty(nextScene) || nextScene == "02NavigationPage")
        {
            return TexCore_LoadScene.BusinessSceneName;
        }

        return nextScene;
    }

    private static bool ShouldEnterBusinessScene(string objectName)
    {
        return objectName.Contains("开始")
            || objectName.Contains("实验")
            || objectName.Contains("btnSolo")
            || objectName.Contains("MainPage ActionButton");
    }

    private static bool ShouldQuit(string objectName)
    {
        return objectName.Contains("退出") || objectName.Contains("Quit");
    }
}

