
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background_Common : MonoBehaviour
{
   
}

/// <summary>
/// 通用后台接收数据类
/// </summary>
[Serializable]
public class WebResult//接收web返回值
{
    public bool State;
    public string Code;
    public string Msg;
    public object Data;
}
#region 登录退出

/// <summary>
/// 用户数据类
/// </summary>
[Serializable]
public class User//登录用
{
    public string userid;
    public string password;
    public User(string id, string pass)
    {
        userid = id;
        password = pass;
    }
}

/// <summary>
/// 登录接口返回数据
/// </summary>
[Serializable]
public class LoginResult//接收web返回值
{
    public bool State;
    public string Code;
    public string Msg;
    public LoginData Data;
}

[Serializable]
public class MgrLoginResult//接收web返回值
{
    public bool State;
    public string Code;
    public string Msg;
    public LoginData Data;
}
/// <summary>
/// 后台用户数据
/// </summary>
[Serializable]
public class LoginData
{
    public string StuNo;
    public string StuName;
    public string Password;
    public string ClassID;
    public string Remark;
    public string CreateBy;
    public string CreateDate;
    public string CreateSourve;
}

#endregion

#region 考试
/// <summary>
/// 获取考试列表接口返回数据
/// </summary>
[Serializable]
public class ExamListResult
{
    public bool State;
    public string Code;
    public string Msg;
    public ExamModel[] Data;
}

/// <summary>
/// 考试信息
/// </summary>
[Serializable]
public class ExamModel
{
    public int FlowID;// 考试ID ,
    public string OperID;// 操作人ID ,
    public string CreateDate;//创建时间 ,
    public string State;//考试状态，0:未开始 1:已开始 2:已结束 ,
    public string StartTime;//开始时间 ,
    public string EndTime;// 结束时间 ,
    public int PersonNum;//参考人数 ,
    public int AverageMark;//平均分 ,
    public int TimeLast;//考试时长，单位:分钟 ,
    public string ModuleCode;//考试名称 ,
    public int PaperID;//试卷编号
}



/// <summary>
/// 开启考试接口返回数据
/// </summary>
[Serializable]
public class BeginExamResult//接收web返回值
{
    public bool State;
    public string Code;
    public string Msg;
    public BeginExamData Data;
}
/// <summary>
/// 答卷信息
/// </summary>
[Serializable]
public class BeginExamData
{
    public int FlowID;//考试流水号
    public int ExamID;//考试id
    public string State;//考试状态 1：已开始 2：已结束 ,
    public string StudentNo;// 学号 ,
    public string StudentName;// 学生姓名 ,
    public string BeginTime;// 考试开始时间 ,
    public string EndTime; //交卷时间 ,
    public float QuestScore;// 随机习题得分 ,
    public string Questions;// 随机的习题编号 ,
    public string StuNote;//心得体会 ,
    public float StuNoteScore;// 心得体会评分 ,
    public int TotalScore;//考试总分 ,
    public int TimeLast;//考试时长 单位:分钟 ,
    public List<ExamQuestion> QuestionList;//考题列表

    public ExamPaperInfo ExamPaperInfo;
}

public class ExamPaperInfo
{
    public int QuestSingleCount;// 单选题数量 ,
    public int QuestMultiCount;// 多选题数量 ,
    public int QuestJudgeCount;// 判断题数量 ,
    public int QuestBlankCount;// 填空题数量 ,
    public int QuestSingleMark;// 单选题分值 ,
    public int QuestMultiMark;// 多选题分值 ,
    public int QuestMultiPartMark;// 多选题漏选分值 ,
    public int QuestJudgeMark;// 判断题分值 ,
    public int QuestBlankMark;//填空题分值
}

#endregion

#region 练习题
/// <summary>
/// 获取练习题接口返回数据
/// </summary>
[Serializable]
public class GetExercisesReturn
{
    public bool State;
    public string Code;
    public string Msg;
    public List<QuestionModel> Data;
}
#endregion

#region 考题
/// <summary>
/// 考题数据类
/// </summary>
[Serializable]
public class ExamQuestion
{
    /// <summary>
    /// 考题流水号
    /// </summary>
    public int FlowID;
    /// <summary>
    /// 功能点ID
    /// </summary>
    public string PointCode;
    /// <summary>
    /// 试题题目题干
    /// </summary>
    public string QuestionTitle;
    /// <summary>
    /// 试题类型  0: 选择 1: 判断题 2: 多选题3:实操题4:主观题
    /// </summary>
    public string QuestionType;
    /// <summary>
    /// 选项
    /// </summary>
    public List<ExamQuestionOption> Options;
    /// <summary>
    /// 备注
    /// </summary>
    public string Remark;
}
/// <summary>
/// 选项信息
/// </summary>
[Serializable]
public class ExamQuestionOption
{
    public int ID;
    public string Text;
    public ExamQuestionOption(int id, string text)
    {
        this.ID = id;
        this.Text = text;
    }
    public ExamQuestionOption()
    {

    }
}
#endregion

#region 纯文字练习题
/// <summary>
/// 纯文字题数据类
/// </summary>
[Serializable]
public class QuestionModel
{
    public int FlowID;
    public string PointCode;//(string, optional) // 功能点ID ,
    public string QuestionTitle;//(string, optional) // 试题题目 ,
    public string QuestionType;//(string, optional) // 试题类型 0:选择题 1:判断题 2:多选题 ,
    public string Option1;//(string, optional) // 选项1 ,
    public string Option2;//(string, optional)// 选项2 ,
    public string Option3;//(string, optional) // 选项3 ,
    public string Option4;//(string, optional) // 选项4 ,
    public string Option5;//(string, optional) // 选项5 ,
    public string Option6;//(;//string, optional) // 选项6 ,
    public string Answers;//(string, optional) // 标答 ,
    public string Analyze;//(;//string, optional) // 题目分析 ,
    public string Remark;//(string, optional) // 备注
}
#endregion

#region 提交题目数据类


[Serializable]
public class ExamRecordBase
{
    /// <summary>
    /// 考试编号 61
    /// </summary>

    public int ExamID;
    /// <summary>
    /// 学号 20191004289
    /// </summary>
    public string StudentNo;

    /// <summary>
    /// 操作人学号
    /// </summary>
    public string OperNo;
    /// <summary>
    /// 记录类型 3
    /// 0：选择题
    /// 1：判断题
    /// 2：多选题
    /// 3：实操题
    /// </summary>
    public string QuestionType;

    /// <summary>
    /// 题目编号 001
    /// </summary>
    public string QuestionRel;

    /// <summary>
    /// 题目所属菜单 嫁接时间
    /// </summary>
    public string SubMenu;
    /// <summary>
    /// 对应知识点  1:芽苗砧嫁接  2:XXXXXX(后续考试项目) 1
    /// </summary>
    public string PointCode;
    /// <summary>
    /// 学生答题内容 5月中旬-6月上旬
    /// </summary>
    public string Answers;
    /// <summary>
    /// 是否正确 0:不正确  1:正确 1
    /// </summary>
    public string IsCorrect;
    /// <summary>
    /// 得分 10
    /// </summary>
    public int Mark;
    /// <summary>
    /// 题干 时间段选择
    /// </summary>
    public string Question;
    /// <summary>
    /// 标答 5月中旬-6月上旬
    /// </summary>
    public string Correct;
}
#endregion

#region 资料
/// <summary>
/// 获取资料树返回
/// </summary>
[Serializable]
public class GetFileTreeResult 
{
    public bool State;
    public string Code;
    public string Msg;
    public List< PointTreeModel> Data;
}

/// <summary>
/// 资料结构树
/// </summary>
[Serializable]
public class PointTreeModel 
{
    public string Name;// (string, optional),知识点名称
    public string Code;// (object, optional),为知识点时，是知识点序号，用于获取只存在于这个知识点下的资料
    public string PCode;// (string, optional),上一级目录的序号
    public bool IsModule;// (boolean, optional),是否是文件夹，当两个布尔值都为false时，则为知识点，使用Code来获取资料
    public bool IsLession;// (boolean, optional),是否是一级目录
    public List<PointTreeModel> Children;// (Array[PointTreeModel], optional)
}

/// <summary>
/// 获取资料返回
/// </summary>
[Serializable]
public class GetFileResult
{
    public bool State;
    public string Code;
    public string Msg;
    public List<OnlineFileModel> Data;
}

/// <summary>
/// 资料数据
/// </summary>
[Serializable]
public class OnlineFileModel 
{
   public int FileID;//(integer, optional) :编号 ,
   public int  FileType;//(integer, optional) :文件类型 0:其他 1:图片 jpg、jpeg、png、gif、bmp 2:Word doc、docx 3:Excel xls、xlsx 4:PPT ppt、pptx 5:PDF pdf 6:压缩包 rar、zip ,
   public string FileName;//(string, optional) :文件名 ,
   public string FileUrl;//(string, optional) :文件地址 ,
   public int  PointCode;//(integer, optional) :知识点编号 ,
   public string  UploaderID;//(string, optional) :上传人 ,
   public string UploadDate;//(string, optional) :上传时间 ,
   public string  Remark;//(string, optional) :备注
}


#endregion