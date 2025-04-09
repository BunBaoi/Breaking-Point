using System.Collections.Generic;
using UnityEngine;

public class PageTracker : MonoBehaviour
{
    [SerializeField] private List<JournalPage> pages = new List<JournalPage>();
    [SerializeField] private List<JournalPage> allAvailablePages;
    private int currentPageIndex = 0;

    public int CurrentPageIndex => currentPageIndex;
    public List<JournalPage> Pages => pages;

    // Singleton pattern
    public static PageTracker Instance { get; private set; }

    // [Header("Journal Pages")]
    // [SerializeField] private TextPage[] textPages;
    // [SerializeField] private ObjectivesPage[] objectivesPages;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // InitialisePages();
    }

    /*private void InitialisePages()
    {
        // Add TextPages to journal and dictionary
        foreach (var page in textPages)
        {
            AddPage(page);
        }

        // Add ObjectivesPages to journal and dictionary
        foreach (var page in objectivesPages)
        {
            AddPage(page);
        }
    }*/

    public JournalPage FindJournalPageByID(string pageID)
    {
        // Search through your list of available pages
        foreach (var page in allAvailablePages)
        {
            if (page.pageID == pageID)
            {
                return page;
            }
        }
        return null;
    }

    public void AddPage(JournalPage newPage)
    {
        if (newPage != null)
        {
            pages.Add(newPage);
        }
    }

    public void NextPage()
    {
        if (currentPageIndex + 2 < pages.Count)
        {
            currentPageIndex += 2;
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex -= 2;
        }
    }

    public void ClearPages()
    {
        pages.Clear();
        currentPageIndex = 0;
    }

    // Setter method for currentPageIndex
    public void SetCurrentPageIndex(int newIndex)
    {
        if (newIndex >= 0 && newIndex < pages.Count)
        {
            currentPageIndex = newIndex;
        }
    }
}