using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private CinematicSequence chapter1CinematicSequence;
    [SerializeField] private CinematicSequence chapter2CinematicSequence;
    [SerializeField] private CinematicSequence chapter3CinematicSequence;
    [SerializeField] private CinematicSequence level3Flashback;

    [SerializeField] private SceneTransitionController sceneTransition;
   [SerializeField] private string sceneToLoad = "MainMenu";
    [SerializeField] private string boolName = "";
    [SerializeField] private bool returnToMainMenuTriggered = false;

   [SerializeField] private GameObject loadPanel;
    [SerializeField] private Image loadImage;
    [SerializeField] private float rotationSpeed = 100f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        switch (scene.name)
        {
            case "Level1":
                LoadLevel1Cinematic();
                break;
        }
    }

    public void ShowLoadingPanel()
    {
        StartCoroutine(ShowLoadPanelAndStartRotation());
    }

    public void HideLoadingPanel()
    {
        StartCoroutine(HideLoadPanel());
    }

    private IEnumerator ShowLoadPanelAndStartRotation()
    {
        loadPanel.SetActive(true);

        float fadeDuration = 1f;
        float timeElapsed = 0f;
        CanvasGroup panelCanvasGroup = loadPanel.GetComponent<CanvasGroup>();
        while (timeElapsed < fadeDuration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timeElapsed / fadeDuration);
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        panelCanvasGroup.alpha = 1f;

        while (true)
        {
            loadImage.transform.Rotate(Vector3.forward, rotationSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
    }

    private IEnumerator HideLoadPanel()
    {
        float fadeDuration = 1f;
        float timeElapsed = 0f;
        CanvasGroup panelCanvasGroup = loadPanel.GetComponent<CanvasGroup>();
        while (timeElapsed < fadeDuration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timeElapsed / fadeDuration);
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        panelCanvasGroup.alpha = 0f;

        loadPanel.SetActive(false);
    }

    void Start()
    {
      
    }

    void Update()
    {
        if (BoolManager.Instance.GetBool(boolName) && !returnToMainMenuTriggered)
        {
            if (sceneTransition != null)
            {
                returnToMainMenuTriggered = true;
                sceneTransition.StartTransition(sceneToLoad);
            }
        }
    }

    public void LoadLevel1Cinematic()
    {
        chapter1CinematicSequence.StartCinematic();
    }

    public void LoadLevel2Cinematic()
    {
        chapter2CinematicSequence.StartCinematic();
    }

    public void LoadLevel3Cinematic()
    {
        chapter3CinematicSequence.StartCinematic();
    }
    public void StartLevel3Flashback()
    {
        level3Flashback.StartCinematic();
    }
}
