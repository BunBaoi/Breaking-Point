using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using FMODUnity;

public class JournalUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject journalUI; // Parent UI container
    [SerializeField] private EventReference pageTurnSFX;

    [Header("Left Page UI")]
    [SerializeField] private Image leftPageBackground;
    [SerializeField] private TMP_Text leftTitleText;
    [SerializeField] private TMP_Text leftContentText;
    [SerializeField] private Transform leftChecklistContainer;

    [Header("Right Page UI")]
    [SerializeField] private Image rightPageBackground;
    [SerializeField] private TMP_Text rightTitleText;
    [SerializeField] private TMP_Text rightContentText;
    [SerializeField] private Transform rightChecklistContainer;

    [Header("Checklist Prefab")]
    [SerializeField] private GameObject checklistItemPrefab; // A prefab with a checkbox & text

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Image nextPageImage;
    [SerializeField] private Image prevPageImage;
    private PlayerMovement playerMovement;
    private CameraController cameraController;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string useItemName = "Use";
    [SerializeField] private string nextPageName = "Next";
    [SerializeField] private string previousPageName = "Previous";
    private InputAction useItem;
    private InputAction nextPage;
    private InputAction previousPage;
    [SerializeField] private int previousPageIndex = 0;
    [SerializeField] private bool nextOrPreviousPageCalled = false;

    // [Header("Journal Pages")]
    [SerializeField] private List<JournalPage> pages => PageTracker.Instance != null ? PageTracker.Instance.Pages : new List<JournalPage>();

    [Header("Inventory Setups")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;

    private int currentPageIndex = 0;
    [SerializeField] private bool isJournalOpen = false;

    private void Awake()
    {
        // If inputActions is not assigned via the inspector, load it from the Resources/Keybinds folder
        if (inputActions == null)
        {
            // Load from the "Keybinds" folder in Resources
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");

            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }

        journalUI.SetActive(false);
    }

    private void Start()
    {
        // Find the InventoryManager and Inventory Canvas by names
        GameObject player = GameObject.Find("Alice");
        if (player != null)
        {
            inventoryManager = player.GetComponent<InventoryManager>();
            inventoryCanvas = player.transform.Find("Inventory Canvas")?.GetComponent<Canvas>(); 
        }
        journalUI.SetActive(false); // Ensure journal is hidden at start
        nextPageButton.onClick.AddListener(NextPage);
        prevPageButton.onClick.AddListener(PreviousPage);

        useItem = inputActions.FindAction(useItemName);
        nextPage = inputActions.FindAction(nextPageName);
        previousPage = inputActions.FindAction(previousPageName);

        if (useItem != null)
        {
            useItem.Enable(); // Enable the action
        }
        if (nextPage != null)
        {
            nextPage.Enable(); // Enable the action
        }
        if (previousPage != null)
        {
            previousPage.Enable(); // Enable the action
        }
    }

    private void Update()
    {
        if (CinematicSequence.IsCinematicActive) return;

        // Right-click to toggle journal visibility
        if (useItem.triggered)
        {
            ToggleJournal();
        }

        // cursor stays visible when journal is open
        if (isJournalOpen)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();
                cameraController = player.GetComponentInChildren<CameraController>();
            }
            else
            {
                Debug.LogError("No GameObject with the tag 'Player' found in the scene.");
                return;
            }
            if (isJournalOpen)
            {
                // Disable movement and camera when journal is open
                if (playerMovement != null) playerMovement.SetMovementState(false);
                if (cameraController != null) cameraController.enabled = false;
                if (inventoryManager != null)
                {
                    inventoryManager.enabled = false;
                    inventoryCanvas.gameObject.SetActive(false);
                }

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                currentPageIndex = PageTracker.Instance != null ? PageTracker.Instance.CurrentPageIndex : 0;
                UpdateJournalUI();
                UpdateNextPrevPageImage(nextPageImage.gameObject, nextPageName);
                UpdateNextPrevPageImage(prevPageImage.gameObject, previousPageName);

                var pages = PageTracker.Instance.Pages;

                if (nextPage.triggered && pages.Count > 2 && currentPageIndex + 1 < pages.Count)
                {
                    NextPage();
                }

                if (previousPage.triggered)
                {
                    PreviousPage();
                }
            }
        }

        /*if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Debug.Log($"Current Page Index: {currentPageIndex}");

            if (PageTracker.Instance != null)
            {
                var pages = PageTracker.Instance.Pages;
                Debug.Log($"Total Pages: {pages.Count}");

                // Log the titles of all pages
                for (int i = 0; i < pages.Count; i++)
                {
                    Debug.Log($"Page {i + 1} Title: {pages[i].pageTitle}");
                }

                // log current left and right pages
                if (currentPageIndex < pages.Count)
                {
                    Debug.Log($"Left Page Title: {pages[currentPageIndex].pageTitle}");
                }
                else
                {
                    Debug.Log("No left page available.");
                }

                if (currentPageIndex + 1 < pages.Count)
                {
                    Debug.Log($"Right Page Title: {pages[currentPageIndex + 1].pageTitle}");
                }
                else
                {
                    Debug.Log("No right page available.");
                }
            }
            else
            {
                Debug.Log("PageTracker.Instance is null.");
            }
        }*/
    }

    public void SetJournalState(bool state)
    {
        isJournalOpen = state;
    }

    private void UpdateNextPrevPageImage(GameObject imageObject, string actionName)
    {
        if (KeyBindingManager.Instance == null || imageObject == null || inputActions == null) return;

        InputAction action = inputActions.FindAction(actionName);
        if (action == null) return;

        int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;
        if (action.bindings.Count <= bindingIndex) return;

        InputBinding binding = action.bindings[bindingIndex];

        string boundKeyOrButton = KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath);
        if (string.IsNullOrEmpty(boundKeyOrButton))
        {
            Debug.LogWarning($"No key binding found for action: {actionName}");
            return;
        }

        KeyBinding keyBinding = KeyBindingManager.Instance.GetKeybinding(actionName);
        if (keyBinding == null) return;

        Image indicatorImage = imageObject.GetComponent<Image>();
        if (indicatorImage == null) return;

        indicatorImage.sprite = KeyBindingManager.Instance.IsUsingController() ? keyBinding.controllerSprite : keyBinding.keySprite;

        Animator animator = imageObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = imageObject.AddComponent<Animator>();
        }

        animator.enabled = true;

        string folderPath = KeyBindingManager.Instance.IsUsingController() ? "UI/Controller/" : "UI/Keyboard/";
        string animatorName = KeyBindingManager.Instance.GetSanitisedKeyName(boundKeyOrButton);
        RuntimeAnimatorController assignedAnimator = Resources.Load<RuntimeAnimatorController>(folderPath + animatorName);

        if (assignedAnimator != null)
        {
            animator.runtimeAnimatorController = assignedAnimator;
            Debug.Log($"Assigned animator '{animatorName}' to {imageObject.name}");
        }
        else
        {
            Debug.LogError($"Animator '{animatorName}' not found in {folderPath}");
        }
    }

    private void ToggleJournal()
    {
        if (CinematicSequence.IsCinematicActive) return;
        isJournalOpen = !isJournalOpen;
        journalUI.SetActive(isJournalOpen);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            cameraController = player.GetComponentInChildren<CameraController>();
        }
        else
        {
            Debug.LogError("No GameObject with the tag 'Player' found in the scene.");
            return;
        }

        if (isJournalOpen)
        {
            // Disable movement and camera when journal is open
            if (playerMovement != null) playerMovement.SetMovementState(false);
            if (cameraController != null) cameraController.enabled = false;
            if (inventoryManager != null)
            {
                inventoryManager.enabled = false;
                inventoryCanvas.gameObject.SetActive(false);
            }

            UpdateNextPrevPageImage(nextPageImage.gameObject, nextPageName);
            UpdateNextPrevPageImage(prevPageImage.gameObject, previousPageName);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            currentPageIndex = PageTracker.Instance != null ? PageTracker.Instance.CurrentPageIndex : 0;
            UpdateJournalUI();
        }
        else
        {
            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
                inventoryCanvas.gameObject.SetActive(true);
            }
            // Re-enable movement and camera when journal is closed
            if (playerMovement != null) playerMovement.SetMovementState(true);
            if (cameraController != null) cameraController.enabled = true;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void UpdatePreviousPageImage()
    {
        if (KeyBindingManager.Instance == null) return;

        KeyBinding binding = KeyBindingManager.Instance.GetKeybinding(previousPageName);
        if (binding == null) return;

        prevPageImage.sprite = KeyBindingManager.Instance.IsUsingController() ?
                                            binding.controllerSprite : binding.keySprite;
    }

    private void UpdateNextPageImage()
    {
        if (KeyBindingManager.Instance == null) return;

        KeyBinding binding = KeyBindingManager.Instance.GetKeybinding(nextPageName);
        if (binding == null) return;

        nextPageImage.sprite = KeyBindingManager.Instance.IsUsingController() ?
                                            binding.controllerSprite : binding.keySprite;
    }

    private void UpdateJournalUI()
    {
        if (PageTracker.Instance == null || PageTracker.Instance.Pages.Count == 0) return;

        var pages = PageTracker.Instance.Pages;

        if (useItem.triggered)
        {
            Debug.Log("Force UI update due to input action");
        }
        else
        {
            // Only skip if no new page is added and useItem.triggered wasn't triggered
            if (!nextOrPreviousPageCalled && pages.Count == previousPageIndex)
            {
                Debug.Log("No new page added, skipping UI update");
                return;
            }
        }

        previousPageIndex = pages.Count;

        nextOrPreviousPageCalled = false;
        Debug.Log("updated journal ui");

        // Clear previous content from both pages
        ClearPageUI(leftTitleText, leftContentText, leftChecklistContainer);
        ClearPageUI(rightTitleText, rightContentText, rightChecklistContainer);

        // Update Left Page
        if (currentPageIndex < pages.Count)
        {
            UpdatePageUI(pages[currentPageIndex], leftTitleText, leftContentText, leftChecklistContainer);
        }

        // Update Right Page
        if (currentPageIndex + 1 < pages.Count)
        {
            // If there's a next page, show it on the right
            UpdatePageUI(pages[currentPageIndex + 1], rightTitleText, rightContentText, rightChecklistContainer);
        }
        else
        {
            // If there is no next page, leave the right side blank
            ClearPageUI(rightTitleText, rightContentText, rightChecklistContainer);
        }

        UpdatePreviousPageImage();
        UpdateNextPageImage();

        // Adjust button interactability based on available pages
        prevPageButton.interactable = currentPageIndex > 0;

        nextPageButton.interactable = currentPageIndex + 2 < pages.Count;
    }

    private void UpdatePageUI(JournalPage page, TMP_Text title, TMP_Text content, Transform checklistContainer)
    {
        title.text = page.pageTitle;

        if (page is TextPage textPage)
        {
            content.gameObject.SetActive(true);
            checklistContainer.gameObject.SetActive(false);
            content.text = textPage.content;
        }
        else if (page is ObjectivesPage objectivesPage)
        {
            content.gameObject.SetActive(false);
            checklistContainer.gameObject.SetActive(true);
            UpdateChecklist(objectivesPage, checklistContainer);
        }
    }

    private void ClearPageUI(TMP_Text title, TMP_Text content, Transform checklistContainer)
    {
        title.text = "";
        content.text = "";
        content.gameObject.SetActive(false);
        checklistContainer.gameObject.SetActive(false);
    }

    private void UpdateChecklist(ObjectivesPage objectivesPage, Transform checklistContainer)
    {
        foreach (Transform child in checklistContainer)
        {
            Destroy(child.gameObject);
        }

        List<bool> checklistStatus = objectivesPage.GetChecklistStatus();
        for (int i = 0; i < objectivesPage.objectives.Count; i++)
        {
            GameObject item = Instantiate(checklistItemPrefab, checklistContainer);
            TMP_Text itemText = item.GetComponentInChildren<TMP_Text>();
            Toggle itemToggle = item.GetComponentInChildren<Toggle>();

            itemText.text = objectivesPage.objectives[i].text;
            itemToggle.isOn = checklistStatus[i];
            itemToggle.interactable = false;
        }
    }

    public void NextPage()
    {
        Debug.Log("Next Page clicked");

        if (PageTracker.Instance == null) return;

        nextOrPreviousPageCalled = true;
        // Check if there's a valid next page (if we're not already at the last page)
        if (currentPageIndex + 2 < PageTracker.Instance.Pages.Count)
        {
            currentPageIndex += 2; // Move forward by two pages
            PageTracker.Instance.SetCurrentPageIndex(currentPageIndex);
            UpdateJournalUI();
            RuntimeManager.PlayOneShot(pageTurnSFX);
            Debug.Log($"Pages {currentPageIndex + 1} and {currentPageIndex + 2} updated");
        }
        /*else if (currentPageIndex + 1 < PageTracker.Instance.Pages.Count)
        {
            currentPageIndex += 1; // Move to the last page (if we have an odd number of pages)
            PageTracker.Instance.SetCurrentPageIndex(currentPageIndex);
            UpdateJournalUI();
            Debug.Log($"Page {currentPageIndex + 1} updated with blank right side");
        }*/
        else
        {
            // If there's no valid next page, do nothing
            Debug.Log("No more pages to show.");
            return;
        }
    }

    public void PreviousPage()
    {
        Debug.Log("Previous Page clicked");

        if (PageTracker.Instance == null) return;

        nextOrPreviousPageCalled = true;
        // Check if we can move back by two pages
        if (currentPageIndex - 2 >= 0)
        {
            currentPageIndex -= 2; // Move back by two pages
            PageTracker.Instance.SetCurrentPageIndex(currentPageIndex);
            UpdateJournalUI();
            RuntimeManager.PlayOneShot(pageTurnSFX);
            Debug.Log($"Pages {currentPageIndex + 1} and {currentPageIndex + 2} updated");
        }
        else if (currentPageIndex - 1 >= 0)
        {
            currentPageIndex -= 1; // Move back by one page
            PageTracker.Instance.SetCurrentPageIndex(currentPageIndex);
            UpdateJournalUI();
            Debug.Log($"Page {currentPageIndex + 1} updated with blank right side");
        }
        else
        {
            // If we're already on the first page, do nothing
            Debug.Log("Already on the first set of pages.");
        }
    }
}

