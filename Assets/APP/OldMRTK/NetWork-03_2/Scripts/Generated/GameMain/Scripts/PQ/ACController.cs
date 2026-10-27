using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetWork03_2.ExportRuntime
{
    public class ACController : MonoBehaviour
    {
        public Image ACImage;
        // Start is called before the first frame update
        public void Attach()
        {
            ACImage.gameObject.SetActive(true);

        }

        public void ExitAttach()
        {
            ACImage.gameObject.SetActive(false);

        }


    }
}
