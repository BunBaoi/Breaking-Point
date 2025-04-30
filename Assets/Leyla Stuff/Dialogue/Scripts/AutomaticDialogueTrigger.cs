using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AutomaticDialogueTrigger : MonoBehaviour
{
    [Header("Automatic Dialogue Settings")]
    [SerializeField] private AutomaticDialogue automaticDialogue;
    [SerializeField] private string dialogueKey = "AutoDialogueTriggered";
    [SerializeField] private bool triggerOnce = true; // Only trigger dialogue once
    [SerializeField] private bool triggerOnStart = false; // Trigger when scene starts
    [SerializeField] private bool triggerOnEnter = true; // Trigger when player enters collider

    [Header("Manual Trigger Settings")]
    [SerializeField] private bool allowManualTrigger = true; // Allow manual trigger with input
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string interactActionName = "Interact";

    [Header("Bool Conditions")]
    [SerializeField] private bool checkBoolConditions = false; // Whether to check bool conditions
    [SerializeField] private string[] requiredBoolKeysTrue; // Bool keys that must be true
    [SerializeField] private string[] requiredBoolKeysFalse; // Bool keys that must be false

    private bool playerInRange = false;
    private bool dialoguePlayed = false;
    private InputAction interactAction;
    private Coroutine autoDialogueCoroutine;

    private void Awake()
    {
        // If inputActions is not assigned, load it from Resources
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");
            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }

        // Load dialogue played state
        // dialoguePlayed = PlayerPrefs.GetInt(dialogueKey, 0) == 1;
    }

    private void Start()
    {
        // Setup the interact action if needed
        if (allowManualTrigger)
        {
            interactAction = inputActions.FindAction(interactActionName);
            if (interactAction != null)
            {
                interactAction.Enable();
            }
            else
            {
                Debug.LogError($"Input action '{interactActionName}' not found in Input Action Asset!");
            }
        }

        // Trigger dialogue on start if required
        if (triggerOnStart && !dialoguePlayed && CanTriggerDialogue())
        {
            StartAutoDialogue();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            // Auto-trigger dialogue if configured to do so
            if (triggerOnEnter && !dialoguePlayed && CanTriggerDialogue())
            {
                StartAutoDialogue();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void Update()
    {
        // Allow manual triggering if enabled and player is in range
        if (allowManualTrigger && playerInRange && interactAction.triggered && !dialoguePlayed && CanTriggerDialogue())
        {
            StartAutoDialogue();
        }
    }

    // Check if dialogue can be triggered based on bool conditions
    private bool CanTriggerDialogue()
    {
        if (!checkBoolConditions) return false;

        // Check all required TRUE conditions
        foreach (string boolKey in requiredBoolKeysTrue)
        {
            if (!BoolManager.Instance.GetBool(boolKey))
            {
                return false;
            }
        }

        // Check all required FALSE conditions
        foreach (string boolKey in requiredBoolKeysFalse)
        {
            if (BoolManager.Instance.GetBool(boolKey))
            {
                return false;
            }
        }

        return true;
    }

    // Start the automatic dialogue process
    public void StartAutoDialogue()
    {
        if (automaticDialogue == null)
        {
            Debug.LogError("No AutomaticDialogue assigned!");
            return;
        }

        dialoguePlayed = true;

        if (triggerOnce)
        {
            // PlayerPrefs.SetInt(dialogueKey, 1);
            // PlayerPrefs.Save();
        }

        // Set the automatic dialogue flag in DialogueManager
        DialogueManager.Instance.SetAutomaticDialogueState(true);

        // Start the dialogue using DialogueManager
        if (autoDialogueCoroutine != null)
        {
            StopCoroutine(autoDialogueCoroutine);
        }

        autoDialogueCoroutine = StartCoroutine(RunAutomaticDialogue());
    }

    // Public method to reset the dialogue (can be called from other scripts)
    /*public void ResetDialogue()
    {
        dialoguePlayed = false;
        PlayerPrefs.SetInt(dialogueKey, 0);
        PlayerPrefs.Save();
    }*/

    private IEnumerator RunAutomaticDialogue()
    {
        // Start the dialogue
        DialogueManager.Instance.StartDialogue(automaticDialogue.DialogueTree);

        // Get a reference to the DialogueManager component
        DialogueManager dialogueManager = DialogueManager.Instance;

        // Wait for the initial dialogue to appear
        yield return new WaitForSeconds(0.5f);

        // Keep advancing dialogue until it's complete
        while (dialogueManager.IsDialogueActive())
        {
            // Wait until the current text is fully shown if required
            if (automaticDialogue.WaitForFullTextBeforeProceeding)
            {
                // Wait until text scrolling is complete
                while (dialogueManager.IsTextScrolling())
                {
                    yield return null;
                }

                // Add additional pause after text is fully shown
                yield return new WaitForSeconds(automaticDialogue.AdditionalPauseAfterText);
            }
            else
            {
                // Otherwise just wait the specified time
                yield return new WaitForSeconds(automaticDialogue.TimeBetweenDialogues);
            }

            // If options are visible, auto-select the first option if enabled
            if (dialogueManager.OptionsAreVisible() && automaticDialogue.AutoSelectFirstOption)
            {
                // Wait before auto-selecting the option
                yield return new WaitForSeconds(automaticDialogue.TimeBeforeAutoSelectingOption);

                // Select the first option
                dialogueManager.SelectOption(0);
            }
            else if (!dialogueManager.OptionsAreVisible())
            {
                // If no options, just advance to next dialogue
                dialogueManager.ShowNextDialogue();
            }

            // Small wait to prevent advancing too quickly
            yield return new WaitForSeconds(0.1f);
        }

        // Reset the automatic dialogue flag when done
        DialogueManager.Instance.SetAutomaticDialogueState(false);
    }
}
