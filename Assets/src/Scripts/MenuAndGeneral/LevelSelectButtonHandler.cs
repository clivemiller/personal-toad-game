using UnityEngine;
/// <summary>
/// Handles button clicks to select a level and load the constructor scene.
/// Plays a click sound on button press instead of hover.
/// Requires a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
public class LevelSelectButtonHandler : MouseInteractable2D
{
    [Header("Level Settings")]
    [SerializeField]
    private int levelNumber;

    [Header("Constructor Scene")]
    [SerializeField]
    private string constructorSceneName = "Constructor";

    [Header("Click Sound")]
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
        if (string.IsNullOrEmpty(constructorSceneName))
        {
            Debug.LogWarning("LevelSelectButtonHandler: No constructor scene name assigned!", this);
            return;
        }

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        // Store the selected level number for the constructor to use
        PlayerPrefs.SetInt("SelectedLevel", levelNumber);
        PlayerPrefs.Save();

        Debug.Log($"Level {levelNumber} selected. Loading constructor scene: {constructorSceneName}");
        SceneTransitionManager.Load(constructorSceneName, false);
    }
}
