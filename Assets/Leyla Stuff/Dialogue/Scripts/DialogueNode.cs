using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using System.Collections;

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

    public string npcName; // Name of the NPC in the dialogue
    public string npcTag; // Optional tag to search for the NPC in the scene
}