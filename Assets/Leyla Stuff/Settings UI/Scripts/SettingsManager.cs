using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Rendering.PostProcessing;

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

        foreach (var panel in panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        settingsCanvas.SetActive(isMenuOpen);

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

        if (scrollSpeedInput != null)
        {
            scrollSpeedInput.text = scrollSpeed.ToString("0.00");
            scrollSpeedInput.onEndEdit.AddListener(UpdateScrollSpeed);
        }

        // Setup Brightness
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
            // Make sure the brightness goes from dark (-2) to normal (0) and bright (2)
            colorGrading.postExposure.value = Mathf.Lerp(-2f, 2f, value);  // Adjust the brightness curve
            PlayerPrefs.SetFloat("Brightness", value);
        }
    }
}