/****************************************************
	文件名：		DataManager.cs

	功能：		存储场景内的数据
*****************************************************/
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DataScene
{
    public string dataName;
    public string data;

    public DataScene(string dataName, string data)
    {
        this.dataName = dataName;
        this.data = data;
    }
}

public class DataManager : MonoBehaviour
{
	public static DataManager data;

    public List<DataScene> dataList;

    public void Awake()
    {
        data = this;
    }

    public void SetData(string dataName, string data)
    {
        DataScene _sceneData = new DataScene(dataName, data);

        DataScene _oldSceneData = FindDataList(dataName);
        if (_oldSceneData != null) //找到数据
        {
            _oldSceneData = _sceneData;
        }
        else
        {
            dataList.Add(_sceneData);
        }
    }

    public DataScene GetData(string dataName)
    {
        DataScene _findSceneData = FindDataList(dataName);
        if (_findSceneData != null)
        {
            return _findSceneData;
        }
        else
        {
            return null;
        }
    }

    public DataScene FindDataList(string dataName)
    {
        for (int i = 0; i < dataList.Count; i++)
        {
            if (dataList[i].dataName == dataName)
            {
                return dataList[i];
            }
        }
        return null;   
    }
}
