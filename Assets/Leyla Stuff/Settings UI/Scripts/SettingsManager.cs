using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;

public class SettingsManager : MonoBehaviour
{
    [Header("Settings UI")]
    public GameObject settingsCanvas;

    [Header("Game Over Menu")]
    public GameObject gameOverCanvas;

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
    [SerializeField] private float scrollSpeed = 0.03f;

    [Header("Brightness Settings")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Volume volume;

    [Header("Mouse Sensitivity Settings")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_InputField sensitivityInputField;
    public static float mouseSensitivity = 100f;

    [Header("Mouse Scroll Settings")]
    [SerializeField] private Slider scrollSensitivitySlider;
    private List<ScrollRect> scrollRects;
    [SerializeField] private TMP_InputField mouseScrollSensitivityInput;
    [SerializeField] private float defaultScrollSensitivity = 2f; 
    private float scrollSensitivity;

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

    [Header("Inventory Setups")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;

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

        // --- RESET TO DEFAULT SETTINGS ---
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefaultSettings);
        }
        else
        {
            Debug.LogError("Reset Button is not assigned in the Inspector!");
        }

        // --- DIALOGUE TEXT SCROLL SPEED SETTINGS ---
        if (scrollSpeedSlider != null)
        {
            scrollSpeed = PlayerPrefs.GetFloat("ScrollSpeed", 0.03f);
            scrollSpeedSlider.minValue = 0.01f;
            scrollSpeedSlider.maxValue = 0.5f;
            scrollSpeedSlider.value = scrollSpeed;

            UpdateScrollSpeedText(scrollSpeed);

            scrollSpeedSlider.onValueChanged.AddListener(UpdateScrollSpeed);
        }

        if (scrollSpeedInput != null)
        {
            scrollSpeedInput.onEndEdit.AddListener(UpdateScrollSpeedFromInput);
        }

        // --- BRIGHTNESS SETTINGS ---
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

        // --- MOUSE LOOK SENSITIVITY SETTINGS ---
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 100f);

        // Set up slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.01f;
            sensitivitySlider.maxValue = 20f;
            sensitivitySlider.value = mouseSensitivity / 100f;
            sensitivitySlider.onValueChanged.AddListener(UpdateSensitivityFromSlider);
        }

        // Set up input field
        if (sensitivityInputField != null)
        {
            sensitivityInputField.text = (mouseSensitivity / 100f).ToString("0.00");
            sensitivityInputField.onEndEdit.AddListener(UpdateSensitivityFromInput);
        }

        // --- MOUSE SCROLL SENSITIVTY SETTINGS ---

        FindAllScrollRects();

        scrollSensitivity = PlayerPrefs.GetFloat("ScrollSensitivity", defaultScrollSensitivity);

        scrollSensitivitySlider.minValue = 0.1f;
        scrollSensitivitySlider.maxValue = 40f;

        scrollSensitivitySlider.value = scrollSensitivity;
        mouseScrollSensitivityInput.text = scrollSensitivity.ToString("0.0");

        UpdateScrollSensitivity(scrollSensitivity);

        scrollSensitivitySlider.onValueChanged.AddListener(UpdateScrollSensitivityFromSlider);
        mouseScrollSensitivityInput.onEndEdit.AddListener(UpdateScrollSensitivityFromInputField);

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

        // Debug logs to check if buses are initialised
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
            resetButton.onClick.RemoveListener(ResetToDefaultSettings);
        }

        // Remove listeners when the object is destroyed
        masterSlider.onValueChanged.RemoveListener(UpdateMasterVolume);
        dialogueSlider.onValueChanged.RemoveListener(UpdateDialogueVolume);
        sfxSlider.onValueChanged.RemoveListener(UpdateSFXVolume);
    }

    void ToggleMenu(InputAction.CallbackContext context)
    {
        if (gameOverCanvas.activeSelf)
        {
            Debug.Log("Game Over is active. Cannot access settings.");
            return;
        }

        isMenuOpen = !isMenuOpen;
        settingsCanvas.SetActive(isMenuOpen);

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        // Pause or resume time based on the menu state
        if (isMenuOpen)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerObject != null)
            {
                inventoryManager = playerObject.GetComponent<InventoryManager>();
                if (inventoryManager != null)
                {
                    inventoryManager.enabled = false;
                    Debug.Log("InventoryManager disabled.");
                }
                Transform inventoryCanvasTransform = playerObject.transform.Find("Inventory Canvas");
                if (inventoryCanvasTransform != null)
                {
                    inventoryCanvas = inventoryCanvasTransform.GetComponent<Canvas>();
                    if (inventoryCanvas != null)
                    {
                        inventoryCanvas.gameObject.SetActive(false);
                        Debug.Log("Inventory Canvas disabled.");
                    }
                }
            }
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

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
        scrollSpeed = Mathf.Clamp(newSpeed, 0.01f, 0.5f);

        UpdateScrollSpeedText(scrollSpeed);

        PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);
        Debug.Log($"Scroll Speed Updated: {scrollSpeed}");
    }

    private void UpdateScrollSpeedText(float speed)
    {
        if (scrollSpeedInput != null)
        {
            scrollSpeedInput.text = $"{speed:0.00}";
        }
    }

    private void UpdateScrollSpeedFromInput(string input)
    {
        if (float.TryParse(input, out float newSpeed))
        {
            // Clamp the value and update
            newSpeed = Mathf.Clamp(newSpeed, 0.01f, 0.5f);
            scrollSpeed = newSpeed;

            if (scrollSpeedSlider != null)
            {
                scrollSpeedSlider.value = scrollSpeed;
            }

            scrollSpeedInput.text = $"{scrollSpeed:0.00}";
            // Save to PlayerPrefs
            PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);
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

    private void UpdateSensitivityFromSlider(float value)
    {
        mouseSensitivity = Mathf.Clamp(value * 100f, 1f, 2000f);
        if (sensitivityInputField != null)
        {
            sensitivityInputField.text = value.ToString("0.00");
        }
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
    }

    private void UpdateSensitivityFromInput(string input)
    {
        if (float.TryParse(input, out float newSensitivity))
        {
            newSensitivity = Mathf.Clamp(newSensitivity, 0.01f, 20f);

            sensitivityInputField.text = newSensitivity.ToString("0.00");

            mouseSensitivity = newSensitivity * 100f;

            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = newSensitivity;
            }
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        }
        else
        {
            Debug.LogError("Invalid input for sensitivity.");
            sensitivityInputField.text = (mouseSensitivity / 100f).ToString("0.00");
        }
    }

    private void UpdateScrollSensitivityFromSlider(float value)
    {
        scrollSensitivity = value;
        UpdateScrollSensitivity(scrollSensitivity);

        mouseScrollSensitivityInput.text = scrollSensitivity.ToString("0.0");
    }

    private void UpdateScrollSensitivityFromInputField(string value)
    {
        if (float.TryParse(value, out float newSensitivity))
        {

            newSensitivity = Mathf.Clamp(newSensitivity, 0.1f, 40f);

            mouseScrollSensitivityInput.text = newSensitivity.ToString("0.0");

            scrollSensitivity = newSensitivity;
            scrollSensitivitySlider.value = scrollSensitivity;
            UpdateScrollSensitivity(scrollSensitivity);
        }
        else
        {
            mouseScrollSensitivityInput.text = scrollSensitivity.ToString("0.0");
        }
    }

    private void UpdateScrollSensitivity(float sensitivity)
    {
        foreach (var scrollRect in scrollRects)
        {
            scrollRect.scrollSensitivity = sensitivity;
        }

        PlayerPrefs.SetFloat("ScrollSensitivity", sensitivity);
        PlayerPrefs.Save();
    }

    private void FindAllScrollRects()
    {
        scrollRects = new List<ScrollRect>();

        // Find all ScrollRects in the scene
        scrollRects.AddRange(FindObjectsOfType<ScrollRect>(true));

        GameObject[] rootObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject root in rootObjects)
        {
            if (root.scene.name == null)
            {
                scrollRects.AddRange(root.GetComponentsInChildren<ScrollRect>(true));
            }
        }
    }

    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }

    public void ResetToDefaultSettings()
    {
        scrollSpeed = 0.03f;

        if (scrollSpeedSlider != null)
        {
            scrollSpeedSlider.value = scrollSpeed;
        }

        if (scrollSpeedInput != null)
        {
            scrollSpeedInput.text = $"{scrollSpeed:0.00}";
        }

        PlayerPrefs.SetFloat("ScrollSpeed", scrollSpeed);

        scrollSensitivity = defaultScrollSensitivity;
        if (scrollSensitivitySlider != null)
        {
            scrollSensitivitySlider.value = scrollSensitivity;
        }

        if (mouseScrollSensitivityInput != null)
        {
            mouseScrollSensitivityInput.text = $"{scrollSensitivity:0.00}";
        }
        PlayerPrefs.SetFloat("ScrollSensitivity", scrollSensitivity);

        // Reset Brightness
        brightnessSlider.value = 0.5f;
        UpdateBrightness(0.5f);
        PlayerPrefs.SetFloat("Brightness", 0.5f);

        // Reset Mouse Sensitivity
        mouseSensitivity = 100f;
        sensitivityInputField.text = (mouseSensitivity / 100f).ToString("0.00");
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);

        // Reset Keybinds
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

        if (resetMessageCoroutine != null)
        {
            StopCoroutine(resetMessageCoroutine);
        }

        resetMessageText.gameObject.SetActive(true);

        resetMessageCoroutine = StartCoroutine(ShowResetMessage());
    }

    private IEnumerator ShowResetMessage()
    {
        yield return new WaitForSecondsRealtime(3f);

        resetMessageText.text = "";
        resetMessageText.gameObject.SetActive(false);
    }

    public void ResumeGame()
    {
        isMenuOpen = false;
        settingsCanvas.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Master Volume Adjustment
    public void UpdateMasterVolume(float value)
    {
        masterBus.setVolume(value);
        PlayerPrefs.SetFloat("MasterVolume", value);
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