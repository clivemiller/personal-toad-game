using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public sealed class SpriteTextGlyph : MonoBehaviour
{
    private RectTransform cachedRectTransform;
    private Image cachedImage;
    private Vector2 baseAnchoredPosition;
    private bool isVisible;

    public bool IsVisible => isVisible;

    public void Configure(Sprite sprite, Vector2 anchoredPosition, Vector2 size, Color tint)
    {
        EnsureReferences();

        baseAnchoredPosition = anchoredPosition;
        cachedRectTransform.anchorMin = new Vector2(0f, 1f);
        cachedRectTransform.anchorMax = new Vector2(0f, 1f);
        cachedRectTransform.pivot = new Vector2(0f, 1f);
        cachedRectTransform.sizeDelta = size;
        cachedRectTransform.anchoredPosition = anchoredPosition;
        cachedRectTransform.localScale = Vector3.one;
        cachedRectTransform.localRotation = Quaternion.identity;

        cachedImage.sprite = sprite;
        cachedImage.color = tint;
        cachedImage.preserveAspect = true;

        SetVisible(sprite != null);
    }

    public void SetVisible(bool visible)
    {
        EnsureReferences();

        isVisible = visible;
        cachedImage.enabled = visible && cachedImage.sprite != null;

        if (!visible)
        {
            ResetAnimatedOffset();
        }
    }

    public void SetAnimatedOffset(Vector2 offset, float rotationDegrees)
    {
        EnsureReferences();

        cachedRectTransform.anchoredPosition = baseAnchoredPosition + offset;
        cachedRectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
    }

    public void ResetAnimatedOffset()
    {
        EnsureReferences();

        cachedRectTransform.anchoredPosition = baseAnchoredPosition;
        cachedRectTransform.localRotation = Quaternion.identity;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void EnsureReferences()
    {
        if (cachedRectTransform == null)
        {
            cachedRectTransform = GetComponent<RectTransform>();
        }

        if (cachedImage == null)
        {
            cachedImage = GetComponent<Image>();
        }
    }
}
