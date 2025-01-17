using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public TMP_Text dialogueTextUI;
    public Canvas dialogueCanvas;
    public GameObject buttonPrefab;
    public Transform buttonParent;

    public KeyCode advanceKey = KeyCode.Space;

    private DialogueTree currentDialogueTree;
    private int currentIndex = 0;
    private bool isTextScrolling = false;
    private bool isFullTextShown = false;
    private bool optionsAreVisible = false;
    private Coroutine scrollingCoroutine;
    private List<GameObject> instantiatedButtons = new List<GameObject>();

    private FMOD.Studio.EventInstance currentDialogueEvent; // Store current FMOD event instance

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
        currentDialogueTree = dialogueTree;
        currentIndex = 0;
        dialogueCanvas.enabled = true;
        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        ClearOptions();
        if (currentDialogueTree != null && currentIndex < currentDialogueTree.dialogueNodes.Count)
        {
            isFullTextShown = false;
            DisplayDialogue(currentDialogueTree.dialogueNodes[currentIndex]);
            currentIndex++;
        }
        else
        {
            dialogueCanvas.enabled = false;
        }
    }

    private void DisplayDialogue(DialogueNode node)
    {
        if (scrollingCoroutine != null)
        {
            StopCoroutine(scrollingCoroutine);
        }

        scrollingCoroutine = StartCoroutine(ScrollText(node.dialogueText));

        // Stop the previous FMOD event if one is playing
        if (currentDialogueEvent.isValid())
        {
            currentDialogueEvent.stop(FMOD.Studio.STOP_MODE.IMMEDIATE); // Stop the previous event
            currentDialogueEvent.release(); // Release the event after stopping it
        }

        // Play FMOD event if available
        if (!node.fmodAudioEvent.IsNull)
        {
            currentDialogueEvent = RuntimeManager.CreateInstance(node.fmodAudioEvent);
            currentDialogueEvent.start();
        }

        // Trigger all Scene-based events using eventIDs
        foreach (var eventID in node.eventIDs)
        {
            if (!string.IsNullOrEmpty(eventID))
            {
                // Trigger the event tied to the eventID
                DialogueEventManager.Instance?.TriggerDialogueEvent(eventID);
            }
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
        isFullTextShown = true;
        ShowOptions(currentDialogueTree.dialogueNodes[currentIndex - 1]);
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

