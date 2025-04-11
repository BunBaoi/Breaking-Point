using UnityEngine;
using System;

public abstract class JournalPage : ScriptableObject
{
    public string pageTitle;

    public string pageID;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(pageID))
        {
            pageID = Guid.NewGuid().ToString();
        }
    }
}