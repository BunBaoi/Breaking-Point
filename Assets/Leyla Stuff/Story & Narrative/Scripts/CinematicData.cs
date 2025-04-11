using UnityEngine;
using FMODUnity;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CinematicData", menuName = "Cinematic/CinematicData")]
public class CinematicData : ScriptableObject
{
    [Header("Chapter Info")]
    public string chapterName; // Chapter title
    public List<string> eventIds = new List<string>();

    [Header("Dialogue and Audio Events")]
    public CinematicDataDialogueAudio[] dialoguesAndAudio;

    [Header("Random Texts")]
    public string[] randomTexts;

    [Header("Camera Points")]
    public string pointA;
    public string pointB;

    [Header("Random Text Prefab")]
    public GameObject randomTextPrefab;
}

[System.Serializable]
public class CinematicDataDialogueAudio
{
    [TextArea]
    public string dialogue;
    public string npcName;
    public DialogueAudio dialogueAudio;

}
