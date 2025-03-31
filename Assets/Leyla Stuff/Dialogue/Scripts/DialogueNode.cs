using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using System.Collections;

[System.Serializable]
public class DialogueOption
{
    public string optionText;
    public DialogueTree nextDialogueTree;
}

[System.Serializable]
public class DialogueNode
{
    [TextArea] public string dialogueText;
    public DialogueAudio dialogueAudio;

    public List<string> eventIds = new List<string>();
    public List<DialogueOption> options;

    public string npcName;
    public string npcTag;

    public bool useDialogueAudio = true;

    [HideInInspector] public EventReference fmodSoundEvent;
}
