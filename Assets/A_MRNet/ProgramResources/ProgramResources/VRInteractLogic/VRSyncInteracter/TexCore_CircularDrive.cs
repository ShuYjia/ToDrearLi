//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Interactable that can be used to move in a circular motion
//
//=============================================================================

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.Animations;
using Bpf.Diagnostics;

public class TexCore_CircularDrive : MonoBehaviour, IHandHoverUpdate
{

    #region 参数定义
    [Tooltip("一个线性映射组件驱动,如果没有指定一个,将动态添加到这个GameObjectA LinearMapping component to drive, if not specified one will be dynamically added to this GameObject")]
        public TexCore_LinearMapping mylinearMapping;
        public enum Axis_t
        {
            XAxis,
            YAxis,
            ZAxis
        };

        [Tooltip("环形传动在局部空间旋转的轴The axis around which the circular drive will rotate in local space")]
        public Axis_t axisOfRotation = Axis_t.XAxis;

        [Tooltip("Child GameObject which has the Collider component to initiate interaction, only needs to be set if there is more than one Collider child")]
        public Collider childCollider = null;


        [Tooltip("如果是真的,只要按钮被按住,如果按钮被按住,如果控制器从对撞机中移动,那么它就会保持操作If true, the drive will stay manipulating as long as the button is held down, if false, it will stop if the controller moves out of the collider")]
        public bool hoverLock = false;

        [HeaderAttribute("Limited Rotation")]
        [Tooltip("如果是真的,旋转将仅限于[minAngle,maxAngle],如果是假的,旋转是无限的If true, the rotation will be limited to [minAngle, maxAngle], if false, the rotation is unlimited")]
        public bool limited = false;
        public Vector2 frozenDistanceMinMaxThreshold = new Vector2(0.1f, 0.2f);
        public UnityEvent onFrozenDistanceThreshold;

        [HeaderAttribute("Limited Rotation Min")]
        [Tooltip("如果有限为真,则指定较低的限制,否则值未使用If limited is true, the specifies the lower limit, otherwise value is unused")]
        public float minAngle = -45.0f;
        [Tooltip("如果有限,设置是否驱动将冻结它的角度,当最小角到达If limited, set whether drive will freeze its angle when the min angle is reached")]
        public bool freezeOnMin = false;
        [Tooltip("如果有限,当到达minAngle时调用的事件If limited, event invoked when minAngle is reached")]
        public UnityEvent onMinAngle;

        [HeaderAttribute("Limited Rotation Max")]
        [Tooltip("如果有限为真,则指定上限,否则值未使用If limited is true, the specifies the upper limit, otherwise value is unused")]
        public float maxAngle = 45.0f;
        [Tooltip("如果有限,设置驱动器是否会冻结它的角度,当最大角度到达时If limited, set whether drive will freeze its angle when the max angle is reached")]
        public bool freezeOnMax = false;
        [Tooltip("如果在maxAngle到达时调用的事件If limited, event invoked when maxAngle is reached")]
        public UnityEvent onMaxAngle;

        [Tooltip("如果有限是真的,这迫使开始角为星图,夹在[minAngle,maxAngle]If limited is true, this forces the starting angle to be startAngle, clamped to [minAngle, maxAngle]")]
        public bool forceStart = false;
        [Tooltip("如果有限为真,强制启动为真,起起角将是这样,夹在[minAngle,maxAngle]\"If limited is true and forceStart is true, the starting angle will be this, clamped to [minAngle, maxAngle]")]
        public float startAngle = 0.0f;

        [Tooltip("如果是真的,这个组件的变换对象将会相应地旋转If true, the transform of the GameObject this component is on will be rotated accordingly")]
        public bool rotateGameObject = true;

        [Tooltip("如果是真的,手的路径(红色)和投影值(绿色)将被绘制If true, the path of the Hand (red) and the projected value (green) will be drawn")]
        public bool debugPath = false;
        [Tooltip("如果debugPath是正确的,这是创建路径的最大数量的配子If debugPath is true, this is the maximum number of GameObjects to create to draw the path")]
        public int dbgPathLimit = 50;

        [Tooltip("如果不为空,文本网格将显示该循环驱动的线性值和角值If not null, the TextMesh will display the linear value and the angular value of this circular drive")]
        public TextMesh debugText = null;

        [Tooltip("自由度的输出角值,无限将增加或减少不受约束,以360模量来找到旋转的数量The output angle value of the drive in degrees, unlimited will increase or decrease without bound, take the 360 modulus to find number of rotations")]
        public float outAngle;

        private Quaternion start;

        private Vector3 worldPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);
        private Vector3 localPlaneNormal = new Vector3(1.0f, 0.0f, 0.0f);

        private Vector3 lastHandProjected;

        private Color red = new Color(1.0f, 0.0f, 0.0f);
        private Color green = new Color(0.0f, 1.0f, 0.0f);

        private GameObject[] dbgHandObjects;
        private GameObject[] dbgProjObjects;
        private GameObject dbgObjectsParent;
        private int dbgObjectCount = 0;
        private int dbgObjectIndex = 0;

        private bool driving = false;

        // If the drive is limited as is at min/max, angles greater than this are ignored
        private float minMaxAngularThreshold = 1.0f;

        private bool frozen = false;
        private float frozenAngle = 0.0f;
        private Vector3 frozenHandWorldPos = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector2 frozenSqDistanceMinMaxThreshold = new Vector2(0.0f, 0.0f);
    #endregion

    #region 初始化方法
    private void Freeze(SingleTransform hand)
        {
            frozen = true;
            frozenAngle = outAngle;
            frozenHandWorldPos = hand.virtualTransform.position;
            frozenSqDistanceMinMaxThreshold.x = frozenDistanceMinMaxThreshold.x * frozenDistanceMinMaxThreshold.x;
            frozenSqDistanceMinMaxThreshold.y = frozenDistanceMinMaxThreshold.y * frozenDistanceMinMaxThreshold.y;
        }

        //-------------------------------------------------
        private void UnFreeze()
        {
            frozen = false;
            frozenHandWorldPos.Set(0.0f, 0.0f, 0.0f);
        }

        //-------------------------------------------------
        private void Start()
        {
            
            limited = false;
            //rpcTransform = GetComponent<TexCore_RPCTransform>();
            if (childCollider == null)
            {
                childCollider = GetComponentInChildren<Collider>();
            }
          
            if (mylinearMapping == null)
            {
                mylinearMapping = GetComponent<TexCore_LinearMapping>();
            }

            worldPlaneNormal = new Vector3(0.0f, 0.0f, 0.0f);
            worldPlaneNormal[(int)axisOfRotation] = 1.0f;

            localPlaneNormal = worldPlaneNormal;

            if (transform.parent)
            {
                worldPlaneNormal = transform.parent.localToWorldMatrix.MultiplyVector(worldPlaneNormal).normalized;
            }

            if (limited)
            {
                start = Quaternion.identity;
                outAngle = transform.localEulerAngles[(int)axisOfRotation];

                if (forceStart)
                {
                    outAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
                }
            }
            else
            {
                start = Quaternion.AngleAxis(transform.localEulerAngles[(int)axisOfRotation], localPlaneNormal);
               
            }

            if (debugText)
            {
                debugText.alignment = TextAlignment.Left;
                debugText.anchor = TextAnchor.UpperLeft;
            }

            UpdateAll();
        }

        //-------------------------------------------------
      
      
        public void OpenHand()
        {
            childCollider.enabled = true;
        }

    #endregion

    #region 交互接口
    private void OnHandHoverBegin(SingleTransform hand)
        {

        }
        //-------------------------------------------------
       
        private void OnHandHoverEnd(SingleTransform hand)
        {
            
            grabbedWithType = GrabTypes.None;
            driving = false;
            mylinearMapping.IsHandOn = false;
        }

        private GrabTypes grabbedWithType;
        //-------------------------------------------------
     

        public void HandHoverUpdate(SingleTransform hand)
        {
            GrabTypes startingGrabType = hand.startingGrabType;
            bool isGrabEnding = (hand.startingGrabType == grabbedWithType) == false;//按下状态和上次不同
            //Debug.Log(startingGrabType + "__" + grabbedWithType);
            mylinearMapping.player = hand.virtualTransform.root.name;

            if (hand.startingGrabType != GrabTypes.None)
            {
                mylinearMapping.IsHandOn = true;
            }
            if (grabbedWithType == GrabTypes.None && startingGrabType != GrabTypes.None)//第一次按下//初始
            {
                //Debug.Log("转动手轮:第一次按下//初始");
                mylinearMapping.IsHandOn = true;
                grabbedWithType = startingGrabType;
                // Trigger was just pressed
                lastHandProjected = ComputeToTransformProjected(hand);

                driving = true;

                ComputeAngle(hand);
                UpdateAll();
            }
            else if (grabbedWithType != GrabTypes.None && isGrabEnding)//切换按下的按键//松开//转动
            {
                //Debug.Log("转动手轮:切换按下的按键//松开//转动");
                mylinearMapping.IsHandOn = false;
                // Trigger was just released

                driving = false;
                grabbedWithType = GrabTypes.None;
            }
            if (driving && isGrabEnding == false)//松开按键//不动
            {
                //Debug.Log("转动手轮://松开按键//不动");
                mylinearMapping.IsHandOn = true;
                //Debug.Log(gameObject.name + "按键按住");
                ComputeAngle(hand);
                UpdateAll();
            }
        }
    #endregion

    #region 旋转运算方法
    /// <summary>
    /// 计算转换投影
    /// </summary>
    /// <param name="xForm"></param>
    /// <returns></returns>
    private Vector3 ComputeToTransformProjected(Transform xForm)
        {
            Vector3 toTransform = (xForm.position - transform.position).normalized;
            Vector3 toTransformProjected = new Vector3(0.0f, 0.0f, 0.0f);

            // Need a non-zero distance from the hand to the center of the CircularDrive
            if (toTransform.sqrMagnitude > 0.0f)
            {
                toTransformProjected = Vector3.ProjectOnPlane(toTransform, worldPlaneNormal).normalized;
            }
            else
            {
                BpfLog.Warning(BpfLogCategory.Input, "tex_core_circular_drive.min_distance", string.Format("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject));
                Debug.Assert(false, string.Format("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString()));
            }

            if (debugPath && dbgPathLimit > 0)
            {
                DrawDebugPath(xForm, toTransformProjected);
            }

            return toTransformProjected;
        }
        /// <summary>
        /// 计算转换投影
        /// </summary>
        /// <param name="xForm"></param>
        /// <returns></returns>
        private Vector3 ComputeToTransformProjected(SingleTransform xForm)
        {
            Vector3 toTransform = (xForm.virtualTransform.position - transform.position).normalized;
            Vector3 toTransformProjected = new Vector3(0.0f, 0.0f, 0.0f);

            // Need a non-zero distance from the hand to the center of the CircularDrive
            if (toTransform.sqrMagnitude > 0.0f)
            {
                toTransformProjected = Vector3.ProjectOnPlane(toTransform, worldPlaneNormal).normalized;
            }
            else
            {
                BpfLog.Warning(BpfLogCategory.Input, "tex_core_circular_drive.min_distance", string.Format("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject));
                Debug.Assert(false, string.Format("<b>[SteamVR Interaction]</b> The collider needs to be a minimum distance away from the CircularDrive GameObject {0}", gameObject.ToString()));
            }

            if (debugPath && dbgPathLimit > 0)
            {
                DrawDebugPath(xForm, toTransformProjected);
            }

            return toTransformProjected;
        }


        //-------------------------------------------------
        private void DrawDebugPath(Transform xForm, Vector3 toTransformProjected)
        {
            if (dbgObjectCount == 0)
            {
                dbgObjectsParent = new GameObject("Circular Drive Debug");
                dbgHandObjects = new GameObject[dbgPathLimit];
                dbgProjObjects = new GameObject[dbgPathLimit];
                dbgObjectCount = dbgPathLimit;
                dbgObjectIndex = 0;
            }

            //Actual path
            GameObject gSphere = null;

            if (dbgHandObjects[dbgObjectIndex])
            {
                gSphere = dbgHandObjects[dbgObjectIndex];
            }
            else
            {
                gSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gSphere.transform.SetParent(dbgObjectsParent.transform);
                dbgHandObjects[dbgObjectIndex] = gSphere;
            }

            gSphere.name = string.Format("actual_{0}", (int)((1.0f - red.r) * 10.0f));
            gSphere.transform.position = xForm.position;
            gSphere.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            gSphere.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
            gSphere.gameObject.GetComponent<Renderer>().material.color = red;

            if (red.r > 0.1f)
            {
                red.r -= 0.1f;
            }
            else
            {
                red.r = 1.0f;
            }

            //Projected path
            gSphere = null;

            if (dbgProjObjects[dbgObjectIndex])
            {
                gSphere = dbgProjObjects[dbgObjectIndex];
            }
            else
            {
                gSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gSphere.transform.SetParent(dbgObjectsParent.transform);
                dbgProjObjects[dbgObjectIndex] = gSphere;
            }

            gSphere.name = string.Format("projed_{0}", (int)((1.0f - green.g) * 10.0f));
            gSphere.transform.position = transform.position + toTransformProjected * 0.25f;
            gSphere.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            gSphere.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
            gSphere.gameObject.GetComponent<Renderer>().material.color = green;

            if (green.g > 0.1f)
            {
                green.g -= 0.1f;
            }
            else
            {
                green.g = 1.0f;
            }

            dbgObjectIndex = (dbgObjectIndex + 1) % dbgObjectCount;
        }
        private void DrawDebugPath(SingleTransform xForm, Vector3 toTransformProjected)
        {
            if (dbgObjectCount == 0)
            {
                dbgObjectsParent = new GameObject("Circular Drive Debug");
                dbgHandObjects = new GameObject[dbgPathLimit];
                dbgProjObjects = new GameObject[dbgPathLimit];
                dbgObjectCount = dbgPathLimit;
                dbgObjectIndex = 0;
            }

            //Actual path
            GameObject gSphere = null;

            if (dbgHandObjects[dbgObjectIndex])
            {
                gSphere = dbgHandObjects[dbgObjectIndex];
            }
            else
            {
                gSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gSphere.transform.SetParent(dbgObjectsParent.transform);
                dbgHandObjects[dbgObjectIndex] = gSphere;
            }

            gSphere.name = string.Format("actual_{0}", (int)((1.0f - red.r) * 10.0f));
            gSphere.transform.position = xForm.virtualTransform.position;
            gSphere.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            gSphere.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
            gSphere.gameObject.GetComponent<Renderer>().material.color = red;

            if (red.r > 0.1f)
            {
                red.r -= 0.1f;
            }
            else
            {
                red.r = 1.0f;
            }

            //Projected path
            gSphere = null;

            if (dbgProjObjects[dbgObjectIndex])
            {
                gSphere = dbgProjObjects[dbgObjectIndex];
            }
            else
            {
                gSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                gSphere.transform.SetParent(dbgObjectsParent.transform);
                dbgProjObjects[dbgObjectIndex] = gSphere;
            }

            gSphere.name = string.Format("projed_{0}", (int)((1.0f - green.g) * 10.0f));
            gSphere.transform.position = transform.position + toTransformProjected * 0.25f;
            gSphere.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            gSphere.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
            gSphere.gameObject.GetComponent<Renderer>().material.color = green;

            if (green.g > 0.1f)
            {
                green.g -= 0.1f;
            }
            else
            {
                green.g = 1.0f;
            }

            dbgObjectIndex = (dbgObjectIndex + 1) % dbgObjectCount;
        }


        /// <summary>
        /// 从角度更新线性映射值
        /// </summary>
        // Updates the LinearMapping value from the angle
        //-------------------------------------------------
        private void UpdateLinearMapping()
        {
            if (limited)
            {
                // Map it to a [0, 1] value
                //把它映射到[0,1]值
                if (mylinearMapping != null)
                    
                mylinearMapping.Value = (outAngle - minAngle) / (maxAngle - minAngle);

            }
            else
            {
                // Normalize to [0, 1] based on 360 degree windings
                //根据360度绕组规范[0,1]
                //            float flTmp = outAngle / 360.0f;
                //            if (linearMapping != null)
                //                linearMapping.Value = flTmp - Mathf.Floor( flTmp );
                //mylinearMapping.Value = -outAngle ;
                LimitOutAngle();
                if (mylinearMapping != null)
                   
                mylinearMapping.Value = (outAngle - minAngle) / (maxAngle - minAngle);

            }

            UpdateDebugText();
        }
        public void LimitOutAngle()
        {
            if (outAngle < minAngle)
            {
                outAngle = minAngle;
            }
            else if (outAngle > maxAngle)
            {
                outAngle = maxAngle;
            }

        }
    #endregion

    #region 平滑

    /// <summary>
    /// 平滑值，随时间和速度减小
    /// </summary>
    public float smooth;//逐渐减小
    /// <summary>
    /// 初始平滑比例
    /// </summary>
    public float smoothValue=0.2f;
    /// <summary>
    /// 减速速度
    /// </summary>
    public float smoothSpeed=1f;

        /// <summary>
        /// 从角度更新线性映射值
        /// </summary>
        // Updates the LinearMapping value from the angle
        //-------------------------------------------------
    private void Update()
    {
        if (rotateGameObject && smooth!=0)
        {
            outAngle += smooth;
            if (smooth>0)
            {
                smooth -= smoothSpeed * Time.deltaTime;
                if (smooth < 0) smooth = 0;
            }
            else
            {
                if (smooth > 0) smooth = 0;
                smooth -=- smoothSpeed * Time.deltaTime;
            }
            transform.localRotation = start * Quaternion.AngleAxis(outAngle, localPlaneNormal);
        }else if (smoothValue == 0)
        {
            transform.localRotation = start * Quaternion.AngleAxis(outAngle, localPlaneNormal);
        }
    }


        /// <summary>
        /// 用线性映射值和角度更新调试文本网格
        /// </summary>
        // Updates the Debug TextMesh with the linear mapping value and the angle
        //-------------------------------------------------
        private void UpdateDebugText()
        {
            if (debugText)
            {
                debugText.text = string.Format("Linear: {0}\nAngle:  {1}\n", mylinearMapping.Value, outAngle);
            }
        }


        /// <summary>
        /// 用线性映射值和角度更新调试文本网格
        /// </summary>
        // Updates the Debug TextMesh with the linear mapping value and the angle
        //-------------------------------------------------
        private void UpdateAll()
        {
            UpdateLinearMapping();
            //UpdateGameObject();
            //UpdateDebugText();
        }


        //-------------------------------------------------
        // Computes the angle to rotate the game object based on the change in the transform
        // 基于转换的变化计算角度旋转游戏对象
        //-------------------------------------------------
       
        private void ComputeAngle(SingleTransform hand)
        {
            Vector3 toHandProjected = ComputeToTransformProjected(hand);

            if (!toHandProjected.Equals(lastHandProjected))
            {
                float absAngleDelta = Vector3.Angle(lastHandProjected, toHandProjected);

                if (absAngleDelta > 0.0f)
                {
                    if (frozen)
                    {
                        float frozenSqDist = (hand.virtualTransform.position - frozenHandWorldPos).sqrMagnitude;
                        if (frozenSqDist > frozenSqDistanceMinMaxThreshold.x)
                        {
                            outAngle = frozenAngle + Random.Range(-1.0f, 1.0f);

                            if (frozenSqDist >= frozenSqDistanceMinMaxThreshold.y)
                            {
                                onFrozenDistanceThreshold.Invoke();
                            }
                        }
                    }
                    else
                    {
                        Vector3 cross = Vector3.Cross(lastHandProjected, toHandProjected).normalized;
                        float dot = Vector3.Dot(worldPlaneNormal, cross);

                        float signedAngleDelta = absAngleDelta;

                        if (dot < 0.0f)
                        {
                            signedAngleDelta = -signedAngleDelta;
                        }

                        if (limited)
                        {
                            float angleTmp = Mathf.Clamp(outAngle + signedAngleDelta, minAngle, maxAngle);

                            if (outAngle == minAngle)
                            {
                                if (angleTmp > minAngle && absAngleDelta < minMaxAngularThreshold)
                                {
                                    outAngle = angleTmp;
                                    lastHandProjected = toHandProjected;
                                }
                            }
                            else if (outAngle == maxAngle)
                            {
                                if (angleTmp < maxAngle && absAngleDelta < minMaxAngularThreshold)
                                {
                                    outAngle = angleTmp;
                                    lastHandProjected = toHandProjected;
                                }
                            }
                            else if (angleTmp == minAngle)
                            {
                                outAngle = angleTmp;
                                lastHandProjected = toHandProjected;
                                onMinAngle.Invoke();
                                if (freezeOnMin)
                                {
                                    Freeze(hand);
                                }
                            }
                            else if (angleTmp == maxAngle)
                            {
                                outAngle = angleTmp;
                                lastHandProjected = toHandProjected;
                                onMaxAngle.Invoke();
                                if (freezeOnMax)
                                {
                                    Freeze(hand);
                                }
                            }
                            else
                            {
                                outAngle = angleTmp;
                                lastHandProjected = toHandProjected;
                            }
                        }
                        else
                        {
                            smooth = signedAngleDelta * smoothValue;
                            outAngle += signedAngleDelta;
                            lastHandProjected = toHandProjected;
                        }
                    }
                }
            }
        }
    #endregion
}


