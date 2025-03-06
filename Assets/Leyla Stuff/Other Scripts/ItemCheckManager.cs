using UnityEngine;

public class ItemCheckManager : MonoBehaviour
{
    [SerializeField] private GameObject targetObject; // Reference to the object that contains the script to enable/disable
    [SerializeField] private string scriptToEnableOrDisableName; // The name of the script you want to enable/disable
    private MonoBehaviour scriptToEnableOrDisable;

    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Item itemToCheckFor; // The specific item that should enable the script

    void Start()
    {
        // Dynamically find the script by name on the targetObject
        if (targetObject != null)
        {
            scriptToEnableOrDisable = targetObject.GetComponent(scriptToEnableOrDisableName) as MonoBehaviour;

            if (scriptToEnableOrDisable == null)
            {
                Debug.LogWarning($"No script with name '{scriptToEnableOrDisableName}' found on {targetObject.name}.");
            }
        }
        else
        {
            Debug.LogWarning("Target object is not assigned.");
        }
    }

    void Update()
    {
        // Get the current item equipped by the player
        Item currentItem = inventoryManager.GetEquippedItem();

        // Check if the equipped item matches the specified item and whether it's equipped
        if (currentItem != null && currentItem == itemToCheckFor)
        {
            EnableScript(); // Enable the script if the item matches
        }
        else
        {
            DisableScript(); // Disable the script if the item doesn't match or isn't equipped
        }
    }

    private void EnableScript()
    {
        if (scriptToEnableOrDisable != null && !scriptToEnableOrDisable.enabled)
        {
            scriptToEnableOrDisable.enabled = true;
            Debug.Log($"{scriptToEnableOrDisable.GetType().Name} enabled.");
        }
    }

    private void DisableScript()
    {
        if (scriptToEnableOrDisable != null && scriptToEnableOrDisable.enabled)
        {
            scriptToEnableOrDisable.enabled = false;
            Debug.Log($"{scriptToEnableOrDisable.GetType().Name} disabled.");
        }
    }
}