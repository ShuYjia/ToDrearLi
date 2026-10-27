//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: A linear mapping value that is used by other components
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using DG.Tweening;
using Bpf.Diagnostics;



    [Serializable]
    public class Range 
    {
        public bool isActive=true;
        public float max;
        public float min;
    }

    //-------------------------------------------------------------------------
    public class TexCore_LinearMapping : MonoBehaviour
	{
    #region 属性
    public float value;
        public Slider slider;

        public float startValue;
        public float endValue;
        [SerializeField]
        [Tooltip("多组触发范围，当值在其中某个范围内，则调用触发事件")]
        public List<Range> triggers;

        [Tooltip("多组触发范围，当值在其中某个范围内，则调用触发事件")]
        public List<Range> re_triggers;


        public bool isStart = true;

        public bool singleSlider = false;

        [SerializeField]
        public bool isHandOn = false;

        public bool isTriggers = false;
        public float Value
        {
            get => value;
            set
            {
                this.value = value;
                OnValueChange?.Invoke(value);
            }
        }

        public bool IsHandOn 
        { 
            get => isHandOn; 
            set
            {
                isHandOn = value;
                OnGrab?.Invoke(value);
            }  
        }

        public UnityEvent<float> OnValueChange;
        public UnityEvent OnTrigger;
        public UnityEvent<bool> OnGrab;

        public int lastTriggerIndex=-1;

        public string player;
    #endregion
    private void Start()
        {
            if (startValue!=0)
            {
                Value = startValue;
            }
            if (slider!=null)
            {
                if (singleSlider)
                    slider.onValueChanged.AddListener((v) => { Value = v; });
                else
                    slider.onValueChanged.AddListener((v) => {  value =v; });

                OnValueChange.AddListener((v) => { slider.value = v; });
            }

            if (triggers!=null&&triggers.Count>0)
            {
                if (isStart)
                    OnValueChange.AddListener(OnValue);
                else
                    OnValueChange.AddListener(OnReValue);

                OnTrigger.AddListener(() =>
                {
                    BpfLog.Info(BpfLogCategory.Network, "tex_core_linear_mapping.start", $"线性移动触发:{transform.name}");
                    isTriggers = true;

                    TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name/*, player*/);
                });
            }
          
        }
        
        public void OnValue(float v)
        {
            for (int i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].isActive)
                {
                    if (i != lastTriggerIndex && v >= triggers[i].min && v <= triggers[i].max) //当值处于某一个触发范围内
                    {
                        triggers[i].isActive = false;
                        lastTriggerIndex = i;
                        gameObject.SendMessage("MyExitHand");
                        gameObject.GetComponent<Collider>().enabled = false;
                        OnTrigger.Invoke();
                    }
                }
            }
        }
        public void OnReValue(float v)
        {
            for (int i = 0; i < re_triggers.Count; i++)
            {
                if (i != lastTriggerIndex && v >= re_triggers[i].min && v <= re_triggers[i].max) //当值处于某一个触发范围内
                {
                    lastTriggerIndex = i;
                    gameObject.SendMessage("MyExitHand");
                    OnTrigger.Invoke();
                }
            }
        }

        public void Reverse()
        {
            Value = endValue;
            isStart = false;
        }
    }


