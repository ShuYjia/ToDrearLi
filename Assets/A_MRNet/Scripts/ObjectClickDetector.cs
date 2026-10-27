using UnityEngine;
using Bpf.Diagnostics;

/// <summary>
/// 无射线检测的物体鼠标点击检测脚本
/// 要求：物体必须有Collider组件，场景有EventSystem
/// </summary>
public class ObjectClickDetector : MonoBehaviour
{
    // 当鼠标点击到物体时触发（Unity内置回调）
    private void OnMouseDown()
    {
        // 触发自定义点击处理函数
        HandleObjectClick();
    }

    // 可选：鼠标按下并抬起后触发（点击完成）
    private void OnMouseUpAsButton()
    {
        // 若需要“点击松开”才触发，可改用这个回调
        // HandleObjectClick();
        BpfLog.Info(BpfLogCategory.Input, "object_click_detector.on_mouse_up_as_button", "[" + gameObject.name + "] 鼠标点击并松开");
        ToolsManager.self.t = null;
        TexCore_MouseCameraTarget.self.targetTransform = null;

    }

    /// <summary>
    /// 自定义的点击处理函数，你可以在这里写业务逻辑
    /// </summary>
    private void HandleObjectClick()
    {
        BpfLog.Info(BpfLogCategory.Input, "object_click_detector.handle_object_click", "你点击了物体：" + gameObject.name);

        ToolsManager.self.t = this.gameObject;
        TexCore_MouseCameraTarget.self.targetTransform = this.transform;
        TexCore_MouseCameraTarget.self.TargetingRaycast();

        // 示例逻辑：点击后物体缩放变化

        // 你可以添加任意逻辑：播放音效、调用其他脚本、改变颜色等
        // GetComponent<Renderer>().material.color = Color.red;
    }

    // 可选：鼠标悬停在物体上时触发
    private void OnMouseOver()
    {
        // 可用于实现鼠标悬浮提示、高亮等效果
        // Debug.Log($"鼠标悬停在 {gameObject.name} 上");
    }
}