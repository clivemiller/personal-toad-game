using UnityEngine;

/// <summary>
/// Handles button clicks to exit/quit the game.
/// Requires a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
public class ExitButtonHandler : MouseInteractable2D
{
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
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        Debug.Log("Exiting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
