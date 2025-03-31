using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("Dialogue Settings")]
    [SerializeField] private TMP_Text dialogueTextUI;
    // [SerializeField] private TMP_Text npcNameUI;
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private Image nextDialogueIndicatorImage;
    [SerializeField] private CanvasGroup nextDialogueIndicatorCanvasGroup;
    [SerializeField] private Camera mainCamera;  // Player camera
    [SerializeField] private float scrollSpeed = 0.05f;

   [Header("Colour Settings")]
    [SerializeField] private string npcNameColorHex = "#D95959"; // Default colour
    [SerializeField] private string dialogueTextColorHex = "#4DB7C0"; // Default colour

    [Header("Option Button Settings")]
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.red;
    [SerializeField] private float hoverScaleMultiplier = 1.2f;  // Scale multiplier when hovering
    [SerializeField] private Transform optionIndicatorParent;
    [SerializeField] private Transform switchOptionsIndicatorParent;
    [SerializeField] private float optionIndicatorOffset = 0f;
    [SerializeField] private float scrollIndicatorOffset = 0f;

    private bool isOptionKeyPressed = false;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private KeybindSettings keybindSettings;
    [SerializeField] private string advanceDialogueName = "Advance Dialogue";
    [SerializeField] private string selectOptionName = "Select Option";
    [SerializeField] private string scrollName = "Interaction Scroll";

    [Header("Testing Purposes")]
    [SerializeField] private bool isTextScrolling = false;
    [SerializeField] private bool isFullTextShown = false;
    [SerializeField] private bool optionsAreVisible = false;
    [SerializeField] private bool isDialogueActive = false;

    [Header("Inventory Setups")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;
    [SerializeField] private GameObject[] playerHands;

    [Header("Automatic Dialogue")]
    public bool isAutomaticDialogueActive = false;
    public bool IsAutomaticDialogueActive => isAutomaticDialogueActive;

    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 1f;  // Cooldown time in seconds.
    private float cooldownTimer = 0f;  // Timer to track the cooldown
    public bool canStartDialogue = true;  // Flag to check if dialogue can start

    private InputAction advanceDialogue;
    private InputAction selectOption;
    private InputAction scroll;

    private int selectedOptionIndex = 0;
    [SerializeField] private GameObject scrollIndicatorPrefab;
    [SerializeField] private GameObject selectOptionIndicatorPrefab;
    private GameObject instantiatedScrollIndicator;
    private GameObject instantiatedSelectOptionIndicator;

    private Coroutine currentLookAtNpcCoroutine;
    private Coroutine scrollingCoroutine;
    private Coroutine indicatorCoroutine;

    private List<GameObject> instantiatedButtons = new List<GameObject>();

    private FMOD.Studio.EventInstance currentDialogueEvent;
    private FMOD.Studio.EventInstance currentSound = default;

    private DialogueTree originalDialogueTree;
    private int originalIndex;
    private bool returningToOriginal = false;
    private DialogueTree currentDialogueTree;
    private int currentIndex = 0;

    private PlayerMovement playerMovement;

    // access the status variables
    public bool IsTextScrolling() => isTextScrolling;
    public bool IsFullTextShown() => isFullTextShown;
    public bool OptionsAreVisible() => optionsAreVisible;
    public bool IsDialogueActive() => isDialogueActive;

    // OLD KEYBINDS
    /* [SerializeField] private string scrollUpName = "Scroll Up";
    // [SerializeField] private string scrollDownName = "Scroll Down";*/
    /* public KeyCode advanceKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode selectOptionKey = KeyCode.F;*/

    private void Awake()
    {
        // Find the action dynamically using the interactActionName string
        advanceDialogue = inputActions.FindAction(advanceDialogueName);
        selectOption = inputActions.FindAction(selectOptionName);
        scroll = inputActions.FindAction(scrollName);

        if (advanceDialogue != null)
        {
            advanceDialogue.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{advanceDialogueName}' not found in Input Action Asset!");
        }
        if (selectOption != null)
        {
            selectOption.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{selectOptionName}' not found in Input Action Asset!");
        }
        if (scroll != null)
        {
            scroll.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{scrollName}' not found in Input Action Asset!");
        }
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

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
        if (SettingsManager.Instance != null)
        {
            scrollSpeed = SettingsManager.Instance.GetScrollSpeed();
        }
    }

    private void OnEnable()
    {
        KeyBindingManager.OnInputDeviceChanged += UpdateAllIndicatorSprites;

        // Correctly subscribe using a lambda function
        KeybindSettings.OnKeyBindingsChanged += (_, _) => UpdateAllIndicatorSprites();
    }

    private void OnDisable()
    {
        KeyBindingManager.OnInputDeviceChanged -= UpdateAllIndicatorSprites;

        // Unsubscribe correctly
        KeybindSettings.OnKeyBindingsChanged -= (_, _) => UpdateAllIndicatorSprites();
    }

    private void UpdateAllIndicatorSprites()
    {
        UpdateAdvanceDialogueIndicatorSprite();
        UpdateScrollIndicatorSprite();
        UpdateSelectOptionIndicatorSprite();
    }

    public void SetInventoryActive(bool isActive)
    {
        // Activate or deactivate the InventoryManager script itself
        if (inventoryManager != null)
        {
            inventoryManager.enabled = isActive;
        }

        // Also manage the other objects
        if (inventoryCanvas != null)
            inventoryCanvas.gameObject.SetActive(isActive); 

        foreach (var hand in playerHands)
        {
            if (hand != null)
                hand.SetActive(isActive);
        }
    }

    private void Update()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.isMenuOpen)
        {
            return; // Prevent dialogue from advancing while the settings menu is open
        }

        if (!canStartDialogue)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f)
            {
                canStartDialogue = true;  // Allow dialogue to start again
            }
        }

        if (isFullTextShown && indicatorCoroutine == null && !isAutomaticDialogueActive)
        {
            indicatorCoroutine = StartCoroutine(FadeInAndOutIndicator());
        }
        else if (!isFullTextShown && indicatorCoroutine != null)
        {
            StopCoroutine(indicatorCoroutine);
            indicatorCoroutine = null; // Clear reference
            nextDialogueIndicatorCanvasGroup.alpha = 0f;
        }

        if (optionsAreVisible)
        {
            HandleOptionSelection();
            if (!isOptionKeyPressed)
            {
                UpdateSelectOptionIndicatorPosition();
                UpdateScrollIndicatorPosition();
            }
        }

        // Only process manual dialogue advancement if not in automatic dialogue mode
        if (!isAutomaticDialogueActive && !optionsAreVisible)
        {
            if (advanceDialogue.triggered && isDialogueActive)
            {
                if (isTextScrolling)
                {
                    StopCoroutine(scrollingCoroutine);
                    dialogueTextUI.text = $"<color={npcNameColorHex}>{currentDialogueTree.dialogueNodes[currentIndex - 1].npcName}:</color> <color={dialogueTextColorHex}>{currentDialogueTree.dialogueNodes[currentIndex - 1].dialogueText}</color>";
                    isTextScrolling = false;
                    isFullTextShown = true;

                    if (currentSound.isValid())
                    {
                        currentSound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                        currentSound.release();
                    }

                    ShowOptions(currentDialogueTree.dialogueNodes[currentIndex - 1]);
                }
                else if (isFullTextShown)
                {
                    ShowNextDialogue();
                }
            }
        }

        if (SettingsManager.Instance != null)
        {
            scrollSpeed = SettingsManager.Instance.GetScrollSpeed();
        }
    }

    private void UpdateIndicator(GameObject indicatorObject, string actionName)
    {
        if (KeyBindingManager.Instance == null || indicatorObject == null || inputActions == null) return;

        // Get input action
        InputAction action = inputActions.FindAction(actionName);
        if (action == null) return;

        // Get the first binding (keyboard) or second binding (controller)
        int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;
        if (action.bindings.Count <= bindingIndex) return;

        InputBinding binding = action.bindings[bindingIndex];

        // Get the display name for the key/button bound
        string boundKeyOrButton = KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);
        if (string.IsNullOrEmpty(boundKeyOrButton))
        {
            Debug.LogWarning($"No key binding found for action: {actionName}");
            return;
        }

        KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(actionName);
        if (keyBinding == null) return;

        Image indicatorImage = indicatorObject.GetComponent<Image>();
        if (indicatorImage == null) return;

        indicatorImage.sprite = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

        Animator animator = indicatorObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = indicatorObject.AddComponent<Animator>();
        }

        animator.enabled = true;

        // Load the correct animator based on the key/button
        string folderPath = KeyBindingManager.Instance.IsUsingController() ? "UI/Controller/" : "UI/Keyboard/";
        string animatorName = KeyBindingManager.Instance.GetSanitisedKeyName(boundKeyOrButton);
        RuntimeAnimatorController assignedAnimator = Resources.Load<RuntimeAnimatorController>(folderPath + animatorName);

        if (assignedAnimator != null)
        {
            animator.runtimeAnimatorController = assignedAnimator;
            Debug.Log($"Assigned animator '{animatorName}' to {indicatorObject.name}");
        }
        else
        {
            Debug.LogError($"Animator '{animatorName}' not found in {folderPath}");
        }
    }

    private void UpdateAdvanceDialogueIndicatorSprite()
    {
        UpdateIndicator(nextDialogueIndicatorImage.gameObject, advanceDialogueName);
    }

    private void UpdateScrollIndicatorSprite()
    {
        UpdateIndicator(instantiatedScrollIndicator, scrollName);
    }

    private void UpdateSelectOptionIndicatorSprite()
    {
        UpdateIndicator(instantiatedSelectOptionIndicator, selectOptionName);
    }

    private void HandleOptionSelection()
    {
        if (instantiatedButtons.Count == 0) return;

        int previousIndex = selectedOptionIndex; // Store previous index for debugging

        // --- Read Axis-Based Scroll Input ---
        float scrollValue = scroll.ReadValue<float>(); // Use float for single-axis input

        if (Mathf.Abs(scrollValue) > 0.1f) // Deadzone threshold
        {
            if (scrollValue > 0)
            {
                if (selectedOptionIndex > 0)
                {
                    selectedOptionIndex--;
                    Debug.Log($"Scroll Up! New Index: {selectedOptionIndex}");
                    UpdateHighlightedOption();
                }
            }
            else if (scrollValue < 0)
            {
                if (selectedOptionIndex < instantiatedButtons.Count - 1)
                {
                    selectedOptionIndex++;
                    Debug.Log($"Scroll Down! New Index: {selectedOptionIndex}");
                    UpdateHighlightedOption();
                }
            }
        }

        // --- FIXED BUTTON PRESS DETECTION ---
        if (selectOption.WasPressedThisFrame())
        {
            isOptionKeyPressed = true;
            TMP_Text buttonText = instantiatedButtons[selectedOptionIndex].GetComponentInChildren<TMP_Text>();
            buttonText.color = pressedColor;

            InstantiateSelectOptionIndicator();
        }

        if (isOptionKeyPressed && selectOption.WasReleasedThisFrame())
        {
            if (selectedOptionIndex >= 0 && selectedOptionIndex < instantiatedButtons.Count)
            {
                instantiatedButtons[selectedOptionIndex].GetComponent<Button>().onClick.Invoke();
            }

            isOptionKeyPressed = false;

            DestroyAllIndicatorChildren();

            UpdateHighlightedOption();
        }
    }

    private void UpdateHighlightedOption()
    {
        // If no options are available, return early
        if (instantiatedButtons.Count == 0) return;

        for (int i = 0; i < instantiatedButtons.Count; i++)
        {
            TMP_Text buttonText = instantiatedButtons[i].GetComponentInChildren<TMP_Text>();
            RectTransform buttonRectTransform = instantiatedButtons[i].GetComponent<RectTransform>();

            if (i == selectedOptionIndex)
            {
                if (!isOptionKeyPressed)  // Only apply hover colour if the key is not pressed
                {
                    buttonText.color = hoverColor;

                    // Scale the button when hovered
                    buttonRectTransform.localScale = Vector3.one * hoverScaleMultiplier;

                    // Instantiate the select option indicator when the option is hovered
                    if (instantiatedSelectOptionIndicator == null)  // Only instantiate if not already done
                    {
                        InstantiateSelectOptionIndicator();
                    }
                    else
                    {
                        // Update the indicator's position if it already exists
                        UpdateSelectOptionIndicatorPosition();
                    }
                }
                else  // If the key is pressed, use pressed colour
                {
                    buttonText.color = pressedColor;
                    buttonRectTransform.localScale = Vector3.one * hoverScaleMultiplier; // Maintain scale when pressed
                }
            }
            else
            {
                buttonText.color = originalColor;

                // Reset scale for buttons that are not highlighted
                buttonRectTransform.localScale = Vector3.one;
            }
        }
    }

    private void InstantiateSelectOptionIndicator()
    {
        // Destroy the previous indicator if it exists
        if (instantiatedSelectOptionIndicator != null)
        {
            Destroy(instantiatedSelectOptionIndicator);
        }

        RectTransform buttonRectTransform = instantiatedButtons[selectedOptionIndex].GetComponent<RectTransform>();

        Vector3 indicatorPosition = new Vector3(
            buttonRectTransform.localPosition.x + optionIndicatorOffset,
            buttonRectTransform.localPosition.y,
            buttonRectTransform.localPosition.z
        );

        instantiatedSelectOptionIndicator = Instantiate(selectOptionIndicatorPrefab, indicatorPosition, Quaternion.identity);

        instantiatedSelectOptionIndicator.transform.SetParent(optionIndicatorParent, true);

        // Set the correct sprite based on input device
        UpdateSelectOptionIndicatorSprite();
    }

    private void UpdateSelectOptionIndicatorPosition()
    {
        // Is it within valid range?
        if (selectedOptionIndex < 0 || selectedOptionIndex >= instantiatedButtons.Count)
        {
            // Debug.LogError($"Selected option index {selectedOptionIndex} is out of range. Valid range is 0 to {instantiatedButtons.Count - 1}.");
            return;
        }

        // Get the position
        RectTransform buttonRectTransform = instantiatedButtons[selectedOptionIndex].GetComponent<RectTransform>();

        // Calculate the new position
        Vector3 newPosition = new Vector3(buttonRectTransform.localPosition.x + optionIndicatorOffset, buttonRectTransform.localPosition.y, buttonRectTransform.localPosition.z);

        // Update the position of the existing indicator
        instantiatedSelectOptionIndicator.transform.localPosition = newPosition;
    }

    public void OnButtonHover(int index)
    {
        selectedOptionIndex = index;
        UpdateHighlightedOption();
    }

    public void OnButtonPressed()
    {
        if (selectedOptionIndex >= 0 && selectedOptionIndex < instantiatedButtons.Count)
        {
            TMP_Text buttonText = instantiatedButtons[selectedOptionIndex].GetComponentInChildren<TMP_Text>();
            buttonText.color = pressedColor;
        }
    }

    private void DestroyAllIndicatorChildren()
    {
        // Destroy children of option indicators
        foreach (Transform child in optionIndicatorParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in switchOptionsIndicatorParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void StartDialogue(DialogueTree dialogueTree)
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.isMenuOpen)
        {
            return; // Prevent dialogue from advancing while the settings menu is open
        }

        if (inventoryManager != null)
        {
            inventoryManager.enabled = false;
            inventoryCanvas.gameObject.SetActive(false);
        }

        if (playerHands != null)
        {
            foreach (GameObject hand in playerHands)
            {
                hand.SetActive(false);
            }
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                Debug.Log("Disabling movement");
                playerMovement.SetMovementState(false);
            }
            else
            {
                Debug.LogWarning("PlayerMovement component not found on Player.");
            }
        }
        else
        {
            Debug.LogWarning("Player object not found with tag 'Player'.");
        }
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        isDialogueActive = true;
        nextDialogueIndicatorCanvasGroup.alpha = 0f;
        currentDialogueTree = dialogueTree;
        currentIndex = 0;
        dialogueCanvas.enabled = true;
        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.isMenuOpen)
        {
            return; // Prevent dialogue from advancing while the settings menu is open
        }

        StopCoroutine(FadeInAndOutIndicator());
        nextDialogueIndicatorCanvasGroup.alpha = 0f;
        ClearOptions();

        // Trigger events from the previous node before moving to the next
        if (currentIndex > 0 && currentIndex <= currentDialogueTree.dialogueNodes.Count)
        {
            DialogueNode previousNode = currentDialogueTree.dialogueNodes[currentIndex - 1];
            foreach (var eventId in previousNode.eventIds)
            {
                if (!string.IsNullOrEmpty(eventId))
                {
                    DialogueEventManager.Instance?.TriggerDialogueEvent(eventId);
                }
            }
        }

        if (currentDialogueTree != null && currentIndex < currentDialogueTree.dialogueNodes.Count)
        {
            isFullTextShown = false;
            DisplayDialogue(currentDialogueTree.dialogueNodes[currentIndex]);
            currentIndex++;
        }
        else if (returningToOriginal && originalDialogueTree != null && originalIndex < originalDialogueTree.dialogueNodes.Count)
        {
            // Return to the original dialogue tree if there's more dialogue
            currentDialogueTree = originalDialogueTree;
            currentIndex = originalIndex;
            returningToOriginal = false;

            ShowNextDialogue(); // Continue where it left off
        }
        else
        {
            // No more dialogue, end conversation
            StartCooldown();
            isDialogueActive = false;
            isAutomaticDialogueActive = false;
            nextDialogueIndicatorCanvasGroup.alpha = 0f;
            nextDialogueIndicatorImage.gameObject.SetActive(false);
            dialogueCanvas.enabled = false;
            // Stop the previous coroutine if it's running
            if (currentLookAtNpcCoroutine != null)
            {
                StopCoroutine(currentLookAtNpcCoroutine);
            }
            if (playerMovement != null)
            {
                playerMovement.SetMovementState(true);
            }
            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
                inventoryCanvas.gameObject.SetActive(true);
            }
            if (playerHands != null)
            {
                foreach (GameObject hand in playerHands)
                {
                    hand.SetActive(true);
                }
            }
        }
    }

    private void StartCooldown()
    {
        cooldownTimer = cooldownTime;
        canStartDialogue = false;
    }

    private void DisplayDialogue(DialogueNode node)
    {
        if (scrollingCoroutine != null)
        {
            StopCoroutine(scrollingCoroutine);
        }

        // THIS IS OLD COROUTINE, KEEPING IN CASE: scrollingCoroutine = StartCoroutine(ScrollText(node.dialogueText));

        scrollingCoroutine = StartCoroutine(ScrollText(node));

        // Apply the NPC name colour
        if (ColorUtility.TryParseHtmlString(npcNameColorHex, out Color npcColor))
        {
            dialogueTextUI.color = npcColor;
        }

        // Apply the dialogue text colour
        if (ColorUtility.TryParseHtmlString(dialogueTextColorHex, out Color textColor))
        {
            dialogueTextUI.color = textColor;
        }

        /*if (npcNameUI != null)
        {
            npcNameUI.text = node.npcName;
        }*/

        // Stop fmod event and if useDialogueAudio is false for the current node, play the fmodSoundEvent variable instead
        if (currentDialogueEvent.isValid())
        {
            currentDialogueEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentDialogueEvent.release();
        }

        if (!node.useDialogueAudio)
        {
            if (!node.fmodSoundEvent.IsNull)
            {
                currentDialogueEvent = FMODUnity.RuntimeManager.CreateInstance(node.fmodSoundEvent);
                currentDialogueEvent.start();
            }
        }

        /*if (!node.fmodAudioEvent.IsNull)
        {
            currentDialogueEvent = RuntimeManager.CreateInstance(node.fmodAudioEvent);
            currentDialogueEvent.start();
        }*/

        // Trigger the camera look at the NPC if npcTag is provided
        TriggerCameraLookAtNpc(node);

        /*foreach (var eventId in node.eventIds)
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                DialogueEventManager.Instance?.TriggerDialogueEvent(eventId);
            }
        }*/
    }

    private void TriggerCameraLookAtNpc(DialogueNode node)
    {
        if (!string.IsNullOrEmpty(node.npcTag))
        {
            // Find all NPCs with the given tag
            GameObject[] npcs = GameObject.FindGameObjectsWithTag(node.npcTag);
            if (npcs.Length > 0)
            {
                GameObject closestNpc = null;
                float closestDistance = Mathf.Infinity; // Start with a very large distance

                // Find the closest NPC
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                foreach (GameObject npc in npcs)
                {
                    if (npc != null && player != null)
                    {
                        // Calculate the distance from the player to the center of the NPC
                        Vector3 npcCenter = npc.transform.position;
                        float distance = Vector3.Distance(player.transform.position, npcCenter);

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestNpc = npc;
                        }
                    }
                }

                // If closest NPC found, start the look at NPC process
                if (closestNpc != null)
                {
                    if (currentLookAtNpcCoroutine != null)
                    {
                        StopCoroutine(currentLookAtNpcCoroutine);
                    }

                    // Start a new coroutine to look at the closest NPC
                    currentLookAtNpcCoroutine = StartCoroutine(SmoothLookAtNpc(closestNpc));
                }
            }
        }
    }

    private IEnumerator SmoothLookAtNpc(GameObject npc)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (npc != null && mainCamera != null && player != null)
        {
            // Get the position of the NPC
            Vector3 npcPosition = npc.transform.position;

            // Calculate the direction vector from the camera to the NPC
            Vector3 targetDirection = npcPosition - mainCamera.transform.position;

            // Calculate the target rotation based on the direction (this will affect both yaw and pitch)
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            CameraController cameraController = mainCamera.GetComponent<CameraController>();

            // Smoothly rotate the camera to the target rotation
            while (Quaternion.Angle(mainCamera.transform.rotation, targetRotation) > 0.1f)
            {
                // Smoothly rotate the camera horizontally (yaw) and vertically (pitch)
                Quaternion currentRotation = mainCamera.transform.rotation;
                currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * 2f);

                // Smoothly adjust the pitch (xRotation) towards the target pitch
                float targetPitch = Mathf.LerpAngle(cameraController.xRotation, targetRotation.eulerAngles.x, Time.deltaTime * 2f);

                // Apply the smooth pitch (vertical) and yaw (horizontal) to the camera
                mainCamera.transform.rotation = Quaternion.Euler(targetPitch, currentRotation.eulerAngles.y, 0);

                // Also rotate the player (body) to face the NPC (yaw only)
                player.transform.rotation = Quaternion.Euler(0, currentRotation.eulerAngles.y, 0);

                // Update the camera's xRotation to reflect the smooth pitch (vertical rotation)
                cameraController.xRotation = targetPitch;

                yield return null;
            }

            // Final alignment with the NPC (ensure no overshooting)
            mainCamera.transform.rotation = targetRotation;
            player.transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }
    }

    private IEnumerator ScrollText(DialogueNode node)
    {
        isTextScrolling = true;
        dialogueTextUI.text = ""; // Clear the text field

        string fullTextWithRichText = $"<color={npcNameColorHex}>{node.npcName}:</color> {node.dialogueText}";

        // For counting characters in text
        string rawFullText = $"{node.npcName}: {node.dialogueText}";

        // Set text and force update for TMP calculations
        dialogueTextUI.text = fullTextWithRichText;
        dialogueTextUI.ForceMeshUpdate();

        // Center single-line text if applicable
        if (dialogueTextUI.textInfo.lineCount == 1)
        {
            TMP_TextInfo textInfo = dialogueTextUI.textInfo;
            int totalChars = rawFullText.Length + 2;
            if (totalChars > 0)
            {
                int middleIndex = Mathf.Clamp(totalChars / 2, 0, textInfo.characterCount - 1);
                float middleCharX = textInfo.characterInfo[middleIndex].origin;
                RectTransform rt = dialogueTextUI.GetComponent<RectTransform>();
                rt.localPosition = new Vector3(-middleCharX, rt.localPosition.y, rt.localPosition.z);
            }
        }
        else
        {
            RectTransform rt = dialogueTextUI.GetComponent<RectTransform>();
            rt.localPosition = new Vector3(0, rt.localPosition.y, rt.localPosition.z);
        }

        // Audio variables
        DialogueAudio audioSettings = node.dialogueAudio;

        // Start the FMOD audio immediately
        if (audioSettings != null && node.useDialogueAudio)
        {
            int charHash = node.dialogueText[0].GetHashCode();
            int soundIndex = Mathf.Abs(charHash) % audioSettings.fmodSoundEvents.Length;
            EventReference soundEventReference = audioSettings.fmodSoundEvents[soundIndex];

            currentSound = FMODUnity.RuntimeManager.CreateInstance(soundEventReference);

            // Set the pitch based on character hash
            float pitch = Mathf.Lerp(audioSettings.minPitch, audioSettings.maxPitch, Mathf.Abs(charHash % 100) / 100f);

            currentSound.setPitch(pitch);
            currentSound.start();
        }

        for (int i = 0; i < node.dialogueText.Length; i++)
        {
            dialogueTextUI.text = $"<color={npcNameColorHex}>{node.npcName}:</color> {node.dialogueText.Substring(0, i + 1)}";

            // Play audio based on frequency
            if (audioSettings != null && i % audioSettings.frequency == 0 && node.useDialogueAudio)
            {
                // Stop previous sound if any
                if (currentSound.isValid())
                {
                    currentSound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    currentSound.release();
                }

                // Get the character to hash for new sound
                char currentChar = node.dialogueText[i];
                int charHash = currentChar.GetHashCode();

                int soundIndex = Mathf.Abs(charHash) % audioSettings.fmodSoundEvents.Length;
                EventReference soundEventReference = audioSettings.fmodSoundEvents[soundIndex];

                // Create a new FMOD EventInstance using EventReference
                currentSound = FMODUnity.RuntimeManager.CreateInstance(soundEventReference);

                // Determine consistent pitch based on character hash
                float pitch = Mathf.Lerp(audioSettings.minPitch, audioSettings.maxPitch, Mathf.Abs(charHash % 100) / 100f);

                // Set the pitch and start the sound
                currentSound.setPitch(pitch);
                currentSound.start();
            }

            yield return new WaitForSeconds(scrollSpeed);
        }

        // Stop any remaining audio after text scrolling
        if (currentSound.isValid())
        {
            currentSound.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentSound.release();
        }

        // Display the full formatted text
        dialogueTextUI.text = fullTextWithRichText;

        isTextScrolling = false;
        isFullTextShown = true;

        ShowOptions(node);
    }

    private IEnumerator FadeInAndOutIndicator()
    {
        nextDialogueIndicatorCanvasGroup.alpha = 1f;
        nextDialogueIndicatorImage.gameObject.SetActive(true);

        UpdateAdvanceDialogueIndicatorSprite();

        while (true) // Infinite loop until coroutine is stopped
        {
            yield return FadeCanvasGroup(nextDialogueIndicatorCanvasGroup, 1f, 0f, 1f); // Fade out
            yield return FadeCanvasGroup(nextDialogueIndicatorCanvasGroup, 0f, 1f, 1f); // Fade in
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float fromAlpha, float toAlpha, float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = toAlpha;
    }

    private void ShowOptions(DialogueNode node)
    {
        if (node.options.Count > 0)
        {
            optionsAreVisible = true;
            foreach (var option in node.options)
            {
                GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
                TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
                buttonText.text = option.optionText;

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    optionsAreVisible = false;

                    if (option.nextDialogueTree != null)
                    {
                        // Store the original tree and index before switching
                        originalDialogueTree = currentDialogueTree;
                        originalIndex = currentIndex;
                        returningToOriginal = true;

                        StartDialogue(option.nextDialogueTree);
                    }
                    else
                    {
                        ShowNextDialogue();
                    }
                });

                instantiatedButtons.Add(buttonObj);
            }

            GameObject indicator = Instantiate(scrollIndicatorPrefab, Vector3.zero, Quaternion.identity);

            indicator.transform.SetParent(switchOptionsIndicatorParent, false);

            indicator.transform.localPosition = Vector3.zero;

            instantiatedScrollIndicator = indicator;

            UpdateScrollIndicatorSprite();

            // **Ensure the first option is highlighted**
            selectedOptionIndex = 0;
            UpdateHighlightedOption();
            UpdateSelectOptionIndicatorPosition();
            UpdateScrollIndicatorPosition();

            /*Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;*/
        }
    }

    private void UpdateScrollIndicatorPosition()
    {
        // Check if the selectedOptionIndex is within the valid range
        if (selectedOptionIndex < 0 || selectedOptionIndex >= instantiatedButtons.Count)
        {
            return; // Exit the method if the index is invalid
        }

        // Get the position of the currently highlighted button
        RectTransform buttonRectTransform = instantiatedButtons[selectedOptionIndex].GetComponent<RectTransform>();

        // Calculate the new position
        Vector3 newPosition = new Vector3(buttonRectTransform.localPosition.x + scrollIndicatorOffset, buttonRectTransform.localPosition.y, buttonRectTransform.localPosition.z);

        // Update the position of the existing indicator
        instantiatedScrollIndicator.transform.localPosition = newPosition;
    }

    private void ClearOptions()
    {
        foreach (var button in instantiatedButtons)
        {
            Destroy(button);
        }
        instantiatedButtons.Clear();

        /*Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;*/

        optionsAreVisible = false;
    }

    //======AutoDialogueFunctions=======//

    //allow external option selection
    public void SelectOption(int optionIndex)
    {
        if (optionsAreVisible && optionIndex >= 0 && optionIndex < instantiatedButtons.Count)
        {
            selectedOptionIndex = optionIndex;
            UpdateHighlightedOption();
            instantiatedButtons[selectedOptionIndex].GetComponent<Button>().onClick.Invoke();
        }
    }

    public void SetAutomaticDialogueState(bool isAutomatic)
    {
        isAutomaticDialogueActive = isAutomatic;
    }
}