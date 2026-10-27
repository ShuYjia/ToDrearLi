using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _syc_resource._my_scripts
{
    public class ButtonManager : MonoBehaviour
    {
        [Header("Canvas")] 
        [SerializeField] private GameObject _canvas;

        [Header("Start Panel")] 
        [SerializeField] private GameObject _StartBtnPanel;
        [Header("Btn")] 
        [SerializeField] private Button _btnToSceneStartLAN;

        
        

        // Start is called before the first frame update
        void Start()
        {
            _canvas = GameObject.Find("Canvas");
            _StartBtnPanel = _canvas.transform.GetChild(1).gameObject;
            _btnToSceneStartLAN = _StartBtnPanel.transform.GetChild(0).gameObject.GetComponent<Button>();
            _btnToSceneStartLAN.onClick.AddListener(ToSceneStartLAN);
        }
        

        void ToSceneStartLAN()
        {
            SceneManager.LoadScene("UDPLobby");
        }
    }
}
