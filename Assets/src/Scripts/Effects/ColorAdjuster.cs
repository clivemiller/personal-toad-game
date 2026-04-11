using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ColorAdjuster : MonoBehaviour
{
    [Header("Color Settings")]
    [Tooltip("The color to apply to the object's SpriteRenderer.")]
    public Color targetColor = Color.white;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyColor();
    }

    void Update()
    {
        if (spriteRenderer != null && spriteRenderer.color != targetColor)
        {
            ApplyColor();
        }
    }

    // OnValidate allows the color changes to be visible in the Unity Editor even when the game isn't running
    void OnValidate()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            ApplyColor();
        }
    }

    private void ApplyColor()
    {
        spriteRenderer.color = targetColor;
    }

    // Public method in case other scripts need to change the color via code
    public void SetColor(Color newColor)
    {
        targetColor = newColor;
        ApplyColor();
    }
}
