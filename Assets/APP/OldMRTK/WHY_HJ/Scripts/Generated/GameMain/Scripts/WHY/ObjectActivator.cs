using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class ObjectActivator : MonoBehaviour
    {
        public GameObject MultCon;
        public GameObject[] objectsA;
        // public GameObject[] uprights;//安装立柱x4
        // public GameObject[] Putscrew;//将螺丝放到固定位置x16

        public GameObject objectB;
        public GameObject objectC;

        public LoadSceneObj.Tesks tesks;

        private void Start()
        {
            MultCon = GameObject.Find("MultController");
            if(tesks != LoadSceneObj.Tesks.None) {
                LoadSceneObj.task = tesks;
                if (LoadSceneObj.isMult)
                    {
                        MultCon.GetComponent<MultCheckInfo>().CheckMultTask(1f);
                    }
                    else
                    {
                        MultCon.GetComponent<ShiXun>().SendUserInfo();
                    }
            }
            
            bool allAObjectsActive = true;

            foreach (GameObject objA in objectsA)
            {
                if (!objA.activeSelf)
                {
                    allAObjectsActive = false;
                    break;
                }
            }

            if (allAObjectsActive)
            {
                objectB.SetActive(true);
                objectC.SetActive(true);
            }
        }
    }
}
