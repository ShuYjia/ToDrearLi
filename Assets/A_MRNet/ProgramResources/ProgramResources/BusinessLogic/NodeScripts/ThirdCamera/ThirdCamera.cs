using DG.Tweening;
using Bpf.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ThirdCamera : MonoBehaviour
{
    public static TexCore_Character self;
    public Camera mainCamera;
    public Camera thirdPerson;
    public Vector3 thirdCameraRot;


    [Tooltip("相机回正速度")]
    [Range(1f, 10f)]
    public float cameraReRotSpeed;

    [Tooltip("相机旋转速度")]
    [Range(1f, 10f)]
    public float cameraRotSpeed;

    [Tooltip("三称相机距离")]
    [Range(1f, 10000f)]
    public float cameraDistance;

    private float h;
    private float v;
    private void Awake()
    {
    }
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.C))
        {
            mainCamera.gameObject.SetActive(mainCamera.gameObject.activeSelf == false);
            thirdPerson.gameObject.SetActive(thirdPerson.gameObject.activeSelf == false);
        }

        if (thirdPerson.gameObject.activeSelf)
        {
            CameraRotate();
        }
        if (mainCamera.gameObject.activeSelf)
        {
            MainCameraRotate();
        }
    }

    public void CameraRotate()
    {
        if (Input.touchCount == 0 && Input.GetMouseButton(1))
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
            BpfLog.Info(BpfLogCategory.XR, "third_camera.zoom", null, null, BpfLogField.Of("delta", distenceDelta));
            float dis = distenceDelta * cameraDistance;
            thirdPerson.transform.localPosition += -Vector3.forward * dis;
        }
    }
    public void MainCameraRotate()
    {
        if (Input.touchCount == 0 && Input.GetMouseButton(1))
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

}
