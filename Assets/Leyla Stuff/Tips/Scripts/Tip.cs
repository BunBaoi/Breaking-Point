using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System;

[CreateAssetMenu(fileName = "NewTip", menuName = "Game Tips/Tip")]
public class Tip : ScriptableObject
{
    public string tipID;
    [TextArea(3, 5)]
    public string tipText;  // The text that will be displayed
    public VideoClip tipVideo;  // The video associated with the tip
    [SerializeField] private InputActionAsset inputActions;

    [Tooltip("Array of actions for which the sprites will be displayed.")]
    public string[] selectedActions;

    [Tooltip("Array of colours for the keybinds.")]
    public Color[] actionColors;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(tipID))
        {
            tipID = Guid.NewGuid().ToString(); // Generate a unique ID
        }
    }

    public void UpdateSprite(GameObject iconObject, string actionName)
    {
        if (KeyBindingManager.Instance == null || iconObject == null || inputActions == null) return;

        InputAction action = inputActions.FindAction(actionName);
        if (action == null) return;

        int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;
        if (action.bindings.Count <= bindingIndex) return;

        InputBinding binding = action.bindings[bindingIndex];
        string boundKeyOrButton = KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);

        if (string.IsNullOrEmpty(boundKeyOrButton))
        {
            Debug.LogWarning($"No key binding found for action: {actionName}");
            return;
        }

        // Check if we are using a controller or keyboard
        bool isUsingController = KeyBindingManager.Instance.IsUsingController();
        KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(actionName);
        if (keyBinding == null) return;

        // Get or add an Image component
        Image imageComponent = iconObject.GetComponent<Image>();
        if (imageComponent == null)
        {
            imageComponent = iconObject.AddComponent<Image>();
        }

        // Set the sprite based on whether using a controller or keyboard
        imageComponent.sprite = isUsingController ? keyBinding.controllerSprite : keyBinding.keySprite;

        Animator animator = iconObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = iconObject.AddComponent<Animator>();
        }

        animator.enabled = true;

        // Load the appropriate animator for the key/button
        string folderPath = isUsingController ? "UI/Controller/" : "UI/Keyboard/";
        string animatorName = KeyBindingManager.Instance.GetSanitisedKeyName(boundKeyOrButton);
        RuntimeAnimatorController assignedAnimator = Resources.Load<RuntimeAnimatorController>(folderPath + animatorName);

        if (assignedAnimator != null)
        {
            animator.runtimeAnimatorController = assignedAnimator;
            Debug.Log($"Assigned animator '{animatorName}' to {iconObject.name}");
        }
        else
        {
            Debug.LogError($"Animator '{animatorName}' not found in {folderPath}");
        }
    }
}