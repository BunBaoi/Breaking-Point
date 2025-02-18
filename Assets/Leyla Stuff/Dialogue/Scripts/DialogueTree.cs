using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogueTree", menuName = "Dialogue/DialogueTree")]
public class DialogueTree : ScriptableObject
{
    public List<DialogueNode> dialogueNodes; // List of all dialogue nodes in the tree
}

