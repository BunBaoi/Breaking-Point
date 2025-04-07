using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private CanvasGroup introCanvasGroup;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;

    [SerializeField] private float fadeDuration = 1f; // Duration for the fade effect

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string anyKeyPressActionName = "AnyKeyPress";
    private InputAction anyKeyPressAction;

    [SerializeField] private float textFadeDuration = 1f;
    [SerializeField] private float waitDuration = 0.5f;
    [SerializeField] private TMP_Text pressAnyKeyText;

    private void Awake()
    {
        anyKeyPressAction = inputActions.FindAction(anyKeyPressActionName);

        if (anyKeyPressAction != null)
        {
            anyKeyPressAction.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{anyKeyPressAction}' not found in Input Action Asset!");
        }

        anyKeyPressAction.performed += OnAnyKeyPress;
    }

    private void Start()
    {
        StartCoroutine(FadeTextInOut());

        introCanvasGroup.alpha = 1f;
        mainMenuCanvasGroup.alpha = 0f;

        introPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (introPanel.activeSelf || mainMenuPanel.activeSelf)
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private IEnumerator FadeTextInOut()
    {
        while (true)
        {
            // Fade In
            yield return StartCoroutine(FadeTextAlpha(0f, 1f, textFadeDuration));
            yield return new WaitForSecondsRealtime(waitDuration);

            // Fade Out
            yield return StartCoroutine(FadeTextAlpha(1f, 0f, textFadeDuration));
            yield return new WaitForSecondsRealtime(waitDuration);
        }
    }

    private IEnumerator FadeTextAlpha(float startAlpha, float endAlpha, float duration)
    {
        float timeElapsed = 0f;
        Color currentColor = pressAnyKeyText.color;
        float initialAlpha = currentColor.a;

        while (timeElapsed < duration)
        {
            float alpha = Mathf.Lerp(initialAlpha, endAlpha, timeElapsed / duration);
            pressAnyKeyText.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        pressAnyKeyText.color = new Color(currentColor.r, currentColor.g, currentColor.b, endAlpha);
    }

    private void OnDisable()
    {
        anyKeyPressAction.performed -= OnAnyKeyPress;
        anyKeyPressAction.Disable();
    }

    public void OnAnyKeyPress(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            mainMenuPanel.SetActive(true);
            StartCoroutine(FadeOutIntroAndFadeInMainMenu());
        }
    }

    private IEnumerator FadeOutIntroAndFadeInMainMenu()
    {
        float timeElapsed = 0f;

        // Fade out the intro panel
        while (timeElapsed < fadeDuration)
        {
            introCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timeElapsed / fadeDuration);
            mainMenuCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timeElapsed / fadeDuration);
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        introCanvasGroup.alpha = 0f;
        mainMenuCanvasGroup.alpha = 1f;

        introPanel.SetActive(false);
    }

    public void LoadGame()
    {
        SaveManager.Instance.LoadGame();
    }

    public void OpenSettings()
    {
        SettingsManager.Instance.ToggleMenu();
    }
}
