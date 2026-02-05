using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Button hover effect that smoothly levitates a target GameObject upward when hovered.
/// 
/// Requires a Collider2D (BoxCollider2D recommended) for hover detection.
/// The target GameObject to levitate should be assigned in the inspector.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ButtonHoverEffect : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The GameObject to levitate on hover")]
    [SerializeField]
    private GameObject targetObject;

    [Header("Hover Detection")]
    [SerializeField]
    private Camera hoverCamera;

    [Header("Levitation Settings")]
    [Tooltip("How far up the button moves when hovered")]
    [SerializeField]
    private float hoverHeight = 10f;
    
    [Tooltip("Smooth transition time in seconds")]
    [SerializeField]
    private float transitionSeconds = 0.15f;

    private Collider2D col2D;
    private Vector3 basePosition;
    private Vector3 targetPosition;
    private Vector3 positionVelocity;
    private bool isHovered;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();
        
        if (targetObject != null)
        {
            basePosition = targetObject.transform.localPosition;
            targetPosition = basePosition;
        }

        if (hoverCamera == null)
        {
            hoverCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        // Reset position when object is disabled
        if (targetObject != null)
        {
            targetObject.transform.localPosition = basePosition;
        }
        isHovered = false;
    }

    private void Update()
    {
        if (targetObject == null)
        {
            return;
        }

        UpdateHoverState();

        float smoothTime = Mathf.Max(0.0001f, transitionSeconds);
        targetObject.transform.localPosition = Vector3.SmoothDamp(targetObject.transform.localPosition, targetPosition, ref positionVelocity, smoothTime);
    }

    private void UpdateHoverState()
    {
        if (hoverCamera == null || col2D == null)
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        // For 2D orthographic cameras, we need proper Z distance
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = hoverCamera.WorldToScreenPoint(transform.position).z;
        Vector2 mouseWorldPos = hoverCamera.ScreenToWorldPoint(mousePos);
        
        bool hoveredNow = col2D.OverlapPoint(mouseWorldPos);

        if (hoveredNow == isHovered)
        {
            return;
        }

        isHovered = hoveredNow;
        targetPosition = isHovered ? basePosition + new Vector3(0, hoverHeight, 0) : basePosition;
    }
}
