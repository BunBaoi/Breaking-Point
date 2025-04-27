using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddInventoryItem : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Item item;


    public void AddTheInventoryItem()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (inventoryManager == null)
        {
            inventoryManager = playerObject.GetComponent<InventoryManager>();
        }

        if (inventoryManager != null && item != null)
        {
            // Check if the player already has the item
            if (!inventoryManager.HasItem(item))
            {
                bool added = inventoryManager.AddItem(item);
                if (added)
                {
                    Debug.Log("Starting item added: " + item.name);
                }
                else
                {
                    Debug.LogWarning("Failed to add item. Inventory might be full.");
                }
            }
            else
            {
                Debug.Log("Player already has the item: " + item.name);
            }
        }
        else
        {
            Debug.LogError("InventoryManager or Item is not assigned!");
        }
    }
}
