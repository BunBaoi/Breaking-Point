using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using FMOD.Studio;
using FMODUnity;

public class SettingsManager : MonoBehaviour
{
    [Header("Settings UI")]
    public GameObject settingsCanvas;

    [Header("Panels & Buttons")]
    public Button[] panelButtons;
    public GameObject[] panels;
    [SerializeField] private Color originalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color pressedColor = Color.red;
    [SerializeField] private float hoverScaleMultiplier = 1.2f;

    [Header("Resume Game Settings")]
    [SerializeField] private Button resumeButton;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string toggleMenuAction = "Pause";

    [Header("Scroll Text Speed")]
    [SerializeField] private Slider scrollSpeedSlider;
    [SerializeField] private TMP_InputField scrollSpeedInput;
    [SerializeField] private float scrollSpeed = 0.05f;

    [Header("Brightness Settings")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Volume volume;

    [Header("Mouse Sensitivity Settings")]
    [SerializeField] private TMP_InputField sensitivityInputField;
    public static float mouseSensitivity = 100f;

    [Header("Audio Settings")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider dialogueSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider masterMusicSlider;
    [SerializeField] private Slider menuMusicSlider;

    private Bus masterBus;
    private Bus dialogueBus;
    private Bus sfxBus;
    private Bus masterMusicBus;
    private Bus menuMusicBus;

    [Header("Reset to Default Settings")]
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text resetMessageText;
    private Coroutine resetMessageCoroutine;

    private ColorAdjustments colorAdjustments;
    public static SettingsManager Instance;

    private InputAction pauseAction;
    public bool isMenuOpen = false;
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

        foreach (var button in panelButtons)
        {
            if (button != null)
            {
                ButtonHoverEffect effect = button.gameObject.GetComponent<ButtonHoverEffect>();
                if (effect == null)
                {
                    effect = button.gameObject.AddComponent<ButtonHoverEffect>();
                }

                button.GetComponent<ButtonHoverEffect>();
                if (effect != null)
                {
                    effect.originalColor = originalColor;
                    effect.hoverColor = hoverColor;
                    effect.pressedColor = pressedColor;
                    effect.hoverScaleMultiplier = hoverScaleMultiplier;
                }
            }
        }

        // Panel buttons
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

        // Reset button
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefaultSettings);
        }
        else
        {
            Debug.LogError("Reset Button is not assigned in the Inspector!");
        }

        // Scroll speed settings
        if (scrollSpeedSlider != null)
        {
            scrollSpeed = PlayerPrefs.GetFloat("ScrollSpeed", 0.05f);  // Load from PlayerPrefs
            scrollSpeedSlider.minValue = 0.01f;
            scrollSpeedSlider.maxValue = 5f;
            scrollSpeedSlider.value = scrollSpeed;

            // Update the input field text to match the slider value
            UpdateScrollSpeedText(scrollSpeed);

            // Add listener for the slider to update the scroll speed
            scrollSpeedSlider.onValueChanged.AddListener(UpdateScrollSpeed);
        }

        // If the input field is assigned, add a listener to update scroll speed when the text changes
        if (scrollSpeedInput != null)
        {
            scrollSpeedInput.onEndEdit.AddListener(UpdateScrollSpeedFromInput);
        }

        // Brightness settings
        if (volume.profile.TryGet(out ColorAdjustments colorAdjustments))
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
            brightnessSlider.value = savedBrightness;
            UpdateBrightness(savedBrightness);
        }
        else
        {
            Debug.LogError("Color Adjustments effect not found in the Volume profile.");
        }
        if (volume.profile.TryGet(out colorAdjustments))
        {
            float savedBrightness = PlayerPrefs.GetFloat("Brightness", 0.5f);
            brightnessSlider.value = savedBrightness;
            UpdateBrightness(savedBrightness);
        }
        else
        {
            Debug.LogError("Color Adjustments effect not found in the Volume profile.");
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

        // Resume button
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        else
        {
            Debug.LogError("Reset Button is not assigned in the Inspector!");
        }

        // Initialise FMOD buses
        masterBus = RuntimeManager.GetBus("bus:/");
        dialogueBus = RuntimeManager.GetBus("bus:/dialogue");
        sfxBus = RuntimeManager.GetBus("bus:/sfx");
        masterMusicBus = RuntimeManager.GetBus("bus:/masterMusic");
        menuMusicBus = RuntimeManager.GetBus("bus:/masterMusic/menuMusic");

        // Debug logs to check if buses are initialized
        Debug.Log($"Master Bus Initialised: {masterBus.isValid()}");
        Debug.Log($"Dialogue Bus Initialised: {dialogueBus.isValid()}");
        Debug.Log($"SFX Bus Initialised: {sfxBus.isValid()}");
        Debug.Log($"Master Music Bus Initialised: {masterMusicBus.isValid()}");
        Debug.Log($"Menu Music Bus Initialised: {menuMusicBus.isValid()}");


        // Set initial slider values from PlayerPrefs or defaults
        if (masterSlider != null && dialogueSlider != null && sfxSlider != null && masterMusicSlider != null && menuMusicSlider != null)
        {
            float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float savedDialogueVolume = PlayerPrefs.GetFloat("DialogueVolume", 1f);
            float savedSfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            float savedMasterMusicVolume = PlayerPrefs.GetFloat("MasterMusicVolume", 1f);
            float savedMenuMusicVolume = PlayerPrefs.GetFloat("MenuMusicVolume", 1f);
            masterSlider.value = savedMasterVolume;
            dialogueSlider.value = savedDialogueVolume;
            sfxSlider.value = savedSfxVolume;
            masterMusicSlider.value = savedMasterMusicVolume;
            menuMusicSlider.value = savedMenuMusicVolume;
            UpdateMasterVolume(savedMasterVolume);
            UpdateDialogueVolume(savedDialogueVolume);
            UpdateSFXVolume(savedSfxVolume);
            UpdateMasterMusicVolume(savedMasterMusicVolume);
            UpdateMenuMusicVolume(savedMenuMusicVolume);
        }

        if (masterSlider != null && dialogueSlider != null && sfxSlider != null && masterMusicSlider != null && menuMusicSlider != null)
        {
            // Add listeners for the sliders to adjust volumes
            masterSlider.onValueChanged.AddListener(UpdateMasterVolume);
            dialogueSlider.onValueChanged.AddListener(UpdateDialogueVolume);
            sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);
            masterMusicSlider.onValueChanged.AddListener(UpdateMasterMusicVolume);
            menuMusicSlider.onValueChanged.AddListener(UpdateMenuMusicVolume);
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

        // Remove listeners when the object is destroyed
        masterSlider.onValueChanged.RemoveListener(UpdateMasterVolume);
        dialogueSlider.onValueChanged.RemoveListener(UpdateDialogueVolume);
        sfxSlider.onValueChanged.RemoveListener(UpdateSFXVolume);
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

    private void UpdateScrollSpeed(float newSpeed)
    {
        // Clamp the new speed between 0.01f and 5f
        scrollSpeed = Mathf.Clamp(newSpeed, 0.01f, 5f);

        // Update the Input Field Text
        UpdateScrollSpeedText(scrollSpeed);

        // Save the updated scroll speed in PlayerPrefs
        PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);
        Debug.Log($"Scroll Speed Updated: {scrollSpeed}");
    }

    private void UpdateScrollSpeedText(float speed)
    {
        // Update the TextMeshPro input field with the new scroll speed
        if (scrollSpeedInput != null)
        {
            scrollSpeedInput.text = $"{speed:0.00}";
        }
    }

    private void UpdateScrollSpeedFromInput(string input)
    {
        // Try to parse the input and update the scroll speed
        if (float.TryParse(input, out float newSpeed))
        {
            // Clamp the value and update
            newSpeed = Mathf.Clamp(newSpeed, 0.01f, 5f);
            scrollSpeed = newSpeed;

            // Update the slider with the new value
            if (scrollSpeedSlider != null)
            {
                scrollSpeedSlider.value = scrollSpeed;
            }

            // Save to PlayerPrefs
            PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);
            Debug.Log($"Scroll Speed Updated from Input: {scrollSpeed}");
        }
        else
        {
            Debug.LogError("Invalid input for scroll speed.");
        }
    }

    public float GetScrollSpeed()
    {
        return scrollSpeed;
    }

    void UpdateBrightness(float value)
{
    if (volume.profile.TryGet(out ColorAdjustments colorAdjustments))
    {
        colorAdjustments.postExposure.value = Mathf.Lerp(-2f, 2f, value);
        PlayerPrefs.SetFloat("Brightness", value);
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
        // Reset Scroll Speed
        scrollSpeed = 0.05f;

        // Reset Slider Value
        if (scrollSpeedSlider != null)
        {
            scrollSpeedSlider.value = scrollSpeed;  // Reset Slider Value
        }

        // Reset Input Field Text
        if (scrollSpeedInput != null)
        {
            scrollSpeedInput.text = $"{scrollSpeed:0.00}";  // Reset Text Display
        }

        // Save the default scroll speed in PlayerPrefs
        PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);

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

        masterSlider.value = 1f;
        dialogueSlider.value = 1f;
        sfxSlider.value = 1f;
        masterMusicSlider.value = 1f;
        menuMusicSlider.value = 1f;
        UpdateMasterVolume(1f);
        UpdateDialogueVolume(1f);
        UpdateSFXVolume(1f);
        UpdateMasterMusicVolume(1f);
        UpdateMenuMusicVolume(1f);
        PlayerPrefs.SetFloat("MasterVolume", 1f);
        PlayerPrefs.SetFloat("DialogueVolume", 1f);
        PlayerPrefs.SetFloat("SFXVolume", 1f);
        PlayerPrefs.SetFloat("MasterMusicVolume", 1f);
        PlayerPrefs.SetFloat("MenuMusicVolume", 1f);

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

    public void ResumeGame()
    {
        isMenuOpen = false;
        settingsCanvas.SetActive(false);

        // Resume time and hide cursor
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Master Volume Adjustment
    public void UpdateMasterVolume(float value)
    {
        masterBus.setVolume(value); // Set the volume for the master bus
        PlayerPrefs.SetFloat("MasterVolume", value); // Save the value
    }

    // Dialogue Volume Adjustment
    public void UpdateDialogueVolume(float value)
    {
        dialogueBus.setVolume(value);
        PlayerPrefs.SetFloat("DialogueVolume", value);
    }

    // SFX Volume Adjustment
    public void UpdateSFXVolume(float value)
    {
        sfxBus.setVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    // Master Music Volume Adjustment
    public void UpdateMasterMusicVolume(float value)
    {
        masterMusicBus.setVolume(value);
        PlayerPrefs.SetFloat("MasterMusicVolume", value);
    }

    // Menu Music Volume Adjustment
    public void UpdateMenuMusicVolume(float value)
    {
        menuMusicBus.setVolume(value);
        PlayerPrefs.SetFloat("MenuMusicVolume", value);
    }
}