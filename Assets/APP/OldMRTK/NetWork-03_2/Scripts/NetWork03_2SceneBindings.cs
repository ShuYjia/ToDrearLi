using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NetWork03_2.ExportRuntime
{
    public class NetWork03_2SceneBindings : MonoBehaviour
    {
        [Serializable]
        private struct ExternalBindingRecord
        {
            public string ownerPath;
            public string componentTypeName;
            public string propertyName;
            public string targetPath;
        }

        [Header("Scene Overrides")]
        [SerializeField] private GameObject playerControllerOverride;
        [SerializeField] private GameObject guidingArrowOverride;
        [SerializeField] private GameObject globalSyncManagerOverride;
        [SerializeField] private GameObject multControllerOverride;
        [SerializeField] private GameObject rightHandControllerOverride;
        [SerializeField] private GameObject shixunRootOverride;

        [Header("Lookup")]
        [SerializeField] private string playerControllerName = "PlayerController";
        [SerializeField] private string playerControllerTag = "PlayerController";
        [SerializeField] private string guidingArrowName = "GuidingArrow";
        [SerializeField] private string globalSyncManagerName = "GlobalSyncManager";
        [SerializeField] private string multControllerName = "MultController";
        [SerializeField] private string rightHandControllerName = "MRTK RightHand Controller";
        [SerializeField] private string sourceShixunRootName = "SHIXUN";

        [Header("Support Prefabs")]
        [SerializeField] private GameObject guidingArrowPrefab;

        [Header("Guiding Arrow Spawn")]
        [SerializeField] private Vector3 guidingArrowLocalPosition = new Vector3(0f, 0f, 0.8457f);
        [SerializeField] private Vector3 guidingArrowLocalEuler = Vector3.zero;
        [SerializeField] private Vector3 guidingArrowLocalScale = Vector3.one;

        [Header("External Scene Bindings")]
        [SerializeField] private ExternalBindingRecord[] externalBindings = Array.Empty<ExternalBindingRecord>();

        private void Awake()
        {
            GameObject playerController = ResolvePlayerController();
            Camera mainCamera = Camera.main;
            GameObject guidingArrow = ResolveGuidingArrow(mainCamera);
            Transform mainCameraTransform = mainCamera != null ? mainCamera.transform : null;
            GameObject shixun = ResolveSceneObject(shixunRootOverride, sourceShixunRootName);
            GameObject rightHandController = ResolveSceneObject(rightHandControllerOverride, rightHandControllerName);
            GameObject globalSyncManager = ResolveSceneObject(globalSyncManagerOverride, globalSyncManagerName);
            GameObject multController = ResolveSceneObject(multControllerOverride, multControllerName);

            BindPersonMove(playerController);
            BindStepTasks(guidingArrow);
            BindStepTaskControllers(guidingArrow);
            BindGuidingArrowControls(guidingArrow);
            BindHjInstall(guidingArrow);
            BindLazyFollowTargets(mainCameraTransform);
            BindCameraConsumers(mainCamera);
            BindRightHandController(rightHandController);
            BindMultCheckInfo(shixun);
            ApplyExternalBindings();
            WarnIfMissingSceneSupport(globalSyncManager, multController);
        }

        private void BindPersonMove(GameObject playerController)
        {
            BindFieldOnComponents("PersonMove", "personposition", playerController);
            if (playerController == null)
            {
                Debug.LogWarning("Export module: PlayerController was not found. PersonMove will stay inactive until a scene object with that name exists.", this);
            }
        }

        private GameObject ResolvePlayerController()
        {
            GameObject playerController = ResolveSceneObject(playerControllerOverride, playerControllerName);
            if (playerController != null)
            {
                return playerController;
            }

            if (!string.IsNullOrEmpty(playerControllerTag))
            {
                try
                {
                    playerController = GameObject.FindWithTag(playerControllerTag);
                }
                catch (UnityException)
                {
                    playerController = null;
                }
            }

            return playerController;
        }

        private GameObject ResolveGuidingArrow(Camera mainCamera)
        {
            GameObject guidingArrow = ResolveSceneObject(guidingArrowOverride, guidingArrowName);
            if (guidingArrow == null && guidingArrowPrefab != null)
            {
                if (mainCamera != null)
                {
                    guidingArrow = Instantiate(guidingArrowPrefab, mainCamera.transform);
                    guidingArrow.name = guidingArrowPrefab.name;
                    guidingArrow.transform.localPosition = guidingArrowLocalPosition;
                    guidingArrow.transform.localEulerAngles = guidingArrowLocalEuler;
                    guidingArrow.transform.localScale = guidingArrowLocalScale;
                }
            }

            if (guidingArrow == null)
            {
                Debug.LogWarning("Export module: GuidingArrow was not found and no support prefab could be spawned.", this);
            }

            return guidingArrow;
        }

        private GameObject ResolveSceneObject(GameObject overrideObject, string objectName)
        {
            if (overrideObject != null)
            {
                return overrideObject;
            }

            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            GameObject sceneObject = GameObject.Find(objectName);
            return sceneObject != null ? sceneObject : FindSceneObjectByName(objectName);
        }

        private void BindStepTasks(GameObject guidingArrow)
        {
            BindFieldOnComponents("StepTesk", "GuidingArrow", guidingArrow);
        }

        private void BindStepTaskControllers(GameObject guidingArrow)
        {
            BindFieldOnComponents("StepTeskController", "GuidingArrow", guidingArrow);
        }

        private void BindGuidingArrowControls(GameObject guidingArrow)
        {
            BindFieldOnComponents("GuidingArrowControl", "GuidingArrowing", guidingArrow);
        }

        private void BindHjInstall(GameObject guidingArrow)
        {
            BindFieldOnComponents("HJinstall", "GuidingArrow", guidingArrow);
        }

        private void BindLazyFollowTargets(Transform targetTransform)
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().FullName != "UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow")
                {
                    continue;
                }

                FieldInfo field = component.GetType().GetField("m_Target", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.GetValue(component) == null)
                {
                    field.SetValue(component, targetTransform);
                }
            }
        }

        private void BindCameraConsumers(Camera mainCamera)
        {
            if (mainCamera == null)
            {
                return;
            }

            BindFieldOnComponents("ShowLength", "MainCam", mainCamera);
            BindFieldOnComponents("DrawGridV2", "MainCam", mainCamera);
        }

        private void BindRightHandController(GameObject rightHandController)
        {
            if (rightHandController == null)
            {
                return;
            }

            BindFieldOnComponents("QRPaste", "GestureRightHand", rightHandController);
            BindFieldOnComponents("QRComplete", "GrabInteractable", rightHandController);
        }

        private void BindMultCheckInfo(GameObject shixun)
        {
            GameObject multiplayer = FindSceneObjectByName("DuoRenXieTong");
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != "MultCheckInfo")
                {
                    continue;
                }

                FieldInfo shixunField = component.GetType().GetField("shixun", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (shixunField != null && shixunField.GetValue(component) == null && shixun != null)
                {
                    shixunField.SetValue(component, shixun);
                }

                FieldInfo multiplayerField = component.GetType().GetField("multiplayer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (multiplayerField != null && multiplayerField.GetValue(component) == null && multiplayer != null)
                {
                    multiplayerField.SetValue(component, multiplayer);
                }
            }
        }

        private void ApplyExternalBindings()
        {
            if (externalBindings == null || externalBindings.Length == 0)
            {
                return;
            }

            foreach (ExternalBindingRecord record in externalBindings)
            {
                if (string.IsNullOrEmpty(record.componentTypeName) ||
                    string.IsNullOrEmpty(record.propertyName) ||
                    string.IsNullOrEmpty(record.targetPath))
                {
                    continue;
                }

                Transform owner = string.IsNullOrEmpty(record.ownerPath) ? transform : transform.Find(record.ownerPath);
                Transform target = FindSceneTransformByPath(record.targetPath);
                if (owner == null || target == null)
                {
                    continue;
                }

                foreach (MonoBehaviour component in owner.GetComponents<MonoBehaviour>())
                {
                    if (component == null)
                    {
                        continue;
                    }

                    Type componentType = component.GetType();
                    if (!string.Equals(componentType.FullName, record.componentTypeName, StringComparison.Ordinal) &&
                        !string.Equals(componentType.Name, record.componentTypeName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    FieldInfo field = componentType.GetField(record.propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field == null || field.GetValue(component) != null)
                    {
                        continue;
                    }

                    if (field.FieldType == typeof(GameObject))
                    {
                        field.SetValue(component, target.gameObject);
                    }
                    else if (typeof(Component).IsAssignableFrom(field.FieldType))
                    {
                        Component targetComponent = target.GetComponent(field.FieldType);
                        if (targetComponent != null)
                        {
                            field.SetValue(component, targetComponent);
                        }
                    }
                }
            }
        }

        private void BindFieldOnComponents(string componentTypeName, string fieldName, object value)
        {
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component.GetType().Name != componentTypeName)
                {
                    continue;
                }

                FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null && field.GetValue(component) == null)
                {
                    field.SetValue(component, value);
                }
            }
        }

        private GameObject FindSceneObjectByName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            foreach (Transform sceneTransform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (sceneTransform == null || !sceneTransform.gameObject.scene.IsValid() || sceneTransform.hideFlags != HideFlags.None)
                {
                    continue;
                }

                if (sceneTransform.name == objectName)
                {
                    return sceneTransform.gameObject;
                }
            }

            return null;
        }

        private Transform FindSceneTransformByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string[] parts = path.Split('/');
            GameObject rootObject = FindSceneObjectByName(parts[0]);
            if (rootObject == null)
            {
                return null;
            }

            Transform current = rootObject.transform;
            for (int i = 1; i < parts.Length; i++)
            {
                current = current.Find(parts[i]);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private void WarnIfMissingSceneSupport(GameObject globalSyncManager, GameObject multController)
        {
            if (globalSyncManager == null)
            {
                Debug.LogWarning("Export module: GlobalSyncManager is missing from the active scene. SceneItemSync active/transform sync will be unavailable.", this);
            }

            bool hasGetGoal = false;
            foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component != null && component.GetType().Name == "GetGoal")
                {
                    hasGetGoal = true;
                    break;
                }
            }

            if (hasGetGoal && multController == null)
            {
                Debug.LogWarning("Export module: MultController is missing from the active scene. Multiplayer score/reporting callbacks will be skipped.", this);
            }
        }
    }
}