using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAutomaticDialogue", menuName = "Dialogue/AutomaticDialogue")]
public class AutomaticDialogue : ScriptableObject
{
    [Header("Dialogue Settings")]
    [SerializeField] private DialogueTree dialogueTree;
    [SerializeField] private float timeBetweenDialogues = 1.5f; // Time to wait between each dialogue node
    [SerializeField] private bool waitForFullTextBeforeProceeding = true; // Wait until text has finished typing
    [SerializeField] private float additionalPauseAfterText = 1.0f; // Extra pause after text is fully shown

    [Header("Auto Options")]
    [SerializeField] private bool autoSelectFirstOption = true; // Automatically select first option when choices appear
    [SerializeField] private float timeBeforeAutoSelectingOption = 2.0f; // Time to wait before auto-selecting an option

    public DialogueTree DialogueTree => dialogueTree;
    public float TimeBetweenDialogues => timeBetweenDialogues;
    public bool WaitForFullTextBeforeProceeding => waitForFullTextBeforeProceeding;
    public float AdditionalPauseAfterText => additionalPauseAfterText;
    public bool AutoSelectFirstOption => autoSelectFirstOption;
    public float TimeBeforeAutoSelectingOption => timeBeforeAutoSelectingOption;
}
