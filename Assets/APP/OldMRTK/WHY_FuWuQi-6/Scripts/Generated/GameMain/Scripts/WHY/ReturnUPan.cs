using UnityEngine;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class ReturnUPan : MonoBehaviour
    {
        public GameObject targetB; // 要监视的物体B  
        public Transform targetC; // 物体A应该返回的物体C的Transform组件  
        private Vector3 defaultPosition; // 物体A的默认位置（即物体C的位置）  
        private bool isAtDefaultPosition = false; // 标记物体A是否当前在默认位置  

        private void Start()
        {
            // 在开始时设置默认位置为物体C的位置  
            defaultPosition = targetC.position;
        }

        private void Update()
        {
            // 检查物体B是否“可见”  
            bool isBVisible = IsTargetVisible(targetB);

            // 如果物体B不可见，并且物体A当前不在默认位置，则移动物体A到默认位置  
            if (!isBVisible && !isAtDefaultPosition)
            {
                transform.position = defaultPosition;
                isAtDefaultPosition = true; // 标记物体A现在在默认位置  
            }
            else if (isBVisible)
            {
                isAtDefaultPosition = false; // 如果物体B可见，则重置标记，允许移动物体A  
            }

        }

        private bool IsTargetVisible(GameObject target)
        {
            // 检查物体B是否激活且其Renderer组件是否可见  
            if (target == null || !target.activeInHierarchy)
            {
                return false; // 物体B未激活或不存在，认为不可见  
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.isVisible; // 根据Renderer的isVisible属性来判断是否可见  
            }

            // 如果没有Renderer组件，你可能需要其他逻辑来判断可见性，或者默认认为它可见。  
            return true;
        }
    }
}
