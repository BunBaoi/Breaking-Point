using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public int defaultSlotCount = 4;  // Default number of inventory slots
    public GameObject inventorySlotPrefab;  // Prefab for the inventory slots
    public Transform inventoryPanel;  // UI panel for inventory display
    public Transform leftHandPosition;  // Position for left hand item
    public Transform rightHandPosition;  // Position for right hand item
    public KeyCode dropKey = KeyCode.Q; // Key to drop an item
    public LayerMask groundLayer;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private int selectedSlotIndex = 0; // Currently selected inventory slot
    private GameObject heldLeftHandItemInstance; // Instance of the item in the player's left hand
    private GameObject heldRightHandItemInstance; // Instance of the item in the player's right hand
    private Item currentItem;  // Reference to the currently equipped item
    private bool isSwitchingDisabled = false; // Flag to disable item switching

    private const float MaxSwitchableWeight = 10.0f; // Maximum weight allowed for switching
    private const float WeightThreshold1 = 10.0f; // Weight threshold for first action
    private const float WeightThreshold2 = 15.0f; // Weight threshold for second action

    private GameObject playerController; // CHANGE TO ACTUAL PLAYER MOVEMENT SCRIPT LATER

    private PlayerClimbingState playerClimbingState;

    private void Start()
    {
        CreateSlots(defaultSlotCount);

        // Retrieve the PlayerClimbingState component instead of using GetComponent in the method call
        playerClimbingState = GetComponent<PlayerClimbingState>();
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
        if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
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
            // Clear the slot
            selectedSlot.ClearSlot();

            // Destroy left hand item
            if (heldLeftHandItemInstance != null)
            {
                Destroy(heldLeftHandItemInstance);
                heldLeftHandItemInstance = null;
            }

            // Destroy right hand item
            if (heldRightHandItemInstance != null)
            {
                Destroy(heldRightHandItemInstance);
                heldRightHandItemInstance = null;
            }

            // Reset climbing state if exists and component is available
            if (playerClimbingState != null)
            {
                playerClimbingState.ExitClimbingState(); // Use ExitClimbingState() method instead
            }

            SpawnDroppedItem(itemToDrop);
            currentItem = null;
            isSwitchingDisabled = false;
        }
    }

    private void SpawnDroppedItem(Item item)
    {
        if (item.itemPrefab != null)  // Check if the item has a prefab
        {
            // Use leftHandPosition instead of the removed handPosition
            Vector3 startPosition = leftHandPosition.position + Vector3.up * 10f;
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
                    // Use leftHandPosition's rotation instead of handPosition
                    droppedItem.transform.rotation = Quaternion.Euler(0, leftHandPosition.eulerAngles.y, 0);
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
                // Use leftHandPosition instead of handPosition
                Vector3 fallbackPosition = leftHandPosition.position - Vector3.up * 1f;
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

        // If the selected slot is empty or the item prevents climbing
        if (selectedSlot.GetItem() == null)
        {
            if (playerClimbingState != null)
            {
                playerClimbingState.DisableClimbing();
            }
        }
        else
        {
            // Re-enable climbing if the new item allows it
            if (playerClimbingState != null)
            {
                playerClimbingState.EnableClimbing();
            }
        }

        EquipItem(selectedSlot.GetItem());
    }

    // Equips an item to the player's hand(s)
    public void EquipItem(Item item)
    {
        // Destroy any existing held items
        if (heldLeftHandItemInstance != null)
        {
            Destroy(heldLeftHandItemInstance);
            heldLeftHandItemInstance = null;
        }

        if (heldRightHandItemInstance != null)
        {
            Destroy(heldRightHandItemInstance);
            heldRightHandItemInstance = null;
        }

        // Reset climbing state if exists
        if (playerClimbingState != null)
        {
            playerClimbingState.ExitClimbingState(); // Use ExitClimbingState() method
        }

        if (item != null)
        {
            // Check if the item is too heavy to switch
            if (item.weight > MaxSwitchableWeight)
            {
                // Equip with weight-based switching restrictions
                EquipItemWithWeightRestrictions(item);
            }
            else
            {
                // Normal item equipping
                EquipNormalItem(item);
            }
        }
    }

    private void EquipNormalItem(Item item)
    {
        // For single-hand items, equip on left hand
        if (item.handType == Item.HandType.SingleHand)
        {
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);
        }
        // For double-hand items, equip on both hands
        else if (item.handType == Item.HandType.DoubleHand)
        {
            // Left hand item
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);

            // Right hand item
            heldRightHandItemInstance = Instantiate(item.itemPrefab, rightHandPosition.position, rightHandPosition.rotation);
            heldRightHandItemInstance.transform.SetParent(rightHandPosition);
        }

        currentItem = item;
        isSwitchingDisabled = false; // Enable switching if the item is light
    }

    private void EquipItemWithWeightRestrictions(Item item)
    {
        // For single-hand items, equip on left hand
        if (item.handType == Item.HandType.SingleHand)
        {
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);
        }
        // For double-hand items, equip on both hands
        else if (item.handType == Item.HandType.DoubleHand)
        {
            // Left hand item
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);

            // Right hand item
            heldRightHandItemInstance = Instantiate(item.itemPrefab, rightHandPosition.position, rightHandPosition.rotation);
            heldRightHandItemInstance.transform.SetParent(rightHandPosition);
        }

        currentItem = item;
        isSwitchingDisabled = true; // Disable switching if the item is too heavy
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
