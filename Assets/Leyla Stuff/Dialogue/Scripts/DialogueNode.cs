using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using FMODUnity;

[System.Serializable]
public class DialogueOption
{
    public string optionText; // Text to display on the button
    public DialogueTree nextDialogueTree; // New dialogue tree to start (if the option leads to a new tree)
}

[System.Serializable]
public class DialogueNode
{
    [TextArea] public string dialogueText;
    public EventReference fmodAudioEvent; // FMOD event for audio

    public List<string> eventIds = new List<string>(); // List to store multiple eventIDs
    public List<DialogueOption> options;
}
