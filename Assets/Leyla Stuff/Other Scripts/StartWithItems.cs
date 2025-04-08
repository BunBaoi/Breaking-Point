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
            Debug.LogError("InventoryManager or Item is not assigned!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
