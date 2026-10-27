using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetWork03_2.ExportRuntime
{
    public class BJBController : MonoBehaviour
    {
        public Image NoteBook;
        // Start is called before the first frame update
        public void Attach()
        {
            NoteBook.gameObject.SetActive(true);

        }

        public void ExitAttach()
        {
            NoteBook.gameObject.SetActive(false);

        }

        // Update is called once per frame

    }
}
