using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class GlowButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;
    private TextMeshProUGUI buttonText;
    private Material textMaterial;

    [Header("Text Colors")]
    public Color defaultColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color pressedColor = Color.red;

    [Header("Glow Settings")]
    public Color defaultGlowColor = Color.white;
    public float defaultGlowPower = 0.1f;

    public Color hoverGlowColor = Color.yellow;
    public float hoverGlowPower = 0.5f;

    public Color pressedGlowColor = Color.red;
    public float pressedGlowPower = 0.7f;

    [Header("Hover Scale")]
    public float hoverScaleMultiplier = 1.2f;

    private bool isHovered = false;

    void Start()
    {
        originalScale = transform.localScale;
        buttonText = GetComponentInChildren<TextMeshProUGUI>();

        if (buttonText != null)
        {
            textMaterial = buttonText.fontMaterial;
            ApplyState(defaultColor, defaultGlowColor, defaultGlowPower);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHovered)
        {
            transform.localScale = originalScale * hoverScaleMultiplier;
            ApplyState(hoverColor, hoverGlowColor, hoverGlowPower);
            isHovered = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
        ApplyState(defaultColor, defaultGlowColor, defaultGlowPower);
        isHovered = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ApplyState(pressedColor, pressedGlowColor, pressedGlowPower);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Return to hover state if still hovering, otherwise default
        if (isHovered)
        {
            ApplyState(hoverColor, hoverGlowColor, hoverGlowPower);
        }
        else
        {
            ApplyState(defaultColor, defaultGlowColor, defaultGlowPower);
        }
    }

    private void ApplyState(Color textColor, Color glowColor, float glowPower)
    {
        if (buttonText != null && textMaterial != null)
        {
            buttonText.color = textColor;

            // Set glow parameters
            textMaterial.EnableKeyword("GLOW_ON");
            textMaterial.SetColor("_GlowColor", glowColor);
            textMaterial.SetFloat("_GlowPower", glowPower);

            // Force material update
            buttonText.fontMaterial = textMaterial;
        }
    }
}