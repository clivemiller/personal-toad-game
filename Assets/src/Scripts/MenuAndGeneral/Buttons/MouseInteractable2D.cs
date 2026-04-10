using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shared 2D mouse interaction handling for button-like objects that use a Collider2D.
/// Derived classes can respond to hover enter/exit and clicks without duplicating hit tests.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public abstract class MouseInteractable2D : MonoBehaviour
{
    private Camera interactionCamera;

    protected Collider2D InteractionCollider { get; private set; }
    protected bool IsHovered { get; private set; }

    protected virtual void Awake()
    {
        InteractionCollider = GetComponent<Collider2D>();
    }

    protected void ProcessMouseInteraction()
    {
        if (!CanProcessMouseInteraction())
        {
            ClearHoverState();
            return;
        }

        if (!TryGetMouseWorldPosition(out Vector2 mouseWorldPos))
        {
            ClearHoverState();
            return;
        }

        bool hoveredNow = InteractionCollider.OverlapPoint(mouseWorldPos);
        SetHoverState(hoveredNow);

        if (hoveredNow)
        {
            OnHovering();
        }

        if (hoveredNow && Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnMouseClicked();
        }
    }

    protected void ClearHoverState()
    {
        SetHoverState(false);
    }

    protected virtual bool CanProcessMouseInteraction()
    {
        return true;
    }

    protected virtual void OnHoverEntered()
    {
    }

    protected virtual void OnHoverExited()
    {
    }

    protected virtual void OnMouseClicked()
    {
    }

    protected virtual void OnHovering()
    {
    }

    protected virtual void OnDisable()
    {
        ClearHoverState();
    }

    private void SetHoverState(bool hoveredNow)
    {
        if (hoveredNow == IsHovered)
        {
            return;
        }

        IsHovered = hoveredNow;

        if (hoveredNow)
        {
            OnHoverEntered();
        }
        else
        {
            OnHoverExited();
        }
    }

    private bool TryGetMouseWorldPosition(out Vector2 mouseWorldPos)
    {
        mouseWorldPos = default;

        if (InteractionCollider == null || Mouse.current == null)
        {
            return false;
        }

        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (interactionCamera == null)
        {
            return false;
        }

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = interactionCamera.WorldToScreenPoint(transform.position).z;
        mouseWorldPos = interactionCamera.ScreenToWorldPoint(mousePos);
        return true;
    }
}
