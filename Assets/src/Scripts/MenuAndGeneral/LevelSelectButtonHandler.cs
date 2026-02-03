using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles button clicks to select a level and load the constructor scene.
/// Plays a click sound on button press instead of hover.
/// Requires a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class LevelSelectButtonHandler : MonoBehaviour
{
    [Header("Click Detection")]
    [SerializeField]
    private Camera clickCamera;

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
        if (string.IsNullOrEmpty(constructorSceneName))
        {
            Debug.LogWarning("LevelSelectButtonHandler: No constructor scene name assigned!", this);
            return;
        }

        // Play click sound if assigned
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        // Store the selected level number for the constructor to use
        PlayerPrefs.SetInt("SelectedLevel", levelNumber);
        PlayerPrefs.Save();

        Debug.Log($"Level {levelNumber} selected. Loading constructor scene: {constructorSceneName}");
        SceneManager.LoadScene(constructorSceneName);
    }
}
