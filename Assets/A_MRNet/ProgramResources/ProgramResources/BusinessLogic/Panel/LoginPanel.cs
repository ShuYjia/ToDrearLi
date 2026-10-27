using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using Bpf.Diagnostics;


public class LoginPanel : TexCore_PlaneBase
{
    private Coroutine _coroutine;
    private string _showText;

    public string nextPage;
  
    public static LoginPanel instance;

    public  TMP_InputField inputFileID;
    public TMP_InputField inputFildPass;

    public TextMeshProUGUI hit;

    public GameObject about;

    public Transform usersParent;

    /// <summary>
    /// 0:学员 1：教员  2：后台
    /// </summary>
    public int user = 0;

    private void Awake()
    {

    }

    private void Start()
    {
        if (TexCore_Const.user!=null)
        {
            TexCore_Background.instance.Logout(LogOutResult);

        }
        //usersParent.GetChild(0).GetComponent<TexCore_UISelect>().SetState(SelectState.Click);
        //user = 0;
    }

    public void LogOutResult(WebResult result)
    {
        TexCore_Const.user = null;
        if (true)
        {

        }
    }


    public override void OnClick(GameObject obj)
    {
        base.OnClick(obj);

        switch (obj.name)
        {
            case "学员":
                user = 0;
                break;
             
            case "登录":
                HandleLogin();
                
                break;
            case "游客访问":
                TexCore_LoadScene.instance.OnlyLoad(nextPage);
                break;
            case "关于":
                //about.SetActive(!about.activeSelf);
                break;
            case "关闭关于":
                //about.SetActive(false);
                break;
            default:
                break;
        }

        ShowHint(_showText);
    }

    public void OnEnable()
    {
        instance = this;
        hit.text = string.Empty;
        about.SetActive(false);
    }
    public void LoginReturn(LoginResult result)
    {
        hit.text = result.Msg;
        if (result.State)
        {
            TexCore_Const.playerName = result.Data.StuNo+"_" + result.Data.StuName;
            TexCore_Const.user = result.Data;
            TexCore_Const.token = result.Code;
            TexCore_LoadScene.instance.OnlyLoad(nextPage);
            //topicManager.SetActive(true);
            //gameObject.SetActive(false);
        }
    }

    //新增

    private void HandleLogin()
    {
        if (Input.GetKey(KeyCode.T))
        {
            TexCore_LoadScene.instance.OnlyLoad(nextPage);
           
            return;
        }

        BpfLog.Info(BpfLogCategory.Network, "login_panel.handle_login", "登录逻辑");
        string id = inputFileID.text;
        string pass = inputFildPass.text;

        if (string.IsNullOrWhiteSpace(id))
        {
            _showText = "请输入账号！！！";
            return;
        }

        if (string.IsNullOrWhiteSpace(pass))
        {
            _showText = "请输入密码！！！";
            return;
        }

        switch (user)
        {
            case 0: TexCore_Background.instance.StudentLogin(id, pass, LoginReturn); break;
            //case 1: inter = BackInterface.Self.DoMgrLogin; break;
            //case 2: inter = BackInterface.Self.DoStuLogin; break;

            default: break;
        }
        
        _showText = "登录中，请等待。。。";
    }

    private void ShowHint(string str)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(ShowHideHint(str));
    }

    /// <summary>
    /// 打开关闭Hint
    /// </summary>
    /// <returns></returns>
    private IEnumerator ShowHideHint(string str)
    {
        hit.gameObject.SetActive(true);
        hit.text = str;
        yield return new WaitForSeconds(3);
        hit.gameObject.SetActive(false);
    }
}
