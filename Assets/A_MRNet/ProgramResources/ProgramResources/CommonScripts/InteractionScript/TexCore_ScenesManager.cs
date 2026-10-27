using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TexCore_ScenesManager : MonoBehaviour
{
    public static TexCore_ScenesManager self;

    public string activeSceneName;

    public void OnEnable()
    {
        activeSceneName = SceneManager.GetActiveScene().name;
    }
}
