using UnityEngine;

public class JournalPageAdder : MonoBehaviour
{
    [SerializeField] private PageTracker pageTracker; // Reference to the PageTracker

    public void AddTextPage(TextPage textPage)
    {
        if (PageTracker.Instance != null && textPage != null)
        {
            PageTracker.Instance.AddPage(textPage);
        }
    }

    public void AddObjectivesPage(ObjectivesPage objectivesPage)
    {
        if (pageTracker != null && objectivesPage != null)
        {
            PageTracker.Instance.AddPage(objectivesPage);
        }
    }
}
