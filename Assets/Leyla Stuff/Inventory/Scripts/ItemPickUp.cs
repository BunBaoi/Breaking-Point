using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickUp : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Item item; // Reference to the item that can be picked up
    [SerializeField] private float raycastDistance = 5f; // Distance to check for raycast
    [SerializeField] private float pickupRadius = 1f; // Radius around the centre of the screen for pickup detection
    [SerializeField] private string playerCameraTag = "PlayerCamera"; // Tag for the player's camera
    [SerializeField] private string playerTag = "Player"; // Tag for the player object
    [SerializeField] private LayerMask itemLayer; // Layer mask to specify which layers are considered as items
    [SerializeField] private LayerMask pickUpColliderLayer; // Layer mask to specify which layers are considered as pick-up colliders
    [SerializeField] private string uniqueItemID;

    [SerializeField] private bool canPickUp = false; // Flag to check if the player is in range
    [SerializeField] private bool isPickingUp = false; // Flag to prevent picking up multiple items at once

    [Header("Input Settings")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string pickupActionName = "PickUp"; // Action name for item pickup

    private InputAction pickupAction; // Reference to the input action for pickup

    void Awake()
    {
        // If inputActions is not assigned via the inspector, load it from the Resources/Keybinds folder
        if (inputActions == null)
        {
            // Load from the "Keybinds" folder in Resources
            inputActions = Resources.Load<InputActionAsset>("Keybinds/PlayerInputs");

            if (inputActions == null)
            {
                Debug.LogError("PlayerInputs asset not found in Resources/Keybinds folder!");
            }
        }
    }
    void Start()
    {
        // Get Pickup action from Input Action Asset
        pickupAction = inputActions.FindAction(pickupActionName);

        if (pickupAction != null)
        {
            pickupAction.Enable();
        }
        else
        {
            Debug.LogError($"Input action '{pickupActionName}' not found in Input Action Asset!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            canPickUp = true; // Player is in range to pick up the item
            Debug.Log("Player is in range to pick up the item.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            canPickUp = true; // Player is in range to pick up the item
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            canPickUp = false; // Player is out of range
            Debug.Log("Player is out of range to pick up the item.");
        }
    }

    void Update()
    {
        if (canPickUp && pickupAction.triggered) // Check if pickup action is triggered
        {
            Camera playerCamera = FindCameraWithTag(playerCameraTag);

            if (playerCamera != null)
            {
                RaycastHit hit;
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

                // Create a layer mask that includes pickUpColliderLayer but excludes itemLayer
                LayerMask combinedMask = pickUpColliderLayer & ~itemLayer;

                // Perform a raycast to find the item in the center of the view
                if (Physics.Raycast(ray, out hit, raycastDistance, combinedMask))
                {
                    // Check if the hit collider matches the 'Pick Up Collider' child
                    if (IsHitOnPickUpCollider(hit.collider))
                    {
                        // Check if the item is within the camera's view frustum
                        if (IsWithinCameraView(playerCamera, hit.point))
                        {
                            if (isPickingUp)
                            {
                                Debug.Log("Already picking up an item.");
                                return;
                            }

                            // Prevent pickup if the player already has this item in the inventory
                            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                            InventoryManager inventory = player.GetComponent<InventoryManager>();

                            if (inventory != null)
                            {
                                if (inventory.HasItem(item)) // Check if player already has the item
                                {
                                    Debug.Log("Player already has this item.");
                                    return;
                                }

                                bool added = inventory.AddItem(item);
                                Debug.Log("Item pickup attempt: " + (added ? "Success" : "Failed"));

                                if (added)
                                {
                                    inventory.DisableItemPickup(item);
                                    Destroy(gameObject); // Destroy the item in the world after picking it up
                                }
                            }
                            else
                            {
                                Debug.LogWarning("InventoryManager component not found on player.");
                            }

                            Invoke("ResetPickingUpFlag", 0.5f); // Adjust the delay as needed
                        }
                        else
                        {
                            Debug.Log("Item is not within the camera's view.");
                        }
                    }
                    else
                    {
                        Debug.Log("Hit collider does not match the 'Pick Up Collider' child.");
                    }
                }
                else
                {
                    Debug.Log("No item detected by the raycast.");
                }
            }
            else
            {
                Debug.LogWarning("Player Camera not found with the tag " + playerCameraTag + ".");
            }
        }
    }

    private bool IsWithinCameraView(Camera camera, Vector3 worldPosition)
    {
        // Convert world position to screen point
        Vector3 screenPoint = camera.WorldToScreenPoint(worldPosition);

        // Define a rect in screen space (center of the screen with some radius)
        Rect viewRect = new Rect(Screen.width / 2 - pickupRadius, Screen.height / 2 - pickupRadius, pickupRadius * 2, pickupRadius * 2);

        // Check if the screen point is within the rect
        return viewRect.Contains(screenPoint);
    }

    private bool IsHitOnPickUpCollider(Collider hitCollider)
    {
        // Check if the hit collider is the "Pick Up Collider" child
        Transform pickUpCollider = transform.Find("Pick Up Collider");

        if (pickUpCollider != null)
        {
            Collider collider = pickUpCollider.GetComponent<Collider>();

            if (collider == hitCollider && hitCollider.gameObject.layer == LayerMask.NameToLayer("Pick Up Item Collider"))
            {
                Debug.Log("Hit collider matches the 'Pick Up Collider' child and is on the correct layer.");
                return true;
            }
            else
            {
                Debug.Log("Hit collider does not match the 'Pick Up Collider' child or is not on the correct layer.");
            }
        }
        else
        {
            Debug.Log("No 'Pick Up Collider' child found.");
        }

        return false;
    }

    private Camera FindCameraWithTag(string tag)
    {
        GameObject cameraObject = GameObject.FindGameObjectWithTag(tag);
        if (cameraObject != null)
        {
            return cameraObject.GetComponent<Camera>();
        }
        return null;
    }

    private void ResetPickingUpFlag()
    {
        isPickingUp = false;
    }
}
