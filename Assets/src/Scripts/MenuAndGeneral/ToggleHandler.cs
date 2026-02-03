using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles a 2-frame animation toggle that controls the b_c_mode global variable.
/// Click to toggle between frames 0 (off) and 1 (on).
/// Requires an Animator component and a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class ToggleHandler : MonoBehaviour
{
    [Header("Click Detection")]
    [SerializeField]
    private Camera clickCamera;

    private Animator animator;
    private Collider2D col2D;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        col2D = GetComponent<Collider2D>();

        if (clickCamera == null)
        {
            clickCamera = Camera.main;
        }

        Debug.Log($"ToggleHandler Awake: animator={animator != null}, col2D={col2D != null}, camera={clickCamera != null}");

        // Stop animator from auto-playing
        if (animator != null)
        {
            animator.speed = 0;
        }

        // Initialize animation to match current global state
        UpdateAnimationFrame();
    }

    private void Update()
    {
        DetectClick();
    }

    private void DetectClick()
    {
        if (clickCamera == null || col2D == null)
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        // Check for mouse click
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Debug.Log("Click detected!");

        // Check if click is over this object
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = clickCamera.WorldToScreenPoint(transform.position).z;
        Vector2 mouseWorldPos = clickCamera.ScreenToWorldPoint(mousePos);

        bool isOver = col2D.OverlapPoint(mouseWorldPos);
        Debug.Log($"Click at {mouseWorldPos}, overlap={isOver}, object position={transform.position}");

        if (isOver)
        {
            ToggleState();
        }
    }

    private void ToggleState()
    {
        // Toggle the global variable
        GlobalVariables.b_c_mode = !GlobalVariables.b_c_mode;
        Debug.Log($"Toggled b_c_mode to: {GlobalVariables.b_c_mode}");

        // Update animation to match new state
        UpdateAnimationFrame();
    }

    private void UpdateAnimationFrame()
    {
        if (animator == null)
        {
            Debug.LogWarning("Animator is null in UpdateAnimationFrame");
            return;
        }

        // Set normalized time to 0 (frame 0) or 0.5 (frame 1) for a 2-frame animation
        // Frame 0 = off (false), Frame 1 = on (true)
        float normalizedTime = GlobalVariables.b_c_mode ? 0.5f : 0f;
        Debug.Log($"Setting animation frame: normalizedTime={normalizedTime}, b_c_mode={GlobalVariables.b_c_mode}");
        
        animator.Play("BC_toggle", 0, normalizedTime);
        animator.Update(0); // Force update to apply the normalized time immediately
        animator.speed = 0; // Keep speed at 0 to prevent auto-playing
    }
}
