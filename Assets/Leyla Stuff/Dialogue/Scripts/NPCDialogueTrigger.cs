using UnityEngine;
using System.Collections.Generic;

public class NPCDialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueTree npcDialogueTree; // NPC's dialogue tree reference
    [SerializeField] private CompanionScript companionScript;
    private bool isDialogueTriggered;

    private Transform player; // Reference to the player's transform
    [SerializeField] private string dialogueKey = "DialogueTriggered";


    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>(); // List of bool keys that should be true
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>(); // List of bool keys that should be false

    private void Start()
    {
        isDialogueTriggered = PlayerPrefs.GetInt("DialogueTriggered", 0) == 1;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player has entered the trigger zone
        if (other.CompareTag("Player"))
        {
            player = other.transform; // Store player reference
            // Automatically trigger dialogue if conditions are met and it hasn't been triggered yet
            if (!isDialogueTriggered && CanStartDialogue())
            {
                StartDialogue();
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Continuously check if the player is still within the trigger zone
        if (other.CompareTag("Player"))
        {
            player = other.transform; // Keep track of the player's position

            // Recheck if the conditions are met and the dialogue has not been triggered yet
            if (!isDialogueTriggered && CanStartDialogue())
            {
                StartDialogue();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = null; // Reset the player reference when they exit the trigger zone
        }
    }

    private bool CanStartDialogue()
    {
        if (!DialogueManager.Instance.canStartDialogue)
        {
            return false;
        }
        // Check if all required bool conditions are met (true or false)
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
                return false;
            }
        }

        return true; // All conditions are met, return true
    }

    private void StartDialogue()
    {
        if (npcDialogueTree != null)
        {
            if (companionScript != null)
            {
                companionScript.TeleportToPlayer();
            }
            isDialogueTriggered = true;
            PlayerPrefs.SetInt(dialogueKey, 1);
            PlayerPrefs.Save();
            DialogueManager.Instance.StartDialogue(npcDialogueTree);
        }
    }
}