using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class GamepadCursor : MonoBehaviour
{
    public Image virtualMouseUIImage;

    private VirtualMouseInput virtualMouseInput;

    public InputActionReference stickAction;

    private void Awake()
    {
        virtualMouseInput = GetComponent<VirtualMouseInput>();
    }

    private void LateUpdate()
    {
        if (!Cursor.visible || Cursor.lockState == CursorLockMode.Locked)
        {
            // Do not update virtual mouse while cursor is locked/hidden
            return;
        }

        Vector2 virtualMousePosition = virtualMouseInput.virtualMouse.position.value;
        virtualMousePosition.x = Mathf.Clamp(virtualMousePosition.x, 0f, Screen.width);
        virtualMousePosition.y = Mathf.Clamp(virtualMousePosition.y, 0f, Screen.height);
        InputState.Change(virtualMouseInput.virtualMouse.position, virtualMousePosition);
    }

    private void Update()
    {
        if (KeyBindingManager.Instance != null)
        {
            bool isUsingController = KeyBindingManager.Instance.IsUsingController();
            bool isCursorUnlocked = Cursor.lockState != CursorLockMode.Locked;

            // Decide if we want to show the virtual cursor
            bool showCursor = isUsingController && isCursorUnlocked;

            // Show/hide virtual cursor image
            virtualMouseUIImage.gameObject.SetActive(showCursor);
            Debug.Log($"IsUsingController: {isUsingController}, Cursor.visible: {Cursor.visible}, Cursor.lockState: {Cursor.lockState}");
            Debug.Log($"Virtual mouse image {(showCursor ? "shown" : "hidden")}.");

            if (virtualMouseInput != null && virtualMouseInput.stickAction != null)
            {
                if (showCursor && !virtualMouseInput.stickAction.action.enabled)
                {
                    virtualMouseInput.stickAction.action.Enable();
                    Debug.Log("VirtualMouseInput moveAction enabled.");
                }
                else if (!showCursor && virtualMouseInput.stickAction.action.enabled)
                {
                    virtualMouseInput.stickAction.action.Disable();
                    Debug.Log("VirtualMouseInput moveAction disabled.");
                }
            }
        }
    }
}
