using UnityEngine;
using UnityEngine.UI;

namespace HuaXian.ExportRuntime
{
    public class ShowLength : MonoBehaviour
    {
        public Camera MainCam;

        public LineRenderer lineRenderer;
        //public LineRenderer paneLineRender;
        public GameObject moDou;
       // public GameObject gragCube;
        public Transform pane;
        public Text text;
        //bool isDown = false;
        // Start is called before the first frame update
        void Start()
        {

            //paneLineRender.useWorldSpace = true;
            lineRenderer.useWorldSpace = true;
         //   gragCube.transform.GetComponent<MeshRenderer>().enabled = false;

        }
        private void OnMouseDrag()
        {
            Vector3 mousePos = Input.mousePosition;
            //将当前物体位置转换为屏幕坐标并赋值给鼠标位置，保证物体深度不会发生变化
            mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
            //将鼠标位置转化为世界坐标
            Vector3 objectPos = Camera.main.ScreenToWorldPoint(mousePos);
            transform.position = new Vector3(objectPos.x, transform.position.y, transform.position.z);
            lineRenderer.SetPosition(0, moDou.transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }


        // Update is called once per framelineRenderer
        void Update()
        {
            if (MainCam == null)
            {
                MainCam = Camera.main;
                if (MainCam == null)
                {
                    return;
                }
            }

            Vector3 CO_Direction = transform.position - MainCam.transform.position;
            float cPlane = Vector3.Dot(CO_Direction, MainCam.transform.forward);
            Vector3 pos = MainCam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, cPlane));
            CreateString(null);

        }


        public bool IsPointInLine(Vector3 pt_start, Vector3 pt_end, Vector3 pt_hit, float linesize)
        {
            if (((pt_hit.x - pt_start.x) * (pt_hit.x - pt_end.x) + (pt_hit.y - pt_start.y) * (pt_hit.y - pt_end.y)) < 0)
            {//必须是钝角
             //计算点到直线的距离
                float B = -(pt_end.x - pt_start.x);
                float A = pt_end.y - pt_start.y;
                float C = -(A * pt_start.x + B * pt_start.y);
                float t = A * pt_hit.x + B * pt_hit.y + C;
                float dd = (t * t) / (A * A + B * B);
                if (dd <= linesize * linesize)
                {
                    return true;
                }
            }
            return false;
        }


        public void CreateString(Vector3? midPosition)
        {
            Vector3[] linePoints = new Vector3[midPosition == null ? 2 : 3];
            linePoints[0] = moDou.transform.position;
            float len = Vector3.Distance(transform.position, moDou.transform.position);
            if (len > 0.01)
            {
                text.enabled = true;
                 ;

                text.text = len.ToString("F2") + " m";
            }
            else
            {
                text.enabled = false;
            }
            if (midPosition == null)
            {
                linePoints[1] = transform.position;
            }
            else
            {
                linePoints[1] = (Vector3)midPosition;
                linePoints[2] = transform.position;
            }

            lineRenderer.positionCount = linePoints.Length;
            lineRenderer.SetPositions(linePoints);
        }



    }
}
