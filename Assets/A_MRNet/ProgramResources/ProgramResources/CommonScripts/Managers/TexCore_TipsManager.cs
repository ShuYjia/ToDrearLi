
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.Examples.Demos;
using static System.Net.Mime.MediaTypeNames;
using Bpf.Diagnostics;
public class TexCore_TipsManager : MonoBehaviour
{
    //public TextMeshProUGUI titleText;

    public GameObject TitleImage;
    public GameObject HintImage;
    public GameObject cueMask;
    public InspectorDrivenDialog inspectorDrivenDialog;
    public DialogExample dialogExample;
    public TextMeshProUGUI TitleText;
    public TextMeshProUGUI HintText;
    public Transform endPos;

    public Vector3 v3;

    public bool isEnble = true;

    /// <summary>
    /// Probed on the authored title font. Every guidance line and every reject
    /// reason contains these characters, so a font that can draw them can draw
    /// the whole hint. The digit is part of the probe because the hint text also
    /// carries step numbers and value ranges.
    /// </summary>
    private const string TitleFontProbe = "确定8";

    private void Awake()
    {
        HideDialogPoolTemplateGraphics();
    }

    private void HideDialogPoolTemplateGraphics()
    {
        if (dialogExample == null || dialogExample.DialogPool == null)
        {
            return;
        }

        DialogPool dialogPool = dialogExample.DialogPool;
        dialogPool.transform.localScale = Vector3.zero;
        foreach (Graphic graphic in dialogPool.GetComponents<Graphic>())
        {
            graphic.enabled = false;
        }

        foreach (TMP_Text text in dialogPool.GetComponentsInChildren<TMP_Text>(true))
        {
            text.gameObject.SetActive(false);
        }
    }
    
    public void Start()
    {
        HideDialogPoolTemplateGraphics();
      //  v3 = HintImage.transform.position;
    }

    public void OnEnable()
    {
        //titleText.gameObject.SetActive(false);
        //tipText.gameObject.SetActive(false);
        switch (TexCore_Const.mode)
        {
            case Mode.练习:
               // HintImage.SetActive(true);
                TexCore_EventCenter.AddListener<string, float>(TexCore_EventType.显示提示, Tip);
                TexCore_EventCenter.AddListener(TexCore_EventType.关闭提示, HodelReText);
                if(isEnble)
                {
                    TitleImage.SetActive(true);
                    TexCore_EventCenter.AddListener<string>(TexCore_EventType.显示标题, Title);
                }       
                break;
            case Mode.考核:
                TitleImage.SetActive(true);
                HintImage.SetActive(false);
                TexCore_EventCenter.AddListener<string>(TexCore_EventType.显示标题, Title);
                //TexCore_EventCenter.AddListener<string, float>(TexCore_EventType.显示提示, Tip);
                break;
            default:
                break;
        }
        TexCore_EventCenter.AddListener<bool>(TexCore_EventType.开关标题, SwitchTitle);
        if (UnityEngine.Application.isPlaying)
        {
            EnsureTitleFont();
        }

        BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tips_manager.on_enable", "提示初始化完成");
        
    }

    /// <summary>
    /// The authored title font only covers Latin, and TMP then draws every
    /// Chinese hint as an empty box. When the loaded scene already provides a
    /// font that can draw the hint text, use that one instead. The scenes keep
    /// their authored values: this only swaps the font of the live instance.
    /// </summary>
    public void EnsureTitleFont()
    {
        if (TitleText == null)
        {
            return;
        }

        TMP_FontAsset current = TitleText.font;
        if (CanDrawTitle(current))
        {
            return;
        }

        TMP_FontAsset replacement = FindSceneTitleFont();
        if (replacement == null)
        {
            BpfLog.Warning(BpfLogCategory.TexCore, "tex_core_tips_manager.title_font_missing",
                "标题栏字体缺少中文字形，当前场景里也没有可替换的中文字体。", this);
            return;
        }

        TitleText.font = replacement;
        TitleText.fontSharedMaterial = replacement.material;
        BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tips_manager.title_font_replaced",
            $"标题栏字体已从“{(current != null ? current.name : "空")}”换成“{replacement.name}”，中文提示不再显示为方框。",
            this);
    }

    /// <summary>
    /// Asks the font itself whether it can produce the glyphs, which also lets a
    /// dynamic font add a missing character to its atlas. Fallback tables are
    /// ignored on purpose: the title text needs a font whose own atlas covers the
    /// hint, not one that only reaches a fallback that a scene may not ship.
    /// </summary>
    private static bool CanDrawTitle(TMP_FontAsset font)
    {
        if (font == null)
        {
            return false;
        }

        for (int index = 0; index < TitleFontProbe.Length; index++)
        {
            if (!font.HasCharacter(TitleFontProbe[index], false, true))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The replacement comes from a text object of the loaded scene, so it is a
    /// font this scene already ships with instead of a new dependency.
    /// </summary>
    private TMP_FontAsset FindSceneTitleFont()
    {
        foreach (TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())
        {
            if (text == null || text == TitleText || text.font == null)
            {
                continue;
            }

            if (!text.gameObject.scene.IsValid())
            {
                continue;
            }

            if (CanDrawTitle(text.font))
            {
                return text.font;
            }
        }

        return null;
    }
    public void Title(string tex)
    {
        TitleText.gameObject.SetActive(true);
        TitleText.text = tex;
        LayoutRebuilder.ForceRebuildLayoutImmediate(TitleText.GetComponent<RectTransform>());
        //retext = tex;
    }

    Tweener tweener;

    public void Tip(string tex)
    {
        TexCore_EventCenter.Broadcast<string, float>(TexCore_EventType.显示提示, tex, 1);
    }

    public void Tip(string tex, float hodel = 1)
    {
        //HintImage.transform.position = v3;
        ////判断提示框是否在播放动画
        //if (tweener.IsActive())
        //{
        //    HintImage.transform.DOKill();
        //}
        //CancelInvoke("HodelReText");
        //StopAllCoroutines();
        //HintImage.gameObject.SetActive(true);
        //HintText.text = tex.Replace("\\n", "\n");
        //LayoutRebuilder.ForceRebuildLayoutImmediate(HintImage.GetComponent<RectTransform>());
        //tweener = HintImage.transform.DOMove(endPos.position, 0.5f);

        //inspectorDrivenDialog.Header = "提示";
        //inspectorDrivenDialog.Body = tex.Replace("\\n", "\n"); ;
        dialogExample.SpawnNeutralDialogFromCode("提示", tex.Replace("\\n", "\n"));

    }

public void SwitchTitle(bool bl)
    {
        TitleImage.SetActive(bl);
    }

    public void HodelReText()
    {
        //tweener = HintImage.transform.DOMove(v3, 0.5f).OnComplete(() => 
        //{
        //    //cueMask.gameObject.SetActive(false);
        //    HintText.text = string.Empty;
        //    LayoutRebuilder.ForceRebuildLayoutImmediate(HintImage.GetComponent<RectTransform>());
        //});
    }


    //private void ShowTitleText(string tex)
    //{
    //    titleText.gameObject.SetActive(true);
    //    titleText.text = tex;
    //}
    //private void ShowTipText(string tex)
    //{
    //    tipText.gameObject.SetActive(true);
    //    tipText.text = tex;
    //}

    private void ShowTipText(string tex,float hodel=3f)
    {
        TitleText.gameObject.SetActive(true);
        TitleText.text = tex;

        Invoke("CloseTip", hodel);
    }

    public void CloseTip()
    {
        TitleText.gameObject.SetActive(false);
    }

    public void OnDisable()
    {
        //HintImage.gameObject.SetActive(false);
        switch (TexCore_Const.mode)
        {
            case Mode.练习:
                TexCore_EventCenter.RemoveListener<string, float>(TexCore_EventType.显示提示, Tip);
                TexCore_EventCenter.RemoveListener(TexCore_EventType.关闭提示, HodelReText);
                if (isEnble)
                {
                    TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.显示标题, Title);
                }
                break;
            case Mode.考核:

                TexCore_EventCenter.RemoveListener<string>(TexCore_EventType.显示标题, Title);
                //TexCore_EventCenter.RemoveListener<string, float>(TexCore_EventType.显示提示, Tip);
                break;
            default:
                break;
        }
        TexCore_EventCenter.RemoveListener<bool>(TexCore_EventType.开关标题, SwitchTitle);
    }
}
