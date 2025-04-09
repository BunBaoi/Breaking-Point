using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartWithItems : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Item journalItem;


    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (inventoryManager == null)
        {
            inventoryManager = playerObject.GetComponent<InventoryManager>();
        }

        if (inventoryManager != null && journalItem != null)
        {
            // Check if the player already has the journal item
            if (!inventoryManager.HasItem(journalItem))
            {
                bool added = inventoryManager.AddItem(journalItem);
                if (added)
                {
                    Debug.Log("Starting item added: " + journalItem.name);
                }
                else
                {
                    Debug.LogWarning("Failed to add item. Inventory might be full.");
                }
            }
            else
            {
                Debug.Log("Player already has the item: " + journalItem.name);
            }
        }
        else
        {
            Debug.LogError("InventoryManager or Item is not assigned!");
        }
    }

        // Update is called once per frame
        void Update()
    {
        
    }
}
