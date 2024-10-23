using UnityEngine;

public class NPCDialogue : MonoBehaviour
{
    [SerializeField] private DialogueTree npcDialogueTree; // Reference to the NPC's dialogue tree
    [SerializeField] private KeyCode interactKey = KeyCode.E; // Public key for interaction, can be set in Inspector
    [SerializeField] private bool playerInRange = false; // Tracks if the player is in range to interact
    [SerializeField] private string dialogueKey = "DialogueTriggered";
    private bool isDialoguePressed;

    [SerializeField] private KeyCode clearPlayerPrefs = KeyCode.C;

    private void Start()
    {
        isDialoguePressed = PlayerPrefs.GetInt(dialogueKey, 0) == 1;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player has entered the trigger zone
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("Player entered NPC trigger zone. Press " + interactKey + " to interact.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the player has exited the trigger zone
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("Player exited NPC trigger zone.");
        }
    }

    private void Update()
    {
        // If player is in range and presses the assigned key, start dialogue
        if (playerInRange && Input.GetKeyDown(interactKey) && !isDialoguePressed)
        {
            isDialoguePressed = true;
            PlayerPrefs.SetInt(dialogueKey, 1);
            PlayerPrefs.Save();
            StartDialogue();
        }

        // Call this only for testing purposes to clear PlayerPrefs
        if (Input.GetKeyDown(clearPlayerPrefs))
        {
            ClearPlayerPrefs();
        }
    }

    private void StartDialogue()
    {
        // Call the DialogueManager to start the NPC's dialogue
        if (npcDialogueTree != null)
        {
            DialogueManager.Instance.StartDialogue(npcDialogueTree);
        }
    }

    // Function to clear all PlayerPrefs (use for testing purposes)
    private void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll(); // This will delete all stored PlayerPrefs
        PlayerPrefs.Save(); // Save changes
        Debug.Log("PlayerPrefs cleared!");
        isDialoguePressed = false; // Reset the local variable as well
    }
}