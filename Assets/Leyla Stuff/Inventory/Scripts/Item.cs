using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;  // Icon to display in the inventory
    public ItemType itemType;  // Define the item type
    public string description;
    public string[] playerInputs;
    public GameObject itemPrefab;
    public float weight; // Weight of the item in kilograms (kg)
    public HandType handType = HandType.SingleHand;
    public bool isBound = false;  // Whether the item is bound or not

    public enum ItemType
    {
        Tool,
        Consumable,
    }

    public enum HandType
    {
        SingleHand,   // Item is held in only the left hand
        DoubleHand    // Item is held in both left and right hands
    }
}
