using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [SerializeField] private TMP_Text dialogueTextUI;
    [SerializeField] private TMP_Text npcNameUI;
    [SerializeField] private Canvas dialogueCanvas;
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Transform buttonParent;
    [SerializeField] private Image nextDialogueIndicatorImage;
    [SerializeField] private CanvasGroup nextDialogueIndicatorCanvasGroup;
    [SerializeField] private Camera mainCamera;  // Player camera

    public KeyCode advanceKey = KeyCode.Space;

    private DialogueTree currentDialogueTree;
    private int currentIndex = 0;
    [SerializeField] private bool isTextScrolling = false;
    [SerializeField] private bool isFullTextShown = false;
    [SerializeField] private bool optionsAreVisible = false;
    private Coroutine scrollingCoroutine;
    private List<GameObject> instantiatedButtons = new List<GameObject>();

    private FMOD.Studio.EventInstance currentDialogueEvent;
    private Coroutine indicatorCoroutine;

    private DialogueTree originalDialogueTree;
    private int originalIndex;
    private bool returningToOriginal = false;

    private PlayerMovement playerMovement;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (isFullTextShown && indicatorCoroutine == null)
        {
            indicatorCoroutine = StartCoroutine(FadeInAndOutIndicator());
        }
        else if (!isFullTextShown && indicatorCoroutine != null)
        {
            StopCoroutine(indicatorCoroutine);
            indicatorCoroutine = null; // Clear reference
            nextDialogueIndicatorCanvasGroup.alpha = 0f;
        }
        if (!optionsAreVisible)
        {
            if (Input.GetKeyDown(advanceKey))
            {
                if (isTextScrolling)
                {
                    StopCoroutine(scrollingCoroutine);
                    dialogueTextUI.text = currentDialogueTree.dialogueNodes[currentIndex - 1].dialogueText;
                    isTextScrolling = false;
                    isFullTextShown = true;
                    ShowOptions(currentDialogueTree.dialogueNodes[currentIndex - 1]);
                }
                else if (isFullTextShown)
                {
                    ShowNextDialogue();
                }
            }
        }
    }

    public void StartDialogue(DialogueTree dialogueTree)
    {
        // Disable the InventoryManager when dialogue starts
        if (inventoryManager != null)
        {
            inventoryManager.enabled = false;
            inventoryCanvas.gameObject.SetActive(false);
        }
        // Find the Player object by tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();

            // Disable player movement when dialogue starts
            if (playerMovement != null)
            {
                Debug.Log("Disabling movement");
                playerMovement.SetMovementState(false);
            }
            else
            {
                Debug.LogWarning("PlayerMovement component not found on Player object.");
            }
        }
        else
        {
            Debug.LogWarning("Player object not found with tag 'Player'.");
        }
        nextDialogueIndicatorCanvasGroup.alpha = 0f;
        currentDialogueTree = dialogueTree;
        currentIndex = 0;
        dialogueCanvas.enabled = true;
        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        StopCoroutine(FadeInAndOutIndicator());
        nextDialogueIndicatorCanvasGroup.alpha = 0f;
        ClearOptions();

        // Ensure we trigger events from the previous node before moving to the next
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
            nextDialogueIndicatorCanvasGroup.alpha = 0f;
            nextDialogueIndicatorImage.gameObject.SetActive(false);
            dialogueCanvas.enabled = false;
            if (playerMovement != null)
            {
                playerMovement.SetMovementState(true);
            }
            // Enable the InventoryManager when dialogue ends
            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
                inventoryCanvas.gameObject.SetActive(true);
            }
        }
    }

    private void DisplayDialogue(DialogueNode node)
    {
        if (scrollingCoroutine != null)
        {
            StopCoroutine(scrollingCoroutine);
        }

        scrollingCoroutine = StartCoroutine(ScrollText(node.dialogueText));

        if (npcNameUI != null)
        {
            npcNameUI.text = node.npcName;
        }

        if (currentDialogueEvent.isValid())
        {
            currentDialogueEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            currentDialogueEvent.release();
        }

        if (!node.fmodAudioEvent.IsNull)
        {
            currentDialogueEvent = RuntimeManager.CreateInstance(node.fmodAudioEvent);
            currentDialogueEvent.start();
        }

        // Trigger the camera look at the NPC if npcTag is provided
        TriggerCameraLookAtNpc(node);

        //foreach (var eventId in node.eventIds)
        //{
        //    if (!string.IsNullOrEmpty(eventId))
        //    {
        //        DialogueEventManager.Instance?.TriggerDialogueEvent(eventId);
        //    }
        //}
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

                // If we found the closest NPC, start the camera smooth look-at
                if (closestNpc != null)
                {
                    StartCoroutine(SmoothLookAtNpc(closestNpc));
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

                // Adjust the mouseY position in the CameraController to match the xRotation for consistency
                cameraController.xRotation = targetPitch;

                yield return null;
            }

            // Final alignment with the NPC (ensure no overshooting)
            mainCamera.transform.rotation = targetRotation;
            player.transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y, 0);
        }
    }

    private IEnumerator ScrollText(string fullText)
    {
        isTextScrolling = true;
        dialogueTextUI.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            dialogueTextUI.text += fullText[i];
            yield return new WaitForSeconds(0.05f);
        }

        isTextScrolling = false;
        isFullTextShown = true; // Text is fully shown now
        ShowOptions(currentDialogueTree.dialogueNodes[currentIndex - 1]);
    }

    private IEnumerator FadeInAndOutIndicator()
    {
        nextDialogueIndicatorCanvasGroup.alpha = 1f;
        nextDialogueIndicatorImage.gameObject.SetActive(true);

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

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void ClearOptions()
    {
        foreach (var button in instantiatedButtons)
        {
            Destroy(button);
        }
        instantiatedButtons.Clear();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        optionsAreVisible = false;
    }
}