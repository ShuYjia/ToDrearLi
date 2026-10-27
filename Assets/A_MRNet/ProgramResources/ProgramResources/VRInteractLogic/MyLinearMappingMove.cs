using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MyLinearMappingMove : MonoBehaviour
{
    public List<ControlTransform> pathLists;

    public TexCore_LinearMapping mapping;

    public bool isPlayer = false;

    private void Awake()
    {
        if (mapping==null)
        {
            mapping = GetComponent<TexCore_LinearMapping>();
        }
    }
    private void Start()
    {
        
    }
    private void OnEnable()
    {
        mapping.OnValueChange.AddListener(Move);
    }
    private void OnDisable()
    {
        mapping.OnValueChange.RemoveListener(Move);
    }

    public void OnPathSync(bool isEnable)
    {
        foreach (var item in pathLists)
        {
            item.obj.SendMessage("ChangeIsSyncTrans", isEnable);
        }
    }

    public void Move(float f)
    {
        if (isPlayer)
        {
            foreach (var item in pathLists)
            {
                if (item.isMove)
                    item.obj.position = Vector3.Lerp(item.start.position, item.end.position, item.moveSpeed * f);

                if (item.isTurn)
                    item.obj.localRotation = Quaternion.Lerp(item.start.localRotation, item.end.localRotation, item.moveSpeed * f);

                if (item.isTurnAxis)
                {
                    Vector3 start = item.obj.localEulerAngles;
                    start[(int)item.turnAxis] = item.turnAngleStart;

                    Vector3 end = item.obj.localEulerAngles;
                    end[(int)item.turnAxis] = item.turnAngleEnd;

                    item.obj.eulerAngles = Vector3.Lerp(start, end, item.turnSpeed * f);
                }

            }
        }
        else
        {
            isPlayer = true;
        }
        
    }
}

[Serializable]
public class ControlTransform
{
    public Transform obj;
    public bool isMove;
    [Tooltip("位移和旋转同时时使用")]
    public bool isTurn;
    public Transform end;
    public Transform start;
    [Tooltip("单独旋转时用")]
    public bool isTurnAxis;

    public Dir turnAxis;
    public float turnAngleEnd;
    public float turnAngleStart;

    public float moveSpeed = 1f;
    public float turnSpeed = 1f;
}
/// <summary>
/// 物体需要朝向哪个方向进行水平移动
/// </summary>
public enum Dir
{
    X轴,
    Y轴,
    Z轴
}
//public void MoveInit()
//{
//    switch (dir)
//    {
//        case Dir.X轴:
//            moveDir = Vector3.right;
//            break;
//        case Dir.Y轴:
//            moveDir = Vector3.up;
//            break;
//        case Dir.Z轴:
//            moveDir = Vector3.forward;
//            break;
//        default:
//            break;
//    }
//}

