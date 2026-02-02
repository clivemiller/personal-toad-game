using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages background music across scenes with fade transitions.
/// </summary>
public class MusicManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusicMapping
    {
        public string sceneName;
        public AudioClip musicClip;
    }

    [Header("Music Mappings")]
    [SerializeField]
    private List<SceneMusicMapping> sceneMusicMappings = new List<SceneMusicMapping>();

    [Header("Audio Settings")]
    [SerializeField]
    [Range(0f, 1f)]
    private float musicVolume = 0.5f;

    [SerializeField]
    private float fadeDuration = 1f;

    private AudioSource audioSource;
    private AudioClip currentClip;
    private Coroutine fadeCoroutine;

    private static MusicManager instance;

    private void Awake()
    {
        // Singleton pattern - ensure only one MusicManager exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = musicVolume;

        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Start playing music for the current scene
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip clipForScene = GetMusicForScene(sceneName);

        // If no music is assigned for this scene, stop current music
        if (clipForScene == null)
        {
            if (audioSource.isPlaying)
            {
                FadeOut();
            }
            return;
        }

        // If the same clip is already playing, do nothing
        if (clipForScene == currentClip && audioSource.isPlaying)
        {
            Debug.Log($"MusicManager: Continuing to play '{clipForScene.name}' for scene '{sceneName}'");
            return;
        }

        // Switch to new clip with fade transition
        if (audioSource.isPlaying)
        {
            CrossFade(clipForScene);
        }
        else
        {
            FadeIn(clipForScene);
        }
    }

    private AudioClip GetMusicForScene(string sceneName)
    {
        foreach (var mapping in sceneMusicMappings)
        {
            if (mapping.sceneName == sceneName)
            {
                return mapping.musicClip;
            }
        }
        return null;
    }

    private void FadeIn(AudioClip newClip)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        currentClip = newClip;
        audioSource.clip = newClip;
        audioSource.volume = 0f;
        audioSource.Play();

        fadeCoroutine = StartCoroutine(FadeVolumeCoroutine(0f, musicVolume));
        Debug.Log($"MusicManager: Fading in '{newClip.name}'");
    }

    private void FadeOut()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutAndStopCoroutine());
        Debug.Log("MusicManager: Fading out music");
    }

    private void CrossFade(AudioClip newClip)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(CrossFadeCoroutine(newClip));
        Debug.Log($"MusicManager: Cross-fading from '{currentClip?.name}' to '{newClip.name}'");
    }

    private IEnumerator FadeVolumeCoroutine(float startVolume, float targetVolume)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
        fadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStopCoroutine()
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = musicVolume;
        currentClip = null;
        fadeCoroutine = null;
    }

    private IEnumerator CrossFadeCoroutine(AudioClip newClip)
    {
        // Fade out current music
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2f));
            yield return null;
        }

        // Switch to new clip
        currentClip = newClip;
        audioSource.clip = newClip;
        audioSource.Play();

        // Fade in new music
        elapsed = 0f;
        while (elapsed < fadeDuration / 2f)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / (fadeDuration / 2f));
            yield return null;
        }

        audioSource.volume = musicVolume;
        fadeCoroutine = null;
    }

    // Public methods to add mappings at runtime if needed
    public void AddSceneMusicMapping(string sceneName, AudioClip musicClip)
    {
        // Check if mapping already exists
        foreach (var mapping in sceneMusicMappings)
        {
            if (mapping.sceneName == sceneName)
            {
                mapping.musicClip = musicClip;
                Debug.Log($"MusicManager: Updated music for scene '{sceneName}'");
                return;
            }
        }

        // Add new mapping
        sceneMusicMappings.Add(new SceneMusicMapping
        {
            sceneName = sceneName,
            musicClip = musicClip
        });
        Debug.Log($"MusicManager: Added music mapping for scene '{sceneName}'");
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (!audioSource.isPlaying || fadeCoroutine != null)
        {
            return;
        }
        audioSource.volume = musicVolume;
    }
}
