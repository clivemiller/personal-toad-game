using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles button clicks to switch between in-scene sets.
/// Requires a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class SetButtonHandler : MonoBehaviour
{
    [Header("Click Detection")]
    [SerializeField]
    private Camera clickCamera;

    [Header("Set Settings")]
    [SerializeField]
    private InSceneSetManager setManager;

    [SerializeField]
    private string setName;

    [Header("Optional Sound")]
    [SerializeField]
    private AudioClip clickSound;

    private const float clickSoundVolume = 1f;
    private AudioSource audioSource;

    private Collider2D col2D;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();

        if (clickCamera == null)
        {
            clickCamera = Camera.main;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null && clickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
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

        // Check if click is over this object
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = clickCamera.WorldToScreenPoint(transform.position).z;
        Vector2 mouseWorldPos = clickCamera.ScreenToWorldPoint(mousePos);

        if (col2D.OverlapPoint(mouseWorldPos))
        {
            OnButtonClicked();
        }
    }

    private void OnButtonClicked()
    {
        if (setManager == null)
        {
            Debug.LogWarning("SetButtonHandler: No InSceneSetManager assigned!", this);
            return;
        }

        if (string.IsNullOrEmpty(setName))
        {
            Debug.LogWarning("SetButtonHandler: No set name assigned!", this);
            return;
        }

        // Play click sound if assigned
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        Debug.Log($"Switching to set: {setName}");
        setManager.SwitchToSet(setName);
    }
}
