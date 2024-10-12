using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public int defaultSlotCount = 4;  // Default number of inventory slots
    public GameObject inventorySlotPrefab;  // Prefab for the inventory slots
    public Transform inventoryPanel;  // UI panel for inventory display
    public Transform handPosition;  // Position where the item appears in the player's hand
    public KeyCode dropKey = KeyCode.Q; // Key to drop an item
    public LayerMask groundLayer;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private int selectedSlotIndex = 0; // Currently selected inventory slot
    private GameObject heldItemInstance; // Instance of the item in the player's hand
    private Item currentItem;  // Reference to the currently equipped item
    private bool isSwitchingDisabled = false; // Flag to disable item switching

    private const float MaxSwitchableWeight = 10.0f; // Maximum weight allowed for switching
    private const float WeightThreshold1 = 10.0f; // Weight threshold for first action
    private const float WeightThreshold2 = 15.0f; // Weight threshold for second action

    private GameObject playerController; // CHANGE TO ACTUAL PLAYER MOVEMENT SCRIPT LATER


    private void Start()
    {
        playerController = GetComponent<GameObject>(); // CHANGE TO ACTUAL PLAYER MOVEMENT SCRIPT LATER
        CreateSlots(defaultSlotCount);
    }

    private void Update()
    {
        // Check if the drop key is pressed
        if (Input.GetKeyDown(dropKey))
        {
            DropItem();
        }

        // Check for scroll wheel input
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput > 0)
        {
            // Scroll up
            ScrollInventory(-1);
        }
        else if (scrollInput < 0)
        {
            // Scroll down
            ScrollInventory(1);
        }

        // Check for number key presses to select inventory slots
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectSlot(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectSlot(1);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SelectSlot(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SelectSlot(3);
        }

        // Check total weight and trigger actions
        float totalWeight = GetTotalWeight();

        if (totalWeight > WeightThreshold2)
        {
            if (playerController != null) // ADD MOVEMENT FALLOUTS LATER
            {
                // playerController.SetSprintDuration(3f); // Set a new sprint duration
                // playerController.UpdateMovementSettings(3f, 5f, 0.3f, 3f);
            }
        }
        else if (totalWeight > WeightThreshold1) // ADD MOVEMENT FALLOUTS LATER
        {
            if (playerController != null)
            {
                // playerController.SetSprintDuration(3f); // Set a new sprint duration
               // playerController.UpdateMovementSettings(3f, 5f, 0.3f, 3f);
            }
        }
        else
        {
            if (playerController != null) // ADD MOVEMENT FALLOUTS LATER
            {
                // playerController.SetSprintDuration(5f); // Set a new sprint duration
                // playerController.UpdateMovementSettings(5f, 7f, 0.5f, 4f);
            }
        }
    }

    // Selects the inventory slot by index
    private void SelectSlot(int index)
    {
        if (index >= 0 && index < slots.Count)
        {
            // Prevent switching if switching is disabled
            if (isSwitchingDisabled)
            {
                Debug.Log("Cannot switch items. The currently equipped item is too heavy!");
                return;
            }

            selectedSlotIndex = index;
            UpdateEquippedItem();
        }
    }

    // Creates inventory slots dynamically
    public void CreateSlots(int slotCount)
    {
        for (int i = 0; i < slotCount; i++)
        {
            GameObject newSlot = Instantiate(inventorySlotPrefab, inventoryPanel);
            InventorySlot slotComponent = newSlot.GetComponent<InventorySlot>();
            slots.Add(slotComponent);
        }
    }

    public bool HasItem(Item item)
    {
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item)
            {
                return true;
            }
        }
        return false;
    }

    public void RemoveItem(Item item)
    {
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item)
            {
                slot.ClearSlot();
                return;
            }
        }
    }

    public bool AddItem(Item item)
    {
        Debug.Log($"Attempting to add item: {item.name}");

        // Prevent adding new items if a heavy item is already equipped
        if (isSwitchingDisabled)
        {
            Debug.Log("Cannot pick up new items while a heavy item is equipped!");
            return false;
        }

        // Check if the current slot is occupied
        if (slots[selectedSlotIndex].GetItem() != null)
        {
            Debug.Log($"Slot {selectedSlotIndex} is occupied. Finding next available slot.");
        }

        // Try to add the item to the currently selected slot first
        if (slots[selectedSlotIndex].GetItem() == null)
        {
            slots[selectedSlotIndex].AddItem(item);
            EquipItem(item);
            return true;
        }

        // If the selected slot is full, find the nearest empty slot to the first slot
        for (int i = 0; i < slots.Count; i++)
        {
            int slotIndex = (selectedSlotIndex + i) % slots.Count;
            if (slots[slotIndex].GetItem() == null)
            {
                slots[slotIndex].AddItem(item);
                // Update selected slot index to the slot where the item was added
                selectedSlotIndex = slotIndex;

                // Equip the item if added to the selected slot
                EquipItem(item);
                return true;
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    // Drops the currently selected item
    public void DropItem()
    {
        InventorySlot selectedSlot = slots[selectedSlotIndex];
        Item itemToDrop = selectedSlot.GetItem();

        if (itemToDrop != null)
        {
            selectedSlot.ClearSlot();
            Destroy(heldItemInstance); // Destroy the currently held item in hand
            SpawnDroppedItem(itemToDrop); // Drop the item in the game world
            currentItem = null; // Clear current item reference after dropping
            isSwitchingDisabled = false; // Re-enable switching
        }
    }

    private void SpawnDroppedItem(Item item)
    {
        if (item.itemPrefab != null)  // Check if the item has a prefab
        {
            Vector3 startPosition = handPosition.position + Vector3.up * 10f;
            RaycastHit hit;
            if (Physics.Raycast(startPosition, Vector3.down, out hit, Mathf.Infinity, groundLayer))
            {
                Vector3 dropPosition = hit.point;
                GameObject droppedItem = Instantiate(item.itemPrefab, dropPosition, Quaternion.identity);

                MeshRenderer meshRenderer = droppedItem.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    float itemHeight = meshRenderer.bounds.size.y;
                    Vector3 adjustedDropPosition = dropPosition + Vector3.up * (itemHeight / 2);
                    droppedItem.transform.position = adjustedDropPosition;
                    droppedItem.transform.rotation = Quaternion.Euler(0, handPosition.eulerAngles.y, 0);
                }
                else
                {
                    Debug.LogWarning("No MeshRenderer found on item prefab.");
                }

                droppedItem.GetComponent<ItemPickUp>().item = item;
            }
            else
            {
                Debug.LogWarning("No ground detected. Item may drop in mid-air.");
                Vector3 fallbackPosition = handPosition.position - Vector3.up * 1f;
                GameObject droppedItem = Instantiate(item.itemPrefab, fallbackPosition, Quaternion.identity);
                droppedItem.GetComponent<ItemPickUp>().item = item;
            }
        }
        else
        {
            Debug.LogWarning("Item prefab not assigned!");
        }
    }

    // Handles scrolling through the inventory
    public void ScrollInventory(int direction)
    {
        // Prevent switching if switching is disabled
        if (isSwitchingDisabled)
        {
            Debug.Log("Cannot switch items. The currently equipped item is too heavy!");
            return;
        }

        selectedSlotIndex += direction;
        if (selectedSlotIndex >= slots.Count) selectedSlotIndex = 0;
        if (selectedSlotIndex < 0) selectedSlotIndex = slots.Count - 1;

        UpdateEquippedItem();
    }

    // Updates the currently equipped item
    private void UpdateEquippedItem()
    {
        InventorySlot selectedSlot = slots[selectedSlotIndex];
        EquipItem(selectedSlot.GetItem());
    }

    // Equips an item to the player's hand
    public void EquipItem(Item item)
    {
        if (heldItemInstance != null)
        {
            Destroy(heldItemInstance);
        }

        if (item != null)
        {
            if (item.weight > MaxSwitchableWeight)
            {
                // Switch to the heavy item and disable further switching
                heldItemInstance = Instantiate(item.itemPrefab, handPosition.position, handPosition.rotation);
                heldItemInstance.transform.SetParent(handPosition);
                currentItem = item;
                isSwitchingDisabled = true; // Disable switching if the item is too heavy
            }
            else
            {
                heldItemInstance = Instantiate(item.itemPrefab, handPosition.position, handPosition.rotation);
                heldItemInstance.transform.SetParent(handPosition);
                currentItem = item;
                isSwitchingDisabled = false; // Enable switching if the item is light
            }
        }
    }

    // Method to get the total weight of all items in the inventory
    public float GetTotalWeight()
    {
        float totalWeight = 0f;

        // Iterate through all slots and sum up the weights of the items
        foreach (var slot in slots)
        {
            Item item = slot.GetItem();
            if (item != null)
            {
                totalWeight += item.weight; // Sum up the weight of the item
            }
        }

        return totalWeight;
    }
}
