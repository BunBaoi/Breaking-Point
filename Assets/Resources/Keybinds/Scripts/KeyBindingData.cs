using UnityEngine;

[System.Serializable]
public class KeyBinding
{
    public string actionName; // Example: "Interact", "Jump"
    public Sprite keySprite; // Keyboard/Mouse Sprite
    public Sprite controllerSprite; // Controller Sprite
}

[CreateAssetMenu(fileName = "KeyBindingData", menuName = "Input/KeyBindingData")]
public class KeyBindingData : ScriptableObject
{
    public KeyBinding[] keyBindings;
}
