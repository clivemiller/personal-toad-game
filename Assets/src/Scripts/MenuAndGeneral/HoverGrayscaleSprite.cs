using UnityEngine;
using UnityEngine.InputSystem;

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
[RequireComponent(typeof(Collider2D))]
public sealed class HoverGrayscaleSprite : MonoBehaviour
{
    [Header("Hover Detection")]
    [SerializeField]
    private Camera hoverCamera;

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
    private Collider2D col2D;

    private Vector3 baseScale;
    private Vector3 targetScale;
    private Vector3 scaleVelocity;

    private float currentGrayscale;
    private float targetGrayscale;
    private float grayscaleVelocity;

    private bool isHovered;

    private static readonly int GrayscaleAmountId = Shader.PropertyToID("_GrayscaleAmount");

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col2D = GetComponent<Collider2D>();
        baseScale = transform.localScale;
        targetScale = baseScale;

        if (hoverCamera == null)
        {
            hoverCamera = Camera.main;
        }

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



    private void OnDisable()
    {
        // Reset visuals when object is disabled
        transform.localScale = baseScale;
        currentGrayscale = grayscaleWhenNotHovered;
        targetGrayscale = grayscaleWhenNotHovered;
        ApplyGrayscale(currentGrayscale);

        isHovered = false;
    }

    private void Update()
    {
        UpdateHoverState();

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

    private void UpdateHoverState()
    {
        if (hoverCamera == null || col2D == null)
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        // For 2D orthographic cameras, we need proper Z distance
        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = hoverCamera.WorldToScreenPoint(transform.position).z;
        Vector2 mouseWorldPos = hoverCamera.ScreenToWorldPoint(mousePos);
        
        bool hoveredNow = col2D.OverlapPoint(mouseWorldPos);

        if (hoveredNow == isHovered)
        {
            return;
        }

        isHovered = hoveredNow;
        if (isHovered)
        {
            SetHovered(true);
            PlayHoverSound();
        }
        else
        {
            SetHovered(false);
        }
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
