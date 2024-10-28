using UnityEngine;
using TMPro;
using System.Collections;
using FMODUnity;

public class CinematicSequence : MonoBehaviour
{
    [Header("Cinematic Data")]
    [SerializeField] private CinematicData cinematicData;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text chapterText; // Chapter Title Text
    [SerializeField] private TMP_Text dialogueText; // Dialogue Text
    [SerializeField] private CanvasGroup canvasGroup; // Canvas Group for Background Image
    [SerializeField] private Canvas canvas; // Canvas for all the UI

    [Header("Dialogue Display Settings")]
    [SerializeField] private float dialogueSpeedFactor = 0.05f; // Speed factor for dialogue text appearance
    [SerializeField] private float randomTextMinDuration = 5f; // Min duration time for random text
    [SerializeField] private float randomTextMaxDuration = 10f; // Max duration for random text
    [SerializeField] private float chapterDisplayDuration = 3f; // Duration to show the chapter title
    [SerializeField] private float dialogueDelayDuration = 1.5f; // Duration to wait after displaying current dialogue

    [Header("Fade Settings")]
    [SerializeField] private float textFadeOutDuration = 1f; // Duration for fading out text
    [SerializeField] private float textFadeInDuration = 1f; // Duration for fading in text

    [Header("Camera")]
    [SerializeField] private Camera cinematicCamera;
    [SerializeField] private float cameraPanDuration = 2f;

    private void Start()
    {
        StartCoroutine(PlayCinematic());
    }

    private IEnumerator PlayCinematic()
    {
        // Fade in the canvas group at the start
        Debug.Log("Starting cinematic fade-in");
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 1f));

        // Play each dialogue in order
        for (int i = 0; i < cinematicData.dialoguesAndAudio.Length; i++)
        {
            CinematicDataDialogueAudio dialogueAudio = cinematicData.dialoguesAndAudio[i];
            Debug.Log($"Starting dialogue {i + 1} of {cinematicData.dialoguesAndAudio.Length}");

            // Show the corresponding random text to dialogue if there is one
            if (i < cinematicData.randomTexts.Length && cinematicData.randomTexts[i] != null)
            {
                ShowRandomText(cinematicData.randomTexts[i]);
            }
            else
            {
                Debug.LogWarning($"Random text for dialogue index {i} is null.");
            }

            // Create an FMOD event instance and start the audio
            FMOD.Studio.EventInstance audioEventInstance = RuntimeManager.CreateInstance(dialogueAudio.fmodAudioEvent);
            audioEventInstance.start();

            // Start displaying dialogue
            bool dialogueFinished = false;
            StartCoroutine(DisplayDialogue(dialogueAudio.dialogue, () => dialogueFinished = true));

            // Wait until both the audio finishes playing and the dialogue is fully displayed
            FMOD.Studio.PLAYBACK_STATE playbackState;
            do
            {
                audioEventInstance.getPlaybackState(out playbackState);
                yield return null; // Wait for the next frame
            } while (playbackState == FMOD.Studio.PLAYBACK_STATE.PLAYING || !dialogueFinished);

            Debug.Log($"Audio and dialogue finished for dialogue {i + 1}");

            // Fade out the dialogue text after audio finishes
            yield return StartCoroutine(FadeText(dialogueText, textFadeOutDuration, 0f));

            // Clear the dialogue text before the next one
            dialogueText.text = string.Empty;

            // Wait before the next dialogue appears (if there is another dialogue)
            if (i + 1 < cinematicData.dialoguesAndAudio.Length)
            {
                Debug.Log("Waiting before next dialogue");
                yield return new WaitForSeconds(dialogueDelayDuration);

                // Fade in the dialogue text before displaying the next dialogue
                yield return StartCoroutine(FadeText(dialogueText, textFadeInDuration, 1f));
            }

            // Release the audio event instance to free resources
            audioEventInstance.release();
        }

        // Show chapter title after all dialogues
        yield return StartCoroutine(ShowChapterText());

        // Fade out canvas group
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f));

        // Start Camera Pan
        yield return StartCoroutine(PanCamera());
    }

    private IEnumerator DisplayDialogue(string dialogue, System.Action onDialogueComplete)
    {
        if (dialogueText == null)
        {
            Debug.LogError("Dialogue Text is not assigned!");
            yield break;
        }

        dialogueText.text = string.Empty; // Clear text before showing new dialogue

        // Gradually reveal the dialogue
        foreach (char letter in dialogue)
        {
            dialogueText.text += letter; // Append each letter
            yield return new WaitForSeconds(dialogueSpeedFactor); // Wait based on the speed factor
        }

        // Wait a moment to display the full dialogue before finishing
        yield return new WaitForSeconds(dialogueDelayDuration);

        Debug.Log("Dialogue display complete");

        // Trigger the callback to mark the dialogue as complete
        onDialogueComplete?.Invoke();
    }

    private IEnumerator ShowChapterText()
    {
        HideRandomText(); // Hide random text immediately before showing chapter text

        chapterText.gameObject.SetActive(true);
        chapterText.text = cinematicData.chapterName; // Use the chapter name from the ScriptableObject
        chapterText.color = new Color(chapterText.color.r, chapterText.color.g, chapterText.color.b, 0);

        // Fade in chapter text
        yield return StartCoroutine(FadeText(chapterText, textFadeInDuration, 1f));

        // Wait to show the chapter title
        yield return new WaitForSeconds(chapterDisplayDuration);

        // Fade out the chapter text, using the serialized field
        yield return StartCoroutine(FadeText(chapterText, textFadeOutDuration, 0f));
    }

    private void ShowRandomText(string text)
    {
        // Instantiate the random text prefab from the ScriptableObject
        GameObject randomTextObject = Instantiate(cinematicData.randomTextPrefab, canvas.transform);
        TMP_Text randomText = randomTextObject.GetComponent<TMP_Text>();
        randomText.text = text;

        // Set random position on the canvas
        RectTransform rectTransform = randomTextObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(Random.Range(-300, 300), Random.Range(-200, 200)); // Random position

        // Fade in
        StartCoroutine(FadeText(randomText, 1f, 1f));

        // Calculate a random duration between min and max
        float randomDuration = Random.Range(randomTextMinDuration, randomTextMaxDuration);

        // Wait for the random duration before hiding the text
        StartCoroutine(HideRandomTextAfterDelay(randomTextObject, randomDuration));
    }

    private IEnumerator HideRandomTextAfterDelay(GameObject randomTextObject, float duration)
    {
        yield return new WaitForSeconds(duration); // Wait for the specified duration

        // Fade out random text after waiting
        TMP_Text randomText = randomTextObject.GetComponent<TMP_Text>();
        StartCoroutine(FadeText(randomText, 1f, 0f));

        // Destroy the random text object after fading out
        Destroy(randomTextObject, 1f); // Delay destruction until after fading out
    }

    private void HideRandomText()
    {
        // Find all active random text objects and destroy them
        foreach (Transform child in canvas.transform)
        {
            if (child.CompareTag("RandomText")) // Find RandomText tag
            {
                StartCoroutine(FadeText(child.GetComponent<TMP_Text>(), 1f, 0f));
                Destroy(child.gameObject, 1f); // Delay destruction until after fading out
            }
        }
    }

    private IEnumerator FadeText(TMP_Text text, float duration, float targetAlpha)
    {
        float startAlpha = text.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            yield return null;
        }

        // Ensure the final alpha is set
        text.color = new Color(text.color.r, text.color.g, text.color.b, targetAlpha);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float duration, float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);
            yield return null;
        }

        // Ensure the final alpha is set
        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator PanCamera()
    {
        // Log the names being searched for
        Debug.Log($"Searching for camera points: {cinematicData.pointA} and {cinematicData.pointB}");
        Debug.Log($"Camera Point A: {cinematicData.pointA}, Camera Point B: {cinematicData.pointB}");

        GameObject cameraPointA = GameObject.Find(cinematicData.pointA);
        GameObject cameraPointB = GameObject.Find(cinematicData.pointB);

        if (cameraPointA == null)
        {
            Debug.LogError($"Camera point A not found: {cinematicData.pointA}");
            yield break; // Exit if point A is not found
        }

        if (cameraPointB == null)
        {
            Debug.LogError($"Camera point B not found: {cinematicData.pointB}");
            yield break; // Exit if point B is not found
        }

        float elapsedTime = 0f;
        Vector3 startPosition = cameraPointA.transform.position; // Starting position
        Vector3 endPosition = cameraPointB.transform.position; // Ending position
        Quaternion startRotation = cameraPointA.transform.rotation; // Starting rotation
        Quaternion endRotation = cameraPointB.transform.rotation; // Ending rotation
        float panDuration = cameraPanDuration; // Duration of the camera pan

        while (elapsedTime < panDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / panDuration; // Normalise the elapsed time

            // Smoothly interpolate between start and end positions and rotations using Lerp and Slerp
            cinematicCamera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            cinematicCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            yield return null; // Wait for the next frame
        }

        // Ensure the final position and rotation are set
        cinematicCamera.transform.position = endPosition;
        cinematicCamera.transform.rotation = endRotation;
    }
}
