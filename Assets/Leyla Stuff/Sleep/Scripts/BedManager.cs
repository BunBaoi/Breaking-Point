using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class BedManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform bed;
    [SerializeField] private string bedTag = "Bed";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private CanvasGroup fadeCanvas; // CanvasGroup for fading effect
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float zPositionOffset = 1f;
    [SerializeField] private float xPositionOffset = 1f;
    [SerializeField] private float yPositionOffset = 0.1f;
    [SerializeField] private string boolKey = "SleptInBed";
    [SerializeField] private CinematicSequence cinematicSequence;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private DayNightCycle dayNightCycle;
    [SerializeField] private PlayerStats playerStats;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private GameObject interactTextPrefab;
    private GameObject interactTextInstance;
    private GameObject iconObject;
    private InputAction interactAction;
    private SpriteRenderer spriteRenderer;

    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>();
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>();
    [SerializeField] private bool hasSetTime = false;
    [SerializeField] private bool isInteracting = false;
    [SerializeField] private bool inTrigger = false;

    [Header("Inventory Setups")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;

    private Camera playerCamera;

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

    private void Start()
    {
        playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera")?.GetComponent<Camera>();

        if (playerCamera == null)
        {
            Debug.LogError("PlayerCamera not found! Make sure the camera is tagged correctly.");
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag(playerTag)?.transform;
        }

        // Find the action dynamically using the interactActionName string
        interactAction = inputActions.FindAction(interactActionName);

        if (interactAction != null)
        {
            interactAction.Enable(); // Enable the action
        }
        else
        {
            Debug.LogError($"Input action '{interactActionName}' not found in Input Action Asset!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isInteracting)
        {
            inTrigger = true;
            player = GameObject.FindGameObjectWithTag(playerTag)?.transform;

        }
    }

    void ShowInteractText()
    {
        if (interactTextInstance == null && interactTextPrefab != null)
        {
            interactTextInstance = Instantiate(interactTextPrefab);

            interactTextInstance.transform.SetParent(transform, false);

            Transform objectColliderTransform = transform.Find("Bed Mesh");

            if (objectColliderTransform != null)
            {
                Collider objectCollider = objectColliderTransform.GetComponent<Collider>();

                if (objectCollider != null)
                {
                    Vector3 objectTopWorldPos = objectCollider.bounds.max;

                    // Convert the world position to local position relative to the parent
                    Vector3 pickUpTopLocalPos = interactTextInstance.transform.InverseTransformPoint(objectTopWorldPos);

                    // Position the interact text just above the top of the "Pick Up Collider"
                    interactTextInstance.transform.localPosition = new Vector3(0, pickUpTopLocalPos.y + 0.2f, 0); // Adjust the Y offset as needed
                }
                else
                {
                    // If no collider is attached to "Pick Up Collider", fallback position
                    interactTextInstance.transform.localPosition = new Vector3(0, 0.2f, 0);
                }
            }
            else
            {
                // If no "Pick Up Collider" child is found, fallback position
                interactTextInstance.transform.localPosition = new Vector3(0, 0.2f, 0);
            }

            // Declare the interactText variable
            string interactText = "Sleep"; // Default text

            // Get the keybinding data for "Interact"
            KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(interactActionName);

            // Update text dynamically to match the correct keybinding based on input device
            TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = "Sleep";

                // Now check if we have a keybinding sprite
                if (keyBinding != null)
                {
                    Sprite icon = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

                    // If the sprite exists, display it next to the text
                    if (icon != null)
                    {
                        // Create a object for the sprite and set it next to the text
                        iconObject = new GameObject("KeybindIcon");
                        iconObject.transform.SetParent(interactTextInstance.transform); // Make it a child of the text

                        // Position sprite to left of text
                        // Increase the horizontal space by adjusting the x-position further
                        float horizontalOffset = -textMesh.preferredWidth / 2 - 0.04f; // Increased offset to add more space
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
                        // Get the first binding for keyboard and second for controller directly from the InputActionAsset
                        string keyText = "";

                        // Get the "Interact" action
                        var interactAction = inputActions.FindAction(interactActionName);

                        if (interactAction != null)
                        {
                            // If using a controller, get the second binding (controller binding)
                            if (KeyBindingManager.Instance.IsUsingController())
                            {
                                keyText = interactAction.bindings[1].ToDisplayString();  // Second binding (controller)
                            }
                            else
                            {
                                keyText = interactAction.bindings[0].ToDisplayString();  // First binding (keyboard)
                            }

                            // Remove the word "Press" from the keyText if it exists
                            keyText = keyText.Replace("Press ", "").Trim(); // Removes "Press" and any extra spaces

                            // Set the fallback text to show the keybinding for "Interact"
                            interactText = $"[{keyText}] Sleep";
                        }
                        else
                        {
                            Debug.LogError("Interact action not found in InputActionAsset");
                        }
                    }

                    // Set the updated text (with sprite or keybinding fallback)
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

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && inTrigger)
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
            inTrigger = false;
            // player = null;

            if (interactTextInstance != null)
            {
                HideInteractText();
            }
        }
    }

    private void Update()
    {
        if (inTrigger && interactTextInstance != null && player != null)
        {
            GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");

            if (playerCamera != null)
            {
                Vector3 lookDirection = player.position - interactTextInstance.transform.position;
                lookDirection.y = 0; // Keep the text rotation horizontal (no vertical tilt)

                interactTextInstance.transform.forward = -lookDirection.normalized;

                Vector3 currentEulerAngles = interactTextInstance.transform.eulerAngles;

                // Set the Y rotation of the interaction text based on the camera's X rotation
                currentEulerAngles.x = playerCamera.transform.eulerAngles.x;
                interactTextInstance.transform.eulerAngles = currentEulerAngles;
            }
        }

        RaycastHit hit;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        Debug.DrawRay(ray.origin, ray.direction * 3f, Color.red, 1f); // Visualize the ray

        // Only perform the raycast once
        if (Physics.Raycast(ray, out hit, 3f))
        {
            // Check for the Bed Mesh and inTrigger condition
            if (hit.collider.CompareTag(bedTag) && inTrigger)
            {
                ShowInteractText();
            }
            else
            {
                HideInteractText();
            }

            // Check if the interact action was triggered
            if (interactAction.triggered && inTrigger)
            {
                if (hit.collider.CompareTag(bedTag) && !isInteracting)
                {
                    Debug.Log("Bed detected! Starting interaction.");
                    bed = hit.collider.transform;
                    HideInteractText();
                    StartCoroutine(HandleBedInteraction());
                }
                else
                {
                    Debug.Log("Raycast hit, but not a bed.");
                }
            }
        }
        else
        {
            Debug.Log("Raycast hit nothing, hiding text");
            HideInteractText();
        }

        if (spriteRenderer != null)
        {
            // Dynamically update sprite scale if keybinding changes during the game
            UpdateSpriteScale();
        }
    }

        private IEnumerator HandleBedInteraction()
    {
        isInteracting = true;
        
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

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
            GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
            cameraController = playerCamera.GetComponent<CameraController>();

            if (cameraController != null)
            {
                cameraController.SetLookState(false);
            }

            playerMovement = playerObject.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.SetMovementState(false);
            }

            playerStats = playerObject.GetComponent<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.FadeOut();
            }
        }

        // Get the collider of the bed to determine the height for positioning
        Collider bedCollider = bed.GetComponent<Collider>();
        if (bedCollider == null)
        {
            Debug.LogError("No collider found on the bed object!");
            yield break;
        }

        // Teleport player to the center of the bed's collider but slightly above it (use the collider's center)
        Vector3 colliderCenter = bedCollider.bounds.center; // Center of the collider
        Vector3 targetPosition = new Vector3(colliderCenter.x + xPositionOffset, colliderCenter.y + bedCollider.bounds.extents.y + yPositionOffset, colliderCenter.z + zPositionOffset); // Adjusted height above the bed

        // Disable the CharacterController while teleporting the player
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false; // Disable to avoid collision or unwanted behavior
        }

        // Set the new position for the player
        player.position = targetPosition;

        // Re-enable the CharacterController
        /*if (characterController != null)
        {
            characterController.enabled = true;
        }*/

        // Align player's Y rotation to match the bed's Y rotation (keeping the player's original X and Z rotations intact)
        player.rotation = Quaternion.Euler(player.eulerAngles.x, bed.rotation.eulerAngles.y, player.eulerAngles.z); // Align Y rotation with the bed's rotation

        // Adjust player's rotation to make them lie flat on their back (90 degrees around the X-axis)
        Quaternion targetRotation = Quaternion.Euler(-90, player.rotation.eulerAngles.y, player.rotation.eulerAngles.z);
        while (Quaternion.Angle(player.rotation, targetRotation) > 0.1f)
        {
            player.rotation = Quaternion.Slerp(player.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        // Start blink effect
        yield return StartCoroutine(BlinkScreen());

        if (CanStartCinematic())
        {
            // Set bool in BoolManager
            BoolManager.Instance.SetBool(boolKey, true);
        }

        yield return new WaitForSeconds(0.5f); // Small delay before checking bool

        if (BoolManager.Instance.GetBool(boolKey))
        {
            // Start cinematic sequence if the bool is true
            cinematicSequence.StartCinematic();
            cinematicSequence.OnCinematicFinished += RotatePlayerUpright;
            cinematicSequence.OnCinematicStarted += FadeOutBlack;
        }
        else
        {
            yield return StartCoroutine(FadeScreen(false));

            Quaternion uprightRotation = Quaternion.Euler(0, player.eulerAngles.y, 0);
            while (Quaternion.Angle(player.rotation, uprightRotation) > 0.1f)
            {
                if (inventoryManager != null)
                {
                    inventoryManager.enabled = false;
                    inventoryCanvas.gameObject.SetActive(false);
                }
                cameraController.SetLookState(false);
                playerMovement.SetMovementState(false);
                player.rotation = Quaternion.Slerp(player.rotation, uprightRotation, rotationSpeed * Time.deltaTime);
                yield return null;
            }

            if (playerStats != null)
            {
                playerStats.ReplenishEnergy(100f);
                playerStats.FadeIn();
            }
            dayNightCycle.StartTime();
            cameraController.SetLookState(true);
            playerMovement.SetMovementState(true);
            if (characterController != null)
            {
                characterController.enabled = true; // Disable to avoid collision or unwanted behavior
            }
            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
                inventoryCanvas.gameObject.SetActive(true);
            }
        }

        hasSetTime = false;
        isInteracting = false;
    }

    private bool CanStartCinematic()
    {
        // Check if all required bool conditions are met (true or false based on lists)
        foreach (string boolKey in requiredBoolKeysTrue)
        {
            if (!BoolManager.Instance.GetBool(boolKey))
            {
                return false; // If any bool is false when it should be true, return false
            }
        }

        foreach (string boolKey in requiredBoolKeysFalse)
        {
            if (BoolManager.Instance.GetBool(boolKey))
            {
                return false; // If any bool is true when it should be false, return false
            }
        }

        return true; // All conditions are met, return true
    }

    private void FadeOutBlack()
    {
        StartCoroutine(FadeOutBlackCoroutine());
    }

    private IEnumerator FadeOutBlackCoroutine()
    {
        yield return StartCoroutine(FadeScreen(false));
        cinematicSequence.OnCinematicStarted -= FadeOutBlack;
    }

    private void RotatePlayerUpright()
    {
        StartCoroutine(RotatePlayerUprightCoroutine());
    }

    private IEnumerator RotatePlayerUprightCoroutine()
    {
        Quaternion uprightRotation = Quaternion.Euler(0, player.eulerAngles.y, 0);
        while (Quaternion.Angle(player.rotation, uprightRotation) > 0.1f)
        {
            cameraController.SetLookState(false);
            playerMovement.SetMovementState(false);
            player.rotation = Quaternion.Slerp(player.rotation, uprightRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }

        // Unsubscribe from the event to avoid duplicate calls
        cinematicSequence.OnCinematicFinished -= RotatePlayerUpright;

        if (playerStats != null)
        {
            playerStats.ReplenishEnergy(100f);
            playerStats.FadeIn();
        }
        if (inventoryManager != null)
        {
            inventoryManager.enabled = true;
            inventoryCanvas.gameObject.SetActive(true);
        }
        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true; // Disable to avoid collision or unwanted behavior
        }
        cameraController.SetLookState(true);
        playerMovement.SetMovementState(true);
        dayNightCycle.StartTime();
        hasSetTime = false;

        isInteracting = false; // Allow new interactions
    }

    private IEnumerator BlinkScreen()
    {
        yield return StartCoroutine(FadeScreen(true));
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeScreen(false));
        yield return new WaitForSeconds(0.2f);

        // Final fade to black
        yield return StartCoroutine(FadeScreen(true));

        // Stay on black screen for a longer time
        yield return new WaitForSeconds(1f);

        if (!hasSetTime)
        {
            dayNightCycle.SetTime(6, 00, true); // Set time
            dayNightCycle.StopTime();
            hasSetTime = true;
        }
    }

    private IEnumerator FadeScreen(bool fadeToBlack)
    {
        float targetAlpha = fadeToBlack ? 1f : 0f;
        float startAlpha = fadeCanvas.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
            yield return null;
        }

        fadeCanvas.alpha = targetAlpha;
    }
}