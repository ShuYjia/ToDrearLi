using System.Collections;
using TMPro;
using UnityEngine;

public interface ITexCore_SystemKeyboardBridge
{
    void Bind(TMP_InputField inputField);
    void Unbind(TMP_InputField inputField);
}

public class TexCore_InputFieldBridge : MonoBehaviour
{
    private static ITexCore_SystemKeyboardBridge keyboardBridge;

    [SerializeField]
    private TMP_InputField inputField;

    private bool subscribed;

    public static void SetKeyboardBridge(ITexCore_SystemKeyboardBridge bridge)
    {
        keyboardBridge = bridge;
    }

    private void Awake()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
        if (keyboardBridge != null && inputField != null)
        {
            keyboardBridge.Bind(inputField);
        }
    }

    private void Start()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        if (keyboardBridge != null && inputField != null)
        {
            keyboardBridge.Unbind(inputField);
        }
    }

    private void Subscribe()
    {
        if (subscribed || inputField == null)
        {
            return;
        }

        inputField.onSelect.AddListener(HandleSelected);
        subscribed = true;
    }

    private void HandleSelected(string _)
    {
        if (keyboardBridge != null && inputField != null)
        {
            keyboardBridge.Bind(inputField);
        }
    }
}

public class TexCore_InputFieldBridgeInstaller : MonoBehaviour
{
    private IEnumerator Start()
    {
        while (true)
        {
            InstallOnAllInputFields();
            yield return new WaitForSeconds(1f);
        }
    }

    private static void InstallOnAllInputFields()
    {
        TMP_InputField[] inputFields = Resources.FindObjectsOfTypeAll<TMP_InputField>();
        for (int i = 0; i < inputFields.Length; i++)
        {
            TMP_InputField inputField = inputFields[i];
            if (inputField == null || inputField.gameObject.scene.rootCount == 0)
            {
                continue;
            }

            if (inputField.GetComponent<TexCore_InputFieldBridge>() == null)
            {
                inputField.gameObject.AddComponent<TexCore_InputFieldBridge>();
            }
        }
    }
}

public class TexCore_AndroidTouchScreenKeyboardBridge : MonoBehaviour, ITexCore_SystemKeyboardBridge
{
    private TMP_InputField activeInputField;
    private TouchScreenKeyboard keyboard;
    private string originalText;

    private void Awake()
    {
        TexCore_InputFieldBridge.SetKeyboardBridge(this);
    }

    private void Update()
    {
        if (keyboard == null || activeInputField == null)
        {
            return;
        }

        activeInputField.text = keyboard.text;

        switch (keyboard.status)
        {
            case TouchScreenKeyboard.Status.Canceled:
                activeInputField.DeactivateInputField();
                ClearKeyboard();
                break;
            case TouchScreenKeyboard.Status.Done:
                activeInputField.DeactivateInputField();
                ClearKeyboard();
                break;
        }
    }

    public void Bind(TMP_InputField inputField)
    {
        if (inputField == null || Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        if (activeInputField == inputField && keyboard != null)
        {
            return;
        }

        ClearKeyboard();
        activeInputField = inputField;
        originalText = inputField.text;
        keyboard = TouchScreenKeyboard.Open(
            inputField.text,
            GetKeyboardType(inputField),
            false,
            inputField.lineType != TMP_InputField.LineType.SingleLine,
            inputField.contentType == TMP_InputField.ContentType.Password,
            false);
    }

    public void Unbind(TMP_InputField inputField)
    {
        if (activeInputField == inputField)
        {
            ClearKeyboard();
        }
    }

    private void ClearKeyboard()
    {
        keyboard = null;
        activeInputField = null;
        originalText = null;
    }

    private static TouchScreenKeyboardType GetKeyboardType(TMP_InputField inputField)
    {
        switch (inputField.contentType)
        {
            case TMP_InputField.ContentType.IntegerNumber:
            case TMP_InputField.ContentType.DecimalNumber:
                return TouchScreenKeyboardType.NumberPad;
            case TMP_InputField.ContentType.EmailAddress:
                return TouchScreenKeyboardType.EmailAddress;
            case TMP_InputField.ContentType.Password:
            case TMP_InputField.ContentType.Pin:
                return TouchScreenKeyboardType.Default;
            default:
                return TouchScreenKeyboardType.Default;
        }
    }
}

public class TexCore_RuntimePlatformBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (Application.platform == RuntimePlatform.Android &&
            GetComponent<TexCore_AndroidTouchScreenKeyboardBridge>() == null)
        {
            gameObject.AddComponent<TexCore_AndroidTouchScreenKeyboardBridge>();
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            TexCore_Const.partfrom = Partfrom.VR;
        }
    }
}
