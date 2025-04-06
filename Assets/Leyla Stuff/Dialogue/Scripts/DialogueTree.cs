using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "NewDialogueTree", menuName = "Dialogue/DialogueTree")]
public class DialogueTree : ScriptableObject
{
    public string treeID;
    public List<DialogueNode> dialogueNodes;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(treeID))
        {
            treeID = Guid.NewGuid().ToString(); // Generate a unique ID
        }
    }
}

