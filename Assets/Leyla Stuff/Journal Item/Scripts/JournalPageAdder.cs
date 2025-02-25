using UnityEngine;

public class JournalPageAdder : MonoBehaviour
{
    [SerializeField] private PageTracker pageTracker; // Reference to the PageTracker

    public void AddTextPage(TextPage textPage)
    {
        if (pageTracker != null && textPage != null)
        {
            pageTracker.AddPage(textPage);
        }
    }

    public void AddObjectivesPage(ObjectivesPage objectivesPage)
    {
        if (pageTracker != null && objectivesPage != null)
        {
            pageTracker.AddPage(objectivesPage);
        }
    }
}
