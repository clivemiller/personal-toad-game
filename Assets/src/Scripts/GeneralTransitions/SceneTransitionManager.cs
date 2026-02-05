using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages scene transitions with a 2-second fuzzy black fade out/fade in effect.
/// Singleton pattern allows easy access from any script via SceneTransitionManager.Instance.
/// </summary>
[DisallowMultipleComponent]
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField]
    [Tooltip("Total duration of the transition (fade out + fade in)")]
    private float transitionDuration = 3.5f;

    [SerializeField]
    [Tooltip("The fuzzy/noise texture for the transition effect. If null, uses solid black.")]
    private Texture2D fuzzyTexture;

    [SerializeField]
    [Tooltip("Color of the fade overlay")]
    private Color fadeColor = Color.black;

    private Canvas transitionCanvas;
    private RawImage fadeImage;
    private CanvasGroup canvasGroup;
    private bool isTransitioning = false;

    /// <summary>
    /// Returns true if a transition is currently in progress.
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        // Singleton setup with persistence across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupTransitionUI();
    }

    private void SetupTransitionUI()
    {
        // Create canvas for the transition overlay
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);
        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; // Ensure it renders on top

        // Add CanvasScaler for proper scaling
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Add GraphicRaycaster (required for Canvas)
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create the fade image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);
        fadeImage = imageObj.AddComponent<RawImage>();

        // Setup RectTransform to fill the screen
        RectTransform rectTransform = fadeImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Apply fuzzy texture or generate one if not provided
        if (fuzzyTexture != null)
        {
            fadeImage.texture = fuzzyTexture;
        }
        else
        {
            fadeImage.texture = GenerateFuzzyTexture();
        }

        fadeImage.color = fadeColor;

        // Add CanvasGroup for alpha control
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    /// <summary>
    /// Generates a procedural fuzzy/noise texture for the transition effect.
    /// </summary>
    private Texture2D GenerateFuzzyTexture()
    {
        int width = 256;
        int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Repeat;

        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Generate fuzzy noise pattern
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                float fineNoise = Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.3f;
                float combinedNoise = Mathf.Clamp01(noise + fineNoise);

                // Create subtle variation in the black
                float grayValue = combinedNoise * 0.15f; // Keep it mostly black with subtle noise
                pixels[y * width + x] = new Color(grayValue, grayValue, grayValue, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Transitions to a new scene with a fuzzy black fade effect.
    /// Call this from other scripts to change scenes with the transition.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Transition already in progress!");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("SceneTransitionManager: Scene name cannot be null or empty!");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName));
    }

    /// <summary>
    /// Transitions to a new scene by build index with a fuzzy black fade effect.
    /// </summary>
    /// <param name="sceneIndex">The build index of the scene to load.</param>
    public void TransitionToScene(int sceneIndex)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Transition already in progress!");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneIndex));
    }

    /// <summary>
    /// Performs the fade transition and loads the scene by name.
    /// </summary>
    private IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;
        float halfDuration = transitionDuration / 2f;

        // Block raycasts during transition
        canvasGroup.blocksRaycasts = true;

        // Fade out (to black)
        yield return StartCoroutine(FadeCoroutine(0f, 1f, halfDuration));

        // Load the scene
        SceneManager.LoadSceneAsync(sceneName);

        // Wait a frame for the scene to load
        yield return null;

        // Fade in (from black)
        yield return StartCoroutine(FadeCoroutine(1f, 0f, halfDuration));

        // Re-enable interaction
        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    /// <summary>
    /// Performs the fade transition and loads the scene by build index.
    /// </summary>
    private IEnumerator TransitionCoroutine(int sceneIndex)
    {
        isTransitioning = true;
        float halfDuration = transitionDuration / 2f;

        // Block raycasts during transition
        canvasGroup.blocksRaycasts = true;

        // Fade out (to black)
        yield return StartCoroutine(FadeCoroutine(0f, 1f, halfDuration));

        // Load the scene
        SceneManager.LoadSceneAsync(sceneIndex);

        // Wait a frame for the scene to load
        yield return null;

        // Fade in (from black)
        yield return StartCoroutine(FadeCoroutine(1f, 0f, halfDuration));

        // Re-enable interaction
        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }

    /// <summary>
    /// Handles the alpha fade animation with easing.
    /// </summary>
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time in case game is paused
            float t = elapsed / duration;

            // Apply smooth easing (ease in/out)
            float easedT = t * t * (3f - 2f * t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedT);

            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// Static convenience method to transition to a scene.
    /// Creates an instance if one doesn't exist.
    /// </summary>
    /// <param name="sceneName">The name of the scene to load.</param>
    public static void LoadScene(string sceneName)
    {
        EnsureInstanceExists();
        
        if (Instance.IsTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Transition already in progress! Ignoring request.");
            return;
        }
        
        Instance.TransitionToScene(sceneName);
    }

    /// <summary>
    /// Static convenience method to transition to a scene by build index.
    /// Creates an instance if one doesn't exist.
    /// </summary>
    /// <param name="sceneIndex">The build index of the scene to load.</param>
    public static void LoadScene(int sceneIndex)
    {
        EnsureInstanceExists();
        
        if (Instance.IsTransitioning)
        {
            Debug.LogWarning("SceneTransitionManager: Transition already in progress! Ignoring request.");
            return;
        }
        
        Instance.TransitionToScene(sceneIndex);
    }

    /// <summary>
    /// Ensures an instance of the SceneTransitionManager exists.
    /// </summary>
    private static void EnsureInstanceExists()
    {
        if (Instance == null)
        {
            GameObject managerObj = new GameObject("SceneTransitionManager");
            managerObj.AddComponent<SceneTransitionManager>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
