
using UnityEngine;


namespace NetWork03_2.ExportRuntime
{
    public class ServerOpen : MonoBehaviour
    {
        public Animator animator;
        // Start is called before the first frame update
        public void StartUp()
        {
            if (animator != null)
            {
                animator.enabled = true;
            }

            LoadSceneObj.task = LoadSceneObj.Tesks.ServerStartup;
            GameObject multCon = GameObject.Find("MultController");
            if (multCon == null)
            {
                Debug.LogWarning("Export module: MultController was not found in the active scene.");
                return;
            }

            if (LoadSceneObj.isMult)
            {
                MultCheckInfo multCheck = multCon.GetComponent<MultCheckInfo>();
                if (multCheck != null)
                {
                    multCheck.CheckMultTask(3f);
                }
            }
            else
            {
                ShiXun shiXun = multCon.GetComponent<ShiXun>();
                if (shiXun != null)
                {
                    shiXun.SendUserInfo();
                }
            }
        }

    }
}
