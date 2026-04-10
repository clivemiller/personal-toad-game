using UnityEngine;

/// <summary>
/// Button hover effect that smoothly levitates a target GameObject upward when hovered.
/// 
/// Requires a Collider2D (BoxCollider2D recommended) for hover detection.
/// The target GameObject to levitate should be assigned in the inspector.
/// </summary>
[DisallowMultipleComponent]
public class ButtonHoverEffect : MouseInteractable2D
{
    [Header("Target")]
    [Tooltip("The GameObject to levitate on hover")]
    [SerializeField]
    private GameObject targetObject;

    [Header("Levitation Settings")]
    [Tooltip("How far up the button moves when hovered")]
    [SerializeField]
    private float hoverHeight = 10f;
    
    [Tooltip("Smooth transition time in seconds")]
    [SerializeField]
    private float transitionSeconds = 0.15f;

    private Vector3 basePosition;
    private Vector3 targetPosition;
    private Vector3 positionVelocity;

    protected override void Awake()
    {
        base.Awake();
        
        if (targetObject != null)
        {
            basePosition = targetObject.transform.localPosition;
            targetPosition = basePosition;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        // Reset position when object is disabled
        if (targetObject != null)
        {
            targetObject.transform.localPosition = basePosition;
        }
    }

    private void Update()
    {
        if (targetObject == null)
        {
            return;
        }

        ProcessMouseInteraction();

        float smoothTime = Mathf.Max(0.0001f, transitionSeconds);
        targetObject.transform.localPosition = Vector3.SmoothDamp(targetObject.transform.localPosition, targetPosition, ref positionVelocity, smoothTime);
    }

    protected override void OnHoverEntered()
    {
        targetPosition = basePosition + new Vector3(0, hoverHeight, 0);
    }

    protected override void OnHoverExited()
    {
        targetPosition = basePosition;
    }
}
