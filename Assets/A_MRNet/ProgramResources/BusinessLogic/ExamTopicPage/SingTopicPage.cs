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


public class SingTopicPage : MonoBehaviour
{
    public static SingTopicPage self;

    public RectTransform topicParent;

    //public Transform node1;//题目模块root
    public Transform node2;
    public Transform node3;
    public Transform node4;

    //public Transform topic1;//解答
    public TextMeshProUGUI topic1Text;
    public TMP_InputField topic1Input;

    public Transform topic2;//选择

    public Transform topic3;//判断

    public Transform topic4;//多选

    //public Transform node1root;//按钮root
    public Transform node2root;
    public Transform node3root;
    public Transform node4root;

    public GameObject btnPrefab;
    public Transform noderootParent;

    public List<SingleTopic> topics;

    public int textCount = 0;

    public int selectCount = 0;
    public int selectMiultyCount = 0;

    public int boolCout = 0;


    public TextMeshProUGUI timeText;
    public float times;

    public GameObject enter;

    public GameObject over;

    public Transform topisCanvas;

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
        //for (int i = 0; i < 13; i++)
        //{
        //    ExamQuestion examQuestion = new ExamQuestion();
        //    examQuestion.PointCode = "001";
        //    examQuestion.QuestionTitle = "问题 ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();" + i.ToString();
        //    examQuestion.QuestionType = "2";
        //    List<ExamQuestionOption> Options = new List<ExamQuestionOption>();
        //    Options.Add(new ExamQuestionOption(2, " ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();答案b"));
        //    Options.Add(new ExamQuestionOption(4, "答案d ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();"));
        //    Options.Add(new ExamQuestionOption(3, "答案c ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();"));
        //    Options.Add(new ExamQuestionOption(1, "答案A ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();"));
        //    examQuestion.Remark = "答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析答案解析";
        //    examQuestion.Options = Options;
        //    list.Add(examQuestion);
        //}
        //for (int i = 0; i < 13; i++)
        //{
        //    ExamQuestion examQuestion = new ExamQuestion();
        //    examQuestion.PointCode = "001";
        //    examQuestion.QuestionTitle = "问题 ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion(); ExamQuestion examQuestion = new ExamQuestion();" + i.ToString();
        //    examQuestion.QuestionType = "1";
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
    private void Start()
    {
        StartExam();
    }

    public void OnClick(GameObject obj)
    {
        switch (obj.tag)
        {
            case "topic1"://点击解答题序号
            case "topic2"://点击选择题序号
            case "topic3"://点击判断题序号
                SingleTopic single = topics[int.Parse(obj.name)-1];
                if (single.select == "")
                {
                    single.btn.isSelect = false;
                    single.btn.SetState(SelectState.Nomal);
                }
                else
                {
                    single.btn.SetState(SelectState.Click);
                }
                //topicParent.DOLocalMoveY(Mathf.Clamp(Math.Abs(single.topic.localPosition.y) - single.topic.sizeDelta.y, 0, topicParent.sizeDelta.y - topisCanvas.GetComponent<RectTransform>().sizeDelta.y), 2);

                break;
            default:
                BpfLog.Info(BpfLogCategory.UI, "sing_topic_page.on_click", null, null, BpfLogField.Of("name", obj.name));
                switch (obj.name)
                {
                    case "return":
                        TexCore_LoadScene.instance.OnlyLoad("00Navigation");
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
                    case "E":
                    case "F":
                    case "t"://对
                    case "f"://错
                        SingleTopic singleTopic = topics[int.Parse(obj.transform.parent.parent.name)].GetComponent<SingleTopic>();
                        singleTopic.OnSelected(obj.name);
                        break;
                    default:
                        string[] s = obj.name.Split("_");
                        switch (s[0])
                        {
                            case "A"://选A
                            case "B"://B
                            case "C"://C
                            case "D"://D
                            case "E":
                            case "F":
                            case "t"://对
                            case "f"://错
                                SingleTopic singleTopic1 = topics[int.Parse(obj.transform.parent.parent.name)].GetComponent<SingleTopic>();
                                singleTopic1.OnSelected(s[0]);
                                break;
                            default:
                                break;
                        }
                        break;
                }
                break;
        }
    }

    public void StartExam()
    {
        //topic1Text.text = BackInterface.Self.情景问题;
        //SingleTopic singleTopic = topic1.GetComponent<SingleTopic>();
        //ExamQuestion exam = new ExamQuestion();
        //singleTopic.topic = topic1.GetComponent<RectTransform>();
        //BuildTopicBtn(node1root, btnPrefab);
        
        //singleTopic.btn = node1root.GetChild(textCount).GetComponent<My_UISelect>();
        //topics.Add(singleTopic);
        BpfLog.Info(BpfLogCategory.UI, "sing_topic_page.start_exam", "开始读取题库");
        if (TexCore_Const.topicDic!=null)
        {
            BpfLog.Info(BpfLogCategory.UI, "sing_topic_page.start_exam", "读取到题库");
            Init(TexCore_Const.topicDic.Data.QuestionList);
            times = TexCore_Const.topicDic.Data.TimeLast * 60;
        }
        else
        {
            times = 10 * 60;
        }
        
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

        List<ExamRecordBase> sendAll = new List<ExamRecordBase>();

        for (int i = 0; i < topics.Count; i++)
        {
            #region
            //if (i==0)
            //{
            //    ExamRecordBase exam = new ExamRecordBase();
            //    exam.ExamID = TexCore_Const.ExamID;
            //    exam.StudentNo = TexCore_Const.user.Data.StuNo;

            //    exam.QuestionRel = 999.ToString();

            //    exam.PointCode = "1";
            //    exam.SubMenu = "情景题";
            //    exam.Question = BackInterface.Self.情景问题;
            //    exam.QuestionType = "4";

            //    exam.Answers = topic1Input.GetComponent<TMP_InputField>().text;
            //    exam.IsCorrect = "1";
            //    exam.Mark = 10;
            //    exam.Correct = "略";
            //    sendAll.Add(exam);
            //}
            //else
            //{
            #endregion
            ExamRecordBase exam = new ExamRecordBase();
                exam.ExamID = TexCore_Const.topicDic.Data.ExamID;
                exam.StudentNo = TexCore_Const.user.StuNo;

                exam.QuestionRel = topics[i].question.FlowID.ToString();

                exam.PointCode = "1";
                exam.SubMenu = topics[i].question.QuestionType.Equals("0") ? "选择题" : "判断题";
                exam.Question = topics[i].question.QuestionTitle;
                exam.QuestionType = topics[i].question.QuestionType;

                exam.Answers = topics[i].select;
                exam.IsCorrect = "1";
                exam.Mark = 10;
                exam.Correct = topics[i].question.Remark;
                sendAll.Add(exam);
            //}
        }

        string postData = JsonConvert.SerializeObject(sendAll);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);
        BpfLog.Info(BpfLogCategory.UI, "sing_topic_page.end_exam", "情景考核组合数据成功"+bodyRaw);
        TexCore_Const.exams2 = sendAll;
        enter.SetActive(false);
        StartCoroutine(HodelReturn());
        //LXF_SendData.instance.SubmitRecord(sendAll);
    }
    IEnumerator HodelReturn()
    {
        yield return new WaitForSeconds(2f);
        TexCore_LoadScene.instance.OnlyLoad("00Navigation");
    }

    public void Init(List< ExamQuestion> questions)
    {
        topisCanvas.localScale = new Vector3(0,1f,1f);

        //乱序
        Shuffle<ExamQuestion>(questions);

        for (int i = 0; i < questions.Count; i++)
        {

            BuildTopic(questions[i]);
        }
        
        SetNodeIndex();
        StartCoroutine(ResetHight());
    }

    public static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public void BuildTopic(ExamQuestion question)
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
                BuildMiutlySelect(question);
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
        //StartCoroutine( ControlSizeForChildCount(node1root.GetComponent<RectTransform>(),5));
        StartCoroutine(ControlSizeForChildCount(node2root.GetComponent<RectTransform>(),5));
        StartCoroutine(ControlSizeForChildCount(node3root.GetComponent<RectTransform>(),5));
        StartCoroutine(ControlSizeForChildCount(node4root.GetComponent<RectTransform>(), 5));
        ControlSizeForChild(noderootParent.GetComponent<RectTransform>());
        ControlSizeForChild(topicParent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(noderootParent.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(topicParent);
        topisCanvas.localScale = new Vector3(1f, 1f, 1f);
    }
    #region
    //public void BuildText(ExamQuestion question)
    //{
    //    GameObject newTopic;
    //    if (selectCount == 0)
    //    {
    //        newTopic = topic1.gameObject;
    //        newTopic.name = topics.Count.ToString();
    //        newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (textCount+1).ToString() + "." + question.QuestionTitle;

    //    }
    //    else
    //    {
    //        newTopic = Instantiate(topic1.gameObject, topicParent);
    //        newTopic.name = topics.Count.ToString();
    //        newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (textCount).ToString() + "." + question.QuestionTitle;

    //    }

    //    newTopic.transform.SetSiblingIndex(node2.GetSiblingIndex());

    //    SingleTopic singleTopic= newTopic.GetComponent<SingleTopic>();

    //    singleTopic.question = question;


    //    topics.Add(singleTopic);
    //    BuildTopicBtn(node3root, btnPrefab);
    //    singleTopic.btn = node1root.GetChild(textCount).GetComponent<My_UISelect>();
    //    textCount++;
    //}
    #endregion
    public void BuildSelect(ExamQuestion question)
    {
        GameObject newTopic;
        if (selectCount==0)
        {
            newTopic = topic2.gameObject;
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (selectCount + 1).ToString() + "." + question.QuestionTitle;

        }
        else
        {
            newTopic = Instantiate(topic2.gameObject, topicParent);
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (selectCount + 1).ToString() + "." + question.QuestionTitle;

        }
        newTopic.SetActive(true);
        int ite = UnityEngine.Random.Range(0, 6);
        for (int i = 0; i < ite; i++)
        {
            ExamQuestionOption option = question.Options[0];
            question.Options.RemoveAt(0);
            question.Options.Add(option);
        }

        string itemName = "ABCDEF";
        //for (int i = 1; i < newTopic.transform.GetChild(1).childCount; i++)
        //{
        //    Destroy(newTopic.transform.GetChild(1).GetChild(i).gameObject);
        //}
        
        for (int i = 0; i < question.Options.Count; i++)
        {
            GameObject item;
            if (i == 0)
                item = newTopic.transform.GetChild(1).GetChild(0).gameObject;
            else
                item =Instantiate( newTopic.transform.GetChild(1).GetChild(0).gameObject, newTopic.transform.GetChild(1));
            switch (question.Options[i].ID)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6: 
                    item.name = itemName[i]+"_"+ question.Options[i].ID; 
                    item.GetComponentInChildren<TextMeshProUGUI>().text = /*itemName[i] + *//*"●" +*/ question.Options[i].Text; 
                    break;
                default: break;
            }
            //switch (question.Options[i].ID)
            //{
            //    case 1: item.name = "A"; item.GetComponentInChildren<TextMeshProUGUI>().text = "A:" + question.Options[i].Text; break;
            //    case 2: item.name = "B"; item.GetComponentInChildren<TextMeshProUGUI>().text = "B:" + question.Options[i].Text; break;
            //    case 3: item.name = "C"; item.GetComponentInChildren<TextMeshProUGUI>().text = "C:" + question.Options[i].Text;break;
            //    case 4: item.name = "D"; item.GetComponentInChildren<TextMeshProUGUI>().text = "D:" + question.Options[i].Text; break;
            //    case 5: item.name = "E"; item.GetComponentInChildren<TextMeshProUGUI>().text = "E:" + question.Options[i].Text; break;
            //    case 6: item.name = "F"; item.GetComponentInChildren<TextMeshProUGUI>().text = "F:" + question.Options[i].Text;break;
            //    default: break;
            //}
            //item.transform.SetSiblingIndex(question.Options[i].ID-1);
            item.transform.SetSiblingIndex(0);
        }

        for (int i = question.Options.Count; i < newTopic.transform.GetChild(1).childCount; i++)
        {
            Destroy(newTopic.transform.GetChild(1).GetChild(i).gameObject);
        }

        SingleTopic singleTopic = newTopic.GetComponent<SingleTopic>();
        singleTopic.topic = newTopic.GetComponent<RectTransform>();
        singleTopic.selfIndex = selectCount;
        singleTopic.allindex = topics.Count;
        singleTopic.QuestionType = question.QuestionType;
        singleTopic.question = question;
        if (singleTopic.hit!=null)
        {
            singleTopic.hit.GetComponentInChildren<TextMeshProUGUI>().text = question.Remark;
        }
        
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


    public void BuildMiutlySelect(ExamQuestion question)
    {
        GameObject newTopic;
        if (selectMiultyCount == 0)
        {
            newTopic = topic4.gameObject;
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (selectMiultyCount + 1).ToString() + "." + question.QuestionTitle;

        }
        else
        {
            newTopic = Instantiate(topic4.gameObject, topicParent);
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (selectMiultyCount + 1).ToString() + "." + question.QuestionTitle;

        }
        newTopic.SetActive(true);
        newTopic.transform.Find("root").GetChild(0).GetComponent<TexCore_Interaction_UGUI>().itemParent = null;
        for (int i = 0; i < question.Options.Count; i++)
        {
            GameObject item;
            if (i == 0)
                item = newTopic.transform.GetChild(1).GetChild(0).gameObject;
            else
                item = Instantiate(newTopic.transform.GetChild(1).GetChild(0).gameObject, newTopic.transform.GetChild(1));
            switch (question.Options[i].ID)
            {
                case 1:
                    item.name = "A";
                    item.GetComponentInChildren<TextMeshProUGUI>().text = "A:" + question.Options[i].Text;
                    break;
                case 2:
                    item.name = "B";
                    item.GetComponentInChildren<TextMeshProUGUI>().text = "B:" + question.Options[i].Text;
                    break;
                case 3:
                    item.name = "C";
                    item.GetComponentInChildren<TextMeshProUGUI>().text = "C:" + question.Options[i].Text;
                    break;
                case 4:
                    item.name = "D";
                    item.GetComponentInChildren<TextMeshProUGUI>().text = "D:" + question.Options[i].Text;
                    break;
                case 5:
                    item.name = "E";
                    item.GetComponentInChildren<TextMeshProUGUI>().text = "E:" + question.Options[i].Text;
                    break;
                case 6:
                    item.name = "F";
                    item.GetComponentInChildren<TextMeshProUGUI>().text = "F:" + question.Options[i].Text;
                    break;
                default:
                    break;
            }
            item.transform.SetSiblingIndex(question.Options[i].ID - 1);
        }
        for (int i = question.Options.Count; i < newTopic.transform.GetChild(1).childCount; i++)
        {
            Destroy(newTopic.transform.GetChild(1).GetChild(i).gameObject);
        }

        SingleTopic singleTopic = newTopic.GetComponent<SingleTopic>();
        singleTopic.topic = newTopic.GetComponent<RectTransform>();
        singleTopic.selfIndex = selectMiultyCount;
        singleTopic.allindex = topics.Count;
        singleTopic.QuestionType = question.QuestionType;
        singleTopic.question = question;
        singleTopic.isMiultySelect = true;

        if (TexCore_Const.isShowSingTopicHit && string.IsNullOrEmpty(question.Remark) == false)
        {
            newTopic.transform.GetChild(2).gameObject.SetActive(true);
            newTopic.transform.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = "解析：" + question.Remark;
            singleTopic.hit = newTopic.transform.GetChild(2).gameObject;
        }

        ControlSizeForChild(newTopic.transform.GetChild(1).GetComponent<RectTransform>());
        ControlSizeForChild(newTopic.GetComponent<RectTransform>());
        //LayoutRebuilder.ForceRebuildLayoutImmediate(newTopic.transform.GetChild(1).GetComponent<RectTransform>());

        newTopic.transform.SetSiblingIndex(node4.GetSiblingIndex() + selectMiultyCount + 1);

        StartCoroutine(HodelClose(3, newTopic.transform.GetChild(2).gameObject));

        topics.Add(singleTopic);
        BuildTopicBtn(node4root, btnPrefab);
        singleTopic.btn = node4root.GetChild(selectMiultyCount).GetComponent<TexCore_Interaction_UGUI>();
        selectMiultyCount++;
    }

    public void BuildBool(ExamQuestion question)
    {
        GameObject newTopic;
        if (boolCout == 0)
        {
            newTopic = topic3.gameObject;
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (boolCout + 1).ToString() +"." + question.QuestionTitle;
        }
        else
        {
            newTopic = Instantiate(topic3.gameObject,topicParent);
            newTopic.name = topics.Count.ToString();
            newTopic.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = (boolCout + 1).ToString() + "." + question.QuestionTitle;
        }
        newTopic.SetActive(true);
        SingleTopic singleTopic = newTopic.GetComponent<SingleTopic>();
        singleTopic.topic = newTopic.GetComponent<RectTransform>();
        singleTopic.selfIndex = boolCout;
        singleTopic.allindex = topics.Count;
        singleTopic.QuestionType = question.QuestionType;
        singleTopic.question = question;
       

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
        BpfLog.Info(BpfLogCategory.UI, "sing_topic_page.build_topic_btn", "创建一个答题卡按钮：" + topics.Count);
        GameObject newBtn = Instantiate(prefab, root);
        newBtn.SetActive(true);
        newBtn.name = topics.Count.ToString();
        newBtn.GetComponentInChildren<TexCore_SelectBase>().itemParent = root;
        newBtn.GetComponentInChildren<TextMeshProUGUI>().text=topics.Count.ToString();
        //print("创建一个答题卡按钮：" + topics.Count+ newBtn.GetComponentInChildren<TextMeshProUGUI>().text);
    }

    public void SetNodeIndex()
    {
        int x = 0;
        #region
        //for (int i = 0; i < node1root.childCount; i++)
        //{
        //    node1root.GetChild(i).name = (x + 1).ToString();
        //    node1root.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = (x + 1).ToString();
        //    x++;
        //}
        #endregion
        for (int i = 0; i < node2root.childCount; i++)
        {
            node2root.GetChild(i).name = (x + 1).ToString();
            node2root.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = (x + 1).ToString();
            x++;
        }
        for (int i = 0; i < node4root.childCount; i++)
        {
            node4root.GetChild(i).name = (x + 1).ToString();
            node4root.GetChild(i).GetComponentInChildren<TextMeshProUGUI>().text = (x + 1).ToString();
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
        //int c = rectTransform.childCount > count ? ((rectTransform.childCount) / count + (rectTransform.childCount % count) > 0 ? 0 : 1 ): 1;
        int c = 0;
        if (rectTransform.childCount > count)
        {
            if (  (rectTransform.childCount % count) > 0)
            {
                c = ((rectTransform.childCount) / count)+1;
            }
            else
            {
                c = (rectTransform.childCount) / count;
            }
        }
        else
        {
            c = 1;
        }

        //if (c > 0 )
        //{
        //    for (int i = 0; i < c; i++)
        //    {
        //        print(rectTransform.name + rectTransform.parent.name + i+c);
        //        h += rectTransform.GetChild(i).GetComponent<RectTransform>().sizeDelta.y;
        //    }
        //}
        if (rectTransform.childCount>0)
        {
            for (int i = 0; i < c; i++)
            {
                BpfLog.Info(BpfLogCategory.UI, "sing_topic_page.control_size_for_child_count", rectTransform.name + rectTransform.parent.name + i + c);
                h += rectTransform.GetChild(i).GetComponent<RectTransform>().sizeDelta.y;
            }
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
