using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;
    private TextMeshProUGUI buttonText;
    private Color originalColor;

    [SerializeField] private float hoverScaleMultiplier = 1.2f;  // Scale multiplier when hovering
    [SerializeField] private Color hoverColor;                   // Color when hovering
    [SerializeField] private Color pressedColor;                 // Color when pressing

    void Start()
    {
        // Store the original size and text color
        originalScale = transform.localScale;
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            originalColor = buttonText.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Change scale and text color on hover
        transform.localScale = originalScale * hoverScaleMultiplier;

        if (buttonText != null)
        {
            buttonText.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Revert to original scale and color when hover ends
        transform.localScale = originalScale;

        if (buttonText != null)
        {
            buttonText.color = originalColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Change text color to pressed color when button is pressed
        if (buttonText != null)
        {
            buttonText.color = pressedColor;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Revert to hover color when the button is released while hovering
        if (buttonText != null)
        {
            buttonText.color = hoverColor;
        }
    }
}

