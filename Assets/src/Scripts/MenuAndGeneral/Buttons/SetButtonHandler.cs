using UnityEngine;

/// <summary>
/// Handles button clicks to switch between in-scene sets.
/// Requires a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
public class SetButtonHandler : MouseInteractable2D
{
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

    protected override void Awake()
    {
        base.Awake();

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
        ProcessMouseInteraction();
    }

    protected override void OnMouseClicked()
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

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        Debug.Log($"Switching to set: {setName}");
        setManager.SwitchToSet(setName);
    }
}
