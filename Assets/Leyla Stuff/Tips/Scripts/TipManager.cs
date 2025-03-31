using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TipManager : MonoBehaviour
{
    [SerializeField] private GameObject tipPanel;       // Main panel that contains both text and video
    [SerializeField] private GameObject videoPanel;     // Panel specifically for displaying video
    [SerializeField] private GameObject textPanel;      // Panel specifically for displaying text
    [SerializeField] private RawImage videoDisplay;     // Image that will display the video
    [SerializeField] private TextMeshProUGUI tipText;   // Text for displaying text tips
    [SerializeField] private VideoPlayer videoPlayer;   // VideoPlayer component

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
            Tip currentTip = tips[tipIndex];
            isDisplaying = true;

            tipText.text = currentTip.tipText;
            videoPlayer.clip = currentTip.tipVideo;

            tipPanel.SetActive(true);

            // Start both animations at the same time
            Coroutine textAnim = StartCoroutine(AnimateTextPanel(true));
            Coroutine videoAnim = StartCoroutine(AnimateVideoPanel(true));

            // Wait until both animations are complete
            yield return textAnim;
            yield return videoAnim;

            // Play the video after the panels are fully expanded
            videoPlayer.Play();
            yield return new WaitForSeconds((float)videoPlayer.clip.length);

            yield return StartCoroutine(HideTip());
        }

        isDisplaying = false;
    }

    private IEnumerator HideTip()
    {
        // Start both animations to hide at the same time
        Coroutine textAnim = StartCoroutine(AnimateTextPanel(false));
        Coroutine videoAnim = StartCoroutine(AnimateVideoPanel(false));

        // Wait until both animations are complete
        yield return textAnim;
        yield return videoAnim;

        // Wait a little extra to ensure the panel has shrunk completely
        yield return new WaitForSeconds(0.1f);

        // Deactivate the tip panel after both animations are done
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