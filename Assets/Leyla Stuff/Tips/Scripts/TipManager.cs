using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Text.RegularExpressions;

public class TipManager : MonoBehaviour
{
    public static TipManager Instance;

    [Header("Tip UI Setup")]
    [SerializeField] private GameObject tipPanel;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject textPanel;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Tip Data")]
    [SerializeField] private Tip[] tips;
    [SerializeField] private float expansionSpeed = 5f;
    [SerializeField] private float videoScaleSpeed = 5f;
    private List<string> shownTipIDs = new List<string>();

    [Header("Panel Dimensions")]
    [SerializeField] private Vector3 textPanelScale = new Vector3(1f, 1f, 1f);
    [SerializeField] private Vector3 videoPanelScale = new Vector3(1f, 1f, 1f);

    private Queue<int> tipQueue = new Queue<int>();
    private bool isDisplaying = false;

    private Vector3 targetVideoScale;
    private Vector3 targetTextScale;

    private int currentTipIndex = -1;
    [SerializeField] private string[] previousKeybinds;

    private void Awake()
    {
        targetVideoScale = videoPanelScale;
        targetTextScale = textPanelScale;

        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        tipPanel.SetActive(false);

        // Set initial scales to zero
        videoPanel.transform.localScale = Vector3.zero;
        textPanel.transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        // Update sprites and text when keybinds change
        if (isDisplaying && currentTipIndex >= 0 && currentTipIndex < tips.Length)
        {
            Tip currentTip = tips[currentTipIndex];

            if (previousKeybinds == null || previousKeybinds.Length != currentTip.selectedActions.Length)
            {
                previousKeybinds = new string[currentTip.selectedActions.Length];
            }

            bool keybindsChanged = false;

            for (int i = 0; i < currentTip.selectedActions.Length; i++)
            {
                string currentKeybind = GetKeyBindForAction(currentTip.selectedActions[i]);

                // If the keybind has changed, mark the flag as true
                if (previousKeybinds[i] != currentKeybind)
                {
                    previousKeybinds[i] = currentKeybind;
                    keybindsChanged = true;
                }
            }

            // Only update if keybinds have changed
            if (keybindsChanged)
            {
                GameObject iconParent = textPanel.transform.Find("IconParent").gameObject;
                foreach (Transform child in iconParent.transform)
                {
                    Destroy(child.gameObject);
                }

                string processedTipText = currentTip.tipText;

                for (int i = 0; i < currentTip.selectedActions.Length; i++)
                {
                    string actionTag = $"<action{i}>";
                    string actionNameTag = $"<actionname{i}>";

                    if (processedTipText.Contains(actionTag))
                    {
                        processedTipText = processedTipText.Replace(actionTag, " ");

                        GameObject iconObj = new GameObject($"Icon_{i}");
                        iconObj.transform.SetParent(iconParent.transform, false);

                        // Update the icon with the appropriate sprite for the action
                        currentTip.UpdateSprite(iconObj, currentTip.selectedActions[i]);
                    }

                    if (processedTipText.Contains(actionNameTag))
                    {
                        string keybind = previousKeybinds[i];
                        Color actionColor = currentTip.actionColors[i]; // Get the color for the action

                        // Convert the color to a hex string and apply to the keybind
                        string colorCode = ColorUtility.ToHtmlStringRGB(actionColor);
                        processedTipText = processedTipText.Replace(actionNameTag, $"<color=#{colorCode}>{keybind}</color>");
                    }
                }

                // Set the processed tip text with the replaced actions
                tipText.text = processedTipText;
            }
        }
    }

    public void ShowTip(int tipIndex)
    {
        if (shownTipIDs.Contains(tips[tipIndex].tipID))
        {
            return; // Skip this tip if it's already been shown
        }

        if (tipIndex >= 0 && tipIndex < tips.Length)
        {
            shownTipIDs.Add(tips[tipIndex].tipID);

            tipQueue.Enqueue(tipIndex);

            if (!isDisplaying)
            {
                StartCoroutine(ProcessTipQueue());
            }
        }
    }

    // Get the list of shown tip IDs
    public List<string> GetShownTipIDs()
    {
        return new List<string>(shownTipIDs);
    }

    // Set the list of shown tip IDs
    public void SetShownTipIDs(List<string> ids)
    {
        shownTipIDs = ids;
    }

    private IEnumerator ProcessTipQueue()
    {
        while (tipQueue.Count > 0)
        {
            int tipIndex = tipQueue.Dequeue();
            currentTipIndex = tipIndex;
            Tip currentTip = tips[tipIndex];
            isDisplaying = true;

            // Process the tip text and replace actions with the corresponding images and text
            string processedTipText = currentTip.tipText;
            GameObject iconParent = textPanel.transform.Find("IconParent").gameObject;

            // Clear any old icons from the previous tip
            foreach (Transform child in iconParent.transform)
            {
                Destroy(child.gameObject);
            }

            List<GameObject> iconObjects = new List<GameObject>();

            for (int i = 0; i < currentTip.selectedActions.Length; i++)
            {
                string actionTag = $"<action{i}>";
                string actionNameTag = $"<actionname{i}>";

                if (processedTipText.Contains(actionTag))
                {
                    processedTipText = processedTipText.Replace(actionTag, " ");

                    // Create the icon for this action
                    GameObject iconObj = new GameObject($"Icon_{i}");
                    iconObj.transform.SetParent(iconParent.transform, false);
                    iconObjects.Add(iconObj);

                    currentTip.UpdateSprite(iconObj, currentTip.selectedActions[i]);
                }

                if (processedTipText.Contains(actionNameTag))
                {
                    string keybind = GetKeyBindForAction(currentTip.selectedActions[i]);
                    Color actionColor = currentTip.actionColors[i]; // Get the colour for this action

                    string colorCode = ColorUtility.ToHtmlStringRGB(actionColor);

                    processedTipText = processedTipText.Replace(actionNameTag, $"<color=#{colorCode}>{keybind}</color>");
                }
            }

            tipText.text = processedTipText;

            videoPlayer.clip = currentTip.tipVideo;

            tipPanel.SetActive(true);

            // Start animations for text and video panels
            Coroutine textAnim = StartCoroutine(AnimateTextPanel(true));
            Coroutine videoAnim = StartCoroutine(AnimateVideoPanel(true));

            yield return textAnim;
            yield return videoAnim;

            // Wait for video to finish playing
            videoPlayer.Play();
            yield return new WaitForSeconds((float)videoPlayer.clip.length);

            yield return StartCoroutine(HideTip(iconObjects));
        }

        isDisplaying = false;
        currentTipIndex = -1;
    }

    private string GetKeyBindForAction(string actionName)
    {
        InputAction action = inputActions.FindAction(actionName);
        if (action != null)
        {
            // Determine if the player is using a controller or keyboard
            bool isUsingController = KeyBindingManager.Instance.IsUsingController();
            int bindingIndex = isUsingController ? 1 : 0;

            // Ensure that the action has enough bindings
            if (action.bindings.Count > bindingIndex)
            {
                string keybind = action.bindings[bindingIndex].ToDisplayString();

                // Define replacements in a dictionary for convenience
                Dictionary<string, string> replacements = new Dictionary<string, string>
            {
                { "Press", "" },
                { "Hold", "" },
                { "LMB", "LMB" },
                { "RMB", "RMB" },
                { "Scroll/Y", "Mouse Scroll" },
                { "Scroll/X", "Mouse Horizontal Scroll" },
                { "Forward", "MB5" },
                { "Back", "MB4" },
                { "dpad/y", "D-Pad Vertical" },
                { "dpad/x", "D-Pad Horizontal" }
            };

                // Apply all replacements
                foreach (var replacement in replacements)
                {
                    keybind = keybind.Replace(replacement.Key, replacement.Value).Trim();
                }

                return keybind;
            }
        }

        return "Unknown";
    }

    private IEnumerator HideTip(List<GameObject> iconObjects)
    {
        Coroutine textAnim = StartCoroutine(AnimateTextPanel(false));
        Coroutine videoAnim = StartCoroutine(AnimateVideoPanel(false));

        yield return textAnim;
        yield return videoAnim;

        // Destroy all the icon objects after hiding the tip
        foreach (GameObject iconObj in iconObjects)
        {
            Destroy(iconObj);
        }

        yield return new WaitForSeconds(0.1f);

        tipPanel.SetActive(false);
    }

    private IEnumerator AnimateTextPanel(bool expand)
    {
        Transform textTransform = textPanel.transform;
        Vector3 targetScale = expand ? targetTextScale : Vector3.zero;

        while (Vector3.Distance(textTransform.localScale, targetScale) > 0.01f)
        {
            textTransform.localScale = Vector3.Lerp(textTransform.localScale, targetScale, Time.deltaTime * expansionSpeed);
            yield return null;
        }

        // Ensure it reaches the exact target scale
        textTransform.localScale = targetScale;
    }

    private IEnumerator AnimateVideoPanel(bool expand)
    {
        Transform videoTransform = videoPanel.transform;
        Vector3 targetScale = expand ? targetVideoScale : Vector3.zero;

        while (Vector3.Distance(videoTransform.localScale, targetScale) > 0.01f)
        {
            videoTransform.localScale = Vector3.Lerp(videoTransform.localScale, targetScale, Time.deltaTime * videoScaleSpeed);
            yield return null;
        }

        // Ensure it reaches the exact target scale
        videoTransform.localScale = targetScale;
    }
}