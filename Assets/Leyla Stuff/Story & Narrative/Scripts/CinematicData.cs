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
    public CinematicDataDialogueAudio[] dialoguesAndAudio; // Array of dialogue and corresponding FMOD audio events

    [Header("Random Texts")]
    public string[] randomTexts; // Array for random texts

    [Header("Camera Points")]
    public string pointA; // Starting camera point
    public string pointB; // Ending camera point

    [Header("Random Text Prefab")]
    public GameObject randomTextPrefab; // Prefab for displaying random texts
}

[System.Serializable]
public class CinematicDataDialogueAudio
{
    [TextArea]
    public string dialogue; // Dialogue text
    public EventReference fmodAudioEvent; // FMOD audio event for this dialogue
}
