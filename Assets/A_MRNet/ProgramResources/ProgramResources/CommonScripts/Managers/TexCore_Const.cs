
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行平台
/// </summary>
public enum Partfrom
{
    PC,
    VR
}
public enum Mode 
{ 
    练习,
    考核
}

public class TexCore_Const
{
    public static string token;

    public static LoginData user;
    public static BeginExamData examData;
    public static string sceneNomalNode;

    public static bool isSinglePerson=true;

    public static int ExamID = 5;
    
    public static Character character=Character.管理员;

    public static string startPanel;

    public static List<int> viewID = new List<int>();

    public static Partfrom partfrom = Partfrom.PC;

    public static Mode mode = Mode.练习;

    public static int QuestionRel=1 ;

    public static string operateMenu=""/*"大部吊装"*/;

    public static string operateNode = ""/*"制退机拆卸"*/;

    public static string examScene;

    public static string playerName = "Player" + Random.Range(1000, 10000);
    internal static bool isShowSingTopicHit=true;

    public static BeginExamResult topicDic;

    public static List<ExamRecordBase> exams1;

    public static List<ExamRecordBase> exams2;

    public static bool ConfirmCharacter(Character topic)
    {
        if (character != topic && character != Character.管理员 && topic != Character.所有人) 
        {

            //TexCore_WindowManager.self.CreateHit("请 <" + character.ToString() + "> 操作！");
            return false;
        }
        return true;
    }
}
