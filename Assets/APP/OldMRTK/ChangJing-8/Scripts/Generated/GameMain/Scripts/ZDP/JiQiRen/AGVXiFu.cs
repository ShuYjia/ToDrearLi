using UnityEngine;
using UnityEngine.UI;

namespace ChangJing_8.ExportRuntime
{
    public class AGVXiFu : MonoBehaviour
    {
        public Transform target; // 吸附点
        public float attractionDistance = 5f; // 吸附距离阈值
        public float attractionSpeed = 2f; // 吸附速度
        public GameObject uiElement; // 要显示的UI元素
        public GameObject AGV;//多人状态下碰撞后更换的模型

        private bool isUIShown = false; // 记录UI显示状态的标志位

        void Start()
        {
            // 确保UI元素在开始时隐藏
            if (uiElement != null)
            {
                uiElement.SetActive(false);
            }
        }

        void Update()
        {
            // 计算物体1与吸附点的距离
            float distance = Vector3.Distance(transform.position, target.position);

            // 如果距离小于等于吸附距离阈值，则吸附物体1到吸附点
            if (distance <= attractionDistance)
            {
                // 平滑地移动物体1到吸附点
                transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * attractionSpeed);

                // 如果物体1已经吸附到物体2，并且UI还没有显示，则显示UI
                if (!isUIShown && Vector3.Distance(transform.position, target.position) < 0.1f)
                {
                    ShowUI();
                }
                if (LoadSceneObj.isMult)
                {
                    NetworkedObject agvNetworkedObject = AGV != null ? AGV.GetComponent<NetworkedObject>() : null;
                    NetworkedObject selfNetworkedObject = GetComponent<NetworkedObject>();
                    if (agvNetworkedObject != null)
                    {
                        agvNetworkedObject.CmdSetVisibilityTrue();
                    }
                    if (selfNetworkedObject != null)
                    {
                        selfNetworkedObject.CmdSetVisibilityFalse();
                    }
                }
            }
            else
            {
                // 如果距离大于吸附距离阈值，并且UI已经显示，则隐藏UI
                if (isUIShown)
                {
                    HideUI();
                }
            }
        }

        void ShowUI()
        {
            if (uiElement != null)
            {
                uiElement.SetActive(true);
                isUIShown = true;
            }
        }

        void HideUI()
        {
            if (uiElement != null)
            {
                uiElement.SetActive(false);
                isUIShown = false;
            }
        }
    }
}
