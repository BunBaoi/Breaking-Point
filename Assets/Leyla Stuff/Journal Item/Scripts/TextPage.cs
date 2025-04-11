using UnityEngine;

[CreateAssetMenu(fileName = "New Text Page", menuName = "Journal/Text Page")]
public class TextPage : JournalPage
{
    [TextArea(5, 10)]
    public string content;
}
