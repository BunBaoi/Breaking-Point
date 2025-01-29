using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;

public class SettingsManager : MonoBehaviour
{
    [Header("Settings UI")]
    public GameObject settingsCanvas;

    [Header("Panels & Buttons")]
    public Button[] panelButtons;
    public GameObject[] panels;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string toggleMenuAction = "Pause";

    [Header("Scroll Text Speed")]
    [SerializeField] private TMP_InputField scrollSpeedInput;
    [SerializeField] private float scrollSpeed = 0.05f;

    [Header("Brightness Settings")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private PostProcessVolume postProcessingVolume;

    [Header("Mouse Sensitivity Settings")]
    [SerializeField] private TMP_InputField sensitivityInputField;
    public static float mouseSensitivity = 100f;

    [Header("Reset to Default Settings")]
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text resetMessageText;
    private Coroutine resetMessageCoroutine;

    private ColorGrading colorGrading;
    public static SettingsManager Instance;

    private InputAction pauseAction;
    private bool isMenuOpen = false;
    private int activePanelIndex = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Setup input actions
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");
            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }

        pauseAction = inputActions.FindAction(toggleMenuAction);
        if (pauseAction != null)
        {
            pauseAction.performed += ToggleMenu;
            pauseAction.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{toggleMenuAction}' not found in Input Action Asset!");
        }

        // Set default UI states
        foreach (var panel in panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        settingsCanvas.SetActive(isMenuOpen);

        // Panel buttons setup
        if (panelButtons.Length == panels.Length)
        {
            for (int i = 0; i < panelButtons.Length; i++)
            {
                int index = i;
                panelButtons[i].onClick.AddListener(() => OpenPanel(index));
            }
        }
        else
        {
            Debug.LogError("Mismatch between panel buttons and panels count!");
        }

        // Reset button setup
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefaultSettings); // Add listener to reset settings
        }
        else
        {
            Debug.LogError("Reset Button is not assigned in the Inspector!");
        }

        // Scroll speed settings
        if (scrollSpeedInput != null)
        {
            scrollSpeed = PlayerPrefs.GetFloat("ScrollSpeed", 0.05f);  // Load scroll speed from PlayerPrefs
            scrollSpeedInput.text = scrollSpeed.ToString("0.00");
            scrollSpeedInput.onEndEdit.AddListener(UpdateScrollSpeed);
        }

        // Brightness settings
        colorGrading = postProcessingVolume.profile.GetSetting<ColorGrading>();
        if (colorGrading != null)
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
            brightnessSlider.value = savedBrightness;
            UpdateBrightness(savedBrightness);
        }
        else
        {
            Debug.LogError("ColorGrading effect not found in the PostProcessVolume profile.");
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(UpdateBrightness);
        }

        // Sensitivity settings
        if (sensitivityInputField != null)
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 100f);  // Load sensitivity from PlayerPrefs
            sensitivityInputField.text = (mouseSensitivity / 100f).ToString("0.00");
            sensitivityInputField.onEndEdit.AddListener(UpdateSensitivity);
        }
    }

    void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= ToggleMenu;
        }

        foreach (var button in panelButtons)
        {
            button.onClick.RemoveAllListeners();
        }

        if (resetButton != null)
        {
            resetButton.onClick.RemoveListener(ResetToDefaultSettings); // Remove listener when destroyed
        }
    }

    void ToggleMenu(InputAction.CallbackContext context)
    {
        isMenuOpen = !isMenuOpen;
        settingsCanvas.SetActive(isMenuOpen);

        // Pause or resume time based on the menu state
        if (isMenuOpen)
        {
            Time.timeScale = 0f; // Pause the game
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f; // Resume the game
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Debug.Log("Settings Menu Toggled: " + isMenuOpen);
    }

    public void OpenPanel(int panelIndex)
    {
        if (panelIndex >= 0 && panelIndex < panels.Length)
        {
            if (activePanelIndex != -1)
            {
                panels[activePanelIndex].SetActive(false);
            }

            panels[panelIndex].SetActive(true);
            activePanelIndex = panelIndex;
            Debug.Log($"Opened Panel Index: {panelIndex}");
        }
        else
        {
            Debug.LogError($"Invalid panel index: {panelIndex}");
        }
    }

    public void UpdateScrollSpeed(string input)
    {
        if (float.TryParse(input, out float newSpeed))
        {
            newSpeed = Mathf.Clamp(newSpeed, 0.01f, 5f);
            scrollSpeed = newSpeed;
            scrollSpeedInput.text = newSpeed.ToString("0.00");
            PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);  // Save scroll speed to PlayerPrefs
            Debug.Log($"Scroll Speed Updated: {scrollSpeed}");
        }
        else
        {
            Debug.LogError("Invalid input for scroll speed.");
            scrollSpeedInput.text = scrollSpeed.ToString("0.00");
        }
    }

    public float GetScrollSpeed()
    {
        return scrollSpeed;
    }

    public void UpdateBrightness(float value)
    {
        if (colorGrading != null)
        {
            colorGrading.postExposure.value = Mathf.Lerp(-2f, 2f, value);  // Adjust the brightness curve
            PlayerPrefs.SetFloat("Brightness", value);  // Save brightness to PlayerPrefs
        }
    }

    public void UpdateSensitivity(string input)
    {
        if (float.TryParse(input, out float newSensitivity))
        {
            newSensitivity = Mathf.Clamp(newSensitivity, 0.01f, 20f) * 100f;
            mouseSensitivity = newSensitivity;
            sensitivityInputField.text = (newSensitivity / 100f).ToString("0.00");
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);  // Save mouse sensitivity to PlayerPrefs
            Debug.Log($"Mouse Sensitivity Updated: {mouseSensitivity}");
        }
        else
        {
            Debug.LogError("Invalid input for sensitivity.");
            sensitivityInputField.text = (mouseSensitivity / 100f).ToString("0.00");
        }
    }

    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }

    public void ResetToDefaultSettings()
    {
        // Reset Scroll Speed
        scrollSpeed = 0.05f;
        scrollSpeedInput.text = scrollSpeed.ToString("0.00");
        PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);  // Reset Scroll Speed in PlayerPrefs

        // Reset Brightness
        brightnessSlider.value = 0.5f;
        UpdateBrightness(0.5f);  // Update the brightness based on default value
        PlayerPrefs.SetFloat("Brightness", 0.5f);  // Reset Brightness in PlayerPrefs

        // Reset Mouse Sensitivity
        mouseSensitivity = 100f;
        sensitivityInputField.text = (mouseSensitivity / 100f).ToString("0.00");
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);  // Reset Mouse Sensitivity in PlayerPrefs

        // Reset Keybinds (this will be handled separately in KeybindSettings)
        KeybindSettings.Instance.ResetToDefaults();
        KeybindSettings.Instance.SaveKeybinds();

        ShowResetText("Settings have been reset to default");

        Debug.Log("Settings Reset to Default");
    }

    void ShowResetText(string message)
    {
        resetMessageText.text = message;

        // If there's an existing coroutine running, stop it
        if (resetMessageCoroutine != null)
        {
            StopCoroutine(resetMessageCoroutine);
        }

        // Show the reset message
        resetMessageText.gameObject.SetActive(true);

        // Start a new coroutine
        resetMessageCoroutine = StartCoroutine(ShowResetMessage());
    }

    private IEnumerator ShowResetMessage()
    {
        // Wait for 3 seconds
        yield return new WaitForSecondsRealtime(3f);

        resetMessageText.text = "";
        // Hide the reset message
        resetMessageText.gameObject.SetActive(false);
    }

}