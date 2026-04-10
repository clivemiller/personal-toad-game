using UnityEngine;

/// <summary>
/// Simple hover effect that:
/// - switches cursor mode on hover
/// - plays a configurable sound
/// 
/// Requires a Collider2D (BoxCollider2D recommended) for hover detection.
/// </summary>
[DisallowMultipleComponent]
public class ButtonEffect : MouseInteractable2D
{
    [Header("Cursor")]
    [SerializeField]
    private CursorManager.CursorState hoverCursorState = CursorManager.CursorState.CanGrab;

    [Header("Sound")]
    [SerializeField]
    private AudioClip hoverSound;

    private const float hoverSoundVolume = 1f;
    private AudioSource audioSource;

    private CursorManager.CursorState cursorStateBeforeHover;

    protected override void Awake()
    {
        base.Awake();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void Update()
    {
        ProcessMouseInteraction();
    }

    protected override void OnHoverEntered()
    {
        SwitchCursorForHover();
        PlayHoverSound();
    }

    protected override void OnHoverExited()
    {
        RestoreCursorStateIfNeeded();
    }

    protected override void OnHovering()
    {
        if (CursorManager.Instance == null)
        {
            return;
        }

        // Re-assert our hover cursor while hovered so neighboring buttons
        // cannot leave the cursor in the wrong state because of update order.
        if (CursorManager.Instance.CurrentState != hoverCursorState)
        {
            CursorManager.Instance.Switch(hoverCursorState);
        }
    }

    private void SwitchCursorForHover()
    {
        if (CursorManager.Instance == null)
        {
            return;
        }

        cursorStateBeforeHover = CursorManager.Instance.CurrentState;
        CursorManager.Instance.Switch(hoverCursorState);
    }

    private void RestoreCursorStateIfNeeded()
    {
        if (CursorManager.Instance == null)
        {
            return;
        }

        // Avoid stomping over other systems: only restore if we're still in our hover state.
        if (CursorManager.Instance.CurrentState == hoverCursorState)
        {
            CursorManager.Instance.Switch(cursorStateBeforeHover);
        }
    }

    private void PlayHoverSound()
    {
        if (hoverSound == null || audioSource == null)
        {
            return;
        }

        // Stop any currently playing instance of this sound
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioSource.PlayOneShot(hoverSound, hoverSoundVolume);
    }
}
