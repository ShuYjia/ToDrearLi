using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public enum DateType
{
    文本,
    图片,
    视频
}
[Serializable]
public class ModelDate : SelfDataBase
{
    public DateType dateType;
    public string date;
}

public class Panel_ShowModel : TexCore_PlaneBase, IGetData
{
    public List<ModelDate> modelDates;

    public List<Transform> models;

    private List<Transform> startTrans;

    public Collider parentCollider;

    public GameObject textData;

    private GameObject nowModel;

    private void Start()
    {
        if (datas.selfDatas != null)
        {
            for (int i = 0; i < datas.selfDatas.Count; i++)
            {
                datas.selfDatas[i] = datas.selfDatas[i] as ModelDate;
            }
        }
       
        startTrans = new List<Transform>();
        foreach (var item in models)
        {
            item.name = "2_" + item.name;
            GameObject trans = new GameObject("pos_"+item.name);
            trans.transform.SetParent(transform.GetChild(0));
            trans.transform.localPosition = item.localPosition;
            trans.transform.rotation = item.rotation;
            trans.transform.localScale = item.localScale;
            startTrans.Add(trans.transform);

            item.gameObject.SetActive(false);
        }
        //ResetModel();
        models[models.Count-1].gameObject.SetActive(true);
        //nowModel=this.gameObject;
    }

    

    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);
        string[] ns = obj.name.Split("_");
        switch (ns[0])
        {
            case "2":
                if (nowModel!=null&&obj.name.Equals(nowModel.name))
                {
                    return;
                }
                for (int i = 0; i < models.Count; i++)
                {
                    if (obj.name.EndsWith(models[i].name))
                    {
                        ShowSingleModel(models[i].gameObject);
                        textData.SetActive(true);
                        textData.GetComponentInChildren<TextMeshProUGUI>().text = modelDates[i].date;
                        //LayoutRebuilder.ForceRebuildLayoutImmediate(textData.GetComponent<RectTransform>());
                    }
                    else
                    {
                        models[i].gameObject.SetActive(false);
                        models[i].transform.localScale= startTrans[i].localScale;
                    }
                }

                break;
        }
        
    }

    public void ResetModel()
    {
        textData.SetActive(false);
        parentCollider.enabled = true;
        for (int i = 0; i < models.Count; i++)
        {
            Collider collider = models[i].GetComponentInChildren<Collider>();
            if (collider) collider.enabled = false;

            if (i != models.Count - 1)
            {
                models[i].gameObject.SetActive(false);
            }
            else 
            {
                models[models.Count - 1].gameObject.SetActive(true);
            }

            //models[i].transform.DOMove(startTrans[i].position, 1f);
            models[i].transform.DOScale(startTrans[i].localScale, 1f);
        }
        nowModel = null;
    }

    public void ShowSingleModel(GameObject model)
    {
        
        parentCollider.enabled = false;
        Collider collider = model.GetComponentInChildren<Collider>();
        if (collider) collider.enabled = true;
        model.SetActive(true);
        //model.transform.DOMove(Vector3.zero,1f);
        model.transform.DOScale(model.transform.localScale.x*1.3f,1f);
        nowModel = model;
    }

    public TexCore_DataClass GetData()
    {
        datas.selfDatas = new List<SelfDataBase>();
        foreach (var item in modelDates)
        {
            datas.selfDatas.Add(item);
        }
        return datas;
    }
}
