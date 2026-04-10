using UnityEngine;

/// <summary>
/// Makes a SpriteRenderer grayscale by default, and on hover:
/// - enlarges slightly
/// - fades back to original color (by animating grayscale amount)
/// - plays a configurable sound
/// 
/// Requires a Collider2D (BoxCollider2D recommended) for hover detection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class HoverGrayscaleSprite : MouseInteractable2D
{
    [Header("Hover Animation")]
    private const float transitionSeconds = 0.12f;
    private const float hoverScaleMultiplier = 1.06f;

    [Header("Grayscale")]
    [SerializeField]
    private Shader grayscaleShader;

    private const float grayscaleWhenNotHovered = 1f;
    private const float grayscaleWhenHovered = 0f;

    [Header("Sound")]
    [SerializeField]
    private AudioClip hoverSound;

    private const float hoverSoundVolume = 1f;
    private AudioSource audioSource;

    private SpriteRenderer spriteRenderer;
    private Material runtimeMaterial;

    private Vector3 baseScale;
    private Vector3 targetScale;
    private Vector3 scaleVelocity;

    private float currentGrayscale;
    private float targetGrayscale;
    private float grayscaleVelocity;

    private static readonly int GrayscaleAmountId = Shader.PropertyToID("_GrayscaleAmount");

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        targetScale = baseScale;

        if (grayscaleShader == null)
        {
            grayscaleShader = Shader.Find("Toads/GrayscaleSprite");
        }

        if (grayscaleShader != null)
        {
            runtimeMaterial = new Material(grayscaleShader);
            spriteRenderer.material = runtimeMaterial;
        }
        else
        {
            Debug.LogWarning(
                "HoverGrayscaleSprite: Could not find shader 'Toads/GrayscaleSprite'. " +
                "Sprite will not grayscale. Assign the shader in the inspector.",
                this);
        }

        currentGrayscale = grayscaleWhenNotHovered;
        targetGrayscale = grayscaleWhenNotHovered;
        ApplyGrayscale(currentGrayscale);

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

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }



    protected override void OnDisable()
    {
        base.OnDisable();
        transform.localScale = baseScale;
        currentGrayscale = grayscaleWhenNotHovered;
        targetGrayscale = grayscaleWhenNotHovered;
        ApplyGrayscale(currentGrayscale);
    }

    private void Update()
    {
        ProcessMouseInteraction();

        float smoothTime = Mathf.Max(0.0001f, transitionSeconds);

        transform.localScale = Vector3.SmoothDamp(transform.localScale, targetScale, ref scaleVelocity, smoothTime);

        if (runtimeMaterial != null)
        {
            currentGrayscale = Mathf.SmoothDamp(currentGrayscale, targetGrayscale, ref grayscaleVelocity, smoothTime);
            ApplyGrayscale(currentGrayscale);
        }
    }

    private void SetHovered(bool hovered)
    {
        targetScale = hovered ? baseScale * hoverScaleMultiplier : baseScale;
        targetGrayscale = hovered ? grayscaleWhenHovered : grayscaleWhenNotHovered;
    }

    protected override void OnHoverEntered()
    {
        SetHovered(true);
        PlayHoverSound();
    }

    protected override void OnHoverExited()
    {
        SetHovered(false);
    }

    private void ApplyGrayscale(float grayscaleAmount)
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        runtimeMaterial.SetFloat(GrayscaleAmountId, Mathf.Clamp01(grayscaleAmount));
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
