
using System.Collections;
using System.Collections.Generic;
using TexCore_UnityTools;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class FlieAssetPanel : TexCore_PlaneBase
{
    public List<PointTreeModel> tree;

    public GameObject prebtn_Module;
    public GameObject prebtn_Files;
    public GameObject prebtn_File;

    public GameObject preImage;
    public GameObject preWord;
    public GameObject lastAsset;

    public Transform btnRoot;

    public List<RectTransform> allBtn;

    public List<string> fileUrl;
    public Transform activeFiles;

    private void Start()
    {
        TexCore_Background.instance.GetFileTree(GetFileTreeResult);
    }

    public void GetFileTreeResult(GetFileTreeResult result)
    {
        if (result.State)
        {
            tree = result.Data;
            InitFilePBS(tree, btnRoot,0);
            foreach (var item in allBtn)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            }
        }
    }

    public void InitFilePBS(List<PointTreeModel> _tree,Transform parent,int lever)
    {
        for (int i = 0; i < _tree.Count; i++)
        {
            GameObject newBtn;
            if (_tree[i].IsLession || _tree[i].IsModule)//创建文件夹
            {
                newBtn = Instantiate(prebtn_Module, parent);
                newBtn.SetActive(true);
                newBtn.name = "M_"+lever+"_" +_tree[i].Name;
                
            }
            else//不是根节点也不是文件夹，创建文件
            {
                newBtn = Instantiate(prebtn_Files, parent);
                newBtn.SetActive(true);
                newBtn.name = "F_" + _tree[i].Code + "_" + _tree[i].Name + "_" +lever ;
            }
            newBtn.transform.GetChild(0).name = newBtn.name;
            TextMeshProUGUI text = newBtn.GetComponentInChildren<TextMeshProUGUI>();
            text.text = _tree[i].Name;
            text.margin = new Vector4(120f + lever * 40f, 0, 0, 0);
            newBtn.transform.GetChild(0).GetComponent<TexCore_UISelect>().itemParent = parent;
            allBtn.Add(newBtn.GetComponent<RectTransform>());


            if (_tree[i].Children != null && _tree[i].Children.Count > 0)
            {
                InitFilePBS(_tree[i].Children, newBtn.transform.GetChild(1), lever+1);
            }
        }
       
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());
       
    }


    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        switch (obj.name)
        {
            case "Return":
                TexCore_PlaneManager.self.ReturnPlane();
               
                break;
            case "Quit":
                TexCore_LoadScene.instance.OnlyLoad("01LoginPage");
                break;

            default:
                string[] ns = obj.name.Split("_");
                switch (ns[0])
                {
                    case "M":
                        //开关子节点
                        //刷新排列
                        GameObject child = obj.transform.parent.GetChild(1).gameObject;
                        child.SetActive(!child.activeSelf);
                        if (child.activeSelf)
                            child.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
                        else
                            obj.transform.parent.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
                        break;
                    case "F":
                        //获取信息
                        GameObject child1 = obj.transform.parent.GetChild(1).gameObject;
                        child1.SetActive(!child1.activeSelf);
                        if (activeFiles != null)
                        {
                            for (int i = 0; i < activeFiles.GetChild(1).childCount; i++)
                            {
                                Destroy(activeFiles.GetChild(1).GetChild(i).gameObject);
                            }
                            activeFiles.GetChild(1).gameObject.SetActive(false);
                            activeFiles.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
                        }
                            
                        if (child1.activeSelf)
                        {
                            activeFiles = obj.transform.parent;
                            if (child1.transform.childCount<1)
                                TexCore_Background.instance.GetOnlineFile(ns[1], GetFileResult);
                            else
                                activeFiles.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
                        }
                        else
                        {
                            obj.transform.parent.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
                            activeFiles = null;
                        }
                        
                        break;
                    case "P":
                        //
                        switch (ns[2])
                        {
                            case "1":
                                //DownLoadPicture(fileUrl[ns[1].ToInt32()]);
                                break;
                            case "2":

                                break;
                            default:
                                break;
                        }
                        
                        break;
                    default:
                        break;
                }
                break;
        }
    }
    public void GetFileResult(GetFileResult result)
    {
        if (result.State)
        {
            //fileUrl = new List<string>();
            //int lever = activeFiles.name.Split("_")[3].ToInt32() + 1;
            //Transform parent = activeFiles.GetChild(1);
            //for (int i = 0; i < result.Data.Count; i++)
            //{
            //    GameObject newBtn;

            //    newBtn = Instantiate(prebtn_File, parent);
            //    newBtn.SetActive(true);
            //    newBtn.name = "P_" + i + "_" + result.Data[i].FileType + "_" + result.Data[i].FileName;

            //    newBtn.transform.GetChild(0).name = newBtn.name;
            //    TextMeshProUGUI text = newBtn.GetComponentInChildren<TextMeshProUGUI>();
            //    text.text = result.Data[i].FileName;
            //    text.margin = new Vector4(120f + lever * 40f, 0, 0, 0);
            //    newBtn.GetComponent<TexCore_UISelect>().itemParent = parent;
            //    fileUrl.Add(result.Data[i].FileUrl);
            //}

            activeFiles.GetComponent<LayoutUpdate>().WaiteUpdateLayoutSingle();
        }
    }

    public void DownLoadPicture(string url)
    {
        GameObject newImg = Instantiate(preImage,preImage.transform.parent);
        //preImage.SetActive(true);
        newImg.SetActive(true);
        //StartCoroutine( PictureTools.GetTexture2DWeb(BackInterface.Self.IP+"/"+ url,newImg.GetComponentInChildren<Image>(),true));
        StartCoroutine(PictureTools.GetTexture2DByWWW(BackInterface.Self.IP+"/"+ url, newImg.GetComponentInChildren<RawImage>(),true));
        if (lastAsset!=null)
            Destroy(lastAsset);
        lastAsset = newImg;
    }

    
    
}
