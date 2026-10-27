
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TexCore_ExamData : MonoBehaviour
{
    public static TexCore_ExamData self;

    [SerializeField]
    public List< ExamRecordBase> answer = new List<ExamRecordBase>();

    public List<string> operateTopic;

    public int activeOperateTopic = 0;

    public GameObject plane;

    private void Awake()
    {
        if (self != null)
        {
            Destroy(gameObject);
        }
        self = this;
    }
    private void Start()
    {
        if (answer == null) answer = new List<ExamRecordBase>();
    }

    public void Show(bool isShow)
    {
        plane.SetActive(isShow);
    }

    public void SetExam()
    {
        if (TexCore_Const.examData!=null)
        {
            operateTopic = new List<string>();
            foreach (var item in TexCore_Const.examData.QuestionList)
            {
                switch (item.QuestionType)
                {
                    case "4":
                        operateTopic.Add(item.QuestionTitle);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public void NextOperateTopic()
    {

    }

    public void OnClick(GameObject obj)
    {
        string[] strs=obj.name.Split('_');
        switch (strs[1])
        {
            case "返回考试":
                Show(false);
                break;
            case "提交答案":
                SubmitAll();
                break;
            default:
                break;
        }
    }
    public void AddAnswer(ExamRecordBase ans)
    {
        if (answer == null) answer = new List<ExamRecordBase>();
        for (int i = 0; i < answer.Count; i++)
        {
            if (answer[i].Question.Equals(ans.Question))
            {
                answer[i] = ans;
                return;
            }
        }
        answer.Add(ans);
    }

    public void SubmitAll()
    {
        
        TexCore_Background.instance.SubmitRecord(answer, SubmitReturn);
    }
    public void SubmitReturn(WebResult result)
    {
        if (result.State)
        {
            
            TexCore_LoadScene.instance.OnlyLoad("00LoginPage");
        }
    }
}
