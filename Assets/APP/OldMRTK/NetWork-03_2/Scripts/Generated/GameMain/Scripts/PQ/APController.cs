using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetWork03_2.ExportRuntime
{
    public class APController : MonoBehaviour
    {
        public Image APImage;
        // Start is called before the first frame update
        public void Attach()
        {
            APImage.gameObject.SetActive(true);

        }

        public void ExitAttach()
        {
            APImage.gameObject.SetActive(false);

        }

        // Update is called once per frame

    }
}
