using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "DialogueAudio", menuName = "Dialogue/Dialogue FMOD Audio")]
public class DialogueAudio : ScriptableObject
{
    public string id;
    public EventReference[] fmodSoundEvents;
    [Range(1, 10)] public int frequency = 1;
    [Range(0, 5)] public float minPitch = 0f;
    [Range(0, 5)] public float maxPitch = 1f;
}