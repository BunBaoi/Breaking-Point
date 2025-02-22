using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
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

    [Header("Setup Settings")]
    public int defaultSlotCount = 4;  // Default number of inventory slots
    public GameObject inventorySlotPrefab;  // Prefab for the inventory slots
    public Transform inventoryPanel;  // UI panel for inventory display
    public Transform leftHandPosition;  // Position for left hand item
    public Transform rightHandPosition;  // Position for right hand item
    // public KeyCode dropKey = KeyCode.Q; // Key to drop an item

    [Header("Layer Setup")]
    public LayerMask groundLayer;
    [SerializeField] private LayerMask itemLayer;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;  // Input Action Asset
    [SerializeField] private string scrollActionName = "Inventory Scroll";
    [SerializeField] private string dropActionName = "Drop";
    [SerializeField] private string slotActionsName = "Slot";
    [SerializeField] private float scrollCooldown = 0.1f; // Cooldown time (in seconds)
    private float lastScrollTime = 0f; // Time when the last scroll occurred
    private InputAction scrollAction;
    private InputAction dropAction;
    private List<InputAction> slotActions = new List<InputAction>();



    private ClimbingSystem climbingSystem;

    private void Awake()
    {
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");

            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }
    }

    private void Start()
    {
        climbingSystem = GetComponent<ClimbingSystem>();
        DisableScriptsOnInventoryItems();
        CreateSlots(defaultSlotCount);

        // Retrieve the PlayerClimbingState component instead of using GetComponent in the method call
        playerClimbingState = GetComponent<PlayerClimbingState>();

        scrollAction = inputActions.FindAction(scrollActionName);
        dropAction = inputActions.FindAction(dropActionName);

        if (scrollAction != null)
        {
            scrollAction.Enable();
        }

        if (dropAction != null)
        {
            dropAction.Enable();
        }

        for (int i = 0; i < defaultSlotCount; i++)
        {
            string actionName = slotActionsName + (i + 1); 
            InputAction slotAction = inputActions.FindAction(actionName);
            if (slotAction != null)
            {
                slotActions.Add(slotAction);
                slotAction.Enable();
            }
        }
    }

    private void Update()
    {
        // Check if the drop key is pressed
        if (dropAction.triggered)
        {
            DropItem();
        }

        // Scroll wheel input (using the scroll action)
        float scrollInput = scrollAction.ReadValue<float>();
        if (Time.time - lastScrollTime >= scrollCooldown) // Check if cooldown has passed
        {
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

            // Update the time of the last scroll
            lastScrollTime = Time.time;
        }

        // Check slot actions based on available slots
        for (int i = 0; i < slotActions.Count; i++)
        {
            if (slotActions[i].triggered)
            {
                SelectSlot(i);
                break; // Exit after triggering the first matching slot action
            }
        }
    }

        // Selects the inventory slot by index
        private void SelectSlot(int index)
        {
            // Check if the index is within valid range
            if (index >= 0 && index < slots.Count)
            {
                // Prevent switching if switching is disabled
                if (isSwitchingDisabled)
                {
                    Debug.Log("Cannot switch items. The currently equipped item is too heavy!");
                    return;
                }

                // Log the correct selected slot index
                Debug.Log("Selecting Slot: " + index);

                // Now actually set the selected slot index
                selectedSlotIndex = index;

                // Update the equipped item (optional, based on your implementation)
                UpdateEquippedItem();
            }
            else
            {
                Debug.LogWarning("Invalid slot index: " + index + ". Available slots: " + slots.Count);
            }
        }

        public void CreateSlots(int slotCount)
    {
        for (int i = 0; i < slotCount; i++)
        {
            // Instantiate the slot prefab and get its components
            GameObject newSlot = Instantiate(inventorySlotPrefab, inventoryPanel);
            InventorySlot slotComponent = newSlot.GetComponent<InventorySlot>();
            slots.Add(slotComponent);

            // Set the name of the slot GameObject based on the index
            newSlot.name = "Slot_" + i;
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
            DisableItemPickup(item);
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
                DisableItemPickup(item);
                return true;
            }
        }

        Debug.Log("Inventory is full!");
        return false;
    }

    public void DisableItemPickup(Item item)
    {
        GameObject itemObject = FindItemInScene(item);

        if (itemObject == null)
        {
            Debug.LogWarning("Item instance not found in the scene: " + item.itemPrefab.name);
            return;
        }

        Debug.Log("Disabling ItemPickUp on item: " + itemObject.name);

        ItemPickUp itemPickUpScript = itemObject.GetComponent<ItemPickUp>();
        if (itemPickUpScript != null)
        {
            Debug.Log("ItemPickUp script found. Disabling it.");
            itemPickUpScript.enabled = false;
        }
        else
        {
            Debug.Log("No ItemPickUp script attached to item.");
        }

        // Enable all other components
        MonoBehaviour[] components = itemObject.GetComponents<MonoBehaviour>();
        foreach (var component in components)
        {
            if (component is ItemPickUp) continue;
            Debug.Log("Enabling script: " + component.GetType().Name);
            component.enabled = true;
        }
    }

    public void EnableItemPickup(Item item)
    {
        GameObject itemObject = FindItemInScene(item);

        if (itemObject == null)
        {
            Debug.LogWarning("Item instance not found in the scene: " + item.itemPrefab.name);
            return;
        }

        Debug.Log("Enabling ItemPickUp on item: " + itemObject.name);

        Component[] itemComponents = itemObject.GetComponents<Component>();
        foreach (Component component in itemComponents)
        {
            if (component is ItemPickUp == false)
            {
                Debug.Log("Disabling component: " + component.GetType().Name);
                Behaviour behaviour = component as Behaviour;
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }
            else
            {
                Debug.Log("Enabling ItemPickUp component.");
                ItemPickUp itemPickUp = component as ItemPickUp;
                if (itemPickUp != null)
                {
                    itemPickUp.enabled = true;
                }
            }
        }
    }

    private GameObject FindItemInScene(Item item)
    {
        GameObject[] sceneObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (GameObject obj in sceneObjects)
        {
            if (obj.name == item.itemPrefab.name || obj.name == item.itemPrefab.name + "(Clone)")
            {
                return obj;
            }
        }

        return null;
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

            if (itemToDrop.name.ToLower().Contains("icepick") && climbingSystem != null)
            {
                climbingSystem.enabled = false;
            }

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
            EnableItemPickup(item);
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
        Item currentItem = selectedSlot.GetItem();

        if (currentItem != null)
        {
            // Check if the current item is an ice pick
            bool hasIcePick = currentItem.name.ToLower().Contains("icepick");

            // Enable/disable climbing based on ice pick
            if (climbingSystem != null)
            {
                climbingSystem.enabled = hasIcePick;
            }
        }
        else
        {
            // Disable climbing when no item is equipped
            if (climbingSystem != null)
            {
                climbingSystem.enabled = false;
            }
        }

        EquipItem(currentItem);
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

    private void DisableScriptsOnInventoryItems()
    {
        // Find all objects in the specified itemLayer
        GameObject[] inventoryItems = FindObjectsInLayer(itemLayer);

        foreach (GameObject item in inventoryItems)
        {
            MonoBehaviour[] scripts = item.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                // Disable all scripts except the ItemPickUp script
                if (script.GetType() != typeof(ItemPickUp))
                {
                    script.enabled = false;
                }
            }
        }
    }

    // Utility function to find all objects in a specific layer
    private GameObject[] FindObjectsInLayer(LayerMask layerMask)
    {
        // Get all objects in the scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        // Filter the objects based on the specified layer
        List<GameObject> objectsInLayer = new List<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & layerMask) != 0)
            {
                objectsInLayer.Add(obj);
            }
        }

        return objectsInLayer.ToArray();
    }
}
