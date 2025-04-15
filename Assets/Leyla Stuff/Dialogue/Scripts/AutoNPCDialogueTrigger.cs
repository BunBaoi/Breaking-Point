using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoNPCDialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueTree dialogueTree;
    [SerializeField] private Transform npcTransform;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();
            if (dialogueManager != null && dialogueTree != null)
            {
                Transform useTransform = npcTransform != null ? npcTransform : transform;
                dialogueManager.StartNPCDialogue(dialogueTree, useTransform);
            }
            else
            {
                Debug.LogWarning("DialogueManager or DialogueTree not found.");
            }
        }
    }
}