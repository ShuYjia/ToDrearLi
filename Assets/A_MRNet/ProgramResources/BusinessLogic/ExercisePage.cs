using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using DG.Tweening;
using Newtonsoft.Json;
using System.Text;
using Bpf.Diagnostics;

public class ExercisePage : MonoBehaviour
{
    
    public static ExercisePage self;

    public RectTransform topicParent;

    public Transform node1;//题目模块root
    public Transform node2;
    public Transform node3;

    public Transform topic1;//解答
    public TextMeshProUGUI topic1Text;
    public TMP_InputField topic1Input;

    public Transform topic2;//选择

    public Transform topic3;//判断

    public Transform node1root;//按钮root
    public Transform node2root;
    public Transform node3root;

    public GameObject btnPrefab;
    public Transform noderootParent;

    public List<SingleTopic> topics;

    public int textCount = 0;

    public int selectCount = 0;

    public int boolCout = 0;

    public TextMeshProUGUI timeText;
    public float times;

    public GameObject enter;

    public GameObject over;

    public Transform topisCanvas;

    public GameObject Over;

    public void Awake()
    {
        self = this;
        //List<ExamQuestion> list = new List<ExamQuestion>();
        //for (int i = 0; i < 13; i++)
        //{
        //    ExamQuestion examQuestion = new ExamQuestion();
        //    examQuestion.PointCode = "001";
        //    examQuestion.QuestionTitle = "问题 ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();" + i.ToString();
        //    examQuestion.QuestionType = "0";
        //    List<ExamQuestionOption> Options = new List<ExamQuestionOption>();
        //    Options.Add(new ExamQuestionOption(2, " ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();答案b"));
        //    Options.Add(new ExamQuestionOption(4, "答案d ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();"));
        //    Options.Add(new ExamQuestionOption(3, "答案c ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();"));
        //    Options.Add(new ExamQuestionOption(1, "答案A ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();"));
        //    examQuestion.Remark = "答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析";
        //    examQuestion.Options = Options;
        //    list.Add(examQuestion);
        //}
        //Init(list);
    }

    public void OnClick(GameObject obj)
    {
        switch (obj.tag)
        {
            case "topic1"://点击解答题序号
            case "topic2"://点击选择题序号
            case "topic3"://点击判断题序号
                SingleTopic single = topics[int.Parse(obj.name)-1];
                topicParent.DOLocalMoveY( Mathf.Clamp( Math.Abs(single.topic.localPosition.y)- single.topic.sizeDelta.y,0, topicParent.sizeDelta.y- topisCanvas.GetComponent<RectTransform>().sizeDelta.y), 2);

                break;
            default:

                switch (obj.name)
                {
                    case "返回主页":
                        TexCore_LoadScene.instance.OnlyLoad("1MainPage");
                        break;
                    case "提交":
                        enter.SetActive(true);
                        break;
                    case "确定提交":
                        EndExam();
                        break;
                    case "不提交":
                        enter.SetActive(false);
                        break;
                    case "A"://选A
                    case "B"://B
                    case "C"://C
                    case "D"://D
                    case "t"://对
                    case "f"://错
                        SingleTopic singleTopic = topics[int.Parse(obj.transform.parent.parent.name)].GetComponent<SingleTopic>();
                        singleTopic.OnSelected(obj.name);
                        break;
                    default:
                        break;
                }
                break;
        }
    }

    public IEnumerator StartExam()
    {
        //topic1Text.text = BackInterface.Self.情景问题;
        SingleTopic singleTopic = topic1.GetComponent<SingleTopic>();
        QuestionModel exam = new QuestionModel();
        singleTopic.topic = topic1.GetComponent<RectTransform>();

        BuildTopicBtn(node1root, btnPrefab);
        yield return null;

        singleTopic.btn = node1root.GetChild(textCount).GetComponent<TexCore_Interaction_UGUI>();

        topics.Add(singleTopic);
        //if (TexCore_Const.examDic!=null)
        //{
        //    Init(TexCore_Const.examDic.Data);
        //    times = 60 * 60;
        //}
        //else
        //{
        //    times = 10 * 60;
        //}
        
        StartCoroutine(TimeLine());
    }

    IEnumerator TimeLine()
    {
        int min = 0;
        while (times>0)
        {
            times -= 1f;
            min = (int)(times / 60f);
            timeText.text = min.ToString("D2") + ((int)(times - 60 * min)).ToString("D2");
            yield return new WaitForSeconds(1);
        }
        EndExam();
    }

    public void EndExam()
    {
        int allScore = 0;

        for (int i = 0; i < topics.Count; i++)
        {
            if (topics[i].question1!=null)
            {
                allScore += topics[i].havaScore;
            }
        }
        enter.SetActive(false);
        over.SetActive(true);
        over.transform.Find("text").GetComponent<TextMeshProUGUI>().text = "答题完成，您的总分为" + allScore.ToString();
        //Over.GetComponentInChildren<TextMeshProUGUI>().text = "答题完成，您的总分为" + allScore.ToString();


        #region
        //List<ExamRecord> sendAll = new List<ExamRecord>();

        //for (int i = 0; i < topics.Count; i++)
        //{
        //    if (i==0)
        //    {
        //        ExamRecord exam = new ExamRecord();
        //        exam.ExamID = TexCore_Const.ExamID;
        //        exam.StudentNo = TexCore_Const.user.Data.StuNo;

        //        exam.QuestionRel = 001;

        //        exam.PointCode = "0";
        //        exam.SubMenu = "情景题";
        //        exam.Question = BackInterface.Self.情景问题;
        //        exam.QuestionType = "4";

        //        exam.Answers = topic1Input.GetComponent<TMP_InputField>().text;
        //        exam.IsCorrect = "1";
        //        exam.Mark = 0;
        //        exam.Correct = "略";
        //        sendAll.Add(exam);
        //    }
        //    else
        //    {
        //        ExamRecord exam = new ExamRecord();
        //        exam.ExamID = TexCore_Const.ExamID;
        //        exam.StudentNo = TexCore_Const.user.Data.StuNo;

        //        exam.QuestionRel = topics[i].question.FlowID;

        //        exam.PointCode = topics[i].question.QuestionType;
        //        exam.SubMenu = topics[i].question.QuestionType.Equals("0") ? "选择题" : "判断题";
        //        exam.Question = topics[i].question.QuestionTitle;
        //        exam.QuestionType = topics[i].question.QuestionType;

        //        exam.Answers = topics[i].select;
        //        exam.IsCorrect = "1";
        //        exam.Mark = 0;
        //        exam.Correct = "略";
        //        sendAll.Add(exam);
        //    }
        //}
        //string postData = JsonConvert.SerializeObject(sendAll);
        //byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
        //Debug.Log("组合数据成功" + postData);

        //LXF_SendData.instance.SubmitRecord1(sendAll);
        #endregion
    }
    public void ExamUpReturn(WebResult result)
    {
        enter.SetActive(false);
        over.SetActive(true);
        over.transform.Find("text").GetComponent<TextMeshProUGUI>().text = result.Msg;
        if (result.State)
        {

        }
        else
        {

        }
    }

    

    public void Init(List<QuestionModel> questions)
    {
        topisCanvas.localScale = new Vector3(0,1f,1f);
        for (int i = 0; i < questions.Count; i++)
        {

            BuildTopic(questions[i]);
        }
        
        SetNodeIndex();
        StartCoroutine(ResetHight());
    }

    public void BuildTopic(QuestionModel question)
    {
        if (question==null)
        {
            return;
        }
        switch (question.QuestionType)
        {
            case "0":
                BuildSelect(question);
                break;
            case "1":
                BuildBool(question);
                break;
            case "2":
                BuildSelect(question);
                break;
            case "3":
                break;
            case "4":
                //BuildText(question);
                break;
            default:
                break;
        }
    }

    public IEnumerator ResetHight()
    {
        yield return 0;
        StartCoroutine( ControlSizeForChildCount(node1root.GetComponent<RectTransform>(),5));
        StartCoroutine(ControlSizeForChildCount(node2root.GetComponent<RectTransform>(),5));
        StartCoroutine(ControlSizeForChildCount(node3root.GetComponent<RectTransform>(),5));
        ControlSizeForChild(noderootParent.GetComponent<RectTransform>());
        ControlSizeForChild(topicParent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(noderootParent.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(topicParent);
        topisCanvas.localScale = new Vector3(1f, 1f, 1f);

        
    }

    public void BuildText(QuestionModel question)
    {
        GameObject newTopic;
        if (selectCount == 0)
        {
            newTopic = topic1.gameObject;
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (textCount+1).ToString() + "." + question.QuestionTitle;

        }
        else
        {
            newTopic = Instantiate(topic1.gameObject, topicParent);
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (textCount).ToString() + "." + question.QuestionTitle;

        }

        newTopic.transform.SetSiblingIndex(node2.GetSiblingIndex());

        SingleTopic singleTopic= newTopic.GetComponent<SingleTopic>();
        
        singleTopic.question1 = question;

        
        topics.Add(singleTopic);
        BuildTopicBtn(node3root, btnPrefab);
        singleTopic.btn = node1root.GetChild(textCount).GetComponent<TexCore_Interaction_UGUI>();
        textCount++;
    }
    public void BuildSelect(QuestionModel question)
    {
        GameObject newTopic;
        if (selectCount==0)
        {
            newTopic = topic2.gameObject;
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (topics .Count+ 1).ToString() + "." + question.QuestionTitle;

        }
        else
        {
            newTopic = Instantiate(topic2.gameObject, topicParent);
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (topics.Count+1).ToString() + "." + question.QuestionTitle;

        }

        int itemCount=0;
        GameObject item;
        if (string.IsNullOrEmpty( question.Option1)==false)
        {
            item = newTopic.transform.GetChild(1).GetChild(0).gameObject;
            item.name = "A";
            item.GetComponentInChildren<TextMeshProUGUI>().text = "A:" + question.Option1;
            item.transform.SetSiblingIndex(itemCount );
            itemCount++;
        }
        if (string.IsNullOrEmpty(question.Option2) == false)
        {
            item = Instantiate(newTopic.transform.GetChild(1).GetChild(0).gameObject, newTopic.transform.GetChild(1));
            item.name = "B";
            item.GetComponentInChildren<TextMeshProUGUI>().text = "B:" + question.Option2;
            item.transform.SetSiblingIndex(itemCount);
            itemCount++;
        }
        if (string.IsNullOrEmpty(question.Option3) == false)
        {
            item = Instantiate(newTopic.transform.GetChild(1).GetChild(0).gameObject, newTopic.transform.GetChild(1));
            item.name = "C";
            item.GetComponentInChildren<TextMeshProUGUI>().text = "C:" + question.Option3;
            item.transform.SetSiblingIndex(itemCount);
            itemCount++;
        }
        if (string.IsNullOrEmpty(question.Option4) == false)
        {
            item = Instantiate(newTopic.transform.GetChild(1).GetChild(0).gameObject, newTopic.transform.GetChild(1));
            item.name = "D";
            item.GetComponentInChildren<TextMeshProUGUI>().text = "D:" + question.Option4;
            item.transform.SetSiblingIndex(itemCount);
            itemCount++;
        }
        if (string.IsNullOrEmpty(question.Option5) == false)
        {
            item = Instantiate(newTopic.transform.GetChild(1).GetChild(0).gameObject, newTopic.transform.GetChild(1));
            item.name = "E";
            item.GetComponentInChildren<TextMeshProUGUI>().text = "E:" + question.Option5;
            item.transform.SetSiblingIndex(itemCount);
            itemCount++;
        }
        if (string.IsNullOrEmpty(question.Option6) == false)
        {
            item = Instantiate(newTopic.transform.GetChild(1).GetChild(0).gameObject, newTopic.transform.GetChild(1));
            item.name = "F";
            item.GetComponentInChildren<TextMeshProUGUI>().text = "F:" + question.Option6;
            item.transform.SetSiblingIndex(itemCount);
            itemCount++;
        }


        for (int i = itemCount; i < newTopic.transform.GetChild(1).childCount; i++)
        {
            Destroy(newTopic.transform.GetChild(1).GetChild(i).gameObject);
        }

        SingleTopic singleTopic = newTopic.GetComponent<SingleTopic>();
        singleTopic.topic = newTopic.GetComponent<RectTransform>();
        singleTopic.selfIndex = selectCount;
        singleTopic.allindex = topics.Count;
        singleTopic.QuestionType = question.QuestionType;
        singleTopic.question1 = question;
        
        if (TexCore_Const.isShowSingTopicHit&&string.IsNullOrEmpty(question.Remark) == false)
        {
            newTopic.transform.GetChild(2).gameObject.SetActive(true);
            newTopic.transform.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = "解析：" + question.Remark;
            singleTopic.hit = newTopic.transform.GetChild(2).gameObject;
        }

        ControlSizeForChild(newTopic.transform.GetChild(1).GetComponent<RectTransform>());
        ControlSizeForChild(newTopic.GetComponent<RectTransform>());
        //LayoutRebuilder.ForceRebuildLayoutImmediate(newTopic.transform.GetChild(1).GetComponent<RectTransform>());

        newTopic.transform.SetSiblingIndex(node2.GetSiblingIndex() + selectCount+1) ;

        StartCoroutine(HodelClose(3, newTopic.transform.GetChild(2).gameObject));

        topics.Add(singleTopic);
        BuildTopicBtn(node2root, btnPrefab);
        singleTopic.btn = node2root.GetChild(selectCount).GetComponent<TexCore_Interaction_UGUI>();
        selectCount++;
    }

    public void BuildBool(QuestionModel question)
    {
        GameObject newTopic;
        if (boolCout == 0)
        {
            newTopic = topic3.gameObject;
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (topics.Count + 1).ToString() +"." + question.QuestionTitle;
        }
        else
        {
            newTopic = Instantiate(topic3.gameObject,topicParent);
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (topics.Count+1).ToString() + "." + question.QuestionTitle;
        }
        SingleTopic singleTopic = newTopic.GetComponent<SingleTopic>();
        singleTopic.topic = newTopic.GetComponent<RectTransform>();
        singleTopic.selfIndex = selectCount;
        singleTopic.allindex = topics.Count;
        singleTopic.QuestionType = question.QuestionType;
        singleTopic.question1 = question;
       

        if (TexCore_Const.isShowSingTopicHit&&string.IsNullOrEmpty(question.Remark)==false)
        {
            newTopic.transform.GetChild(2).gameObject.SetActive(true);
            newTopic.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = "解析：" + question.Remark;
            singleTopic.hit = newTopic.transform.GetChild(2).gameObject;
        }

        ControlSizeForChild(newTopic.transform.GetChild(1).GetComponent<RectTransform>());
        ControlSizeForChild(newTopic.GetComponent<RectTransform>());

        newTopic.transform.SetSiblingIndex(node3.GetSiblingIndex()+ boolCout+1);

        StartCoroutine(HodelClose(3, newTopic.transform.GetChild(2).gameObject));

        topics.Add(newTopic.GetComponent<SingleTopic>());
        BuildTopicBtn(node3root, btnPrefab);
        singleTopic.btn = node3root.GetChild(boolCout).GetComponent<TexCore_Interaction_UGUI>();
        boolCout++;
    }

    public void BuildTopicBtn(Transform root,GameObject prefab)
    {
        BpfLog.Info(BpfLogCategory.UI, "exercise_page.build_topic_btn", "创建一个答题卡按钮：" + topics.Count);
        GameObject newBtn = Instantiate(prefab, root);
        newBtn.SetActive(true);
        newBtn.name = topics.Count.ToString();
        newBtn.GetComponentInChildren<TextMeshProUGUI>().text=topics.Count.ToString();
    }

    public void SetNodeIndex()
    {
        int x = 0;
        for (int i = 0; i < node1root.childCount; i++)
        {
            node1root.GetChild(i).name = (x + 1).ToString();
            node1root.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = (x + 1).ToString();
            x++;
        }
        for (int i = 0; i < node2root.childCount; i++)
        {
            node2root.GetChild(i).name = (x + 1).ToString();
            node2root.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = (x + 1).ToString();
            x++;
        }
        for (int i = 0; i < node3root.childCount; i++)
        {
            node3root.GetChild(i).name = (x + 1).ToString();
            node3root.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = (x + 1).ToString();
            x++;
        }
    }
    IEnumerator ControlSizeForChildCount(RectTransform rectTransform,int count)
    {
        yield return null;
        yield return null;
        float h = 0;
        int c = rectTransform.childCount > count ? (rectTransform.childCount) / count + (rectTransform.childCount % count) : 1;
        for (int i = 0; i <c; i++)
        {
            h += rectTransform.GetChild(i).GetComponent<RectTransform>().sizeDelta.y;
        }
        Vector2 size = rectTransform.sizeDelta;
        size.y = h;
        rectTransform.sizeDelta = size;
    }
    public void ControlSizeForChild(RectTransform rectTransform)
    {
        StartCoroutine(Hodel(rectTransform));
    }
    IEnumerator Hodel(RectTransform rectTransform)
    {
        yield return null;
        yield return null;
        float h = 0;
        for (int i = 0; i < rectTransform.childCount; i++)
        {
            h += rectTransform.GetChild(i).GetComponent<RectTransform>().sizeDelta.y;
        }
        Vector2 size = rectTransform.sizeDelta;
        size.y = h;
        rectTransform.sizeDelta = size;
    }
    IEnumerator HodelClose(int hodel,GameObject obj)
    {
        for (int i = 0; i < hodel; i++)
        {
            yield return null;
        }
        obj.SetActive(false);
    }
    
}
