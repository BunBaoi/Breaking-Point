using UnityEngine;
using TMPro;
using System.Collections;
using FMODUnity;
using System.Collections.Generic;
using Cinemachine;

public class CinematicSequence : MonoBehaviour
{
    [Header("Cinematic Data")]
    [SerializeField] private CinematicData cinematicData;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text chapterText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Canvas canvas;

    [Header("Dialogue Display Settings")]
    [SerializeField] private float dialogueSpeedFactor = 0.05f;
    [SerializeField] private float randomTextMinDuration = 5f;
    [SerializeField] private float randomTextMaxDuration = 10f;
    [SerializeField] private float chapterDisplayDuration = 3f;
    [SerializeField] private float dialogueDelayDuration = 1.5f;
    private List<RectTransform> activeRandomTexts = new List<RectTransform>();

    [Header("Fade Settings")]
    [SerializeField] private float textFadeOutDuration = 1f;
    [SerializeField] private float textFadeInDuration = 1f;

    [Header("Camera")]
    [SerializeField] private CinemachineVirtualCamera cinematicCameraPrefab;
    [SerializeField] private float cameraPanDuration = 2f;
    [SerializeField] private float cameraRemovalDelay = 0.5f;
    private CinemachineVirtualCamera instantiatedCamera;
    private List<Camera> disabledCameras = new List<Camera>();

    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>();
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>();
    [SerializeField] private float eventIdsDelay = 0.5f;

    [Header("Other Scripts")]
    private PlayerMovement playerMovement;
    private CameraController cameraController;
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;
    [SerializeField] private DayNightCycle dayNightCycle;

    public event System.Action OnCinematicFinished;
    public event System.Action OnCinematicStarted;
    public static bool IsCinematicActive { get; private set; } = false;

    private void Start()
    {
        canvas.gameObject.SetActive(false);
    }

    public void StartCinematic()
    {
        if (AreConditionsMet())
        {
            SettingsManager.Instance.SetCinematicActive(true);
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerMovement = playerObject.GetComponent<PlayerMovement>();

                if (playerMovement != null)
                {
                    playerMovement.SetMovementState(false);
                }
                inventoryManager = playerObject.GetComponent<InventoryManager>();
                if (inventoryManager != null)
                {
                    inventoryManager.enabled = false;
                }
                Transform inventoryCanvasTransform = playerObject.transform.Find("Inventory Canvas");
                if (inventoryCanvasTransform != null)
                {
                    inventoryCanvas = inventoryCanvasTransform.GetComponent<Canvas>();
                    if (inventoryCanvas != null)
                    {
                        inventoryCanvas.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogWarning("Player object not found with tag 'Player'.");
            }
            GameObject sun = GameObject.FindGameObjectWithTag("Sun");
            dayNightCycle = sun.GetComponent<DayNightCycle>();
            if (dayNightCycle != null)
            {
                dayNightCycle.StopTime();
            }
            GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
            if (playerCamera !=null)
            {
                cameraController = playerCamera.GetComponent<CameraController>();
                if (cameraController != null)
                {
                    cameraController.SetLookState(false);
                }
            }
            // DisableAllCameras();
            IsCinematicActive = true;
            StartCoroutine(PlayCinematic());

            instantiatedCamera = Instantiate(cinematicCameraPrefab);

            Transform pointATransform = GameObject.Find(cinematicData.pointA)?.transform;

            if (pointATransform != null)
            {
                instantiatedCamera.transform.position = pointATransform.position;
                instantiatedCamera.transform.rotation = pointATransform.rotation;
            }
            else
            {
                Debug.LogWarning("Point A GameObject not found! Camera will use default position.");
            }

            instantiatedCamera.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Conditions not met for cinematic sequence.");
        }
    }

    private void DisableAllCameras()
    {
        Camera[] allCameras = FindObjectsOfType<Camera>(true);

        foreach (Camera cam in allCameras)
        {
            if (cam != instantiatedCamera)
            {
                cam.enabled = false;
                disabledCameras.Add(cam);
            }
        }

        Debug.Log("All non-cinematic cameras disabled.");
    }

    private void EnableAllCameras()
    {
        foreach (Camera cam in disabledCameras)
        {
            if (cam != null)
            {
                cam.enabled = true;
            }
        }

        disabledCameras.Clear();

        Debug.Log("All disabled cameras re-enabled.");
    }

    private bool AreConditionsMet()
    {
        foreach (string key in requiredBoolKeysTrue)
        {
            if (!BoolManager.Instance.GetBool(key))
            {
                Debug.Log($"Required bool key '{key}' is false.");
                return false;
            }
        }

        foreach (string key in requiredBoolKeysFalse)
        {
            if (BoolManager.Instance.GetBool(key))
            {
                Debug.Log($"Required bool key '{key}' is true.");
                return false;
            }
        }

        return true; // All conditions are met
    }

    private IEnumerator PlayCinematic()
    {
        Debug.Log("Starting cinematic fade-in");
        canvas.gameObject.SetActive(true);
        // yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 1f));

        // Play each dialogue in order
        for (int i = 0; i < cinematicData.dialoguesAndAudio.Length; i++)
        {
            CinematicDataDialogueAudio dialogueAudio = cinematicData.dialoguesAndAudio[i];
            Debug.Log($"Starting dialogue {i + 1} of {cinematicData.dialoguesAndAudio.Length}");

            if (i < cinematicData.randomTexts.Length && cinematicData.randomTexts[i] != null)
            {
                ShowRandomText(cinematicData.randomTexts[i]);
            }
            else
            {
                Debug.LogWarning($"text for dialogue index {i} is null.");
            }

            bool dialogueFinished = false;
            yield return StartCoroutine(DisplayDialogue(dialogueAudio.dialogue, dialogueAudio.npcName, dialogueAudio.dialogueAudio, () => dialogueFinished = true));

            while (!dialogueFinished)
            {
                yield return null;
            }

            Debug.Log($"Audio and dialogue finished for dialogue {i + 1}");

            yield return StartCoroutine(FadeText(dialogueText, textFadeOutDuration, 0f));

            dialogueText.text = string.Empty;

            if (i + 1 < cinematicData.dialoguesAndAudio.Length)
            {
                Debug.Log("Waiting before next dialogue");
                yield return new WaitForSeconds(dialogueDelayDuration);

                yield return StartCoroutine(FadeText(dialogueText, textFadeInDuration, 1f));
            }
        }

        yield return StartCoroutine(ShowChapterText());

        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f));

        yield return StartCoroutine(PanCamera());
    }

    private IEnumerator DisplayDialogue(string dialogue, string npcName, DialogueAudio dialogueAudio, System.Action onDialogueComplete)
    {
        if (dialogueText == null)
        {
            Debug.LogError("Dialogue Text is not assigned!");
            yield break;
        }

        dialogueText.text = string.Empty;

        int npcNameHash = npcName.GetHashCode();
        int letterIndex = 0;

        FMOD.Studio.EventInstance? currentAudioEvent = null;

        foreach (char letter in dialogue)
        {
            dialogueText.text += letter;

            char lowerLetter = char.ToLower(letter);
            int audioIndex = lowerLetter - 'a';

            // Play sound based on frequency
            if (audioIndex >= 0 && audioIndex < dialogueAudio.fmodSoundEvents.Length && letterIndex % dialogueAudio.frequency == 0)
            {
                if (currentAudioEvent.HasValue)
                {
                    currentAudioEvent.Value.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                }

                // Play the new sound event
                EventReference soundEvent = dialogueAudio.fmodSoundEvents[audioIndex];
                currentAudioEvent = PlayDialogueSoundEvent(soundEvent);
            }

            letterIndex++;
            yield return new WaitForSeconds(dialogueSpeedFactor);
        }

        yield return new WaitForSeconds(dialogueDelayDuration);

        Debug.Log("Dialogue display complete");

        onDialogueComplete?.Invoke();
    }

    private FMOD.Studio.EventInstance PlayDialogueSoundEvent(EventReference soundEvent)
    {
        FMOD.Studio.EventInstance eventInstance = RuntimeManager.CreateInstance(soundEvent);

        // Get the FMOD listener
        GameObject listener = GameObject.FindGameObjectWithTag("PlayerCamera");

        if (listener != null)
        {
            // Set the 3D attributes to the listener's position
            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(listener.transform));
        }
        else
        {
            Debug.LogWarning("Listener not found! Sound will not be positioned.");
        }

        eventInstance.start();
        return eventInstance;
    }

    private IEnumerator ShowChapterText()
    {
        HideRandomText();

        chapterText.gameObject.SetActive(true);
        chapterText.text = cinematicData.chapterName;
        chapterText.color = new Color(chapterText.color.r, chapterText.color.g, chapterText.color.b, 0);

        yield return StartCoroutine(FadeText(chapterText, textFadeInDuration, 1f));

        yield return new WaitForSeconds(chapterDisplayDuration);

        yield return StartCoroutine(FadeText(chapterText, textFadeOutDuration, 0f));

        OnCinematicStarted?.Invoke();
    }

    private void ShowRandomText(string text)
    {
        GameObject randomTextObject = Instantiate(cinematicData.randomTextPrefab, canvas.transform);
        TMP_Text randomText = randomTextObject.GetComponent<TMP_Text>();
        randomText.text = text;

        RectTransform rectTransform = randomTextObject.GetComponent<RectTransform>();

        Vector2 newPosition;
        int maxAttempts = 10;
        int attempts = 0;
        bool positionFound = false;

        do
        {
            newPosition = new Vector2(Random.Range(-300, 300), Random.Range(-200, 200));
            positionFound = !IsOverlapping(newPosition, rectTransform.sizeDelta);
            attempts++;
        } while (!positionFound && attempts < maxAttempts);

        rectTransform.anchoredPosition = newPosition;

        rectTransform.localRotation = Quaternion.Euler(0, 0, Random.Range(-40f, 40f));

        activeRandomTexts.Add(rectTransform);

        StartCoroutine(FadeText(randomText, 1f, 1f));

        float randomDuration = Random.Range(randomTextMinDuration, randomTextMaxDuration);

        StartCoroutine(HideRandomTextAfterDelay(randomTextObject, randomDuration));
    }

    private bool IsOverlapping(Vector2 position, Vector2 size)
    {
        Rect newRect = new Rect(position, size);

        foreach (RectTransform existingText in activeRandomTexts)
        {
            Rect existingRect = new Rect(existingText.anchoredPosition, existingText.sizeDelta);
            if (newRect.Overlaps(existingRect))
            {
                return true; // Overlapping detected
            }
        }
        return false; // No overlap
    }

    private IEnumerator HideRandomTextAfterDelay(GameObject randomTextObject, float duration)
    {
        yield return new WaitForSeconds(duration);

        TMP_Text randomText = randomTextObject.GetComponent<TMP_Text>();
        StartCoroutine(FadeText(randomText, 1f, 0f));

        activeRandomTexts.Remove(randomTextObject.GetComponent<RectTransform>());

        Destroy(randomTextObject, 1f);
    }

    private void HideRandomText()
    {
        foreach (Transform child in canvas.transform)
        {
            if (child.CompareTag("RandomText"))
            {
                StartCoroutine(FadeText(child.GetComponent<TMP_Text>(), 1f, 0f));
                Destroy(child.gameObject, 1f);
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

        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator PanCamera()
    {
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
        Vector3 startPosition = cameraPointA.transform.position;
        Vector3 endPosition = cameraPointB.transform.position;
        Quaternion startRotation = cameraPointA.transform.rotation;
        Quaternion endRotation = cameraPointB.transform.rotation;
        float panDuration = cameraPanDuration;

        while (elapsedTime < panDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / panDuration;

            instantiatedCamera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            instantiatedCamera.transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);

            yield return null;
        }

        instantiatedCamera.transform.position = endPosition;
        instantiatedCamera.transform.rotation = endRotation;

        StartCoroutine(RemoveCameraAfterDelay());

    }

    private IEnumerator RemoveCameraAfterDelay()
    {
        yield return new WaitForSeconds(cameraRemovalDelay);

        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");

        if (playerCamera != null)
        {
            CinemachineBrain brain = playerCamera.GetComponent<CinemachineBrain>();

            if (brain != null && instantiatedCamera != null)
            {
                CinemachineVirtualCamera defaultCamera = GameObject.Find("Player Virtual Camera").GetComponent<CinemachineVirtualCamera>();

                CinemachineBlendDefinition blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 1f);

                brain.m_DefaultBlend = blend;

                instantiatedCamera.gameObject.SetActive(false);

                defaultCamera.gameObject.SetActive(true);

                yield return new WaitForSeconds(blend.m_Time);

                Destroy(instantiatedCamera.gameObject);
                Debug.Log("Cinematic camera removed.");

                yield return new WaitForSeconds(0.1f);
                StartCoroutine(TriggerEventIDs());
            }
            else
            {
                Debug.LogWarning("CinemachineBrain or Cinematic camera is not assigned!");
            }
        }
        else
        {
            Debug.LogWarning("PlayerCamera with tag not found!");
        }
    }

    private IEnumerator TriggerEventIDs()
    {
        yield return new WaitForSeconds(eventIdsDelay);

        foreach (var eventId in cinematicData.eventIds)
        {
            if (!string.IsNullOrEmpty(eventId))
            {
                CinematicEventManager.Instance?.TriggerCinematicEvent(eventId);
            }
        }
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();

            if (playerMovement != null)
            {
                playerMovement.SetMovementState(true);
            }
            inventoryManager = playerObject.GetComponent<InventoryManager>();
            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
            }
            Transform inventoryCanvasTransform = playerObject.transform.Find("Inventory Canvas");
            if (inventoryCanvasTransform != null)
            {
                inventoryCanvas = inventoryCanvasTransform.GetComponent<Canvas>();
                if (inventoryCanvas != null)
                {
                    inventoryCanvas.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            Debug.LogWarning("Player object not found with tag 'Player'.");
        }
        GameObject playerCamera = GameObject.FindGameObjectWithTag("PlayerCamera");
        if (playerCamera != null)
        {
            cameraController = playerCamera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.SetLookState(true);
            }
        }
        GameObject sun = GameObject.FindGameObjectWithTag("Sun");
        dayNightCycle = sun.GetComponent<DayNightCycle>();
        if (dayNightCycle != null)
        {
            dayNightCycle.StartTime();
        }

        SettingsManager.Instance.SetCinematicActive(false);

        canvas.gameObject.SetActive(false);
        IsCinematicActive = false;
        OnCinematicFinished?.Invoke();
    }
}
