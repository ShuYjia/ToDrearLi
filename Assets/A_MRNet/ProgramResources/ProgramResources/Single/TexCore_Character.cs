using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public enum CamraEnem
{
    第三人称,
    第一人称
}

public class TexCore_Character : MonoBehaviour
{
    public static TexCore_Character self;
    public Transform cameraParent;

    public CamraEnem camraEnem;
    
    public Camera mainCamera;
    public Camera thirdPerson;
    public Vector3 thirdCameraRot;

    public Transform model;

    public GameObject[] stateModels;//0默认 1跑步

    public CharacterController controll;
    public Animator animator;
    public AudioSource runAudio;

    public bool isCamera = true;
    public bool enableMove=true;
    public bool enableRotCamera = true;

    [Tooltip("角色移动速度")]
    [Range(1f,600f)]
    public float moveSpeed;

    [Tooltip("角色旋转耗时")]
    [Range(0.1f,10f)]
    public float rotSpeed;

    [Tooltip("相机回正速度")]
    [Range(1f,10f)]
    public float cameraReRotSpeed;

    [Tooltip("相机旋转速度")]
    [Range(1f,10f)]
    public float cameraRotSpeed;

    [Tooltip("三称相机距离")]
    [Range(1f,10000f)]
    public float cameraDistance;

    private float h;
    private float v;

    public TextMeshProUGUI text;
    public Transform handPoint;

    //public bool onlyBack = false;
    public bool isTransfer = false;

    private void Awake()
    {
        self = this;
    }
    private void OnEnable()
    {
        TexCore_EventCenter.AddListener<bool>(TexCore_EventType.控制角色移动, EnableMove);
        TexCore_EventCenter.AddListener<bool>(TexCore_EventType.角色后退拉尺子, EnableBack);
        TexCore_EventCenter.AddListener<bool>(TexCore_EventType.控制角色视角旋转, EnableRot);
        TexCore_EventCenter.AddListener<bool>(TexCore_EventType.切换视角, EnableIsCamera);
    }
    private void OnDisable()
    {
        TexCore_EventCenter.RemoveListener<bool>(TexCore_EventType.控制角色移动, EnableMove);
        TexCore_EventCenter.RemoveListener<bool>(TexCore_EventType.角色后退拉尺子, EnableBack);
        TexCore_EventCenter.RemoveListener<bool>(TexCore_EventType.控制角色视角旋转, EnableRot);
        TexCore_EventCenter.RemoveListener<bool>(TexCore_EventType.切换视角, EnableIsCamera);
    }
    public void EnableMove(bool e)
    {
        //GetComponent<NavMeshAgent>().enabled = !e;
        enableMove = e;
        animator.SetBool("run", false);
        runAudio.Pause();
    }
    public void EnableBack(bool e)
    {
        thirdPerson.gameObject.SetActive(e==false);
        mainCamera.gameObject.SetActive(false);
        enableRotCamera = e == false;
        enableMove = e;
    }

    public void EnableRot(bool e)
    {
        enableRotCamera = e;
    }

    public void EnableIsCamera(bool e)
    {
        isCamera = e;
    }

    private void Start()
    {
        InvokeRepeating("ShowFPS", 0.5f, 0.5f);
    }
    public void ShowFPS()
    {
        if (text != null) 
        text.text = (1f / Time.deltaTime).ToString("F0");
    }
    void Update()
    {
        if (enableMove)
        {
            Move();
        }
        else 
        {
            runAudio.Pause();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            if(isCamera)

            if (mainCamera.gameObject.activeSelf)
            {
                thirdPerson.gameObject.SetActive(true);
                mainCamera.gameObject.SetActive(false);
                camraEnem = CamraEnem.第三人称;
            }
            else
            {
                mainCamera.gameObject.SetActive(true);
                thirdPerson.gameObject.SetActive(false);
                camraEnem = CamraEnem.第一人称;
            }
        }

        if (enableRotCamera && !KnowledgeManager.Self.transform.GetChild(0).gameObject.activeSelf)
        {
            //CameraRotate();
            if (thirdPerson.gameObject.activeSelf)
            {
                CameraRotate();
            }
            if (mainCamera.gameObject.activeSelf)
            {
                MainCameraRotate();
            }
        }
    }
    //private void OnGUI()
    //{
    //    if (GUI.Button(new Rect(Screen.width - 100f, Screen.height - 40f, 100f, 40f), "切换视角"))
    //    {
    //        mainCamera.gameObject.SetActive(mainCamera.gameObject.activeSelf == false);
    //        thirdPerson.gameObject.SetActive(thirdPerson.gameObject.activeSelf == false);
    //    }
    //}


    private Vector3 moveDirection;
    private Quaternion targetRotation;
    private Vector3 velocity;  // 用于储存角色的整体移动速度（包括重力）
    public float gravity = 9.81f;  // 重力加速度
    public float deceleration = 20f;  // 停止时的减速度

    /// <summary>
    /// 角色的移动逻辑
    /// </summary>
    public void Move()
    {
        // 获取输入
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 检查是否有输入
        if (v != 0 || h != 0)
        {
            // 设置动画和音效（只在开始移动时）
            if (!animator.GetBool("run"))
            {
                animator.SetBool("run", true);
                runAudio.Play();
            }

            // 计算移动方向
            moveDirection = (transform.right * h + transform.forward * v).normalized * moveSpeed;

            // 设置目标旋转方向
            SetTargetRotation(h, v);
            model.localRotation = Quaternion.Lerp(model.localRotation, targetRotation, rotSpeed * Time.deltaTime);
        }
        else
        {
            // 使用减速度使角色自然停止
            //moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, deceleration * Time.deltaTime);

            //// 停止移动时暂停动画和音效
            //if (animator.GetBool("run"))
            //{
            //    animator.SetBool("run", false);
            //    runAudio.Pause();
            //}
            moveDirection = Vector3.zero;
        }

        // 应用重力逻辑
        if (IsGrounded())
        {
            velocity.y = -1f; // 轻微下压以确保角色保持贴地
        }
        else
        {
            velocity.y -= gravity * Time.deltaTime; // 应用重力
        }

        if (!isTransfer && controll.enabled == true)  
        {
            // 合并移动方向和重力
            Vector3 finalMove = new Vector3(moveDirection.x, velocity.y, moveDirection.z);
            controll.Move(finalMove * Time.deltaTime);
        }
        else
        {
            isTransfer = false;
            GetComponent<NavMeshAgent>().enabled = true;
        }
    }

    /// <summary>
    /// 检测角色是否接触地面
    /// </summary>
    private bool IsGrounded()
    {
        float groundCheckDistance = 0.1f; // 地面检测距离
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }

    /// <summary>
    /// 设置八方向的目标旋转
    /// </summary>
    /// <param name="h">水平输入</param>
    /// <param name="v">垂直输入</param>
    private void SetTargetRotation(float h, float v)
    {
        float angle = Mathf.Atan2(h, v) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(0f, angle, 0f);
    }


    public void CameraRotate()
    {
        if (Input.touchCount == 0 && Input.GetMouseButton(0))
        {
            //thirdCameraRot = Vector3.zero;
            //thirdCameraRot.y += Input.GetAxis("Mouse X") * cameraRotSpeed;
            //thirdCameraRot.x += -Input.GetAxis("Mouse Y") * 0.8f * cameraRotSpeed;
            //thirdCameraRot.z = 0;
            //thirdPerson.eulerAngles = new Vector3(thirdCameraRot.x,thirdCameraRot.y,0);

            //transform.DOLocalRotate(new Vector3(0, Input.GetAxis("Mouse X") * cameraRotSpeed+ transform.localEulerAngles.y, 0), touchRotSpeedTime);
            //thirdPerson.DOLocalRotate(new Vector3(-Input.GetAxis("Mouse Y") * cameraRotSpeed+ thirdPerson.localEulerAngles.x, 0,  0), touchRotSpeedTime);
            transform.localEulerAngles = new Vector3(0, Input.GetAxis("Mouse X") * cameraRotSpeed + transform.localEulerAngles.y, 0);
            thirdPerson.transform.localEulerAngles = new Vector3(-Input.GetAxis("Mouse Y") * cameraRotSpeed + thirdPerson.transform.localEulerAngles.x, 0, 0);
        }
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            //1.鼠标滑轮控制视野：
            float distenceDelta = Input.GetAxis("Mouse ScrollWheel");
            //mCamera.GetComponent<Camera>().fieldOfView += -distenceDelta * 10;
            //mCamera.GetComponent<Camera>().fieldOfView = Mathf.Clamp(mCamera.GetComponent<Camera>().fieldOfView, 30, 90);
            //2.鼠标滑轮控制鼠标视距的缩放 
            //thirdPerson.GetChild(0).position += thirdPerson.TransformDirection(-Vector3.forward) * cameraMovingSpeed * distenceDelta;
            //print(distenceDelta);
            float dis = distenceDelta * cameraDistance ;
            thirdPerson.transform.localPosition += -Vector3.forward * dis;
        }
    }
    public void MainCameraRotate()
    {
        if (Input.touchCount == 0 && Input.GetMouseButton(0))
        {
            //thirdCameraRot = Vector3.zero;
            //thirdCameraRot.y += Input.GetAxis("Mouse X") * cameraRotSpeed;
            //thirdCameraRot.x += -Input.GetAxis("Mouse Y") * 0.8f * cameraRotSpeed;
            //thirdCameraRot.z = 0;
            //thirdPerson.eulerAngles = new Vector3(thirdCameraRot.x,thirdCameraRot.y,0);
            //mainCamera.transform.DORotate(thirdCameraRot, touchRotSpeedTime, RotateMode.Fast);

            transform.localEulerAngles = new Vector3(0, Input.GetAxis("Mouse X") * cameraRotSpeed + transform.localEulerAngles.y, 0);
            mainCamera.transform.localEulerAngles = new Vector3(-Input.GetAxis("Mouse Y") * cameraRotSpeed + mainCamera.transform.localEulerAngles.x, 0, 0);
        }
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            //1.鼠标滑轮控制视野：
            float distenceDelta = Input.GetAxis("Mouse ScrollWheel");
            mainCamera.fieldOfView += -distenceDelta * 10;
            mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView, 30, 90);
            //2.鼠标滑轮控制鼠标视距的缩放 
           // mainCamera.position += transform.TransformDirection(-Vector3.forward) * cameraMovingSpeed * distenceDelta;
        }
    }

    public void SetPlayerPos(Transform tr)
    {
        gameObject.transform.position= tr.position;
    }

    public void SetPlayerRot(Transform tr)
    {
        gameObject.transform.rotation = tr.rotation;
    }

    public void SetCamera(CamraEnem camraEnem)
    {
        mainCamera.gameObject.SetActive(camraEnem == CamraEnem.第一人称);
        thirdPerson.gameObject.SetActive(camraEnem == CamraEnem.第三人称);
    }

}
