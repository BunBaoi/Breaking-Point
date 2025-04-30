using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance;

    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private List<string> qteActionNames = new List<string> { "QTEButton1", "QTEButton2", "QTEButton3", "QTEButton4" };

    [Header("UI")]
    [SerializeField] private GameObject qteIconPrefab;
    [SerializeField] private Transform iconSpawnPoint;

    [Header("Timing")]
    [SerializeField] private float qteTimeLimit = 5f;

    private GameObject currentIcon;
    private string currentActionName;
    private bool qteActive = false;
    private Coroutine qteRoutine;
    private PlayerMovement playerMovement;
    private Image fillImage;
    private Transform targetLocation;
    private QTETrigger nextQTETrigger;
    private Animator playerAnimator;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    private void OnEnable()
    {
        // Enable listening to *any* input, not just the QTE action
        foreach (var actionName in qteActionNames)
        {
            InputAction action = inputActions.FindAction(actionName);
            if (action != null)
            {
                action.Enable();
                action.performed += OnAnyButtonPressed;  // Listen for all button presses
            }
        }
    }

    private void OnDisable()
    {
        // Clean up listeners when QTE ends
        foreach (var actionName in qteActionNames)
        {
            InputAction action = inputActions.FindAction(actionName);
            if (action != null)
            {
                action.Disable();
                action.performed -= OnAnyButtonPressed;
            }
        }
    }

    private void OnAnyButtonPressed(InputAction.CallbackContext context)
    {
        // If QTE is active and the wrong button is pressed
        if (qteActive && context.action.name != currentActionName)
        {
            Debug.Log($"Wrong button '{context.action.name}' pressed. QTE Failed.");
            FailQTE();  // Fail QTE
        }
    }

    public void StartQTE(Transform destination, QTETrigger nextTrigger = null)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();

            GameObject playerModel = GameObject.FindGameObjectWithTag("PlayerModel");
            if (playerModel != null)
            {
                playerAnimator = playerModel.GetComponent<Animator>();
            }
        }

        targetLocation = destination;
        nextQTETrigger = nextTrigger;

        // Disable the next trigger's auto-detection to avoid double-triggers
        if (nextQTETrigger != null)
            nextQTETrigger.DisableAutoTrigger();

        if (qteRoutine != null) StopCoroutine(qteRoutine);
        qteRoutine = StartCoroutine(QTERoutine());
    }

    private IEnumerator QTERoutine()
    {
        if (playerMovement != null)
            playerMovement.SetMovementState(false);
        qteActive = true;
        currentActionName = qteActionNames[Random.Range(0, qteActionNames.Count)];

        InputAction action = inputActions.FindAction(currentActionName);
        if (action == null)
        {
            Debug.LogError($"Input Action '{currentActionName}' not found!");
            yield break;
        }

        action.Enable();
        action.performed += OnQTEButtonPressed;

        currentIcon = Instantiate(qteIconPrefab, iconSpawnPoint);
        Transform fillTransform = currentIcon.transform.Find("Fill");
        if (fillTransform != null)
        {
            fillImage = fillTransform.GetComponent<Image>();
            fillImage.fillAmount = 1f;
        }
        else
        {
            Debug.LogWarning("Fill image not found in QTE icon prefab.");
        }
        UpdateSprite(currentIcon, currentActionName);

        float timer = 0f;

        while (timer < qteTimeLimit)
        {
            if (!qteActive) yield break;
            timer += Time.deltaTime;

            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(1 - (timer / qteTimeLimit));

            yield return null;
        }

        // Fail
        FailQTE();
    }

    private void OnQTEButtonPressed(InputAction.CallbackContext context)
    {
        if (!qteActive) return;

        string pressedButton = context.action.name;

        // If the pressed button matches the current QTE action, it's a success
        if (pressedButton == currentActionName)
        {
            Debug.Log("QTE SUCCESS!");

            qteActive = false;

            if (currentIcon != null)
            {
                Destroy(currentIcon);
                currentIcon = null;
            }

            InputAction action = inputActions.FindAction(currentActionName);
            if (action != null)
            {
                action.Disable();
                action.performed -= OnQTEButtonPressed;
            }

            // Proceed to move the player to the target location
            StartCoroutine(MovePlayerToTarget());
        }
    }

    private IEnumerator MovePlayerToTarget()
    {
        if (playerMovement != null)
        {
            playerMovement.SetMovementState(false);
            playerMovement.SetAnimationSpeedOverride(true, 0.5f);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || targetLocation == null)
        {
            EndQTE();
            yield break;
        }

        float duration = 1.5f;
        float elapsed = 0f;
        Vector3 startPos = player.transform.position;
        Vector3 endPos = targetLocation.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            player.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        player.transform.position = endPos;

        EndQTE();

        if (nextQTETrigger != null)
        {
            yield return new WaitForSeconds(0.2f);
            nextQTETrigger.TriggerManually(); // Manual trigger
        }
    }

    private void FailQTE()
    {
        Debug.Log("QTE FAILED! Player dies.");
        // Trigger death logic here
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerStats.Instance.PlayerDeath();
        }
            EndQTE();
    }

    private void EndQTE()
    {
        if (nextQTETrigger == null && playerMovement != null)
        {
            playerMovement.SetMovementState(true);
            playerMovement.SetAnimationSpeedOverride(false);
        }
        if (playerMovement != null)
        {
            playerMovement.SetAnimationSpeedOverride(false);
        }
        qteActive = false;

        if (currentIcon != null)
        {
            Destroy(currentIcon);
            currentIcon = null;
        }

        InputAction action = inputActions.FindAction(currentActionName);
        if (action != null)
        {
            action.Disable();
            action.performed -= OnQTEButtonPressed;
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

