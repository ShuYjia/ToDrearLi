using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bpf.Diagnostics;


public class ToolsManager : MonoBehaviour
{
    public List<Texture2D> tools;

    public List<GameObject> pre_Tools;
    public List<GameObject> obj_Tools=new List<GameObject>();

    public static ToolsManager self;

    public GameObject t;

    public Transform toolsRoot;
    private void Awake()
    {
        self = this;
    }
    
    public void OnClick(GameObject obj)
    {
        if (t != null && t.name.Equals(obj.name)) 
        {
            Close();
            return;
        }
        t = obj;
        string objName = obj.name.Split('_')[1];
        for (int i = 0; i < tools.Count; i++)
        {
            if (objName.Equals(tools[i].name))
            {
                // 设置特定大小的光标 (例如，50x50像素)
                SetMouseCursor(objName, new Vector2(120, 120));

                BpfLog.Error(BpfLogCategory.Scene, "tools_manager.on_click", null, null, BpfLogField.Of("obj_name", objName));
            }
            //Debug.Log(tools[i].name);
        }
    }

    public void SetMouseCursor(string name=null)
    {
        if (string.IsNullOrEmpty(name))
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }
       
        foreach (Texture2D item in tools)
        {
            if (item.name==name)
            {
                Cursor.SetCursor(item, Vector2.zero, CursorMode.ForceSoftware);
            }
        }
    }

    public void SetMouseCursor(string name = null, Vector2? size = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            // 清除光标
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        foreach (Texture2D item in tools)
        {
            if (item.name == name)
            {
                // 设置光标大小
                Texture2D cursorTexture = item;

                if (size.HasValue)
                {
                    // 根据传入的大小调整光标纹理
                    cursorTexture = ResizeTexture(item, (int)size.Value.x, (int)size.Value.y);
                }
                // 设置光标，热点为光标的中心
                Vector2 hotspot = new Vector2(cursorTexture.width / 2, cursorTexture.height / 2);
                Cursor.SetCursor(cursorTexture, hotspot, CursorMode.ForceSoftware);
            }
        }

        
    }
    // 缩放图片
    private Texture2D ResizeTexture(Texture2D originalTexture, int width, int height)
    {
        Texture2D resizedTexture = new Texture2D(width, height);
        Color[] pixels = originalTexture.GetPixels(0, 0, originalTexture.width, originalTexture.height);
        Color[] resizedPixels = new Color[width * height];

        float scaleX = (float)originalTexture.width / width;
        float scaleY = (float)originalTexture.height / height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int px = Mathf.FloorToInt(x * scaleX);
                int py = Mathf.FloorToInt(y * scaleY);
                resizedPixels[y * width + x] = pixels[py * originalTexture.width + px];
            }
        }

        resizedTexture.SetPixels(resizedPixels);
        resizedTexture.Apply();
        return resizedTexture;
    }

    private void OnEnable()
    {
        if(t!=null) t.SetActive(true);
    }
    private void OnDisable()
    {
        if (t != null) t.SetActive(false);
    }



    public void Close()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        t = null;
        foreach (TexCore_Interaction_UGUI item in toolsRoot.GetComponentsInChildren<TexCore_Interaction_UGUI>())
        {
            item.isSelect = false;
            item.SetState(SelectState.Nomal);
        }
    }

    public void ShowTools(List<string> ts)
    {
        t.SetActive(true);
        //将工具全部收起
        for (int i = 0; i < toolsRoot.childCount; i++)
        {
            toolsRoot.GetChild(i).gameObject.SetActive(false);
        }
        //将该题需要的工具显示出来
        foreach (string item in ts)
        {
            for (int i = 0; i < toolsRoot.childCount; i++)
            {
                if (item==toolsRoot.GetChild(i).name)
                {
                    toolsRoot.GetChild(i).gameObject.SetActive(true);
                    break;
                }
            }
        }
    }
}
