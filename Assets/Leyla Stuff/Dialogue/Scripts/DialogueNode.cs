using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using FMODUnity; // FMOD namespace for Unity integration

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
    public EventReference fmodAudioEvent; // Replace AudioClip with FMOD event
    public UnityEvent onDialogueEvent;
    public List<DialogueOption> options;
}
