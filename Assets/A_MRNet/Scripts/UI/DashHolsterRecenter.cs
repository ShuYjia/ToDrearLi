using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

public class DashHolsterRecenter : MonoBehaviour
{
    [SerializeField] private Transform targetRoot;
    [SerializeField] private ObjectManipulator objectManipulator;
    [SerializeField] private float distanceFromView = 0.65f;
    [SerializeField] private float verticalOffset = -0.05f;
    [SerializeField] private float recenterCooldown = 0.35f;
    [SerializeField] private bool recenterOnEnable = true;

    private float lastRecenterTime = float.NegativeInfinity;

    private void Reset()
    {
        targetRoot = transform;
        objectManipulator = GetComponent<ObjectManipulator>();
    }

    private void Awake()
    {
        if (targetRoot == null)
        {
            targetRoot = transform;
        }

        if (objectManipulator == null)
        {
            objectManipulator = GetComponent<ObjectManipulator>();
        }
    }

    private void OnEnable()
    {
        if (recenterOnEnable)
        {
            RecenterNow(force: true);
        }
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy || HasActiveManipulation())
        {
            return;
        }

        if (Time.unscaledTime - lastRecenterTime < recenterCooldown)
        {
            return;
        }

        Camera headCamera = Camera.main;
        if (headCamera == null)
        {
            return;
        }

        Vector3 toHolster = targetRoot.position - headCamera.transform.position;
        if (Vector3.Dot(headCamera.transform.forward, toHolster) <= 0f)
        {
            RecenterNow(force: false);
        }
    }

    public void RecenterNow()
    {
        RecenterNow(force: true);
    }

    private void RecenterNow(bool force)
    {
        Camera headCamera = Camera.main;
        if (headCamera == null)
        {
            return;
        }

        if (!force && HasActiveManipulation())
        {
            return;
        }

        Vector3 forward = headCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = headCamera.transform.forward;
        }
        forward.Normalize();

        Vector3 position = headCamera.transform.position + forward * distanceFromView;
        position.y = headCamera.transform.position.y + verticalOffset;
        targetRoot.position = position;

        Vector3 lookDirection = position - headCamera.transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            lookDirection = -forward;
        }

        targetRoot.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        lastRecenterTime = Time.unscaledTime;
    }

    private bool HasActiveManipulation()
    {
        if (objectManipulator == null)
        {
            return false;
        }

        return objectManipulator.isSelected ||
               objectManipulator.IsGrabSelected.Active ||
               objectManipulator.IsRaySelected.Active;
    }
}
