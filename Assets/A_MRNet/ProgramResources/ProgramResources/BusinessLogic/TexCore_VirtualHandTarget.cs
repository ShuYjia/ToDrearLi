//using Realis;
//using Realis.InputSystem;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using static UnityEditor.Progress;

//public class TexCore_VirtualHandTarget : MonoBehaviour
//{
//    public static TexCore_VirtualHandTarget self;

//    public HandleDevice handleDevice;
//    public TexCore_OverlapCollision virtualHand;

//    public GameObject rayHit;
//    private GameObject downHit;

//    private void Awake()
//    {
//        self = this;
//    }

//    private void OnEnable()
//    {
//        handleDevice.OnObjectEntered.AddListener(HoverEntered);
//        handleDevice.OnObjectExited.AddListener(HoverExited);
//        handleDevice.OnButtonPressed.AddListener(ButtonDown);
//        handleDevice.OnButtonReleased.AddListener(ButtonUp);
//    }

//    public void FixedUpdate()
//    {
       
//        if (virtualHand.gameObject.activeSelf ) 
//        {
//            //virtualHand.transform.position = handleDevice.PointerRay.origin + handleDevice.PointerRay.direction;
//            virtualHand.transform.position = handleDevice.HitInfo.worldPosition;
//            if ( virtualHand.colliders != null && virtualHand.colliders.Length > 0)
//            {
                
//                GrabTypes grabTypes;
//                if (handleDevice.GetButton(0)) grabTypes = GrabTypes.Pinch;
//                else if (handleDevice.GetButton(1)) grabTypes = GrabTypes.Pinch;
//                else if (handleDevice.GetButton(1)) grabTypes = GrabTypes.Grip;
//                else grabTypes = GrabTypes.None;

//                for (int i = 0; i < virtualHand.colliders.Length; i++)
//                {
//                    //virtualHand.colliders[i].gameObject.SendMessage("HandHoverUpdate", new SingleTransform(virtualHand.transform, grabTypes));
//                    IHandHoverUpdate handHoverUpdate = virtualHand.colliders[i].gameObject.GetComponent<IHandHoverUpdate>();
//                    if (handHoverUpdate != null)
//                        handHoverUpdate.HandHoverUpdate(new SingleTransform(virtualHand.transform, grabTypes));

//                    if (grabTypes!=GrabTypes.None)
//                    {
//                        TexCore_ObjSelect OS = virtualHand.colliders[i].gameObject.GetComponent<TexCore_ObjSelect>();
//                        if (OS != null && OS.state != SelectState.Click)
//                        {
//                            OS.SetState(SelectState.Click);
                            
//                        }
                            
//                    }
//                }
//            }
//            //else
//            //{
//            //    virtualHand.gameObject.SetActive(false);
//            //}
           
//        }
//    }

//    public void HoverEntered(Origin _origin,GameObject obj )
//    {
//        if (obj.layer!=5)//判断交互到的三维物体的layer层
//        {
//            virtualHand.gameObject.SetActive(true);
//            virtualHand.transform.position =  handleDevice.HitInfo.worldPosition;

//            TexCore_ObjSelect OS = obj.GetComponent<TexCore_ObjSelect>();
//            if (OS != null && OS.state != SelectState.Click)
//                OS.SetState(SelectState.Hover);
//        }
//        rayHit = obj;
//        //Debug.Log("碰撞目标:" + obj.name);
//    }
//    public void HoverExited(Origin origin, GameObject obj)
//    {
//        //Debug.Log("离开目标:" + obj.name);
//        TexCore_ObjSelect OS = obj.GetComponent<TexCore_ObjSelect>();
//        if (OS != null /*&& OS.state != SelectState.Click*/)
//            OS.SetState(SelectState.Nomal);
//        rayHit=null;
//    }
//    public GrabTypes grabTypes;
//    public void ButtonDown(Origin origin,int btn)
//    {
//        // Debug.Log("按键按下:" + btn);
//        switch (btn)
//        {
//            case 0: grabTypes = GrabTypes.Pinch; break;
//            case 1: grabTypes = GrabTypes.Pinch; break;
//            case 2: grabTypes = GrabTypes.Grip; break;
//            default:
//                break;
//        }
//        downHit = rayHit;
//    }
//    public void ButtonUp(Origin origin, int btn)
//    {
//        // Debug.Log("按键抬起:" + btn);
//        if (rayHit == null) return;
//        if(downHit == null) return;
//        if(downHit.Equals(rayHit)==false) return;
//        switch (btn)
//        {
//            case 0:
//            case 1:
//                TexCore_BusinessManager.OnClick(rayHit, SceneManager.GetActiveScene().name);
//                break;
//            case 2: break;
//            default:
//                break;
//        }
//    }

//    //public void Click()
//    //{
//    //    if (hoverObjs != null && hoverObjs.Count > 0)
//    //    {
            
//    //        if (handleDevice.GetButton(0)) grabTypes = GrabTypes.Pinch;
//    //        else if (handleDevice.GetButton(1)) grabTypes = GrabTypes.Pinch;
//    //        else if (handleDevice.GetButton(2)) grabTypes = GrabTypes.Grip;
//    //        else grabTypes = GrabTypes.None;
//    //        if (grabTypes != GrabTypes.None)
//    //        {
//    //            foreach (var item in hoverObjs)
//    //            {
//    //                TexCore_BusinessManager.OnClick(item, SceneManager.GetActiveScene().name);
//    //            }
//    //        }

//    //    }
//    //}
//}
