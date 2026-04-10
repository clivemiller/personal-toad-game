using UnityEngine;

/// <summary>
/// Handles button clicks to load a different scene.
/// Requires a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
public class SceneButtonHandler : MouseInteractable2D
{
    [Header("Fade Settings")]
    [SerializeField]
    public bool doFadeClick = false;

    [Header("Scene Settings")]
    [SerializeField]
    private string sceneName;

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
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("SceneButtonHandler: No scene name assigned!", this);
            return;
        }

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        Debug.Log($"Loading scene: {sceneName}");
        SceneTransitionManager.Load(sceneName, doFadeClick);
    }
}
