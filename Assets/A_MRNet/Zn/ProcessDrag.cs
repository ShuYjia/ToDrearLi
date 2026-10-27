/****************************************************
	文件名：		ProcessDrag.cs

	功能：		流程拖拽
	功能需求：
        1.点击图片，图片复制然后吸附鼠标
		2.点击流程图四个其中一个碰撞体区域内：
			正确：
				1.使图片吸附到点击的流程图上，变灰
				2.图片不能再交互
			错误：
				1.使图片取消吸附鼠标，图片恢复默认状态
		3.加分方式
			整体分数，做错没有
*****************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProcessDrag : MonoBehaviour
{
    /// <summary>
    /// 右侧按钮父级（用于存放可点击的按钮）
    /// </summary>
    public Transform rightButtonsParent;

    /// <summary>
    /// 流程图中的步骤物体（四个步骤）
    /// </summary>
    public Transform[] processSteps;

    /// <summary>
    /// 当前步骤的分数（后续可与其他数据组合上传）
    /// </summary>
    public int currentScore;

    /// <summary>
    /// 当前选中的 UI 对象
    /// </summary>
    public GameObject selectedUIObject;

    /// <summary>
    /// 当前 UI 对象的 RectTransform
    /// </summary>
    public RectTransform selectedUIRectTransform;

    /// <summary>
    /// 是否正在拖拽 UI
    /// </summary>
    public bool isDragging;

    /// <summary>
    /// 步骤节点
    /// </summary>
    public Transform stepRoot;

    /// <summary>
    /// 确定按钮
    /// </summary>
    public Transform confirm;

    public GameObject title;

    private void OnEnable()
    {
        // 可以添加初始化代码
        DestroyDrag();
    }
    private void Start()
    {
        title.GetComponentInChildren<LayoutUpdate>().WaiteUpdateLayoutSingle();
    }

    /// <summary>
    /// 当点击右侧的按钮时调用，创建拖拽的 UI 并处理相应逻辑
    /// </summary>
    public void OnButtonClick(GameObject clickedButton)
    {
        // 根据按钮名称进行不同处理
        switch (clickedButton.name)
        {
            case "不动产测绘":
            case "准备工作":
            case "权属调查":
            case "资料整理":
                HandleButtonClick(clickedButton);
                break;
            case "确定":
                NavigationPanel.instance.Init("03调查选择");
                break;
            default:
                ValidateProcessStep(clickedButton);
                break;
        }
    }

    private void Update()
    {
        // 更新 UI 位置，使其跟随鼠标
        if (isDragging && selectedUIRectTransform != null)
        {
            Vector2 mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                selectedUIRectTransform.parent as RectTransform,
                Input.mousePosition,
                null,
                out mousePosition
            );
            selectedUIRectTransform.anchoredPosition = mousePosition;
        }
    }

    /// <summary>
    /// 处理按钮点击，创建并拖拽 UI 对象
    /// </summary>
    private void HandleButtonClick(GameObject clickedButton)
    {
        // 如果当前有已选中的 UI 对象，销毁它
        if (selectedUIObject != null)
        {
            Destroy(selectedUIObject);
            // 如果再次点击同一个按钮，直接返回
            if (selectedUIObject.name == clickedButton.name)
            {
                return;
            }
        }

        // 实例化新的 UI 对象
        selectedUIObject = Instantiate(clickedButton, stepRoot);
        selectedUIObject.name = clickedButton.name;
        selectedUIObject.transform.position = clickedButton.transform.position;

        // 获取并设置当前的 RectTransform
        selectedUIRectTransform = selectedUIObject.GetComponent<RectTransform>();
        selectedUIRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        selectedUIRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        selectedUIRectTransform.pivot = new Vector2(0.5f, 0.5f);

        // 禁用 RaycastTarget，防止拖拽时阻挡鼠标点击
        selectedUIObject.GetComponent<Image>().raycastTarget = false;

        // 禁用交互
        var interactionComponent = selectedUIObject.GetComponent<TexCore_Interaction_UGUI>();
        if (interactionComponent != null)
        {
            interactionComponent.SetState(SelectState.Nomal);
            Invoke(nameof(DisableUIInteraction), 1f);
        }

        isDragging = true;
    }

    /// <summary>
    /// 根据步骤名称验证当前是否正确
    /// </summary>
    private void ValidateProcessStep(GameObject processStepObject)
    {
        string[] stepNameParts = processStepObject.name.Split("_");
        if (selectedUIObject == null) return;
        if (stepNameParts.Length > 1 && stepNameParts[1] == selectedUIObject.name)
        {
            TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "步骤正确", 1.5f);
            // 可以在这里执行一些加分或成功逻辑
            selectedUIObject.transform.position = processStepObject.transform.position;
            Vector2 currentPos = selectedUIRectTransform.anchoredPosition;
            currentPos.y -= 30;
            selectedUIRectTransform.anchoredPosition = currentPos;
            selectedUIObject = null;
            selectedUIRectTransform = null;
            ShowConFirm();
        }
        else
        {
            TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, "步骤错误", 1.5f);
            // 可以在这里执行一些反馈或扣分逻辑
            Destroy(selectedUIObject);
        }
    }

    /// <summary>
    /// 禁用 UI 的交互
    /// </summary>
    private void DisableUIInteraction()
    {
        if (selectedUIObject != null)
        {
            var interactionComponent = selectedUIObject.GetComponent<TexCore_Interaction_UGUI>();
            if (interactionComponent != null)
            {
                interactionComponent.enabled = false;
            }
        }
    }

    private void ShowConFirm()
    {
        if (stepRoot.childCount == rightButtonsParent.childCount)
        {
            confirm.gameObject.SetActive(true);
        }
    }

    private void DestroyDrag()
    {
        foreach (Transform item in stepRoot)
        {
            Destroy(item.gameObject);
        }
    }
}
