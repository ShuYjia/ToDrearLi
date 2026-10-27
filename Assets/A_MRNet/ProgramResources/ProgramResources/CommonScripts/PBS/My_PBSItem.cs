using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class My_PBSItem : MonoBehaviour
{
    public string data;
    public Transform parent;
    public List<My_PBSItem> childItem;
    public int lever;

    public TextMeshProUGUI title;

    public Transform model;

    public bool isUguiBtn = true;
    public Button button;

    public TexCore_UISelect state;
    private void Start()
    {
        if (isUguiBtn)
        {
            if (button == null) button = gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => {
                GetComponent<TexCore_SelectBase>().SetState(SelectState.Click);
                TexCore_BusinessManager.OnClick(gameObject, SceneManager.GetActiveScene().name);
            });
        }
    }

    public void BtnInit(string _data, int _lever, Transform _perent)
    {
        this.data = _data;
        this.lever = _lever;
        this.parent = _perent;
        if (parent != null && parent.parent.GetComponent<My_PBSItem>() != null)
        {
            parent.parent.GetComponent<My_PBSItem>().AddChild(this);
        }
        gameObject.name = "p_" + _data;
        title.text = _data.Substring(2);
    }
    public void AddChild(My_PBSItem _child)
    {
        if (childItem == null) childItem = new List<My_PBSItem>();
        childItem.Add(_child);
    }
    public void OnClick(string name)
    {

        if (lever == 0)
        {
            My_PBSManager.self.SetNodeActive(this);
        }
        else
        {
            TexCore_PlaneManager.self.activePlane.SendMessage("MenuLoadChild", data);
        }

    }
    public IEnumerator UpdateLayout()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.Find("root").GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        if (parent != null)
        {
            My_PBSItem parentItem = parent.GetComponent<My_PBSItem>();
            if (parentItem != null)
            {
                StartCoroutine(parentItem.UpdateLayout());
            }
            else
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(My_PBSManager.self.btnParent.GetComponent<RectTransform>());
            }
        }
        yield return 0;
    }
    public void SetActive(bool isShow)
    {
        state.state = (isShow ? SelectState.Click : SelectState.Nomal);
        state.SetSprite();
        if (model != null)
        {
            model.gameObject.SetActive(isShow);
        }
        transform.Find("root").gameObject.SetActive(isShow);
        StartCoroutine(UpdateLayout());
    }
}
