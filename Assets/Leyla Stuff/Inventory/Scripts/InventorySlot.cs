using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;                 // The UI icon for the item
    public Image slotImage;            // The image for the slot itself
    public Image selectedSlotImage;        // The extra image that appears when the slot is selected
    public Sprite defaultImage;        // Default image for the slot
    public Sprite equippedImage;       // Equipped image for the slot
    private Item item;                 // The item in this slot

    public void AddItem(Item newItem)
    {
        item = newItem;
        icon.sprite = item.itemIcon;
        icon.enabled = true;
        UpdateSelection(false);  // Start as not selected
    }

    public void ClearSlot()
    {
        item = null;
        icon.sprite = defaultImage;
        icon.enabled = false;
        UpdateSelection(false);  // Make sure to clear the selection
    }

    public Item GetItem()
    {
        return item;
    }

    // Update the slot image based on whether it's equipped
    public void UpdateSlotImage(bool isEquipped)
    {
        if (isEquipped)
        {
            slotImage.sprite = equippedImage;
        }
        /*else
        {
            slotImage.sprite = defaultImage;
        }*/
    }

    // Toggle the selected state of the slot
    public void UpdateSelection(bool isSelected)
    {
        if (selectedSlotImage != null)
        {
            selectedSlotImage.enabled = isSelected;
        }
    }
}


