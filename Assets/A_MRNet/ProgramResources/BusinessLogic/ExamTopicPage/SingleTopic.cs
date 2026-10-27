using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleTopic : MonoBehaviour
{
    public TexCore_Interaction_UGUI btn;
    public string select = "";
    public RectTransform topic;

    public int allindex;
    public int selfIndex;

    public int havaScore=0;

    /// <summary>
    /// 试题类型  0: 选择 1: 判断题 2: 多选题3:实操题4:主观题
    /// </summary>
    public string QuestionType;
    public ExamQuestion question;

    public GameObject hit;

    public QuestionModel question1;

    public bool isMiultySelect=false;

    public void OnSelected(string Select)
    {
        string select = "";
        switch (Select)
        {
            case "A"://选A
                select = "1";
                break;
            case "B"://B
                select = "2";
                break;
            case "C"://C
                select = "3";
                break;
            case "D"://D
                select = "4";
                break;
            case "t"://对
                select = "Y";
                break;
            case "f"://错
                select = "N";
                break;
            default:
                break;
        }
        if (isMiultySelect)
        {
            this.select += ","+select;
        }
        else
        {
            this.select = select;
        }

        this.btn.itemParent = null;
        this.btn.isToggle = false;
        //print(select + select == "");
        if (this.select=="")
        {
            this.btn.isSelect = false;
            this.btn.SetState(SelectState.Nomal);
        }
        else
        {
            this.btn.SetState(SelectState.Click);
        }
        
        if (hit!=null)
        {
            hit.SetActive(true);
        }
        if (question1!=null)
        {
            if (select.Equals(question1.Answers))
            {
                havaScore = 1;
            }
        }
    }
}
