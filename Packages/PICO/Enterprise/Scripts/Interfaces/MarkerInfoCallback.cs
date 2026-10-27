using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
#if PICO_OPENXR_SDK
using Unity.XR.OpenXR.Features.PICOSupport;
#endif
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.XR;


namespace Unity.XR.PICO.TOBSupport
{
    public class MarkerInfoCallback : AndroidJavaProxy
    {
        public Action<List<MarkerInfo>> mCallback;
        private List<MarkerInfo> mlist = new List<MarkerInfo>();
        private TrackingOriginModeFlags TrackingMode;
        private float YOffset;

        public MarkerInfoCallback(TrackingOriginModeFlags trackingMode, float cameraYOffset,
            Action<List<MarkerInfo>> callback) : base("com.picoxr.tobservice.interfaces.StringCallback")
        {
            TrackingMode = trackingMode;
            YOffset = cameraYOffset;
            mCallback = callback;
            mlist.Clear();
#if PICO_OPENXR_SDK
            OpenXRExtensions.SetMarkMode();
#endif
        }

        public void CallBack(string var1)
        {
            Debug.Log("ToBService MarkerInfo Callback 回调:" + var1);
            List<MarkerInfo> tmp = JsonToMarkerInfos(var1);
            PXR_EnterpriseTools.QueueOnMainThread(() =>
            {
                if (mCallback != null)
                {
                    mCallback(tmp);
                }
            });
        }

        public List<MarkerInfo> JsonToMarkerInfos(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            List<MarkerInfo> ModelList = new List<MarkerInfo>();
            JArray items = JArray.Parse(json);
            for (int i = 0; i < items.Count; i++)
            {
                Debug.Log("TOB TestDemo---- MarkerInfo Callback 回调:1");
                JObject item = (JObject)items[i];

                float OriginHeight = 0;
                if (TrackingMode == TrackingOriginModeFlags.Device || TrackingMode == TrackingOriginModeFlags.Floor)
                {
              
#if !PICO_OPENXR_SDK
                    OriginHeight = PXR_Plugin.System.UPxr_GetConfigFloat(ConfigType.ToDelaSensorY);
#else
                    float trackingorigin_height = PXR_EnterprisePlugin.oxr_get_trackingorigin_height();
                    float locationheight = OpenXRExtensions.GetLocationHeight();
                    if (TrackingMode == TrackingOriginModeFlags.Floor)
                    {
                        YOffset = 0;
                        OriginHeight = -trackingorigin_height;
                    }
                    else
                    {
                        // OriginHeight = trackingorigin_height + locationheight;
                        // OriginHeight = locationheight;
                        OriginHeight = -trackingorigin_height;
                    }
#endif
                }
                else
                {
                    OriginHeight = 0;
                    YOffset = 0;
                } 
   
                Debug.Log("TOB TestDemo---- MarkerInfo Callback 回调:OriginHeight："+OriginHeight );
                MarkerInfo model = new MarkerInfo();
                model.posX = item.Value<double?>("posX") ?? 0d;
                model.posY = (item.Value<double?>("posY") ?? 0d) + OriginHeight + YOffset;
                model.posZ = -(item.Value<double?>("posZ") ?? 0d);

                model.rotationX = -(item.Value<double?>("rotationX") ?? 0d);
                model.rotationY = -(item.Value<double?>("rotationY") ?? 0d);
                model.rotationZ = item.Value<double?>("rotationZ") ?? 0d;
                model.rotationW = item.Value<double?>("rotationW") ?? 0d;

                model.validFlag = item.Value<int?>("validFlag") ?? 0;
                model.markerType = item.Value<int?>("markerType") ?? 0;
                model.iMarkerId = item.Value<int?>("iMarkerId") ?? 0;
                model.dTimestamp = item.Value<double?>("dTimestamp") ?? 0d;

                JArray reserve = item["reserve"] as JArray;
                if (reserve == null)
                {
                    model.reserve = Array.Empty<float>();
                }
                else
                {
                    model.reserve = new float[reserve.Count];
                    for (int j = 0; j < reserve.Count; j++)
                    {
                        model.reserve[j] = reserve[j]?.Value<float>() ?? 0f;
                    }
                }

                ModelList.Add(model);
            }

            return ModelList;
        }
    }
}
