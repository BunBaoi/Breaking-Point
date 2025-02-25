using System.Collections.Generic;
using UnityEngine;

public class PageTracker : MonoBehaviour
{
    private List<JournalPage> pages = new List<JournalPage>();
    private int currentPageIndex = 0;

    public int CurrentPageIndex => currentPageIndex;
    public List<JournalPage> Pages => pages;

    // Singleton pattern
    public static PageTracker Instance { get; private set; }

    // Add serialized fields for TextPage and ObjectivesPage
    [Header("Journal Pages")]
    [SerializeField] private TextPage[] textPages;
    [SerializeField] private ObjectivesPage[] objectivesPages;

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

        AddPagesFromInspector();
    }

    private void AddPagesFromInspector()
    {
        // Add TextPages to journal
        foreach (var page in textPages)
        {
            AddPage(page);
        }

        // Add ObjectivesPages to journal
        foreach (var page in objectivesPages)
        {
            AddPage(page);
        }
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