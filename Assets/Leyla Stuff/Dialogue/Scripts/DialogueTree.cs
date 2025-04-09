using UnityEngine;
using System.Collections.Generic;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NewDialogueTree", menuName = "Dialogue/DialogueTree")]
public class DialogueTree : ScriptableObject
{
    public string treeID;
    public List<DialogueNode> dialogueNodes;

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(treeID))
        {
            treeID = Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this); // Mark the asset as dirty
            AssetDatabase.SaveAssets();   // Save the changes
        }
#endif
    }
}
