using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject journalUI; // Parent UI container

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
    private PlayerMovement playerMovement;
    private CameraController cameraController;

    // [Header("Journal Pages")]
    [SerializeField] private List<JournalPage> pages => PageTracker.Instance != null ? PageTracker.Instance.Pages : new List<JournalPage>();

    private int currentPageIndex = 0;
    private bool isJournalOpen = false;

    private void Awake()
    {
        journalUI.SetActive(false);
    }

    private void Start()
    {
        journalUI.SetActive(false); // Ensure journal is hidden at start
        nextPageButton.onClick.AddListener(NextPage);
        prevPageButton.onClick.AddListener(PreviousPage);
    }

    private void Update()
    {
        // Right-click to toggle journal visibility
        if (Input.GetMouseButtonDown(1))
        {
            ToggleJournal();
        }
        // Ensure cursor stays visible when journal is open
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

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                currentPageIndex = PageTracker.Instance != null ? PageTracker.Instance.CurrentPageIndex : 0;
                UpdateJournalUI();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
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

                // Optionally, log current left and right pages (as you already have it)
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
        }
    }

    private void ToggleJournal()
    {
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

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            currentPageIndex = PageTracker.Instance != null ? PageTracker.Instance.CurrentPageIndex : 0;
            UpdateJournalUI();
        }
        else
        {
            // Re-enable movement and camera when journal is closed
            if (playerMovement != null) playerMovement.SetMovementState(true);
            if (cameraController != null) cameraController.enabled = true;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void UpdateJournalUI()
    {
        if (PageTracker.Instance == null || PageTracker.Instance.Pages.Count == 0) return;

        var pages = PageTracker.Instance.Pages;

        // Clear previous content from both pages
        ClearPageUI(leftTitleText, leftContentText, leftChecklistContainer);
        ClearPageUI(rightTitleText, rightContentText, rightChecklistContainer);

        // Update Left Page (always show the current page)
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

        // Adjust button interactability based on available pages
        prevPageButton.interactable = currentPageIndex > 0;

        // Disable nextPageButton if there are only two pages
        nextPageButton.interactable = pages.Count > 2 && currentPageIndex + 1 < pages.Count;
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

        // Check if there's a valid next page (if we're not already at the last page)
        if (currentPageIndex + 2 < PageTracker.Instance.Pages.Count)
        {
            currentPageIndex += 2; // Move forward by two pages
            PageTracker.Instance.SetCurrentPageIndex(currentPageIndex);
            UpdateJournalUI();
            Debug.Log($"Pages {currentPageIndex + 1} and {currentPageIndex + 2} updated");
        }
        else if (currentPageIndex + 1 < PageTracker.Instance.Pages.Count)
        {
            currentPageIndex += 1; // Move to the last page (if we have an odd number of pages)
            PageTracker.Instance.SetCurrentPageIndex(currentPageIndex);
            UpdateJournalUI();
            Debug.Log($"Page {currentPageIndex + 1} updated with blank right side");
        }
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

        // Check if we can move back by two pages
        if (currentPageIndex - 2 >= 0)
        {
            currentPageIndex -= 2; // Move back by two pages
            PageTracker.Instance.SetCurrentPageIndex(currentPageIndex);
            UpdateJournalUI();
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

