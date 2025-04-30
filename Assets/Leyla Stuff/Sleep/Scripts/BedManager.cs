using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public class BedManager : MonoBehaviour
{
    [Header("Bed Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform bed;
    [SerializeField] private string bedTag = "Bed";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float zPositionOffset = 1f;
    [SerializeField] private float xPositionOffset = 1f;
    [SerializeField] private float yPositionOffset = 0.1f;

    [Header("UI Settings")]
    [SerializeField] private CanvasGroup fadeCanvas; // CanvasGroup for fading effect
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Interact Text Settings")]
    [SerializeField] private float yAxis = 0.2f;
    [SerializeField] private float defaultYAxis = 0.2f;

    [Header("Cinematic Sequence")]
    [SerializeField] private string cinematicSequenceTag = "";
    [SerializeField] private CinematicSequence cinematicSequence;
    [SerializeField] private CameraController cameraController;

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
    [SerializeField] private string boolKey = "SleptInBed";
    [SerializeField] private bool hasSetTime = false;
    [SerializeField] private bool isInteracting = false;
    [SerializeField] private bool inTrigger = false;

    [Header("Other Scripts")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private DayNightCycle dayNightCycle;

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

                    interactTextInstance.transform.localPosition = new Vector3(0, pickUpTopLocalPos.y + yAxis, 0);
                }
                else
                {
                    interactTextInstance.transform.localPosition = new Vector3(0, defaultYAxis, 0);
                }
            }
            else
            {
                interactTextInstance.transform.localPosition = new Vector3(0, defaultYAxis, 0);
            }

            string interactText = "Sleep"; // Default text

            KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(interactActionName);

            // Update text dynamically to match the correct keybinding based on input device
            TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = "Sleep";

                if (keyBinding != null)
                {
                    Sprite icon = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

                    // If the sprite exists, display it next to the text
                    if (icon != null)
                    {
                        iconObject = new GameObject("KeybindIcon");
                        iconObject.transform.SetParent(interactTextInstance.transform);

                        float horizontalOffset = -textMesh.preferredWidth / 2 - 0.04f; // Increased offset to add more space
                        iconObject.transform.localPosition = new Vector3(horizontalOffset, 0f, 0);
                        iconObject.transform.rotation = interactTextInstance.transform.rotation;

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
                                keyText = interactAction.bindings[1].ToDisplayString();  // Second binding (controller)
                            }
                            else
                            {
                                keyText = interactAction.bindings[0].ToDisplayString();  // First binding (keyboard)
                            }

                            keyText = keyText.Replace("Press ", "").Trim(); // Removes "Press" and any extra spaces

                            // Set the fallback text
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
        int layerMask = ~LayerMask.GetMask("Ignore Raycast");
        if (Physics.Raycast(ray, out hit, 3f, layerMask, QueryTriggerInteraction.Ignore))
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
            // update sprite scale if keybinding changes during the game
            UpdateSpriteScale();
        }
    }

    private IEnumerator HandleBedInteraction()
    {
        isInteracting = true;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            // Disable inventory components
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

            // Disable camera look controls
            GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
            cameraController = playerCamera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetLookState(false);
            }

            // Disable player movement
            playerMovement = playerObject.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SetMovementState(false);
            }

            // NEW: Check for ClimbingSystem and disable it
            /*ClimbingSystem climbingSystem = playerObject.GetComponent<ClimbingSystem>();
            if (climbingSystem != null)
            {
                climbingSystem.enabled = false;
            }

            // NEW: Handle Rigidbody if it exists
            Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                playerRigidbody.isKinematic = true; // Make rigidbody kinematic to prevent physics
                playerRigidbody.velocity = Vector3.zero; // Zero out velocity
            }*/

            PlayerStats.Instance.FadeOut();
        }

        Collider bedCollider = bed.GetComponent<Collider>();
        if (bedCollider == null)
        {
            Debug.LogError("No collider found on the bed object!");
            yield break;
        }

        // Get the world-space center of the bed collider
        Vector3 colliderCenter = bedCollider.bounds.center;

        // Adjust the position to be at the collider's center, accounting for rotation
        Vector3 targetPosition = colliderCenter + bed.transform.up * (bedCollider.bounds.extents.y + yPositionOffset) +
                                 bed.transform.right * xPositionOffset + bed.transform.forward * zPositionOffset;

        player.transform.position = targetPosition;

        player.transform.rotation = bed.transform.rotation; // Align the player's rotation with the bed

        Debug.Log($"Teleporting player to: {targetPosition}");

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

        player.rotation = Quaternion.Euler(player.eulerAngles.x, bed.rotation.eulerAngles.y, player.eulerAngles.z); // Align Y rotation with the bed's rotation

        // Adjust player's rotation to make them lie flat on their back (90 degrees)
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
            StartCinematicByTag(cinematicSequenceTag);
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

            // NEW: Reset player position slightly above the bed to prevent falling through
            // player.position = new Vector3(player.position.x, player.position.y + 0.1f, player.position.z);

            PlayerStats.Instance.ReplenishEnergy(100f);
            PlayerStats.Instance.FadeIn();
            SaveManager.Instance.SaveGame();
            DayNightCycle.Instance.StartTime();

            // Re-enable character control components
            cameraController.SetLookState(true);
            playerMovement.SetMovementState(true);

            if (characterController != null)
            {
                characterController.enabled = true;
            }

            // NEW: Reset rigidbody state
            /*Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                playerRigidbody.isKinematic = false;
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.useGravity = true;
            }*/

            // Re-enable climbing system
            /*ClimbingSystem climbingSystem = player.GetComponent<ClimbingSystem>();
            if (climbingSystem != null)
            {
                climbingSystem.enabled = true;
            }*/

            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
                inventoryCanvas.gameObject.SetActive(true);
            }
        }

        hasSetTime = false;
        isInteracting = false;
    }

    void StartCinematicByTag(string tag)
    {
        GameObject cinematicObj = GameObject.FindWithTag(tag);

        if (cinematicObj != null)
        {
            CinematicSequence cinematicSequence = cinematicObj.GetComponent<CinematicSequence>();

            if (cinematicSequence != null)
            {
                cinematicSequence.StartCinematic();
                cinematicSequence.OnCinematicFinished += RotatePlayerUpright;
                cinematicSequence.OnCinematicStarted += FadeOutBlack;
            }
            else
            {
                Debug.LogWarning("No CinematicSequence component found on object with tag: " + tag);
            }
        }
        else
        {
            Debug.LogWarning("No GameObject found with tag: " + tag);
        }
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
        GameObject cinematicObj = GameObject.FindWithTag(tag);

        if (cinematicObj != null)
        {
            CinematicSequence cinematicSequence = cinematicObj.GetComponent<CinematicSequence>();

            if (cinematicSequence != null)
            {
                cinematicSequence.OnCinematicStarted -= FadeOutBlack;
            }
        }
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

        GameObject cinematicObj = GameObject.FindWithTag(tag);

        if (cinematicObj != null)
        {
            CinematicSequence cinematicSequence = cinematicObj.GetComponent<CinematicSequence>();

            if (cinematicSequence != null)
            {
                // Unsubscribe from the event to avoid duplicate calls
                cinematicSequence.OnCinematicFinished -= RotatePlayerUpright;
            }
        }

        // NEW: Reset player position slightly above the bed to prevent falling through
        // player.position = new Vector3(player.position.x, player.position.y + 0.1f, player.position.z);

        PlayerStats.Instance.ReplenishEnergy(100f);
        PlayerStats.Instance.FadeIn();

        if (inventoryManager != null)
        {
            inventoryManager.enabled = true;
            inventoryCanvas.gameObject.SetActive(true);
        }

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // NEW: Reset rigidbody state
        /*Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false;
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.useGravity = true;
        }*/

        // Re-enable climbing system
        /*ClimbingSystem climbingSystem = player.GetComponent<ClimbingSystem>();
        if (climbingSystem != null)
        {
            climbingSystem.enabled = true;
        }*/

        SaveManager.Instance.SaveGame();
        cameraController.SetLookState(true);
        playerMovement.SetMovementState(true);
        DayNightCycle.Instance.StartTime();
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
            DayNightCycle.Instance.SetTime(6, 00, true); // Set time
            DayNightCycle.Instance.StopTime();
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