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
    [SerializeField] private GameObject tipPanel;       // Main panel that contains both text and video
    [SerializeField] private GameObject videoPanel;     // Panel specifically for displaying video
    [SerializeField] private GameObject textPanel;      // Panel specifically for displaying text
    [SerializeField] private RawImage videoDisplay;     // Image that will display the video
    [SerializeField] private TextMeshProUGUI tipText;   // Text for displaying text tips
    [SerializeField] private VideoPlayer videoPlayer;   // VideoPlayer component
    [SerializeField] private InputActionAsset inputActions;

    [Header("Tip Data")]
    [SerializeField] private Tip[] tips;  // Array to hold multiple tips
    [SerializeField] private float expansionSpeed = 5f;  // Speed of the text panel expansion
    [SerializeField] private float videoScaleSpeed = 5f; // Speed of the video panel scaling

    [Header("Panel Dimensions")]
    [SerializeField] private Vector3 textPanelScale = new Vector3(1f, 1f, 1f); // Scale for the text panel (x, y, z)
    [SerializeField] private Vector3 videoPanelScale = new Vector3(1f, 1f, 1f); // Scale for the video panel (x, y, z)

    private Queue<int> tipQueue = new Queue<int>();     // Queue to manage tips
    private bool isDisplaying = false;                  // Check if a tip is currently being shown

    private Vector3 targetVideoScale;
    private Vector3 targetTextScale;

    private int currentTipIndex = -1;
    [SerializeField] private string[] previousKeybinds;

    private void Awake()
    {
        targetVideoScale = videoPanelScale;
        targetTextScale = textPanelScale;
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
        // Ensure we have a valid current tip index and we're displaying the tip
        if (isDisplaying && currentTipIndex >= 0 && currentTipIndex < tips.Length)
        {
            Tip currentTip = tips[currentTipIndex];

            // Initialize the previousKeybinds array to store previous keybinds
            if (previousKeybinds == null || previousKeybinds.Length != currentTip.selectedActions.Length)
            {
                previousKeybinds = new string[currentTip.selectedActions.Length];
            }

            // Check if any of the keybinds have changed since the last update
            bool keybindsChanged = false;

            // Process the action tags and check if the keybinds have changed
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
                // Find the icon parent and clear old icons
                GameObject iconParent = textPanel.transform.Find("IconParent").gameObject;
                foreach (Transform child in iconParent.transform)
                {
                    Destroy(child.gameObject);
                }

                // The processedTipText can now be updated based on keybinds
                string processedTipText = currentTip.tipText;

                // Process the action tags and update icons and keybinds
                for (int i = 0; i < currentTip.selectedActions.Length; i++)
                {
                    string actionTag = $"<action{i}>";
                    string actionNameTag = $"<actionname{i}>";  // The new tag for action names

                    if (processedTipText.Contains(actionTag))  // If the <action{i}> tag is found
                    {
                        processedTipText = processedTipText.Replace(actionTag, " ");

                        // Create the icon for this action
                        GameObject iconObj = new GameObject($"Icon_{i}");
                        iconObj.transform.SetParent(iconParent.transform, false);

                        // Update the icon with the appropriate sprite for the action
                        currentTip.UpdateSprite(iconObj, currentTip.selectedActions[i]);
                    }

                    if (processedTipText.Contains(actionNameTag))  // If the <actionname{i}> tag is found
                    {
                        string keybind = previousKeybinds[i];  // Use the updated keybind from previousKeybinds
                        processedTipText = processedTipText.Replace(actionNameTag, keybind);
                    }
                }

                // Set the processed tip text with the replaced actions
                tipText.text = processedTipText;
            }
        }
    }

    public void ShowTip(int tipIndex)
    {
        if (tipIndex >= 0 && tipIndex < tips.Length)
        {
            tipQueue.Enqueue(tipIndex);

            if (!isDisplaying)
            {
                StartCoroutine(ProcessTipQueue());
            }
        }
    }

    private IEnumerator ProcessTipQueue()
    {
        while (tipQueue.Count > 0)
        {
            int tipIndex = tipQueue.Dequeue();
            currentTipIndex = tipIndex;  // Set the current tip index
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

            // Process the action tags (<action{i}> for icons and <actionname{i}> for action names)
            for (int i = 0; i < currentTip.selectedActions.Length; i++)
            {
                string actionTag = $"<action{i}>";
                string actionNameTag = $"<actionname{i}>";  // The new tag for action names

                if (processedTipText.Contains(actionTag))  // If the <action{i}> tag is found
                {
                    // Replace <action{i}> with a space (or placeholder)
                    processedTipText = processedTipText.Replace(actionTag, " ");

                    // Create the icon for this action
                    GameObject iconObj = new GameObject($"Icon_{i}");
                    iconObj.transform.SetParent(iconParent.transform, false);
                    iconObjects.Add(iconObj);

                    // Update the icon with the appropriate sprite for the action
                    currentTip.UpdateSprite(iconObj, currentTip.selectedActions[i]);
                }

                if (processedTipText.Contains(actionNameTag))  // If the <actionname{i}> tag is found
                {
                    // Get the keybinding for the action from InputActionAsset
                    string keybind = GetKeyBindForAction(currentTip.selectedActions[i]);

                    // Replace <actionname{i}> with the corresponding keybind
                    processedTipText = processedTipText.Replace(actionNameTag, keybind);
                }
            }

            // Set the processed tip text with the replaced actions
            tipText.text = processedTipText;

            // Set the video clip for the tip
            videoPlayer.clip = currentTip.tipVideo;

            // Display the tip panel
            tipPanel.SetActive(true);

            // Start animations for text and video panels
            Coroutine textAnim = StartCoroutine(AnimateTextPanel(true));
            Coroutine videoAnim = StartCoroutine(AnimateVideoPanel(true));

            // Wait until both animations are complete
            yield return textAnim;
            yield return videoAnim;

            // Wait for video to finish playing
            videoPlayer.Play();
            yield return new WaitForSeconds((float)videoPlayer.clip.length);

            // Hide the tip after the video finishes
            yield return StartCoroutine(HideTip(iconObjects));
        }

        isDisplaying = false;
        currentTipIndex = -1; // Reset the current tip index when finished
    }

    private string GetKeyBindForAction(string actionName)
    {
        // Find the action in the InputActionAsset
        InputAction action = inputActions.FindAction(actionName);
        if (action != null)
        {
            // Get the first binding (you can refine this if needed, depending on your input setup)
            string keybind = action.bindings[0].ToDisplayString();

            // Optionally trim any "Press" or "Hold" labels or other extra info
            keybind = keybind.Replace("Press", "").Replace("Hold", "").Trim();

            return keybind;
        }

        return "Unknown";  // If the action isn't found, return "Unknown"
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