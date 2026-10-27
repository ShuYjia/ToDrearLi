using Bpf.Diagnostics;
/**
 * 
 * 
 * 
 * **/
namespace TexCore_UnityTools
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.NetworkInformation;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;

    public class RectTools
    {
        public static void SetSizeFromChild(RectTransform rect,bool isHerizontol=false)
        {
            float a = 0;
            for (int i = 0; i < rect.childCount; i++)
            {
                if (isHerizontol)
                    a+= rect.GetChild(i).GetComponent<RectTransform>().sizeDelta.x;
                else
                    a += rect.GetChild(i).GetComponent<RectTransform>().sizeDelta.y;
            }
            Vector2 vector2 = rect.sizeDelta;
            if (isHerizontol)
                vector2.x = a;
            else
                vector2.y = a;
            rect.sizeDelta = vector2;
        }
    }

    public class PictureTools : MonoBehaviour
    {
        /// <summary>
        /// 添加Mac地址 记忆功能
        /// </summary>
        //public static string GetMacAddress()
        //{
        //    string macadd = string.Empty;
        //    //获取电脑的mac地址，作为唯一标识符：
        //    NetworkInterface[] ins = NetworkInterface.GetAllNetworkInterfaces();
        //    foreach (NetworkInterface net in ins)
        //    {
        //        macadd = net.GetPhysicalAddress().ToString();
        //        break;
        //    }
        //    return macadd;
        //}
        /// <summary>
        ///  Image 赋值
        /// </summary>
        public static void SetImage(Image image, Texture2D tex2d)
        {
            image.sprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), 0.5f * Vector2.one);
            //Vector2 imagesize = image.GetComponent<RectTransform>().sizeDelta;
            
            //image.GetComponent<RectTransform>().sizeDelta = imagesize;
        }

        /// <summary>
        ///  Image 赋值
        /// </summary>
        public static void SetImageAutoSize(Image image, Texture2D tex2d)
        {
            image.sprite = Sprite.Create(tex2d, new Rect(0, 0, tex2d.width, tex2d.height), 0.5f * Vector2.one);
            Vector2 imagesize= image.GetComponent<RectTransform>().sizeDelta;
            if (tex2d.width>tex2d.height)
            {
                //固定宽度
                float i = (float)tex2d.width / (float)tex2d.height;
                if (i> imagesize.x/ imagesize.y)
                {
                    //固定高度
                    float t = imagesize.x / tex2d.width;//图片缩放比例
                    imagesize.y = tex2d.height * t;
                }
                else
                {
                    float t = imagesize.y / tex2d.height;//图片缩放比例
                    imagesize.x = tex2d.width * t;
                }
            }
            else
            {
                //固定高度
                float t = imagesize.y / tex2d.height;//图片缩放比例
                imagesize.x = tex2d.width * t;
            }
            image.GetComponent<RectTransform>().sizeDelta = imagesize;
        }
        public static void SetTexture2DAutoSize(RectTransform rectTransform, Texture2D tex2d)
        {
            Vector2 imagesize = rectTransform.sizeDelta;
            if (tex2d.width > tex2d.height)
            {
                //固定宽度
                float i = (float)tex2d.width / (float)tex2d.height;
                if (i > imagesize.x / imagesize.y)
                {
                    //固定高度
                    float t = imagesize.x / tex2d.width;//图片缩放比例
                    imagesize.y = tex2d.height * t;
                }
                else
                {
                    float t = imagesize.y / tex2d.height;//图片缩放比例
                    imagesize.x = tex2d.width * t;
                }
            }
            else
            {
                //固定高度
                float t = imagesize.y / tex2d.height;//图片缩放比例
                imagesize.x = tex2d.width * t;
            }
            rectTransform.sizeDelta = imagesize;
        }

        public static IEnumerator GetTexture2DByWWW(string url, RawImage raw, bool isAutoSize = false)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (isAutoSize)
                    PictureTools.SetTexture2DAutoSize(raw.GetComponent<RectTransform>(), texture);
                raw.texture = texture;
            }
            else
            {
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture2_d_by_www", null, null, BpfLogField.Of("error", request.error));
            }
        }
        public static IEnumerator GetTexture2DWeb(string filepath, Image image,bool isAutoSize=false)
        {
            BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture2_d_web", "加载图片文件：" + filepath);
            UnityWebRequest www = UnityWebRequest.Get(filepath);
            yield return www.SendWebRequest();
            if (www.isDone)
            {
                //下载完成
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture2_d_web", null, null, BpfLogField.Of("text", www.downloadHandler.text));
                byte[] file = www.downloadHandler.data;
                Texture2D texture2D=new Texture2D(1000,1000);
                texture2D.LoadImage(file);
                if (isAutoSize)
                    SetImageAutoSize(image, texture2D);
                else
                    SetImage(image, texture2D);
                
            }
            else
            {
                //下载出错
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture2_d_web", null, null, BpfLogField.Of("error", www.error));
            }
        }

        /// <summary>
        /// 异步加载图片资源
        /// </summary>
        public static IEnumerator GetTexture2D(string filepath, Image image,bool isSave = false, string key = null, Dictionary<string, Texture2D> dic = null)
        {
            BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture2_d", null, null, BpfLogField.Of("filepath", filepath));
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(filepath);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture2_d", "error:" + request.error);
                yield break;
            }
            else if (request.isDone)
            {
                Texture2D tex2d = DownloadHandlerTexture.GetContent(request);
                SetImage(image, tex2d);
                if (isSave)
                    dic[key] = tex2d;
            }
        }

        public static IEnumerator GetTextureMaterial(string filepath, Material material)
        {
            BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture_material", null, null, BpfLogField.Of("filepath", filepath));
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(filepath);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture_material", "error:" + request.error);
                yield break;
            }
            else if (request.isDone)
            {
                material.mainTexture = DownloadHandlerTexture.GetContent(request);
            }
        }

        public static Texture2D DrawChangeShapeMap(Texture2D _tex, int _width, int _height, float _scaleX, float _scaleY)
        {
            Texture2D tex = new Texture2D(_width, _height, TextureFormat.ARGB32, true);
            int halfWidth = _width / 2;
            int halfHeight = _height / 2;
            int scaleWidth = (int)(halfWidth * _scaleX);
            int scaleHeight = (int)(halfHeight * _scaleY);
            Color[] texColors = _tex.GetPixels();//传入贴图的所有像素点颜色
            Color[] colors = new Color[_width * _height];

            int texX, texY;//生成贴图的x.y
            for (int i = halfHeight - scaleHeight; i < halfHeight + scaleHeight; i++)
            {
                for (int j = halfWidth - scaleWidth; j < halfWidth + scaleWidth; j++)
                {
                    texX = (int)(halfHeight + (i - halfHeight) * 1.0f / _scaleY);
                    texY = (int)(halfWidth + (j - halfWidth) * 1.0f / _scaleX);
                    texX = (texX >= 0) ? texX : 0;
                    texY = (texY >= 0) ? texY : 0;
                    texX = (texX < _height) ? texX : _height - 1;
                    texY = (texY < _width) ? texY : _width - 1;
                    colors[i * _width + j] = texColors[texX * _width + texY];
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            texColors = null;
            colors = null;
            System.GC.Collect();
            return tex;
        }
        public static IEnumerator GetTextureByWWW(string _path, Texture2D _tex)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(_path);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture_by_www", null, null, BpfLogField.Of("error", request.error));
            }
            else
            {
                _tex = DownloadHandlerTexture.GetContent(request);
            }
        }
        protected static byte[] GetBytes(string _path)
        {
            FileStream fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read);
            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, (int)fileStream.Length);
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;
            return bytes;
        }
        public static Texture2D GetTexture(string _path, int _width = 1920, int _height = 1080)
        {
            byte[] bytes;
            try
            {
                bytes = GetBytes(_path);
            }
            catch (System.Exception e)
            {
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.get_texture", null, null, BpfLogField.Of("inner_exception", e.InnerException));
                return null;
            }
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            Texture2D tex = new Texture2D(_width, _height);
            tex.LoadImage(bytes);

            string[] strs = _path.Split('/');
            tex.name = strs[strs.Length - 1].Split('.')[0];
            return tex;
        }
        public static Sprite GetSprite(string _path, int _width = 1920, int _height = 1080)
        {
            if (_path == null)
            {
                return null;
            }
            Texture2D tex = GetTexture(_path, _width, _height);
            if (tex == null)
            {
                return null;
            }
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            sp.name = tex.name;
            return sp;
        }
        public static Texture2D[] GetTextures(string _path, string _pictureType, int _width = 1920, int _height = 1080)
        {
            string[] files = Directory.GetFiles(_path, _pictureType, SearchOption.AllDirectories);
            Texture2D[] texs = new Texture2D[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                texs[i] = GetTexture(files[i], _width, _height);
                string[] strs = files[i].Split('/');
                texs[i].name = strs[strs.Length - 1].Split('.')[0];
            }
            return texs;
        }
        public static Sprite[] GetSprites(string _path, string _pictureType, int _width = 1920, int _height = 1080)
        {
            if (_path == null)
            {
                return null;
            }
            Texture2D[] texs = GetTextures(_path, _pictureType, _width, _height);
            if (texs == null)
            {
                return null;
            }
            Sprite[] sprites = new Sprite[texs.Length];
            for (int i = 0; i < texs.Length; i++)
            {
                sprites[i] = Sprite.Create(texs[i], new Rect(0, 0, texs[i].width, texs[i].height), new Vector2(0.5f, 0.5f));
                sprites[i].name = texs[i].name;
            }
            return sprites;
        }
        public static Texture2D GetTextureByIO(string _path)
        {
            FileStream fileStream = new FileStream(_path, FileMode.Open, FileAccess.Read);
            fileStream.Seek(0, SeekOrigin.Begin);
            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, (int)fileStream.Length);
            fileStream.Close();
            fileStream.Dispose();
            fileStream = null;

            Texture2D tex = new Texture2D(1, 1);
            tex.LoadImage(bytes);
            return tex;
        }
    }
    public class DrawTexture
    {
        /// <summary>
        /// 高斯正态分布概率
        /// </summary>
        /// <param name="_ndc"></param> Normal distribution coefficient 正态分布系数
        /// <param name="_start"></param> 起始点 0
        /// <param name="_end"></param> 终结点 1
        /// <returns></returns>
        protected static float NormalDistribution(float _ndc, float _start, float _end)
        {
            float pi = 1 / Mathf.Sqrt(2 * Mathf.PI);       //0.3989423f;
            float variance = _end * _end;
            float power = -(Mathf.Pow(Mathf.Abs(_ndc - _start), 2) / (2 * variance));
            float probability = (pi / _end) * Mathf.Exp(power);
            return probability;
        }

        /// <summary>
        /// 通过高斯正态分布公式生成一张离散效果图
        /// </summary>
        /// <param name="_width"></param> 图片宽度
        /// <param name="_height"></param> 图片高度
        /// <param name="_radius"></param> 画出圆形的半径
        /// <param name="_alpha"></param> 所有画出像素点的alpha值
        /// <param name="_color"></param> 所有画出像素点的颜色值       1,0,0
        /// <param name="_restrain"></param> 离散约束 ---- 值越小画出的像素点越密集     0.05
        /// <returns></returns>
        public static Texture2D DrawCircluarMap(int _width, int _height, int _radius, float _alpha, Vector3 _color, float _restrain = 0.2f)
        {
            Texture2D tex = new Texture2D(_width, _height, TextureFormat.ARGB32, true);
            Color[] colors = new Color[_width * _height];
            int halfWidth = _width / 2;
            int halfHeight = _height / 2;

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    //根据距离进行高斯分布概率计算
                    float distribution = (Mathf.Sqrt((i - halfWidth) * (i - halfWidth) + (j - halfHeight) * (j - halfHeight))) / _radius;
                    float probability = NormalDistribution(distribution, 0, 1);  ///[0~0.3989423f]

                    //设置绘制区域
                    bool discreteness = UnityEngine.Random.Range(0, probability) > _restrain ? true : false; // _restrain的数据越大，绘制的像素点越少
                    if (discreteness)
                    {
                        colors[i * _width + j] = new Color(_color.x / 255, _color.y / 255, _color.z / 255, probability * _alpha);
                    }
                    else
                    {
                        colors[i * _width + j] = new Color(1, 1, 1, 1);
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            colors = null;
            System.GC.Collect();
            return tex;
        }
    }
    public class TimeTools
    {
        /// <summary>
        /// 时间转换(PS:秒转换为02:30)
        /// </summary>
        public static string GetTexCoreCurrentTime(float seconds)
        {
            string s = string.Empty;
            int min = (int)(seconds / 60);
            int TexCoresceond = (int)(seconds % 60);
            s = string.Format("{0:D2}:{1:D2}", min, TexCoresceond);
            return s;
        }
        public static void GetSpanCode(DateTime now)
        {
            //一.找到第一周的最后一天（先获取1月1日是星期几，从而得知第一周周末是几）
            int firstWeekend = 7 - Convert.ToInt32(DateTime.Parse(now.Year + "-1-1").DayOfWeek);
            //二.获取今天是一年当中的第几天
            int currentDay = now.DayOfYear;
            //三.获取当前时间这一年第几周
            int currentWeek = Convert.ToInt32(Math.Ceiling((currentDay - firstWeekend) / 7.0)) + 1;
            //四.获取当前时间的当前月
            int currentMonth = now.Month;
            //五.获取当前时间的当前年
            int currentYear = now.Year;
            //switch (CONST.progress)
            //{
            //    case ProgressType.总进度:
            //    case ProgressType.总计划:
            //    case ProgressType.总进度计划对比: CONST.SpanCode = currentYear.ToString() + "01A"; break;

            //    case ProgressType.月计划:
            //    case ProgressType.月进度:
            //    case ProgressType.月进度计划对比: CONST.SpanCode = currentYear.ToString() + currentMonth.ToString().PadLeft(2, '0') + "C"; break;

            //    case ProgressType.周计划:
            //    case ProgressType.周进度:
            //    case ProgressType.周进度计划对比:
            //        CONST.SpanCode = currentYear.ToString() + currentWeek.ToString().PadLeft(2, '0') + "D";
            //        CONST.SpanCode_Mon = currentYear.ToString() + currentMonth.ToString().PadLeft(2, '0') + "C";
            //        break;
            //}
        }
    }

    public class GameobjectTools : MonoBehaviour
    {
        /// <summary>
        /// 清除指定节点下面的所有子物体
        /// </summary>
        public static void ClearChildren(Transform root)
        {
            int childrenNum = root.childCount;
            for (int i = 0; i < childrenNum; i++)
            {
                Destroy(root.GetChild(i).gameObject);
                Resources.UnloadUnusedAssets();
            }
        }


        /// <summary>
        ///  控制Root(多个)物体的显示、隐藏
        /// </summary>
        public static void SetRootObjsActive(Transform root, bool isshsw, bool isactivebox = false)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).gameObject.SetActive(isshsw);
                if (isactivebox == true && root.GetChild(i).GetComponent<BoxCollider>())
                {
                    root.GetChild(i).GetComponent<BoxCollider>().enabled = false;
                }
            }
        }

        /// <summary>
        /// 实例化物体
        /// </summary>
        public static GameObject InstantiateObj(string resurl, string name, Transform parent)
        {
            GameObject m_JackObj = (GameObject)Instantiate(Resources.Load(resurl));
            m_JackObj.name = name;
            m_JackObj.transform.SetParent(parent, false);
            //m_JackObj.transform.localPosition = localpos;
            m_JackObj.transform.localScale = Vector3.one;
            return m_JackObj;
        }
        public static GameObject InstantiateObj(string resurl, string name, Transform parent, Vector3 localpos, Vector3 localrot, Vector3 localscale)
        {
            GameObject m_JackObj = Instantiate(Resources.Load<GameObject>(resurl));
            m_JackObj.name = name;
            m_JackObj.transform.SetParent(parent);

            m_JackObj.transform.localPosition = localpos;
            m_JackObj.transform.localEulerAngles = localrot;
            m_JackObj.transform.localScale = localscale;
            return m_JackObj;
        }
        public static GameObject ShowSelectedModels(Transform root, string selectname)
        {
            if (root == null)
                return null;
            GameObject result = null;
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).name.Equals(selectname))
                {
                    root.GetChild(i).gameObject.SetActive(true);
                    result = root.GetChild(i).gameObject;
                }
                else
                    root.GetChild(i).gameObject.SetActive(false);
            }
            return result;
        }
        public static GameObject GetSelectModel(Transform root, string selectname)
        {
            GameObject result = null;
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i).name.Equals(selectname))
                {
                    result = root.GetChild(i).gameObject;
                    break;
                }
            }
            return result;
        }
        public static void ShowCumulativeModels(Transform root, string selectname)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).gameObject.SetActive(false);
            }
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).gameObject.SetActive(true);
                if (root.GetChild(i).name.Equals(selectname))
                    break;
            }
        }

        /// <summary>
        /// 显示root 下一级的所有物体
        /// </summary>
        public static void ShowAllModels(Transform root)
        {
            if (root == null)
                return;
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 显示当前root下的所有高光模型：1为On;2为Off ; 3为flash; 4为flashoff
        /// </summary>
        //public static void ShowRootHightLight(GameObject root, int index, Color color)
        //{
        //    if (root == null) return;

        //    HighlighterController[] hos = root.GetComponentsInChildren<HighlighterController>();
        //    if (hos == null || hos.Length == 0)
        //        return;

        //    switch (index)
        //    {
        //        case 1:
        //            {
        //                for (int i = 0; i < hos.Length; i++)
        //                    hos[i].MouseOver(color);
        //            }
        //            break;
        //        case 2:
        //            {
        //                for (int i = 0; i < hos.Length; i++)
        //                    hos[i].MouseOff();
        //            }
        //            break;
        //        case 3:
        //            {
        //                for (int i = 0; i < hos.Length; i++)
        //                    hos[i].FlashOn(color, color);
        //            }
        //            break;
        //        case 4:
        //            {
        //                for (int i = 0; i < hos.Length; i++)
        //                    hos[i].FlashOff();
        //            }
        //            break;
        //    }
        //}

        /// <summary>
        /// 显示当前root下的所有高光模型
        /// </summary>
        //public static void ShowRootFlashHightLight(GameObject root, Color color1, Color color2)
        //{
        //    if (root == null) return;
        //    HighlighterController[] hos = root.GetComponentsInChildren<HighlighterController>();
        //    if (hos == null || hos.Length == 0)
        //        return;
        //    for (int i = 0; i < hos.Length; i++)
        //        hos[i].FlashOn(color1, color2);
        //}
    }

    public class MathTools
    {
        /// <summary>
        ///  字符串转Int
        /// </summary>
        public static int JudgeInt(string s)
        {
            int i;
            if (int.TryParse(s, out i))
            {
                i = int.Parse(s);
            }
            return i;
        }

        /// <summary>
        ///  字符串转Float
        /// </summary>
        public static float Getfloat(string s)
        {
            float m_jack = 0;
            float f;
            if (float.TryParse(s, out f))
            {
                m_jack = float.Parse(s);
            }
            return m_jack;
        }

        /// <summary>
        /// 计算这一行的坐标
        /// </summary>
        public static Vector3 GetPos(int index, int width, int hight, int RowCount)
        {
            Vector3 v3 = Vector3.zero;
            int y = -(index / RowCount) * hight;
            int x = index % RowCount;
            v3 = new Vector3(x * width, y, 0);
            return v3;
        }

        /// <summary>
        /// 把字符串转换为Vector3
        /// </summary>
        public static Vector3 ConvertString2Vector3(string str, char symbol)
        {
            Vector3 v3 = new Vector3(0, 0, 0);
            if (str != string.Empty || str != null)
            {
                string[] strs = str.Split(symbol);
                if (strs.Length == 3)
                {
                    v3.x = float.Parse(strs[0]);
                    v3.y = float.Parse(strs[1]);
                    v3.z = float.Parse(strs[2]);
                }
                else
                {
                    BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.convert_string2_vector3", "This str is can not convert to a Vector3.");
                }
            }
            else
            {
                BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.convert_string2_vector3", "This str is null.");
            }
            return v3;
        }

        /// <summary>
        /// 把Vector3转换为字符串
        /// </summary>
        public static string ConvertVector2String(Vector3 pos, char symbol = ',')
        {
            string info = string.Empty;
            info = pos.x.ToString(".00") + symbol + pos.y.ToString(".00") + symbol + pos.z.ToString(".00");
            return info;
        }

        public static string GetPbsId(int index, int Number = 5)
        {
            string result = string.Empty;
            result = index.ToString().PadLeft(Number, '0');
            return result;
        }

        /// <summary>
        /// 字符串传颜色
        /// </summary>
        public static Color GetCol(string info)
        {
            //字符串分割：
            Color temp = Color.white;
            string[] str = info.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (str.Length >= 3)
            {
                temp = new Color(Getfloat(str[0]) / 255f, Getfloat(str[1]) / 255f, Getfloat(str[2]) / 255f);
            }
            return temp;
        }

        public static string GetLastStrName(string original)
        {
            string result = string.Empty;
            string[] str = original.Split(new char[] { '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            result = str[str.Length - 1];
            return result;

        }

        /// <summary>
        /// 触屏手势放大或缩小
        /// </summary>
        public static bool isEnlarge(Vector2 oP1, Vector2 oP2, Vector2 nP1, Vector2 nP2)
        {
            //函数传入上一次触摸两点的位置与本次触摸两点的位置计算出用户的手势
            var leng1 = Mathf.Sqrt((oP1.x - oP2.x) * (oP1.x - oP2.x) + (oP1.y - oP2.y) * (oP1.y - oP2.y));
            var leng2 = Mathf.Sqrt((nP1.x - nP2.x) * (nP1.x - nP2.x) + (nP1.y - nP2.y) * (nP1.y - nP2.y));
            if (leng1 < leng2)
            {
                //放大手势
                return true;
            }
            else
            {
                //缩小手势
                return false;
            }
        }

        /// <summary>
        /// 获取两点之间距离一定百分比的一个点
        /// </summary>
        /// <param name="start">起始点</param>
        /// <param name="end">结束点</param>
        /// <param name="distance">起始点到目标点距离百分比</param>
        /// <returns></returns>
        public static Vector3 GetBetweenPoint(Vector3 start, Vector3 end, float percent = 0.5f)
        {
            Vector3 normal = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            return normal * (distance * percent) + start;
        }

    }

    public class AssesbundleTools : MonoBehaviour
    {
        /// <summary>
        /// 加载单张压缩图片
        /// </summary>
        public static IEnumerator loadthumbnail(string asssesbundlename, Image image)
        {
            string fullpath = /*TexCore_CONST.ResDirPath + "/tex/" + asssesbundlename + ".zlp"*/"";
            string filepath = fullpath.Replace("file:///", "");
            if (File.Exists(filepath))
            {
                WWW www = new WWW(fullpath);
                yield return www;
                if (www.error != null)
                {
                    BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.loadthumbnail", "www.error:" + www.error);
                    yield break;
                }
                else if (www.isDone)
                {
                    var TexCoreloadassetbundle = www.assetBundle;
                    if (TexCoreloadassetbundle == null)
                    {
                        BpfLog.Info(BpfLogCategory.TexCore, "tex_core_tools.loadthumbnail", "Failed to load AssetBundle!");
                        yield break;
                    }
                    Texture2D tex = TexCoreloadassetbundle.LoadAsset<Texture2D>(asssesbundlename);
                    image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    TexCoreloadassetbundle.Unload(false);
                }
            }
        }

        /// <summary>
        /// 加载多张图片
        /// </summary>
        public static void loadTexs(AssetBundleRequest request, List<Texture2D> objs)
        {
            UnityEngine.Object[] models = request.allAssets as UnityEngine.Object[];
            for (int i = 0; i < models.Length; i++)
            {
                Texture2D model = models[i] as Texture2D;
                model.name = models[i].name;
                objs.Add(model);
            }
        }

        /// <summary>
        /// 加载单个场景模型
        /// </summary>
        public static void loadSceneModel(AssetBundleRequest request, Transform parent, string scenemodelname, bool isshow = true)
        {
            if (request == null)
                return;
            GameObject obj = Instantiate(request.asset) as GameObject;
            obj.name = scenemodelname;
            obj.transform.parent = parent;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localEulerAngles = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            obj.SetActive(isshow);
        }
        public static void loadSingleModel(AssetBundleRequest request, Transform parent, string scenemodelname)
        {
            if (request == null)
                return;
            GameObject obj = Instantiate(request.asset) as GameObject;
            obj.name = scenemodelname;
            obj.transform.parent = parent;
            //obj.transform.localPosition = Vector3.zero;
            //obj.transform.localEulerAngles = Vector3.zero;
            //obj.transform.localScale = Vector3.one;
        }

        public static void loadSingleModel(GameObject model, Transform parent, string scenemodelname)
        {
            if (model == null)
                return;
            GameObject obj = Instantiate(model) as GameObject;
            obj.name = scenemodelname;
            obj.transform.parent = parent;

            //obj.transform.localPosition = Vector3.zero;
            //obj.transform.localEulerAngles = Vector3.zero;
            //obj.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 加载多模型
        /// </summary>
        public static void loadmodels(AssetBundleRequest request, Transform parent, bool isshow = true)
        {
            UnityEngine.Object[] models = request.allAssets as UnityEngine.Object[];
            for (int i = 0; i < models.Length; i++)
            {
                GameObject model = Instantiate(models[i]) as GameObject;
                model.name = models[i].name;
                model.transform.parent = parent;
                model.transform.localPosition = Vector3.zero;
                model.transform.localEulerAngles = Vector3.zero;
                model.transform.localScale = Vector3.one;
                model.SetActive(isshow);
            }
        }
    }
/*
    public class PBSTools
    {

        private static PBSItem TexCoreid = null;
        /// <summary>
        /// 通过id过滤PBS
        /// </summary>
        public static PBSItem PBSFilterID(PBSItem pbsitem, string id)
        {
            if (pbsitem.id == id)
            {
                TexCoreid = pbsitem;
            }
            else
            {
                if (pbsitem.children.Count > 0)
                {
                    for (int i = 0; i < pbsitem.children.Count; i++)
                    {
                        if (pbsitem.children[i].id == id)
                        {
                            TexCoreid = pbsitem.children[i]; break;
                        }
                        else
                        {
                            PBSFilterID(pbsitem.children[i], id);
                        }
                    }
                }
            }
            return TexCoreid;
        }



        private static PBSItem Name = null;
        /// <summary>
        /// 通过节点名称过滤PBS
        /// </summary>
        public static PBSItem PBSFilterName(PBSItem pbsitem, string name)
        {
            if (pbsitem.Name == name)
            {
                Name = pbsitem;
            }
            else
            {
                if (pbsitem.children.Count > 0)
                {
                    for (int i = 0; i < pbsitem.children.Count; i++)
                    {
                        if (pbsitem.children[i].Name == name)
                        {
                            Name = pbsitem.children[i]; break;
                        }
                        else
                        {
                            PBSFilterName(pbsitem.children[i], name);
                        }
                    }
                }
            }
            return Name;
        }


        private static PBSItem ModelName = null;
        /// <summary>
        /// 通过模型名称过滤PBS
        /// </summary>
        public static PBSItem PBSFilterModelName(PBSItem pbsitem, string modelname)
        {
            if (pbsitem.modelName == modelname)
            {
                ModelName = pbsitem;
            }
            else
            {
                if (pbsitem.children.Count > 0)
                {
                    for (int i = 0; i < pbsitem.children.Count; i++)
                    {
                        if (pbsitem.children[i].modelName == modelname)
                        {
                            ModelName = pbsitem.children[i]; break;
                        }
                        else
                        {
                            PBSFilterModelName(pbsitem.children[i], modelname);
                        }
                    }
                }
            }
            return ModelName;
        }

    }*/

    public class StringParse
    {
        public static Vector2 StringToVector2(string str)
        {

            string[] strs = str.Split('_');
            return new Vector2(float.Parse(strs[0]), float.Parse(strs[1]));
        }

        public static string Vector2ToString(float x, float y)
        {
            return x.ToString() + "_" + y.ToString();
        }
        public static string Vector2ToString(Vector2 v2)
        {
            return v2.x.ToString() + "_" + v2.y.ToString();
        }
        public static string Vector3ToString(Vector3 v3)
        {
            return v3.x + "_" + v3.y + "_" + v3.z;
        }
        public static Vector3 StringToVector3(string str)
        {
            string[] strs = str.Split('_');
            return new Vector3(float.Parse(strs[0]), float.Parse(strs[1]), float.Parse(strs[2]));
        }
        public static Vector4 StringToVector4(string str)
        {
            string[] strs = str.Split('_');
            return new Vector4(float.Parse(strs[0]), float.Parse(strs[1]), float.Parse(strs[2]), float.Parse(strs[3]));
        }
        public static Quaternion StringToQuaternion(string str)
        {
            string[] strs = str.Split('_');
            return new Quaternion(float.Parse(strs[0]), float.Parse(strs[1]), float.Parse(strs[2]), float.Parse(strs[3]));
        }
        public static Color StringToColor(string str)
        {
            string[] strs = str.Split('_');
            return new Color(float.Parse(strs[0]), float.Parse(strs[1]), float.Parse(strs[2]), float.Parse(strs[3]));
        }
        public static string Vector4ToString(float x, float y, float z, float w)
        {

            return x + "_" + y + "_" + z + "_" + w;
        }
        public static string Vector4ToString(Quaternion quaternion)
        {

            return quaternion.x + "_" + quaternion.y + "_" + quaternion.z + "_" + quaternion.w;
        }
        public static string Vector4ToString(Color color)
        {

            return color.r + "_" + color.g + "_" + color.b + "_" + color.a;
        }
       
    }

    
}


