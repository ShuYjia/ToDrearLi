using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetWork03_2.ExportRuntime
{
    public class CKXController : MonoBehaviour
    {
        public Image CKXImage;
        public void Attach()
        {
            CKXImage.gameObject.SetActive(true);

        }

        public void ExitAttach()
        {
            CKXImage.gameObject.SetActive(false);

        }



    }
}
