using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Header("Items Database")]
    public List<Item> allItems = new List<Item>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // find an item by its name
    public Item GetItemByName(string itemName)
    {
        foreach (Item item in allItems)
        {
            if (item.itemName.Equals(itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }
        Debug.LogWarning($"Item with name '{itemName}' not found!");
        return null;
    }
}