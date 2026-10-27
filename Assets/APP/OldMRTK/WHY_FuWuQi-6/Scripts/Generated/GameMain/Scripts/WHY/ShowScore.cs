using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WHY_FuWuQi_6.ExportRuntime
{
    public class ShowScore : MonoBehaviour
    {
        public Text scoreText;
        // Start is called before the first frame update
        void Start()
        {
            scoreText.text  += LoadSceneObj.score.ToString();
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}
