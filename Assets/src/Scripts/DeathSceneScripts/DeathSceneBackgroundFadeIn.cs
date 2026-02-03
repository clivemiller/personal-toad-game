using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DeathSceneBackgroundFadeIn : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField]
    private float fadeDuration = 1.25f;

    [SerializeField]
    private float startDelay = 0f;

    [Header("Music")]
    [SerializeField]
    private AudioClip musicClip;

    [SerializeField]
    [Range(0f, 1f)]
    private float musicVolume = 1f;

    [SerializeField]
    private bool loopMusic = true;

    [Tooltip("If null, will try GetComponent<AudioSource>() or create one.")]
    [SerializeField]
    private AudioSource audioSource;

    private CanvasGroup canvasGroup;
    private Image uiImage;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        uiImage = GetComponent<Image>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure we start fully faded out.
        SetAlpha(0f);

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

    private void Start()
    {
        if (musicClip != null)
        {
            audioSource.clip = musicClip;
            audioSource.volume = musicVolume;
            audioSource.loop = loopMusic;
            audioSource.Play();
        }

        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        if (fadeDuration <= 0f)
        {
            SetAlpha(1f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (uiImage != null)
        {
            Color c = uiImage.color;
            c.a = alpha;
            uiImage.color = c;
            return;
        }

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }
}
