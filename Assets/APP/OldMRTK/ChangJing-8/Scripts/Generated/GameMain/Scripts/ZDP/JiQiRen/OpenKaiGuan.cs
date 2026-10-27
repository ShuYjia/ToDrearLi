using MixedReality.Toolkit;
using MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ChangJing_8.ExportRuntime
{
    public class OpenKaiGuan : MonoBehaviour
    {
        public Animator animator;


        public void open() {
            JiFen.FinishTask(JiFen.taskKaiJi);
            
            if (animator != null)
            {
                animator.enabled = true;
            }

            LoadSceneObj.task = LoadSceneObj.Tesks.Pressswitch;
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
                    multCheck.CheckMultTask(1f);
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
