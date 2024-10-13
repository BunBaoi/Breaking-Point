using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;  // Icon to display in the inventory
    public ItemType itemType;  // Define the item type
    public string description;
    public GameObject itemPrefab;
    public float weight; // Weight of the item in kilograms (kg)

    public enum ItemType
    {
        Tool,
        Consumable,
    }
}
