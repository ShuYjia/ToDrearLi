using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Reports the first MRTK grab of an object to the existing TexCore operation
/// topic flow. This is used for a task whose required action is taking an item.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(ObjectManipulator))]
public sealed class TexCore_GrabStep : TexCore_InteracterBase
{
    [SerializeField] private bool resetWhenInactive = true;
    [SerializeField] private GameObject activateOnGrab;

    private ObjectManipulator manipulator;
    private Rigidbody body;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Canvas[] canvases;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool stepActive;
    private bool completed;

    private void Awake()
    {
        manipulator = GetComponent<ObjectManipulator>();
        body = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        canvases = GetComponentsInChildren<Canvas>(true);
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public new void OnEnable()
    {
        base.OnEnable();
        if (!Application.isPlaying)
        {
            return;
        }

        if (manipulator == null)
        {
            manipulator = GetComponent<ObjectManipulator>();
        }

        manipulator.firstSelectEntered.AddListener(HandleGrabbed);
        ApplyInteractionState();
    }

    private void OnDisable()
    {
        if (Application.isPlaying && manipulator != null)
        {
            manipulator.firstSelectEntered.RemoveListener(HandleGrabbed);
        }

        base.OnDisabel();
    }

    public override void UnFinished()
    {
        base.UnFinished();
        completed = false;
        stepActive = false;
        if (resetWhenInactive)
        {
            ResetToStart();
        }
        ApplyInteractionState();
    }

    public override void UnderWay()
    {
        base.UnderWay();
        completed = false;
        stepActive = true;
        if (resetWhenInactive)
        {
            ResetToStart();
        }
        ApplyInteractionState();
    }

    public override void OnTrigger()
    {
        base.OnTrigger();
        completed = true;
        stepActive = false;
        ApplyInteractionState();
    }

    public override void Finish()
    {
        base.Finish();
        completed = true;
        stepActive = false;
        ApplyInteractionState();
    }

    private void HandleGrabbed(SelectEnterEventArgs args)
    {
        if (!stepActive || completed)
        {
            return;
        }

        completed = true;
        if (activateOnGrab != null)
        {
            activateOnGrab.SetActive(true);
        }
        TexCore_BusinessManager.OnClick(gameObject);
    }

    private void ApplyInteractionState()
    {
        bool enabled = stepActive && !completed;
        if (manipulator != null)
        {
            manipulator.AllowedManipulations = TransformFlags.Move;
            TexCore_03NoScalePolicy.Enforce(manipulator);
            manipulator.enabled = enabled;
        }

        foreach (Renderer item in renderers)
        {
            item.enabled = enabled;
        }

        foreach (Collider item in colliders)
        {
            item.enabled = enabled;
        }

        foreach (Canvas item in canvases)
        {
            item.enabled = enabled;
        }
    }

    private void ResetToStart()
    {
        transform.SetPositionAndRotation(startPosition, startRotation);
        if (body != null)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
