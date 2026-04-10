using UnityEngine;

/// <summary>
/// Handles a 2-frame animation toggle that controls the b_c_mode global variable.
/// Click to toggle between frames 0 (off) and 1 (on).
/// Requires an Animator component and a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class ToggleHandler : MouseInteractable2D
{
    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();

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
        ProcessMouseInteraction();
    }

    protected override void OnMouseClicked()
    {
        GlobalVariables.b_c_mode = !GlobalVariables.b_c_mode;
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
