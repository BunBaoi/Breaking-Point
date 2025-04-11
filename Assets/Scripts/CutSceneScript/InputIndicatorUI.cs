using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputIndicatorUI : MonoBehaviour
{
    [Header("Input References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string leftGrabActionName = "LeftGrab";
    [SerializeField] private string rightGrabActionName = "RightGrab";
    private InputAction leftGrab;
    private InputAction rightGrab;

    [Header("UI References")]
    [SerializeField] private Image leftHandIndicator;  // UI Image to display when left click is held
    [SerializeField] private Image rightHandIndicator; // UI Image to display when right click is held

    [Header("UI Settings")]
    [SerializeField] private float fadeInSpeed = 5f;   // How quickly the indicator appears
    [SerializeField] private float fadeOutSpeed = 3f;  // How quickly the indicator fades
    [SerializeField] private Color activeColor = Color.white; // Color when fully active
    [SerializeField] private Vector2 indicatorSize = new Vector2(64, 64); // Size of the indicators

    // Internal state
    private bool leftActive = false;
    private bool rightActive = false;

    private void Awake()
    {
        // Try to load input actions if not set
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");
            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }

        // Initialize UI elements if not set
        if (leftHandIndicator == null || rightHandIndicator == null)
        {
            Debug.LogWarning("Input indicators not assigned in inspector. Creating dynamic indicators.");
            CreateDynamicIndicators();
        }

        // Set initial states
        SetupIndicators();
        SetupInputActions();
    }

    private void CreateDynamicIndicators()
    {
        // Create a canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InputIndicatorCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create left indicator if needed
        if (leftHandIndicator == null)
        {
            GameObject leftObj = new GameObject("LeftHandIndicator");
            leftObj.transform.SetParent(canvas.transform, false);
            leftHandIndicator = leftObj.AddComponent<Image>();

            // Position on left side of screen
            RectTransform rectTransform = leftHandIndicator.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.5f);
            rectTransform.anchorMax = new Vector2(0, 0.5f);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.anchoredPosition = new Vector2(20, 0);
            rectTransform.sizeDelta = indicatorSize;
        }

        // Create right indicator if needed
        if (rightHandIndicator == null)
        {
            GameObject rightObj = new GameObject("RightHandIndicator");
            rightObj.transform.SetParent(canvas.transform, false);
            rightHandIndicator = rightObj.AddComponent<Image>();

            // Position on right side of screen
            RectTransform rectTransform = rightHandIndicator.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0.5f);
            rectTransform.anchorMax = new Vector2(1, 0.5f);
            rectTransform.pivot = new Vector2(1, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-20, 0);
            rectTransform.sizeDelta = indicatorSize;
        }
    }

    private void SetupIndicators()
    {
        // Set initial colors with zero alpha
        if (leftHandIndicator != null)
        {
            Color c = activeColor;
            c.a = 0;
            leftHandIndicator.color = c;
        }

        if (rightHandIndicator != null)
        {
            Color c = activeColor;
            c.a = 0;
            rightHandIndicator.color = c;
        }
    }

    void SetupInputActions()
    {
        // Setup input actions
        leftGrab = inputActions.FindAction(leftGrabActionName);
        rightGrab = inputActions.FindAction(rightGrabActionName);

        if (leftGrab != null) leftGrab.Enable();
        if (rightGrab != null) rightGrab.Enable();

        // Set up event handlers for input
        if (leftGrab != null)
        {
            leftGrab.performed += _ => leftActive = true;
            leftGrab.canceled += _ => leftActive = false;
        }

        if (rightGrab != null)
        {
            rightGrab.performed += _ => rightActive = true;
            rightGrab.canceled += _ => rightActive = false;
        }
    }

    private void Update()
    {
        UpdateIndicator(leftHandIndicator, leftActive);
        UpdateIndicator(rightHandIndicator, rightActive);
    }

    private void UpdateIndicator(Image indicator, bool active)
    {
        if (indicator == null) return;

        Color color = indicator.color;

        if (active)
        {
            // Fade in
            color.a = Mathf.Min(1f, color.a + Time.deltaTime * fadeInSpeed);
        }
        else
        {
            // Fade out
            color.a = Mathf.Max(0f, color.a - Time.deltaTime * fadeOutSpeed);
        }

        indicator.color = color;
    }

    private void OnEnable()
    {
        SetupInputActions();
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (leftGrab != null)
        {
            leftGrab.Disable();
            leftGrab.performed -= _ => leftActive = true;
            leftGrab.canceled -= _ => leftActive = false;
        }

        if (rightGrab != null)
        {
            rightGrab.Disable();
            rightGrab.performed -= _ => rightActive = true;
            rightGrab.canceled -= _ => rightActive = false;
        }
    }

    // Optional: Method to load custom sprites for the indicators
    public void SetIndicatorSprites(Sprite leftSprite, Sprite rightSprite)
    {
        if (leftHandIndicator != null && leftSprite != null)
        {
            leftHandIndicator.sprite = leftSprite;
        }

        if (rightHandIndicator != null && rightSprite != null)
        {
            rightHandIndicator.sprite = rightSprite;
        }
    }
}