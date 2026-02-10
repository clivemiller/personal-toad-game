using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple hover effect that:
/// - switches cursor mode on hover
/// - plays a configurable sound
/// 
/// Requires a Collider2D (BoxCollider2D recommended) for hover detection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ButtonEffect : MonoBehaviour
{
    [Header("Hover Detection")]
    [SerializeField]
    private Camera hoverCamera;

    [Header("Cursor")]
    [SerializeField]
    private CursorManager.CursorState hoverCursorState = CursorManager.CursorState.CanGrab;

    [Header("Sound")]
    [SerializeField]
    private AudioClip hoverSound;

    private const float hoverSoundVolume = 1f;
    private AudioSource audioSource;

    private Collider2D col2D;

    private bool isHovered;
    private CursorManager.CursorState cursorStateBeforeHover;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();

        if (hoverCamera == null)
        {
            hoverCamera = Camera.main;
        }

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

    private void OnDisable()
    {
        if (isHovered)
        {
            RestoreCursorStateIfNeeded();
        }

        isHovered = false;
    }

    private void Update()
    {
        UpdateHoverState();
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
        if (isHovered)
        {
            SwitchCursorForHover();
            PlayHoverSound();
        }
        else
        {
            RestoreCursorStateIfNeeded();
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
