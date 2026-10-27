using UnityEngine;

namespace WHY_HJ.ExportRuntime
{
    public class NetworkedObject : MonoBehaviour
    {
        public bool isVisible = true;
        public bool isOwned = true;
        public bool isServer = false;
        private Transform cubeposition;

        void Start()
        {
            gameObject.SetActive(isVisible);
            cubeposition = transform;
        }

        public void CmdSetVisibilityTrue()
        {
            isVisible = true;
            gameObject.SetActive(true);
        }

        public void CmdSetVisibilityFalse()
        {
            isVisible = false;
            gameObject.SetActive(false);
        }

        public void CmdSetVisibility()
        {
            isVisible = !isVisible;
            gameObject.SetActive(isVisible);
        }

        public void RpcSetVisibility(bool value)
        {
            isVisible = value;
            gameObject.SetActive(isVisible);
        }

        public void GetAuthority()
        {
        }

        public void ClearAllAuthority()
        {
        }
    }
}
