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
        if (currentDialogueTree != null && currentIndex < currentDialogueTree.dialogueNodes.Count)
        {
            isFullTextShown = false;
            DisplayDialogue(currentDialogueTree.dialogueNodes[currentIndex]);
            currentIndex++;
        }
        else
        {
            nextDialogueIndicatorCanvasGroup.alpha = 0f;
            nextDialogueIndicatorImage.gameObject.SetActive(false);
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

        foreach (var eventId in node.eventIds)
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                DialogueEventManager.Instance?.TriggerDialogueEvent(eventId);
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