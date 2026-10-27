using MixedReality.Toolkit.UX.Experimental;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChangJing_8.ExportRuntime
{
    public class BuyNextStep : MonoBehaviour
    {
        public Animator animator;
        public Animator animator1;
        public Animator animator2;
        public Animator animator3;
        public Animator animator4;
        public Animator animator5;
        public Animator animator6;
        public Animator animator7;
        public GameObject BuyNextStepTips;

        public GameObject gameObject;
        public GameObject gameObject1;
        public GameObject gameObject2;
        public GameObject gameObject3;
        public GameObject gameObject4;
        public GameObject gameObject5;
        public GameObject gameObject6;
        public GameObject gameObject7;
        public static float lastTime = 0f ;
        public bool isDisAble = false;

        static List<Animator> DingZiiList = new List<Animator>();
        static List<GameObject> DingZiiObject = new List<GameObject>();

        // Start is called before the first frame update
        void Start()
        {
            DingZiiList.Add(animator);
            DingZiiList.Add(animator1);
            DingZiiList.Add(animator2);
            DingZiiList.Add(animator3);
            DingZiiList.Add(animator4);
            DingZiiList.Add(animator5);
            DingZiiList.Add(animator6);
            DingZiiList.Add(animator7);
            DingZiiObject.Add(gameObject);
            DingZiiObject.Add(gameObject1);
            DingZiiObject.Add(gameObject2);
            DingZiiObject.Add(gameObject3);
            DingZiiObject.Add(gameObject4);
            DingZiiObject.Add(gameObject5);
            DingZiiObject.Add(gameObject6);
            DingZiiObject.Add(gameObject7);

        }

        // Update is called once per frame
        void Update()
        {
            float now = Time.time * 1000f;
            if (lastTime > 0 && !isDisAble && now - lastTime > 2000) {
                DisAbleDingzi();
                isDisAble = true;
           }
        }

        public static bool IsCanShowAllAnimation()
        {
            foreach (var item in DingZiiList)
            {
                if (!item.isActiveAndEnabled)
                {
                    Debug.Log("IsCanShowAllAnimation");
                    return true;
                }
            }
            return false;
        }
        public static void ShowAllAnimation() {
            lastTime = Time.time * 1000f;
            foreach (var item in DingZiiList)
            {
                if (!item.isActiveAndEnabled) {
                    item.enabled = true;
                }
            }

        }
        public static void DisAbleDingzi() {
            Debug.Log("DisAbleDingzi");
            foreach (var item in DingZiiObject)
            {
                item.SetActive(false);
            }
        }
    }
}
