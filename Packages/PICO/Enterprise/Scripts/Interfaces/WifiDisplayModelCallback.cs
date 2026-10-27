using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unity.XR.PICO.TOBSupport
{
    public class WifiDisplayModelCallback : AndroidJavaProxy
    {
        public Action<List<WifiDisplayModel>> mCallback;

        public WifiDisplayModelCallback(Action<List<WifiDisplayModel>> callback) : base("com.picoxr.tobservice.interfaces.StringCallback")
        {
            mCallback = callback;
        }

        public void CallBack(string var1)
        {
            Debug.Log("ToBService WifiDisplayModelCallback 回调:" + var1);
            List<WifiDisplayModel> tmp = JsonToWifiDisplayModel(var1);
            PXR_EnterpriseTools.QueueOnMainThread(() =>
            {
                if (mCallback != null)
                {
                    mCallback(tmp);
                }
            });
        }
        
        public List<WifiDisplayModel> JsonToWifiDisplayModel(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            List<WifiDisplayModel> ModelList = new List<WifiDisplayModel>();
            JArray items = JArray.Parse(json);
            for (int i = 0; i < items.Count; i++)
            {
                JObject item = (JObject)items[i];
                WifiDisplayModel model = new WifiDisplayModel();
                model.deviceAddress = item.Value<string>("deviceAddress");
                model.deviceName = item.Value<string>("deviceName");
                model.isAvailable = item.Value<bool?>("isAvailable") ?? false;
                model.canConnect = item.Value<bool?>("canConnect") ?? false;
                model.isRemembered = item.Value<bool?>("isRemembered") ?? false;
                model.statusCode = item.Value<int?>("statusCode") ?? 0;
                model.status = item.Value<string>("status");
                model.description = item.Value<string>("description");

                ModelList.Add(model);
            }

            return ModelList;
        }
    }
}
