using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// 触发器基类
namespace WHY_HJ.ExportRuntime
{
    public class TriggerBase : MonoBehaviour
    {
        private GameObject localPlayerGameObject;
        // 定义需要在触发器中操作的游戏对象
        public GameObject blue, green, cube, shi;    // 标记是否有对象进入了红色区域
        public bool IsEnterRed = false;    // 存储所有触发器的静态列表
        //public static List<TriggerBase> AllTriggers = new List<TriggerBase>();    // 当游戏对象创建时，将其添加到触发器列表中
        private GameObject collidedObject;
        private Vector3 cubeposition;

        private void Awake()
        {
            cubeposition = new Vector3(cube.GetComponent<Transform>().position.x, cube.GetComponent<Transform>().position.y, cube.GetComponent<Transform>().position.z);
            // AllTriggers.Add(this);
        }

        /* // 当游戏对象销毁时，将其从触发器列表中移除
        private void OnDestroy()
        {
            AllTriggers.Remove(this);
        }
       */
        // 当有对象进入触发器的Collider时调用
        private void OnTriggerEnter(Collider other)
        {
            // 如果进入的对象标签与触发器对象标签相同
            if (other.gameObject.tag == gameObject.tag)
            {
                green.SetActive(true); // 显示绿色游戏对象
                IsEnterRed = true; // 设置标记为true
                Debug.Log(gameObject.tag); // 输出进入的游戏对象标签
                collidedObject = other.gameObject; // 存储碰撞物体的引用
                NetworkedObject networkedObject = cube != null ? cube.GetComponent<NetworkedObject>() : null;
                if (networkedObject == null || (!networkedObject.isOwned && !networkedObject.isServer))
                {
                    SetObjectsActive();
                }
            }
        }

        // 当对象离开触发器的Collider时调用
        private void OnTriggerExit(Collider other)
        {
            // 如果离开的对象标签与触发器对象标签相同
            if (other.gameObject.tag == gameObject.tag)
            {
                green.SetActive(false); // 隐藏绿色游戏对象
                IsEnterRed = false; // 设置标记为false
                collidedObject = null; // 清除碰撞物体的引用

            }
        }


        // 设置游戏对象的激活状态
        public void SetObjectsActive()
        {
            // 遍历场景中的所有NetworkIdentity组件来查找本地玩家
            // foreach (NetworkIdentity ni in FindObjectsOfType<NetworkIdentity>())
            // {
            //     if (ni.isLocalPlayer)
            //     {
            //         // 找到了本地玩家的游戏对象
            //         localPlayerGameObject = ni.gameObject;
            //         break;
            //     }
            // }  
            if (IsEnterRed == true)
            {
                cube.transform.position = cubeposition; // 将立方体的位置设置为触发器的位置
                Debug.Log(cube.transform.position);
                shi.SetActive(true); // 显示实物对象
                blue.SetActive(false); // 隐藏蓝色游戏对象          
                green.SetActive(false); // 隐藏绿色游戏对象
                IsEnterRed = false;
                if (LoadSceneObj.isMult)
                {
                    NetworkedObject networkedObject = cube != null ? cube.GetComponent<NetworkedObject>() : null;
                    if (networkedObject != null && networkedObject.isOwned)
                    {
                        networkedObject.CmdSetVisibilityFalse(); // 隐藏碰撞物体
                    }
                }
                else
                {
                    cube.SetActive(false);
                }


                Debug.Log("触发器被触发");
            }
        }
    }
}
