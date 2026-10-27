using System.Collections;
using UnityEngine;

namespace HuaXian.ExportRuntime
{
    public sealed class MyMarker : MonoBehaviour
    {
        public bool OnStartDroaw;
        public Material DrawMaterial;
        public Color DrawColor = Color.red;
        public float LineWidth = 0.02f;
        public Transform RaycastStart;
        public LayerMask DrawingLayers;
        public float RaycastLength = 0.01f;
        public float MinDrawDistance = 0.02f;
        public float ReuseTolerance = 0.001f;

        private bool isNewDraw;
        private Vector3 lastDrawPoint;
        private LineRenderer lineRenderer;
        private Transform root;
        private Coroutine drawRoutine;
        private float lastLineWidth;
        private int renderLifeTime;

        public void Awake()
        {
            if (OnStartDroaw && drawRoutine == null)
            {
                drawRoutine = StartCoroutine(WriteRoutine());
            }
        }

        public void OnGrab()
        {
            if (drawRoutine == null)
            {
                drawRoutine = StartCoroutine(WriteRoutine());
            }
        }

        public void OnRelease()
        {
            if (drawRoutine == null)
            {
                return;
            }

            StopCoroutine(drawRoutine);
            drawRoutine = null;
        }

        private IEnumerator WriteRoutine()
        {
            while (true)
            {
                if (Physics.Raycast(
                        RaycastStart.position,
                        RaycastStart.up,
                        out RaycastHit hit,
                        RaycastLength,
                        DrawingLayers,
                        QueryTriggerInteraction.Ignore))
                {
                    root = hit.transform;
                    float tipDistance = Vector3.Distance(hit.point, RaycastStart.position);
                    float tipPercentage = tipDistance / RaycastLength;
                    Vector3 drawStart = hit.point + (-RaycastStart.up * 0.0005f);
                    Quaternion drawRotation = Quaternion.FromToRotation(Vector3.back, hit.normal);
                    float lineWidth = LineWidth * (1f - tipPercentage);

                    InitDraw(drawStart, drawRotation, lineWidth, DrawColor);
                }
                else
                {
                    isNewDraw = true;
                }

                yield return new WaitForFixedUpdate();
            }
        }

        private void InitDraw(
            Vector3 position,
            Quaternion rotation,
            float lineWidth,
            Color lineColor)
        {
            if (isNewDraw)
            {
                lastDrawPoint = position;
                DrawPoint(lastDrawPoint, position, lineWidth, lineColor, rotation);
                isNewDraw = false;
                return;
            }

            float distance = Vector3.Distance(lastDrawPoint, position);
            if (distance > MinDrawDistance)
            {
                lastDrawPoint = DrawPoint(
                    lastDrawPoint,
                    position,
                    lineWidth,
                    DrawColor,
                    rotation);
            }
        }

        private Vector3 DrawPoint(
            Vector3 lastPoint,
            Vector3 position,
            float lineWidth,
            Color lineColor,
            Quaternion rotation)
        {
            float widthDifference = Mathf.Abs(lastLineWidth - lineWidth);
            lastLineWidth = lineWidth;

            if (widthDifference > ReuseTolerance || renderLifeTime >= 98)
            {
                lineRenderer = null;
                renderLifeTime = 0;
            }
            else
            {
                renderLifeTime++;
            }

            if (isNewDraw || lineRenderer == null)
            {
                Transform lineTransform = new GameObject("DrawLine").transform;

                if (root == null)
                {
                    root = new GameObject("MarkerLineHolder").transform;
                }

                lineTransform.parent = root;
                lineTransform.position = lastPoint;
                lineTransform.rotation = rotation;

                lineRenderer = lineTransform.gameObject.AddComponent<LineRenderer>();
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;

                AnimationCurve widthCurve = new AnimationCurve();
                widthCurve.AddKey(0f, lineWidth);
                lineRenderer.widthCurve = widthCurve;

                if (DrawMaterial)
                {
                    lineRenderer.material = DrawMaterial;
                }

                lineRenderer.numCapVertices = 5;
                lineRenderer.alignment = LineAlignment.View;
                lineRenderer.useWorldSpace = false;
                lineRenderer.SetPosition(
                    0,
                    lineRenderer.transform.InverseTransformPoint(lastPoint));
                lineRenderer.SetPosition(
                    1,
                    lineRenderer.transform.InverseTransformPoint(position));
            }
            else
            {
                lineRenderer.widthMultiplier = 1f;
                lineRenderer.positionCount++;

                AnimationCurve widthCurve = lineRenderer.widthCurve;
                widthCurve.AddKey((lineRenderer.positionCount - 1) / 100, lineWidth);
                lineRenderer.widthCurve = widthCurve;
                lineRenderer.SetPosition(
                    lineRenderer.positionCount - 1,
                    lineRenderer.transform.InverseTransformPoint(position));
            }

            return position;
        }
    }
}
