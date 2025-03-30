using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "NewTip", menuName = "Game Tips/Tip")]
public class Tip : ScriptableObject
{
    [TextArea(3, 5)]
    public string tipText;  // The text that will be displayed
    public VideoClip tipVideo;  // The video associated with the tip
}
