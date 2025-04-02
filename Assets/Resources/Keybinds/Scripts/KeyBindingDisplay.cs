using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class KeyBindingDisplay : MonoBehaviour
{
    [SerializeField] private string actionName;
    [SerializeField] private Image keyImage;
    [SerializeField] private TMP_Text actionText;

    private InputAction action;

    private void Start()
    {
        UpdateKeybindingUI();

        // Subscribe to the 'performed' event of the specific action
        action = KeyBindingManager.Instance.inputActionAsset.FindAction(actionName);
        if (action != null)
        {
            action.performed += OnActionPerformed;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event when destroyed to avoid memory leaks
        if (action != null)
        {
            action.performed -= OnActionPerformed;
        }
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        UpdateKeybindingUI();
    }

    private void UpdateKeybindingUI()
    {
        if (KeyBindingManager.Instance == null) return;

        // Get the binding display name for the action
        var keyBinding = KeyBindingManager.Instance.GetKeybinding(actionName);
        if (keyBinding == null) return;

        bool isController = KeyBindingManager.Instance.IsUsingController();
        string bindingDisplay = KeyBindingManager.Instance.GetBindingDisplayName(actionName);

        // Update the action text and image for the correct input
        actionText.text = $"[{bindingDisplay}] {actionName}";
        keyImage.sprite = isController ? keyBinding.controllerSprite : keyBinding.keySprite;
    }
}
