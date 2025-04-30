using UnityEngine;

public class ItemDropZone : MonoBehaviour
{
    [SerializeField] private string boolName = ""; // Name of the bool to set
    [SerializeField] private LayerMask itemLayer; // Layer mask for identifying items
    [SerializeField] private Item item;
    [SerializeField] private bool hasSetBool = false;

    private void OnTriggerStay(Collider other)
    {
        // Check if the object is in the 'Item' layer
        Debug.Log("OnTriggerStay called with: " + other.gameObject.name);

        if (((1 << other.gameObject.layer) & itemLayer) != 0)
        {
            ItemPickUp itemPickUp = other.GetComponent<ItemPickUp>();

            if (itemPickUp != null)
            {
                Debug.Log("ItemPickUp found on: " + other.gameObject.name);

                if (itemPickUp.item != null)
                {
                    Debug.Log("ItemPickUp has an item: " + itemPickUp.item.itemName);
                }

                if (itemPickUp.item != null && itemPickUp.item.itemPrefab == item.itemPrefab && !InventoryManager.Instance.HasItem(item))
                {
                    Debug.Log("Item matches the prefab and is not in inventory.");

                    // Only set the bool if it hasn't been set yet
                    if (!BoolManager.Instance.GetBool(boolName))
                    {
                        Debug.Log("Setting bool: " + boolName);
                        BoolManager.Instance.SetBool(boolName, true);
                        Debug.Log("Item dropped outside inventory: " + item.itemName);
                        hasSetBool = true; // Mark the bool as set
                    }
                    else
                    {
                        Debug.Log("Bool already set, not setting again.");
                    }
                }
                else
                {
                    Debug.Log("Item does not match prefab or is in inventory.");
                }
            }
            else
            {
                Debug.Log("ItemPickUp component not found on: " + other.gameObject.name);
            }
        }
        else
        {
            Debug.Log("Other object is not in the itemLayer: " + other.gameObject.name);
        }
    }

    /*private void OnTriggerExit(Collider other)
    {
        // Reset the flag when the item leaves the trigger zone
        Debug.Log("OnTriggerExit called with: " + other.gameObject.name);

        if (((1 << other.gameObject.layer) & itemLayer) != 0)
        {
            if (hasSetBool) // Only reset if the bool was previously set
            {
                Debug.Log("Resetting bool: " + boolName);
                BoolManager.Instance.SetBool(boolName, false);
                // hasSetBool = false; // Reset the flag when item exits the trigger
            }
            else
            {
                Debug.Log("Bool was not set, no need to reset.");
            }
        }
        else
        {
            Debug.Log("Other object is not in the itemLayer: " + other.gameObject.name);
        }
    }*/
}

