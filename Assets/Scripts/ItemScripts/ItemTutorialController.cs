using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class ItemTutorialController : MonoBehaviour
{
    [System.Serializable]
    public class TutorialInfo
    {
        public string itemName;                // Name of the item that triggers this tutorial
        public GameObject uiContainer;         // Container with RawImage and border (inside Canvas)
        public RawImage videoDisplayImage;     // RawImage component to display the video
        public Image borderImage;              // Border image component (for fading)
        public VideoPlayer videoPlayer;        // VideoPlayer component (outside Canvas)
    }

    [Header("Tutorial Settings")]
    [SerializeField] private TutorialInfo[] tutorials;
    [SerializeField] private float tutorialDuration = 20f;

    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;  // Duration of fade in effect in seconds
    [SerializeField] private float fadeOutDuration = 0.5f; // Duration of fade out effect in seconds

    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string pickupActionName = "PickUp";
    private InputAction pickupAction;

    // Track which tutorials have been shown
    private bool[] tutorialsShown;

    private void Awake()
    {
        // Load input actions if not assigned
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");
        }

        // Initialize tracking array
        tutorialsShown = new bool[tutorials.Length];

        // Disable all tutorial UIs and set initial transparency
        foreach (var tutorial in tutorials)
        {
            if (tutorial.uiContainer != null)
            {
                // Set all UI elements to fully transparent
                if (tutorial.videoDisplayImage != null)
                {
                    Color color = tutorial.videoDisplayImage.color;
                    tutorial.videoDisplayImage.color = new Color(color.r, color.g, color.b, 0f);
                }

                if (tutorial.borderImage != null)
                {
                    Color color = tutorial.borderImage.color;
                    tutorial.borderImage.color = new Color(color.r, color.g, color.b, 0f);
                }

                // Hide the container
                tutorial.uiContainer.SetActive(false);
            }

            // Make sure Play On Awake is disabled
            if (tutorial.videoPlayer != null)
            {
                tutorial.videoPlayer.playOnAwake = false;
            }
        }
    }

    private void Start()
    {
        // Set up input
        pickupAction = inputActions.FindAction(pickupActionName);
        if (pickupAction != null)
        {
            pickupAction.performed += OnPickupPerformed;
            pickupAction.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{pickupActionName}' not found!");
        }
    }

    private void OnDestroy()
    {
        // Clean up input event
        if (pickupAction != null)
        {
            pickupAction.performed -= OnPickupPerformed;
        }
    }

    private void OnPickupPerformed(InputAction.CallbackContext context)
    {
        // When pickup action is performed, check if an item was picked up
        StartCoroutine(CheckForPickedUpItem());
    }

    private IEnumerator CheckForPickedUpItem()
    {
        // Wait a frame for inventory changes to be applied
        yield return null;

        // Access the player's inventory
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory != null)
        {
            Item currentItem = inventory.GetEquippedItem();

            // Check if we have a newly equipped item
            if (currentItem != null)
            {
                // Look for a matching tutorial
                for (int i = 0; i < tutorials.Length; i++)
                {
                    if (currentItem.name == tutorials[i].itemName && !tutorialsShown[i])
                    {
                        // Mark as shown and display tutorial
                        tutorialsShown[i] = true;
                        StartCoroutine(ShowTutorialWithFade(i));
                        break;
                    }
                }
            }
        }
    }

    private IEnumerator ShowTutorialWithFade(int index)
    {
        TutorialInfo tutorial = tutorials[index];

        // Connect the video player to the raw image if needed
        if (tutorial.videoPlayer != null && tutorial.videoDisplayImage != null)
        {
            // Set the RenderTexture of the video as the texture of the RawImage
            if (tutorial.videoPlayer.targetTexture != null)
            {
                tutorial.videoDisplayImage.texture = tutorial.videoPlayer.targetTexture;
            }
        }

        // Reset the video to beginning and play
        if (tutorial.videoPlayer != null)
        {
            tutorial.videoPlayer.time = 0;
            tutorial.videoPlayer.Play();
        }

        // Activate the UI container (but elements still invisible due to alpha=0)
        if (tutorial.uiContainer != null)
        {
            tutorial.uiContainer.SetActive(true);
        }

        // Fade in the tutorial UI
        yield return StartCoroutine(FadeIn(tutorial));

        // Wait for the tutorial duration
        yield return new WaitForSeconds(tutorialDuration);

        // Fade out the tutorial UI
        yield return StartCoroutine(FadeOut(tutorial));

        // Stop the video
        if (tutorial.videoPlayer != null)
        {
            tutorial.videoPlayer.Stop();
        }

        // Deactivate the UI container after fading out
        if (tutorial.uiContainer != null)
        {
            tutorial.uiContainer.SetActive(false);
        }
    }

    private IEnumerator FadeIn(TutorialInfo tutorial)
    {
        float elapsedTime = 0f;

        // Get initial colors
        Color videoImageColor = tutorial.videoDisplayImage != null
            ? tutorial.videoDisplayImage.color
            : Color.white;

        Color borderColor = tutorial.borderImage != null
            ? tutorial.borderImage.color
            : Color.white;

        // Set starting alpha to 0
        if (tutorial.videoDisplayImage != null)
        {
            tutorial.videoDisplayImage.color = new Color(
                videoImageColor.r, videoImageColor.g, videoImageColor.b, 0f);
        }

        if (tutorial.borderImage != null)
        {
            tutorial.borderImage.color = new Color(
                borderColor.r, borderColor.g, borderColor.b, 0f);
        }

        // Gradually increase alpha
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);

            if (tutorial.videoDisplayImage != null)
            {
                tutorial.videoDisplayImage.color = new Color(
                    videoImageColor.r, videoImageColor.g, videoImageColor.b, alpha);
            }

            if (tutorial.borderImage != null)
            {
                tutorial.borderImage.color = new Color(
                    borderColor.r, borderColor.g, borderColor.b, alpha);
            }

            yield return null;
        }

        // Ensure final alpha is exactly 1
        if (tutorial.videoDisplayImage != null)
        {
            tutorial.videoDisplayImage.color = new Color(
                videoImageColor.r, videoImageColor.g, videoImageColor.b, 1f);
        }

        if (tutorial.borderImage != null)
        {
            tutorial.borderImage.color = new Color(
                borderColor.r, borderColor.g, borderColor.b, 1f);
        }
    }

    private IEnumerator FadeOut(TutorialInfo tutorial)
    {
        float elapsedTime = 0f;

        // Get initial colors (should be at full alpha)
        Color videoImageColor = tutorial.videoDisplayImage != null
            ? tutorial.videoDisplayImage.color
            : Color.white;

        Color borderColor = tutorial.borderImage != null
            ? tutorial.borderImage.color
            : Color.white;

        // Gradually decrease alpha
        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsedTime / fadeOutDuration);

            if (tutorial.videoDisplayImage != null)
            {
                tutorial.videoDisplayImage.color = new Color(
                    videoImageColor.r, videoImageColor.g, videoImageColor.b, alpha);
            }

            if (tutorial.borderImage != null)
            {
                tutorial.borderImage.color = new Color(
                    borderColor.r, borderColor.g, borderColor.b, alpha);
            }

            yield return null;
        }

        // Ensure final alpha is exactly 0
        if (tutorial.videoDisplayImage != null)
        {
            tutorial.videoDisplayImage.color = new Color(
                videoImageColor.r, videoImageColor.g, videoImageColor.b, 0f);
        }

        if (tutorial.borderImage != null)
        {
            tutorial.borderImage.color = new Color(
                borderColor.r, borderColor.g, borderColor.b, 0f);
        }
    }
}