using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
namespace HuaXian.ExportRuntime
{
    public struct MarkV2//存储记号笔画点的坐标和是否已画这两个信息的一个结构体
    {
        public Vector3 MarkPostion;// 标记的位置
        public bool IsMarked;// 是否已经标记
        // 构造函数
        public MarkV2(Vector3 markPostion, bool isMarked)
        {
            MarkPostion = markPostion;// 初始化标记位置
            IsMarked = isMarked;// 初始化标记状态
        }
    }

    public struct LinePositionV2//存储线段头尾坐标和是否已划线这三个信息的一个结构体
    {
        public Vector3 Start;// 线段起点
        public Vector3 End;// 线段终点
        public bool IsDraw;// 线段是否已绘制
        public LinePositionV2(Vector3 start, Vector3 end)
        {
            Start = start;// 初始化线段起点
            End = end;// 初始化线段终点
            IsDraw = false;// 初始化绘制状态为未绘制
        }
    }


    public class DrawGridV2 : MonoBehaviour
    {
        // Start is called before the first frame update
        public GameObject modouTou;// 墨斗头:Plane (2)
        public GameObject modouWei;// 墨斗尾:Plane (3)
        public GameObject finishText;// 任务完成跳出的UI：划线完成
        public GameObject errorText;// 画错线提示：弹线错误
        public GameObject mark; // 画的点：JiZhunDian预制体
        public GameObject pen; // 笔尖：Plane (1)
        public GameObject shuiPingYi;// 激光水平仪：ShuiPingYi
        public GameObject rayLine;//Ray
        public GameObject MoDou; //墨斗（发射linerender的物体）:Plane (3)
        public GameObject MoDouWei; //墨斗（发射linerender的物体）:Plane (2)
        public GameObject DragCube; //模拟拉起效果小方块:DragCube
        public GameObject modouTouOutLineBox;// 墨斗头:MoDou_03
        public GameObject modouWeiOutLineBox;// 墨斗尾:MoDou_01
        public GameObject shuiPingYiSwitch;// 水平仪开关:polySurface10


        private float width = 4f;// 网格宽度
        private float height = 4f;// 网格高度
        private float timeInterval = 500;// 错误提示的时间间隔
        private float DrawErrorTime;// 画错线的时间
        public Camera MainCam; // 主相机
        public Transform Plane; //地面

        private Vector3 MoDouTouPosition;
        private Vector3 MoDouWeiPosition;
        public LineRenderer LineRendererMoDou; // 墨斗线渲染器
        public LineRenderer PlaneLineRenderer; //地面线渲染器
        private Outline moudouTouOutLine;
        private Outline moudouWeiOutLine;

        private List<LinePositionV2> linePostions = new List<LinePositionV2>();// 所有要画的线的两个端点
        private List<MarkV2> markPonits = new List<MarkV2>(); // 标记点列表
        private Dictionary<float/*弹线地面线生成的时间*/, LineRenderer/*生成的地面线*/> errLines = new Dictionary<float, LineRenderer>();//存储错误弹线的字典（下方deleteErrorLines方法会清理错误线）

        private bool isShuiPingYiXiFu = false;// 是否水平仪吸附
        private bool isMoDouTouHilight = false;// 墨斗头是否高亮
        private bool isMoDouWeiHilight = false;// 墨斗尾是否高亮
        private bool isEnterDragGube = false;// 是否进入拖拽状态
        private bool isDrawError = false;// 是否绘制错误

        private string mouDouTouName = "mouDouTou";// 模斗头名称
        private string mouDouWeiName = "mouDouWei";// 模斗尾名称
        private float xifuJuli = 0.1f;// 吸附距离
    private bool hasTriggeredTaskAdvance = false;

        void Start()
        {
            moudouTouOutLine = modouTouOutLineBox.GetComponent<Outline>();//获取墨斗头下的outline脚本组件
            moudouTouOutLine.enabled = false;// 初始化墨斗头外轮廓框为不显示
            moudouWeiOutLine = modouWeiOutLineBox.GetComponent<Outline>(); //获取墨斗尾下的outline脚本组件
            moudouWeiOutLine.enabled = false;// 初始化墨斗尾外轮廓框为不显示
            finishText.SetActive(false);// 初始隐藏完成UI
            errorText.SetActive(false);// 初始隐藏错误提示UI
            shuiPingYiSwitch.SetActive(false);// 初始隐藏水平仪开关
            LineRendererMoDou.useWorldSpace = true; // 线渲染器使用世界坐标
            // 初始化线段和标记点
            for (int i = 0; i <= width; i++)//横向地面
            {
                if (i != 0)
                {
                    markPonits.Add(new MarkV2(new Vector3(i, 0, 0), false));// 记录除（0，0，0）外，横向需要用记号笔画的标记点
                }
                linePostions.Add(new LinePositionV2(new Vector3(i, 0, 0), new Vector3(i, 0, height)));//存储地面横向需要划线的高亮点
            }
            for (int i = 0; i <= height; i++)//纵向
            {
                if (i != 0)
                {
                    markPonits.Add(new MarkV2(new Vector3(0, 0, i), false));// 记录除（0，0，0）外，纵向需要用记号笔画的标记点
                }
                linePostions.Add(new LinePositionV2(new Vector3(0, 0, i), new Vector3(width, 0, i))); //存储地面纵向需要划线的高亮点
            }
        }

        void ShowModouHilight(string name, bool isHilight, Vector3 pos)
        {//显示墨斗高亮（名称，是否需要高亮，存储需要墨斗高亮的点的位置）
            if (name.Equals(mouDouTouName))
            {

                if (isHilight)//墨斗头
                {
                    MoDouTouPosition = pos;//记录可弹线的高亮位置
                    isMoDouTouHilight = true;//记录墨斗头处于高亮状态
                    moudouTouOutLine.enabled = true;//设置墨斗头高亮显示
                }
                else
                {
                    isMoDouTouHilight = false;//记录墨斗头不处于高亮状态
                    moudouTouOutLine.enabled = false;//设置墨斗头不高亮显示
                }

            }
            else
            {
                if (isHilight)//墨斗尾 同上
                {
                    MoDouWeiPosition = pos;
                    isMoDouWeiHilight = true;
                    moudouWeiOutLine.enabled = true;
                }
                else
                {
                    isMoDouWeiHilight = false;
                    moudouWeiOutLine.enabled = false;
                }
            }
        }

        void SetMouDouHilight(GameObject modou, string name)
        {//设置高亮
            Vector3 curHilight = name.Equals(mouDouTouName) ? MoDouWeiPosition : MoDouTouPosition;//当墨斗一端已经高亮时，获取高亮这一端的位置
            bool hilight = name.Equals(mouDouTouName) ? isMoDouWeiHilight : isMoDouTouHilight;//判断墨斗另一端是否已经高亮
            float min = float.MaxValue;//记录墨斗离最近的可高亮点的距离 

            foreach (var item in linePostions)//遍历每个要划的线
            {
                if (item.IsDraw)//如果已划线
                {
                    continue;//跳过这一次循环
                }
                //判断一条线两个端点到墨斗的距离
                float distance1 = Vector3.Distance(item.Start, modou.transform.position);
                float distance2 = Vector3.Distance(item.End, modou.transform.position);
                float minValue = Mathf.Min(distance1, distance2);//取两个端点中和墨斗的距离较小的那个端点的长度

                if (minValue < xifuJuli && !IsPointInRectangle(new Vector2(modou.transform.position.x, modou.transform.position.z)))//如果墨斗在网格外面，并且距离可高亮点的最小值小于阈值
                {
                    if (min > minValue)
                    {
                        min = minValue;
                    }
                    //Debug.Log(name + " start x= " + item.Start.x + "y= "+ item.Start.y + "z="+ item.Start.z );
                    //Debug.Log(name + "end x= " + item.End.x + " y= "+ item.End.y + " z="+ item.End.z + " minvalue" + minValue);
                    if (hilight)//如果墨斗已经有一端是高亮的
                    {
                        if (item.Start.Equals(curHilight) && distance2 < xifuJuli)//如果线段的两个端点，墨斗一端已经在其中一个点处高亮，并且墨斗另一端在另一个端点的可高亮的阈值内
                        {
                            ShowModouHilight(name, true, item.End); // 墨斗另一端高亮
                            return;
                        }
                        else if (item.End.Equals(curHilight) && distance1 < xifuJuli)//同上
                        {
                            ShowModouHilight(name, true, item.Start);// 高亮
                            return;
                        }
                        else//如果不是同方向上的点
                        {
                            ShowModouHilight(name, false, item.Start);// 不高亮
                        }
                    }
                    else//设置第一个高亮点
                    {
                        if (distance1 < xifuJuli)//在起点位置高亮
                        {
                            ShowModouHilight(name, true, item.Start);
                            return;
                        }
                        else if (distance2 < xifuJuli)//在终点位置高亮
                        {
                            ShowModouHilight(name, true, item.End);
                            return;
                        }
                        else
                        {
                            ShowModouHilight(name, false, item.Start);
                        }

                    }
                }
                if (min > xifuJuli)
                {//判断墨斗是否在可高亮距离范围内
                    ShowModouHilight(name, false, item.Start);
                }

            }
        }



        void ErrTextXiaoshi()
        {//划线错误提示出现0.5秒后消失

            if (isDrawError && (Time.time * 1000f - DrawErrorTime) > timeInterval)
            {
                errorText.SetActive(false);
                isDrawError = false;
            }

        }

        bool IsPointInRectangle(Vector2 point)//判断墨斗位置是否在基准网格里面，在里面不高亮
        {
            Vector2 center = new Vector2(width / 2, height / 2);
            // 计算长方形的边界
            float left = center.x - width / 2;
            float right = center.x + width / 2;
            float top = center.y + height / 2;
            float bottom = center.y - height / 2;

            // 检查点是否在边界内
            if (point.x >= left && point.x <= right && point.y >= bottom && point.y <= top)
            {
                return true;
            }
            return false;
        }


        void DrawMark()//记号笔画点，靠近需要画点的位置生成圆点（有预制体）
        {
            for (int i = 0; i < markPonits.Count; i++)//遍历所有要画点的位置
            {
                MarkV2 temp = markPonits[i];
                if (!temp.IsMarked)//如果点没被画过
                {
                    float distance = Vector3.Distance(markPonits[i].MarkPostion, pen.transform.position);
                    if (distance < 0.2)//并且小于阈值，就会生成圆点
                    {
                        GameObject newPrefabInstance = Instantiate(mark, markPonits[i].MarkPostion, Quaternion.identity);
                        temp.IsMarked = true;
                        markPonits[i] = temp;
                    }
                }
            }

        }
        bool IsMarkAll()//判断是否所有点都被画了
        {
            for (int i = 0; i < markPonits.Count; i++)
            {
                if (!markPonits[i].IsMarked)
                {
                    return false;
                }
            }
            return true;
        }



        void SetShuiPingYi()//水平仪吸附到（0，0，0）
        {
            Vector3 zero = new Vector3(0, 0, 0.066f);
            if (isShuiPingYiXiFu)
            {
                shuiPingYi.transform.position = zero;
                // 👉 2. 新增这一行：强制将旋转角度归零（回正）
                shuiPingYi.transform.rotation = Quaternion.identity;
            }
            if (!isShuiPingYiXiFu)
            {
                float dis = Vector3.Distance(zero, shuiPingYi.transform.position);

                if (dis < 0.3)//距离小于0.3会吸附
                {
                    isShuiPingYiXiFu = true;
                    shuiPingYiSwitch.SetActive(true);
                }

            }

            if (IsMarkAll())//当所有用记号笔画的点出现时，水平仪消失
            {
                shuiPingYi.SetActive(false);
            }
        }
        void deleteErrorLines() //删除地面错误弹线
        {
            List<float> keysToRemove = new List<float>();
            float now = Time.time * 1000f;
            foreach (var kv in errLines)
            {
                if ((now - kv.Key) > timeInterval) //当前时间减去弹线的时间大于0.5s就删去线
                {
                    Destroy(kv.Value);
                    keysToRemove.Add(kv.Key);
                }
            }
            // 删除标记的键
            foreach (float key in keysToRemove)
            {
                errLines.Remove(key);
            }
        }
        // Update is called once per frame
        void Update()
        {
            SetShuiPingYi();//设置水平仪吸附

            if (rayLine.activeSelf)
            {//水平仪打开出现射线，用记号笔画点
                DrawMark();
            }
            if (IsMarkAll())//所有点都画好，才可以高亮
            {
                SetMouDouHilight(modouTou, mouDouTouName);
                SetMouDouHilight(modouWei, mouDouWeiName);

            }
            if (!isEnterDragGube)//拖拽方块没被射线选中时，一直在线上，选中后可以随意拖拽
            {
                UpdateDragCubePosition();
            }
            ErrTextXiaoshi();//判断错误提示是否消失
            deleteErrorLines(); // 删除画错的线

            CreateString(DragCube.transform.position); // 创建线

        }

        public void EnterDragGube()
        {//当射线进入可拖拽的方块里
            isEnterDragGube = true;//可拖拽
        }

        public void ExitDragGube()//不可拖拽，并且绘制一条线
        {
            isEnterDragGube = false;
            DrawLine();
        }
        // 绘制一条线段，将MoDou和transform的x, z坐标投影到地面上
        public void DrawLine()//划地面线
        {
            LineRenderer secondLine = Instantiate(PlaneLineRenderer, new Vector3(0, 0, 0), Quaternion.identity);//生成地面线
            //设置线起点和终点的位置
            secondLine.useWorldSpace = true;
            secondLine.SetPosition(0, new Vector3(MoDou.transform.position.x, Plane.transform.position.y, MoDou.transform.position.z));
            secondLine.SetPosition(1, new Vector3(MoDouWei.transform.position.x, Plane.transform.position.y, MoDouWei.transform.position.z));
            //判断墨斗头和墨斗尾是否都是高亮的，如果不是，划线错误
            if (isMoDouTouHilight && isMoDouWeiHilight)
            {
                for (int i = 0; i < linePostions.Count; i++)
                {
                    if (linePostions[i].Start.Equals(MoDouTouPosition) && linePostions[i].End.Equals(MoDouWeiPosition) ||
                        linePostions[i].End.Equals(MoDouTouPosition) && linePostions[i].Start.Equals(MoDouWeiPosition))//判断地面划线的两端是否在linePostions列表里，如果在就划线正确
                    {
                        LinePositionV2 temp = linePostions[i];
                        temp.IsDraw = true;
                        linePostions[i] = temp;
                        showFinishText();
                        return;
                    }
                }

            }
            errLines.Add(Time.time * 1000f, secondLine);
            DrawErrorTime = Time.time * 1000f;
            isDrawError = true;
            errorText.SetActive(true);
        }


        void showFinishText()
        {
            for (int i = 0; i < linePostions.Count; i++)
            {
                if (!linePostions[i].IsDraw)
                {
                    return;
                }
            }

            finishText.SetActive(true);
            AutoAdvanceTask();
        }

        private void AutoAdvanceTask()
        {
            if (hasTriggeredTaskAdvance || finishText == null)
            {
                return;
            }

            TaskChange[] taskChanges = finishText.GetComponentsInChildren<TaskChange>(true);
            if (taskChanges == null || taskChanges.Length == 0)
            {
                return;
            }

            hasTriggeredTaskAdvance = true;
            taskChanges[0].ChangeTaskIfNeeded();
        }

        // 判断是否在DragCube上
        public void SkipDrawing()
        {
            for (int i = 0; i < linePostions.Count; i++)
            {
                LinePositionV2 temp = linePostions[i];
                temp.IsDraw = true;
                linePostions[i] = temp;
            }

            showFinishText();
        }

        public bool IsPointInDragCube(Vector3 pos)
        {
            float distance = Vector3.Distance(pos, DragCube.transform.position);
            return distance < 0.1f; // 假设DragCube的大小是0.1的范围内
        }


        // 建立线段的函数，当传进去的值为null时，创建两个端点的线段，否则创建通过点的线段
        public void CreateString(Vector3? midPosition)
        {
            Vector3[] linePoints = new Vector3[midPosition == null ? 2 : 3]; //如果中间位置为null，那么点数组长度为2，否则为3。
            linePoints[0] = MoDou.transform.position;

            if (midPosition == null) //直接将终点设置为物体位置
            {
                linePoints[1] = MoDouWei.transform.position;
            }
            else //先将第二个点设置为拖拽位置，再将第三个点设置为物体位置
            {
                linePoints[1] = (Vector3)midPosition;
                linePoints[2] = MoDouWei.transform.position;
            }
            LineRendererMoDou.positionCount = linePoints.Length; //修改LineRenderer的位置数目为点的数目
            LineRendererMoDou.SetPositions(linePoints); //设置LineRenderer的所有位置
        }

        // 更新DragCube的位置到线的中间，并且让dragcube长度方向跟着线走
        void UpdateDragCubePosition()
        {
            Vector3 start = MoDou.transform.position;
            Vector3 end = MoDouWei.transform.position;

            DragCube.transform.position = (start + end) / 2;
            DragCube.transform.LookAt(end);
            float length = Vector3.Distance(start, end);
            DragCube.transform.localScale = new Vector3(DragCube.transform.localScale.x, DragCube.transform.localScale.y, length);
        }
    }
}
