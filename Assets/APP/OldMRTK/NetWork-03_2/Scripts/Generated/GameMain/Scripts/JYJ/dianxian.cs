using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;


namespace NetWork03_2.ExportRuntime
{
    [RequireComponent(typeof(LineRenderer))]
    public class dianxian : MonoBehaviour
    {
        [Header("目标物体-开始物体")]
        public GameObject[] starrole;
        [Header("目标物体-终点物体")]
        public GameObject[] endrole;

        [Header("长度(0-1)")]
        public float Length;
        [Header("宽度(0-1)")]
        public float Width;
        [Header("曲率(20-100)")]
        public float Step;
        [Header("lineRenderer 材质球")]
        public Material LineMat;


        private LineRenderer lineRenderer;
        private List<Segment2> segments = new List<Segment2>();
        private List<Transform> linePos = new List<Transform>();

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        void Start()
        {

            segments = new List<Segment2>();
            for (int i = 0; i < Step; i++)
            {
                if (i == 0)
                {
                    var seg = new Segment2(new Vector3(0, 1, 0.5f), Length, 0, 0, transform);
                    segments.Add(seg);
                }
                else
                {
                    Segment2 seg = new Segment2(segments[i - 1], Length, 0, 0, transform);
                    segments.Add(seg);
                }
            }
            for (int i = 0; i < transform.childCount; i++)
            {
                linePos.Add(transform.GetChild(i));
            }

            linePos.Add(endrole[0].transform);
            linePos.Add(endrole[0].transform);
            lineRenderer.startWidth = Width;
            lineRenderer.endWidth = Width;
            lineRenderer.material = LineMat;
            lineRenderer.positionCount = linePos.Count;
        }

        private void Update()
        {
            OnPeopleRun();
            LineSet();
        }

        public void LineSet()
        {
            for (int i = 0; i < linePos.Count; i++)
            {
                lineRenderer.SetPosition(i, linePos[i].position);
            }

        }

        public void OnPeopleRun()
        {
            for (int i = segments.Count - 1; i >= 0; i--)
            {
                if (i == segments.Count - 1)
                {
                    segments[i].Follow(endrole[0].transform.position);
                }
                else
                {
                    segments[i].Follow(segments[i + 1].a);
                }
                segments[i].update();
            }

            segments[0].SetA(starrole[1].transform.position);


            for (int i = 1; i < segments.Count; i++)
            {
                segments[i].SetA(segments[i - 1].b);

            }
        }
    }

    //用于处理每一段
    public class Segment2
    {
        public Vector3 a;         //起点
        public Vector3 b;         //终点
        public float len;           //长度
        public float angle1;        //向量b-a  在xoz面的投影，与x轴的夹角
        public float angle2;        //向量b-a 与y轴的夹角
        public GameObject sphere;   //关节处添加一个球体
        public GameObject sylinder = null;   //每一段用一个圆柱体来连接
        public Transform _par;
        public LineRenderer lineRenderer;
        public Segment2(Segment2 parent_, float len_, float angle1_, float angle2_, Transform par)
        {
            a = new Vector3(parent_.b.x, parent_.b.y, parent_.b.z);
            len = len_;
            angle1 = angle1_;
            angle2 = angle2_;
            _par = par;
            calculateB();
            sphere = new GameObject();
            sphere.transform.parent = _par;
            sphere.transform.position = a;
        }
        public Segment2(Vector3 vector3, float len_, float angle1_, float angle2_, Transform par)
        {
            a = vector3;
            len = len_;
            angle1 = angle1_;
            angle2 = angle2_;
            _par = par;
            calculateB();
            sphere = new GameObject();
            sphere.transform.parent = _par;
            sphere.transform.localPosition = a;
        }
        public void calculateB()
        {
            float dy = len * Mathf.Cos(angle2);
            float dx = len * Mathf.Sin(angle2) * Mathf.Cos(angle1);
            float dz = len * Mathf.Sin(angle2) * Mathf.Sin(angle1);
            b = new Vector3(a.x + dx, a.y + dy, a.z + dz);

        }
        //目标位置pos,根据目标位置确定a的位置
        public void Follow(Vector3 pos)
        {
            Vector3 dir = pos - a; //单位向量

            angle1 = Mathf.Atan2(dir.z, dir.x);
            angle2 = Mathf.Atan2(Mathf.Sqrt(dir.x * dir.x + dir.z * dir.z), dir.y);
            dir = Vector3.Normalize(dir);
            dir = dir * len * (-1f);
            a = pos + new Vector3(dir.x, dir.y, dir.z);

        }

        public void SetA(Vector3 a_)
        {
            a = a_;
            update();
        }
        public void update()
        {
            calculateB();
            sphere.transform.position = a;
        }
    }
}
