using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetWork03_2.ExportRuntime
{
    public class EquitmentController : MonoBehaviour
    {
        public Image interchanger;
        public void Attach()
        {
            interchanger.gameObject.SetActive(true);

        }

        public void ExitAttach()
        {
            interchanger.gameObject.SetActive(false);

        }


    }
}
