using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    private List<InventorySlot> slots = new List<InventorySlot>();
    private int selectedSlotIndex = 0; // Currently selected inventory slot
    private GameObject heldLeftHandItemInstance;
    private GameObject heldRightHandItemInstance;
    private Item currentItem;  // Reference to the currently equipped item

    [Header("Setup Settings")]
    public int defaultSlotCount = 4;
    public GameObject inventorySlotPrefab;
    public Transform inventoryPanel;
    public Transform leftHandPosition;
    public Transform rightHandPosition;
    [SerializeField] private TMP_Text updateText;
    private Coroutine currentCoroutine;

    [Header("Layer Setup")]
    public LayerMask groundLayer;
    [SerializeField] private LayerMask itemLayer;

    [Header("Keybinds")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private TMP_Text equippedItemText;
    [SerializeField] private string scrollActionName = "Inventory Scroll";
    [SerializeField] private string dropActionName = "Drop";
    [SerializeField] private string slotActionsName = "Slot";
    [SerializeField] private float scrollCooldown = 0f; // Cooldown time
    private float lastScrollTime = 0f;
    private InputAction scrollAction;
    private InputAction dropAction;
    private List<InputAction> slotActions = new List<InputAction>();


    // IF USING WEIGHT
    private const float WeightThreshold1 = 10.0f; // Weight threshold for first action
    private const float WeightThreshold2 = 15.0f; // Weight threshold for second action
    private const float MaxSwitchableWeight = 10.0f; // Maximum weight allowed for switching
    private bool isSwitchingDisabled = false; // Flag to disable item switching

    private ClimbingSystem climbingSystem;

    private void Awake()
    {
        DisableScriptsOnInventoryItems();
        if (inputActions == null)
        {
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");

            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }

        DisableScriptsOnInventoryItems();
        CreateSlots(defaultSlotCount);

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

        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        SelectSlot(0, -1);

        UpdateSlotImageForAllSlots(selectedSlotIndex);
    }

    public void SaveInventory()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // Save item in the current slot
            Item item = slots[i].GetItem();
            if (item != null)
            {
                PlayerPrefs.SetString("Inventory_Slot_" + i, item.name);
            }
            else
            {
                PlayerPrefs.SetString("Inventory_Slot_" + i, "");
            }
        }
        PlayerPrefs.SetInt("SelectedSlotIndex", selectedSlotIndex);
        PlayerPrefs.Save();
    }
    public void ClearInventory()
    {
        // Clear the items in all slots
        foreach (var slot in slots)
        {
            slot.ClearSlot();
        }

        selectedSlotIndex = 0;

        // Update UI to reflect cleared inventory
        UpdateSlotImageForAllSlots(selectedSlotIndex);
    }


    public void LoadInventory(List<string> inventoryItems)
    {
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            string itemName = inventoryItems[i];
            Item item = FindItemByName(itemName); // Find the item by name
            if (item != null && i < slots.Count)
            {
                slots[i].AddItem(item); // Add item to the corresponding slot
            }
            else
            {
                Debug.LogWarning($"Item {itemName} not found or slot index out of range.");
            }
        }

        // Load selected slot index if available
        selectedSlotIndex = PlayerPrefs.GetInt("SelectedSlotIndex", 0);
        UpdateSlotImageForAllSlots(selectedSlotIndex); // Update the UI
    }

    public List<string> GetInventoryIDs()
    {
        List<string> itemIDs = new List<string>();

        foreach (InventorySlot slot in slots)
        {
            Item item = slot.GetItem();
            if (item != null)
            {
                itemIDs.Add(item.itemName);
            }
        }

        return itemIDs;
    }

    private Item FindItemByName(string itemName)
    {
        return ItemDatabase.Instance.GetItemByName(itemName);
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
                int previousSlotIndex = selectedSlotIndex;  // Store the current selected slot as previous

                // Call the updated SelectSlot method with both current and previous indices
                SelectSlot(i, previousSlotIndex);

                UpdateSlotImageForAllSlots(i);

                break; // Exit after triggering the first matching slot action
            }
        }
    }

        private void UpdateSlotImageForAllSlots(int selectedSlotIndex)
    {
        // Loop through all the slots and update their images based on whether they are selected or not
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == selectedSlotIndex)
            {
                // Update the selected slot
                slots[i].UpdateSlotImage(true);  // Set the selected slot image
                slots[i].UpdateSelection(true);  // Mark it as selected
            }
            else
            {
                // Update the non-selected slots
                slots[i].UpdateSlotImage(false); // Set the default slot image
                slots[i].UpdateSelection(false); // Deselect the slot
            }
        }
    }

    private void SelectSlot(int index, int previousIndex)
    {
        if (index >= 0 && index < slots.Count)
        {
            if (isSwitchingDisabled)
            {
                Debug.Log("Cannot switch items. The currently equipped item is too heavy!");
                return;
            }

            // Update the previous slot to show as unequipped and return to original scale
            if (previousIndex >= 0 && previousIndex < slots.Count)
            {
                slots[previousIndex].UpdateSlotImage(false);
                slots[previousIndex].UpdateSelection(false);

                // Gradually reset the previous slot scale back to normal (1x)
                RectTransform prevRect = slots[previousIndex].GetComponent<RectTransform>();
                if (prevRect != null)
                {
                    StartCoroutine(ScaleSlotOverTime(prevRect, prevRect.localScale, Vector3.one, 0.3f));
                }
            }

            selectedSlotIndex = index;

            // Update the new selected slot to show as equipped
            slots[selectedSlotIndex].UpdateSlotImage(true);
            slots[selectedSlotIndex].UpdateSelection(true);

            // Gradually scale up the selected slot by 1.5x
            RectTransform selectedRect = slots[selectedSlotIndex].GetComponent<RectTransform>();
            if (selectedRect != null)
            {
                StartCoroutine(ScaleSlotOverTime(selectedRect, selectedRect.localScale, new Vector3(1.5f, 1.5f, 1f), 0.3f));
            }

            UpdateEquippedItem();
        }
        else
        {
            Debug.LogWarning("Invalid slot index: " + index + ". Available slots: " + slots.Count);
        }
    }

    // Coroutine to scale the slot over time
    private IEnumerator ScaleSlotOverTime(RectTransform rectTransform, Vector3 fromScale, Vector3 toScale, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            rectTransform.localScale = Vector3.Lerp(fromScale, toScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        rectTransform.localScale = toScale; // Ensure the final scale is set
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

        // Gradually reset the previous slot scale back to normal (1x)
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            RectTransform prevRect = slots[selectedSlotIndex].GetComponent<RectTransform>();
            if (prevRect != null)
            {
                StartCoroutine(ScaleSlotOverTime(prevRect, prevRect.localScale, Vector3.one, 0.3f)); // Smoothly scale back to normal size
            }
        }

        // Try to add the item to the currently selected slot first
        if (slots[selectedSlotIndex].GetItem() == null)
        {
            slots[selectedSlotIndex].AddItem(item);
            EquipItem(item);
            DisableItemPickup(item);

            // Gradually scale up the selected slot by 1.5x
            RectTransform selectedRect = slots[selectedSlotIndex].GetComponent<RectTransform>();
            if (selectedRect != null)
            {
                StartCoroutine(ScaleSlotOverTime(selectedRect, selectedRect.localScale, new Vector3(1.5f, 1.5f, 1f), 0.3f)); // Smoothly scale up
            }

            return true;
        }

        // If the selected slot is full, find the nearest empty slot to the first slot
        for (int i = 0; i < slots.Count; i++)
        {
            int slotIndex = (selectedSlotIndex + i) % slots.Count;
            if (slots[slotIndex].GetItem() == null)
            {
                slots[slotIndex].AddItem(item);
                selectedSlotIndex = slotIndex;

                // Equip the item if added to the selected slot
                EquipItem(item);
                DisableItemPickup(item);

                // Gradually scale up the selected slot by 1.5x
                RectTransform selectedRect = slots[selectedSlotIndex].GetComponent<RectTransform>();
                if (selectedRect != null)
                {
                    StartCoroutine(ScaleSlotOverTime(selectedRect, selectedRect.localScale, new Vector3(1.5f, 1.5f, 1f), 0.3f)); // Smoothly scale up
                }

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

    public void DropItem()
    {
        InventorySlot selectedSlot = slots[selectedSlotIndex];
        Item itemToDrop = selectedSlot.GetItem();

        if (itemToDrop != null)
        {
            // Check if the item is bound and prevent dropping if it is
            if (itemToDrop.isBound)
            {
                if (currentCoroutine != null)
                {
                    StopCoroutine(currentCoroutine);
                }
                updateText.text = "";
                currentCoroutine = StartCoroutine(DisplayUpdateMessage("Item cannot be dropped, it's bound!"));
                Debug.Log("This item is bound and cannot be dropped.");
                return;  // Exit the method without dropping the item
            }

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

            SpawnDroppedItem(itemToDrop);
            currentItem = null;
            isSwitchingDisabled = false;
        }
    }

    private IEnumerator DisplayUpdateMessage(string message)
    {
        updateText.text = message;  // Set the text
        updateText.gameObject.SetActive(true);  // Make sure the text is visible
        yield return new WaitForSeconds(1.5f);  // Display for 2 seconds
        updateText.text = "";
        updateText.gameObject.SetActive(false);  // Hide the text
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

                ObjectTracker.Instance.TrackDroppedItem(item.itemPrefab.name, droppedItem.transform.position);

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

                ObjectTracker.Instance.TrackDroppedItem(item.itemPrefab.name, droppedItem.transform.position);
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

        int previousSlotIndex = selectedSlotIndex;  // Store the previous slot index

        selectedSlotIndex += direction;
        if (selectedSlotIndex >= slots.Count) selectedSlotIndex = 0;
        if (selectedSlotIndex < 0) selectedSlotIndex = slots.Count - 1;

        // Pass the previous slot index to properly reset its scale
        SelectSlot(selectedSlotIndex, previousSlotIndex);
        UpdateEquippedItem();
    }

    // Updates the currently equipped item
    private void UpdateEquippedItem()
    {
        InventorySlot selectedSlot = slots[selectedSlotIndex];
        Item currentItem = selectedSlot.GetItem();

        if (currentItem != null)
        {

        }

        EquipItem(currentItem);
    }

    public Item GetEquippedItem()
    {
        // Check if the selectedSlotIndex is within the valid range
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            InventorySlot selectedSlot = slots[selectedSlotIndex];
            if (selectedSlot != null && selectedSlot.GetItem() != null)
            {
                return selectedSlot.GetItem();
            }
            else
            {
                // Debug.LogWarning("No item equipped in the selected slot.");
            }
        }
        else
        {
            Debug.LogWarning("Selected slot index is out of range.");
        }

        return null;
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

        if (item != null)
        {
            // Check if the item is too heavy to switch
            if (item.weight > MaxSwitchableWeight)
            {
                slots[selectedSlotIndex].UpdateSlotImage(true);
                slots[selectedSlotIndex].UpdateSelection(true);
                EquipItemWithWeightRestrictions(item);
            }
            else
            {
                slots[selectedSlotIndex].UpdateSlotImage(true);
                slots[selectedSlotIndex].UpdateSelection(true);
                EquipNormalItem(item);
            }

            // Update UI text with rebound key names only if the item has playerInputs
            if (item.playerInputs != null && item.playerInputs.Length > 0)
            {
                equippedItemText.text = "Item Keybind: " + GetReboundKeyNames(item);
            }
            else
            {
                equippedItemText.text = ""; // No input bindings, clear the text
            }
        }
        else
        {
            equippedItemText.text = ""; // Clear text when no item is equipped
        }
    }

    private string GetReboundKeyNames(Item item)
    {
        if (item.playerInputs == null || item.playerInputs.Length == 0) return "Unknown Input";

        string[] keyNames = item.playerInputs
            .Select(inputName =>
            {
                InputAction action = inputActions.FindAction(inputName);
                if (action != null && action.controls.Count > 0)
                {
                // Determine the control scheme being used (Controller or Keyboard)
                int bindingIndex = KeyBindingManager.Instance.IsUsingController() ? 1 : 0;

                // Check if the binding exists for the selected control scheme
                if (action.bindings.Count > bindingIndex)
                    {
                        InputBinding binding = action.bindings[bindingIndex];
                        return KeyBindingManager.Instance.GetSanitisedKeyName(binding.effectivePath); // Get rebounded key name
                }
                }
                return inputName; // Default to action name if no binding exists
        })
            .ToArray();

        // Handle the formatting for 2 or more actions
        if (keyNames.Length == 1)
        {
            return keyNames[0]; // Single action name
        }
        else if (keyNames.Length == 2)
        {
            return keyNames[0] + " & " + keyNames[1]; // Two actions with "&" in the middle
        }
        else
        {
            // More than two actions, put "&" before the last action
            string lastAction = keyNames[keyNames.Length - 1];
            string actionsExceptLast = string.Join(", ", keyNames.Take(keyNames.Length - 1));
            return actionsExceptLast + " & " + lastAction;
        }
    }

    private void PositionIcepickByHinge(GameObject icepickInstance, Transform handPosition)
    {
        if (icepickInstance == null || handPosition == null) return;

        // Find the IcepickHinge in the instantiated object
        Transform hingeTransform = icepickInstance.transform.Find("IcepickHinge");

        if (hingeTransform != null)
        {
            // We want to position the icepick so the hinge aligns with the hand position
            Vector3 offset = handPosition.position - hingeTransform.position;
            icepickInstance.transform.position += offset;

            Debug.Log($"Positioned icepick by the hinge point for {handPosition.name}");
        }
        else
        {
            Debug.LogWarning("IcepickHinge not found on the instantiated icepick");
        }
    }

    private void EquipNormalItem(Item item)
    {
        if (item.handType == Item.HandType.SingleHand)
        {
            // Create the equipped item instance
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);

            // Check if this is an icepick by name or tag
            if (item.name.ToLower().Contains("icepick") || item.itemPrefab.CompareTag("Icepick"))
            {
                PositionIcepickByHinge(heldLeftHandItemInstance, leftHandPosition);
            }

            DisablePickUpCollider(heldLeftHandItemInstance);
        }
        else if (item.handType == Item.HandType.DoubleHand)
        {
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);

            // Check if this is an icepick by name or tag
            if (item.name.ToLower().Contains("icepick") || item.itemPrefab.CompareTag("Icepick"))
            {
                PositionIcepickByHinge(heldLeftHandItemInstance, leftHandPosition);
            }

            heldRightHandItemInstance = Instantiate(item.itemPrefab, rightHandPosition.position, rightHandPosition.rotation);
            heldRightHandItemInstance.transform.SetParent(rightHandPosition);

            // If it's a double-handed icepick, do the same for the right hand
            if (item.name.ToLower().Contains("icepick") || item.itemPrefab.CompareTag("Icepick"))
            {
                PositionIcepickByHinge(heldRightHandItemInstance, rightHandPosition);
            }

            DisablePickUpCollider(heldLeftHandItemInstance);
            DisablePickUpCollider(heldRightHandItemInstance);
        }

        currentItem = item;
        isSwitchingDisabled = false;
    }

    private void DisablePickUpCollider(GameObject itemInstance)
    {
        if (itemInstance != null)
        {
            // Check for the collider on the parent object
            Collider parentCollider = itemInstance.GetComponent<Collider>();
            if (parentCollider != null)
            {
                parentCollider.enabled = false;  // Disable the collider on the parent object
                Debug.Log($"Disabled collider on parent: {itemInstance.name}");
            }
            else
            {
                Debug.LogWarning("No collider found on parent " + itemInstance.name);
            }

            // Also, check and disable the collider on the child (if it exists)
            Transform pickupColliderTransform = itemInstance.transform.Find("Pick Up Collider");
            if (pickupColliderTransform != null)
            {
                Collider pickupCollider = pickupColliderTransform.GetComponent<Collider>();
                if (pickupCollider != null)
                {
                    pickupCollider.enabled = false;
                    Debug.Log($"Disabled 'Pick Up Collider' on child: {pickupColliderTransform.name}");
                }
            }
            else
            {
                Debug.LogWarning("No 'Pick Up Collider' found on " + itemInstance.name);
            }
        }
        else
        {
            Debug.LogWarning("itemInstance is null");
        }
    }


    private void EquipItemWithWeightRestrictions(Item item)
    {
        // For single-hand items, equip on left hand
        if (item.handType == Item.HandType.SingleHand)
        {
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);

            // Check if this is an icepick
            if (item.name.ToLower().Contains("icepick") || item.itemPrefab.CompareTag("Icepick"))
            {
                PositionIcepickByHinge(heldLeftHandItemInstance, leftHandPosition);
            }

            DisablePickUpCollider(heldLeftHandItemInstance);
        }
        // For double-hand items, equip on both hands
        else if (item.handType == Item.HandType.DoubleHand)
        {
            // Left hand item
            heldLeftHandItemInstance = Instantiate(item.itemPrefab, leftHandPosition.position, leftHandPosition.rotation);
            heldLeftHandItemInstance.transform.SetParent(leftHandPosition);

            if (item.name.ToLower().Contains("icepick") || item.itemPrefab.CompareTag("Icepick"))
            {
                PositionIcepickByHinge(heldLeftHandItemInstance, leftHandPosition);
            }

            DisablePickUpCollider(heldLeftHandItemInstance);

            // Right hand item
            heldRightHandItemInstance = Instantiate(item.itemPrefab, rightHandPosition.position, rightHandPosition.rotation);
            heldRightHandItemInstance.transform.SetParent(rightHandPosition);

            if (item.name.ToLower().Contains("icepick") || item.itemPrefab.CompareTag("Icepick"))
            {
                PositionIcepickByHinge(heldRightHandItemInstance, rightHandPosition);
            }

            DisablePickUpCollider(heldRightHandItemInstance);
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
