using UnityEngine;
using TMPro; // Import TextMeshPro
using System.Collections.Generic;

public class NPCDialogue : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private DialogueTree npcDialogueTree; // NPC's dialogue tree reference
    [SerializeField] private string dialogueKey = "DialogueTriggered";

    [Header("Testing Purposes")]
    [SerializeField] private bool playerInRange = false; // Is player in range?
    [SerializeField] private bool isDialoguePressed;

    [Header("Keybinds")]
    [SerializeField] private KeyCode interactKey = KeyCode.E; // Interaction key
    [SerializeField] private KeyCode clearPlayerPrefs = KeyCode.C;
    [SerializeField] private GameObject interactTextPrefab; // Prefab for interaction text

    private GameObject interactTextInstance; // Reference to instantiated text

    private Transform player; // Reference to the player's transform

    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>(); // List of bool keys that should be true
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>(); // List of bool keys that should be false

    private void Start()
    {
        isDialoguePressed = PlayerPrefs.GetInt(dialogueKey, 0) == 1;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.transform; // Store player reference
            Debug.Log("Player entered NPC trigger zone. Press " + interactKey + " to interact.");

            // Prevent text from appearing if dialogue is already triggered
            if (!isDialoguePressed && interactTextPrefab != null && interactTextInstance == null && CanStartDialogue())
            {
                interactTextInstance = Instantiate(interactTextPrefab);
                interactTextInstance.transform.SetParent(transform, false); // Keep local position
                interactTextInstance.transform.localPosition = new Vector3(0, 0.5f, 0); // Position 0.5 above NPC

                // Update text dynamically to match the interact key
                TextMeshPro textMesh = interactTextInstance.GetComponent<TextMeshPro>();
                if (textMesh != null)
                {
                    textMesh.text = $"[{interactKey}] to Interact";
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            player = null;
            Debug.Log("Player exited NPC trigger zone.");

            if (interactTextInstance != null)
            {
                Destroy(interactTextInstance);
                interactTextInstance = null;
            }
        }
    }

    private void Update()
    {
        if (playerInRange && interactTextInstance != null && player != null)
        {
            // Make the text only rotate left and right (Y-axis only)
            Vector3 lookDirection = player.position - interactTextInstance.transform.position;
            lookDirection.y = 0; // Ignore vertical rotation
            interactTextInstance.transform.forward = -lookDirection.normalized; // Fix backwards issue
        }

        if (playerInRange && Input.GetKeyDown(interactKey) && !isDialoguePressed && CanStartDialogue())
        {
            isDialoguePressed = true;
            PlayerPrefs.SetInt(dialogueKey, 1);
            PlayerPrefs.Save();
            StartDialogue();

            // Remove interact text when dialogue starts
            if (interactTextInstance != null)
            {
                Destroy(interactTextInstance);
                interactTextInstance = null;
            }
        }

        if (Input.GetKeyDown(clearPlayerPrefs))
        {
            ClearPlayerPrefs();
        }
    }

    private bool CanStartDialogue()
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

    private void StartDialogue()
    {
        if (npcDialogueTree != null)
        {
            DialogueManager.Instance.StartDialogue(npcDialogueTree);
        }
    }

    private void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs cleared!");
        isDialoguePressed = false;
    }
}