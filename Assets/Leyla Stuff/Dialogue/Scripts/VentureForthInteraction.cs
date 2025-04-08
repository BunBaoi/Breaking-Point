using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VentureForthInteraction : MonoBehaviour
{
    [SerializeField] private GameObject interactTextPrefab;
    private GameObject interactTextInstance;
    private bool playerInRange = false;
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private InputActionAsset inputActions;
    private SpriteRenderer spriteRenderer; 
    private GameObject iconObject;
    [SerializeField] private GameObject ventureForthPanel;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;
    [SerializeField] private Button ventureForthButton;

    [SerializeField] private string sceneName;
    [SerializeField] private string teleportLocationTag = "";

    private InputAction interactAction;

    void Awake()
    {
        // If inputActions is not assigned via the inspector, load it from the Resources/Keybinds folder
        if (inputActions == null)
        {
            // Load from the "Keybinds" folder in Resources
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");

            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }
    }

    void Start()
    {
        interactAction = inputActions.FindAction(interactActionName);

        if (interactAction != null)
        {
            interactAction.Enable(); // Enable the action
        }
        else
        {
            Debug.LogError($"Input action '{interactActionName}' not found in Input Action Asset!");
        }

        if (ventureForthButton != null)
        {
            ventureForthButton.onClick.AddListener(OnVentureForthButtonClicked);
        }
    }

    void OnVentureForthButtonClicked()
    {
        PlayerManager.Instance.TeleportToScene(sceneName, teleportLocationTag);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            ShowInteractText();

            if (interactTextInstance != null)
            {
                UpdateSprite(iconObject.gameObject, interactActionName);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && playerInRange)
        {
            if (interactTextInstance != null)
            {
                UpdateSprite(iconObject.gameObject, interactActionName);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            HideInteractText();
        }
    }

    void ShowInteractText()
    {

        if (interactTextPrefab != null && interactTextInstance == null)
        {
            interactTextInstance = Instantiate(interactTextPrefab);

            interactTextInstance.transform.SetParent(transform, false);

            Transform objectColliderTransform = transform.Find("Venture Forth");

            if (objectColliderTransform != null)
            {
                Collider objectCollider = objectColliderTransform.GetComponent<Collider>();

                if (objectCollider != null)
                {
                    Vector3 objectTopWorldPos = objectCollider.bounds.max;

                    // Convert the world position to local position relative to the parent
                    Vector3 pickUpTopLocalPos = interactTextInstance.transform.InverseTransformPoint(objectTopWorldPos);

                    interactTextInstance.transform.localPosition = new Vector3(0, pickUpTopLocalPos.y + -5f, 0);
                }
                else
                {
                    // If no collider is attached to "Pick Up Collider", fallback position
                    interactTextInstance.transform.localPosition = new Vector3(0, 0.2f, 0);
                }
            }
            else
            {
                interactTextInstance.transform.localPosition = new Vector3(0, 0.2f, 0);
            }

            string interactText = "Venture Forth"; // Default text

            KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(interactActionName);

            // Update text dynamically to match the correct keybinding based on input device
            TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = "Venture Forth";

                // Now check if we have a keybinding sprite
                if (keyBinding != null)
                {
                    Sprite icon = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

                    // If the sprite exists, display it next to the text
                    if (icon != null)
                    {
                        iconObject = new GameObject("KeybindIcon");
                        iconObject.transform.SetParent(interactTextInstance.transform);

                        float horizontalOffset = -textMesh.preferredWidth / 2 - 0.04f;
                        iconObject.transform.localPosition = new Vector3(horizontalOffset, 0f, 0);

                        // Add a SpriteRenderer to display the icon
                        spriteRenderer = iconObject.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = icon;
                        spriteRenderer.sortingOrder = 1;

                        UpdateSpriteScale();

                        UpdateSprite(iconObject.gameObject, interactActionName);
                    }
                    else
                    {
                        string keyText = "";

                        var interactAction = inputActions.FindAction(interactActionName);

                        if (interactAction != null)
                        {
                            // If using a controller, get the second binding (controller binding)
                            if (KeyBindingManager.Instance.IsUsingController())
                            {
                                keyText = interactAction.bindings[1].ToDisplayString();
                            }
                            else
                            {
                                keyText = interactAction.bindings[0].ToDisplayString();
                            }

                            keyText = keyText.Replace("Press ", "").Trim();

                            interactText = $"[{keyText}] Venture Forth";
                        }
                        else
                        {
                            Debug.LogError("Interact action not found in InputActionAsset");
                        }
                    }

                    textMesh.text = interactText;
                }
            }
        }
    }

    void HideInteractText()
    {
        if (interactTextInstance != null)
        {
            Destroy(interactTextInstance);
            interactTextInstance = null;
        }
    }

    void UpdateSpriteScale()
    {
        if (spriteRenderer == null) return;

        // Get the keybinding and check the device being used (controller or keyboard/mouse)
        InputAction action = inputActions.FindAction(interactActionName);
        if (action == null) return;

        int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;
        if (action.bindings.Count <= bindingIndex) return;

        InputBinding binding = action.bindings[bindingIndex];
        string boundKeyOrButton = KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);
        if (string.IsNullOrEmpty(boundKeyOrButton))
        {
            Debug.LogWarning($"No key binding found for action: {interactActionName}");
            return;
        }

        Debug.Log($"Bound Key or Button for action '{interactActionName}': {boundKeyOrButton}");

        // Check if it's a mouse button
        bool isMouseButton = boundKeyOrButton.Contains("Mouse") || boundKeyOrButton.Contains("Click") || boundKeyOrButton.Contains("Scroll")
            || boundKeyOrButton.Contains("leftStick") || boundKeyOrButton.Contains("rightStick");

        // Set the scale based on whether it's a mouse button or not
        float scale = isMouseButton ? 0.2f : 0.08f;
        spriteRenderer.transform.localScale = new Vector3(scale, scale, scale);
    }

    private void UpdateSprite(GameObject iconObject, string actionName)
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

        bool isUsingController = KeyBindingManager.Instance.IsUsingController();

        KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(actionName);
        if (keyBinding == null) return;

        SpriteRenderer spriteRenderer = iconObject.GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

        Animator animator = iconObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = iconObject.AddComponent<Animator>();
        }

        animator.enabled = true; // Ensure animator is enabled

        string folderPath = isUsingController ? "UI/Controller/" : "UI/Keyboard/";
        string animatorName = KeyBindingManager.Instance.GetSanitisedKeyName(boundKeyOrButton) + ".sprite";
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

    void Update()
    {
        if (playerInRange && interactAction.triggered)
        {
            OpenVentureForthPanel();
        }
    }

    void OpenVentureForthPanel()
    {
        if (ventureForthPanel != null)
        {
            ventureForthPanel.SetActive(true);
        }

        Time.timeScale = 0f;

        // Unlock and show the cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            inventoryManager = playerObject.GetComponent<InventoryManager>();
            if (inventoryManager != null)
            {
                inventoryManager.enabled = false;
                Debug.Log("InventoryManager disabled.");
            }
            Transform inventoryCanvasTransform = playerObject.transform.Find("Inventory Canvas");
            if (inventoryCanvasTransform != null)
            {
                inventoryCanvas = inventoryCanvasTransform.GetComponent<Canvas>();
                if (inventoryCanvas != null)
                {
                    inventoryCanvas.gameObject.SetActive(false);
                    Debug.Log("Inventory Canvas disabled.");
                }
            }
        }
    }

    public void HideVentureForthPanel()
    {
        if (ventureForthPanel != null)
        {
            ventureForthPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            inventoryManager = playerObject.GetComponent<InventoryManager>();
            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
            }
            Transform inventoryCanvasTransform = playerObject.transform.Find("Inventory Canvas");
            if (inventoryCanvasTransform != null)
            {
                inventoryCanvas = inventoryCanvasTransform.GetComponent<Canvas>();
                if (inventoryCanvas != null)
                {
                    inventoryCanvas.gameObject.SetActive(true);
                }
            }
        }
    }
}

