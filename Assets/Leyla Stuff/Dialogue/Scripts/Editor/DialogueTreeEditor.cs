using UnityEngine;
using UnityEditor;
using FMODUnity;
using System.Collections.Generic;

[CustomEditor(typeof(DialogueTree))]
public class DialogueTreeEditor : Editor
{
    // Which DialogueNode should have its fmodSoundEvent visible
    private Dictionary<DialogueNode, bool> nodeFmodVisibility = new Dictionary<DialogueNode, bool>();

    private void OnEnable()
    {
        DialogueTree dialogueTree = (DialogueTree)target;
        foreach (var node in dialogueTree.dialogueNodes)
        {
            // Set the initial visibility of fmodSoundEvent to false for all nodes
            if (!nodeFmodVisibility.ContainsKey(node))
            {
                nodeFmodVisibility[node] = false;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        DialogueTree dialogueTree = (DialogueTree)target;

        DrawDefaultInspector();

        // Go through all dialogue nodes and ensure the visibility state is tracked
        foreach (var node in dialogueTree.dialogueNodes)
        {
            if (!nodeFmodVisibility.ContainsKey(node))
            {
                nodeFmodVisibility[node] = false;
            }

            // Only display fmod sound event when useDialogueAudio is false
            if (!node.useDialogueAudio)
            {
                // Display a button to toggle visibility of fmodSoundEvent for each DialogueNode
                if (GUILayout.Button($"Toggle FMOD Sound Event for {node.npcName}"))
                {
                    nodeFmodVisibility[node] = !nodeFmodVisibility[node];
                }

                // If the button was clicked and the fmodSoundEvent should be shown
                if (nodeFmodVisibility[node])
                {
                    SerializedProperty fmodSoundEventProp = serializedObject.FindProperty("dialogueNodes")
                        .GetArrayElementAtIndex(dialogueTree.dialogueNodes.IndexOf(node))
                        .FindPropertyRelative("fmodSoundEvent");

                    // Display the FMOD Sound Event field for the node
                    EditorGUILayout.PropertyField(fmodSoundEventProp, new GUIContent("FMOD Sound Event"));
                }
            }
        }

        // Apply changes to the serialized object of DialogueTree
        serializedObject.ApplyModifiedProperties();
    }
}

