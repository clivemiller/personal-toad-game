using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Clickable button that fades out a list of objects and then plays an assigned animation.
/// Requires a Collider2D for click detection.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class PlayRouletteButton : MonoBehaviour
{
    [Header("Click Detection")]
    [SerializeField]
    private Camera clickCamera;

    [Header("Fade Targets")]
    [Tooltip("GameObjects to fade out when the button is clicked.")]
    [SerializeField]
    private List<GameObject> objectsToFade = new List<GameObject>();

    [Tooltip("Duration of the fade out in seconds.")]
    [SerializeField]
    private float fadeDuration = 0.35f;

    [Tooltip("If true, disables faded objects after the fade finishes.")]
    [SerializeField]
    private bool disableAfterFade = true;

    [Header("Animation")]
    [Tooltip("Animator to trigger after fading finishes. If empty, will try GetComponent<Animator>().")]
    [SerializeField]
    private Animator rouletteAnimator;

    [Tooltip("If set, this trigger will be fired on the animator.")]
    [SerializeField]
    private string animatorTriggerName = "Play";

    [Tooltip("Optional: if set, Play this state directly instead of using a trigger.")]
    [SerializeField]
    private string animatorStateName;

    [Header("Click Sound")]
    [SerializeField]
    private AudioClip clickSound;

    [SerializeField]
    [Range(0f, 1f)]
    private float clickSoundVolume = 1f;

    private AudioSource audioSource;
    private Collider2D col2D;
    private bool isBusy;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();

        if (clickCamera == null)
        {
            clickCamera = Camera.main;
        }

        if (rouletteAnimator == null)
        {
            rouletteAnimator = GetComponent<Animator>();
        }

        audioSource = GetComponent<AudioSource>();
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
        if (isBusy)
        {
            return;
        }

        if (clickCamera == null || col2D == null)
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

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
        if (isBusy)
        {
            return;
        }

        // Play click sound if assigned
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound, clickSoundVolume);
        }

        StartCoroutine(FadeThenPlay());
    }

    private IEnumerator FadeThenPlay()
    {
        isBusy = true;

        if (objectsToFade != null && objectsToFade.Count > 0 && fadeDuration > 0f)
        {
            yield return StartCoroutine(FadeOutObjects(objectsToFade, fadeDuration));

            if (disableAfterFade)
            {
                for (int i = 0; i < objectsToFade.Count; i++)
                {
                    if (objectsToFade[i] != null)
                    {
                        objectsToFade[i].SetActive(false);
                    }
                }
            }
        }

        PlayAssignedAnimation();
        isBusy = false;
    }

    private void PlayAssignedAnimation()
    {
        if (rouletteAnimator == null)
        {
            Debug.LogWarning("PlayRouletteButton: No Animator assigned or found on this object.", this);
            return;
        }

        if (!string.IsNullOrEmpty(animatorStateName))
        {
            rouletteAnimator.Play(animatorStateName, 0, 0f);
            return;
        }

        if (!string.IsNullOrEmpty(animatorTriggerName))
        {
            rouletteAnimator.SetTrigger(animatorTriggerName);
            return;
        }

        Debug.LogWarning("PlayRouletteButton: No trigger or state name provided.", this);
    }

    private static IEnumerator FadeOutObjects(List<GameObject> targets, float duration)
    {
        float elapsed = 0f;

        // Cache fade-able components per target so we don't repeatedly search.
        var canvasGroups = new List<CanvasGroup>();
        var spriteRenderers = new List<SpriteRenderer>();
        var renderers = new List<Renderer>();

        for (int i = 0; i < targets.Count; i++)
        {
            GameObject go = targets[i];
            if (go == null)
            {
                continue;
            }

            // Prefer CanvasGroup (UI)
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                canvasGroups.Add(cg);
                continue;
            }

            // Prefer SpriteRenderer (2D)
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                spriteRenderers.Add(sr);
                continue;
            }

            // Fallback: any Renderer with a color property
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                renderers.Add(r);
            }
        }

        // Capture starting alphas so we fade from current state.
        float[] cgStart = new float[canvasGroups.Count];
        for (int i = 0; i < canvasGroups.Count; i++)
        {
            cgStart[i] = canvasGroups[i] != null ? canvasGroups[i].alpha : 1f;
        }

        Color[] srStart = new Color[spriteRenderers.Count];
        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            srStart[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }

        Color[] rStart = new Color[renderers.Count];
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r != null && r.material != null && r.material.HasProperty("_Color"))
            {
                rStart[i] = r.material.color;
            }
            else
            {
                rStart[i] = Color.white;
            }
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float a = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < canvasGroups.Count; i++)
            {
                if (canvasGroups[i] != null)
                {
                    canvasGroups[i].alpha = cgStart[i] * a;
                }
            }

            for (int i = 0; i < spriteRenderers.Count; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    Color c = srStart[i];
                    c.a = srStart[i].a * a;
                    spriteRenderers[i].color = c;
                }
            }

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer r = renderers[i];
                if (r != null && r.material != null && r.material.HasProperty("_Color"))
                {
                    Color c = rStart[i];
                    c.a = rStart[i].a * a;
                    r.material.color = c;
                }
            }

            yield return null;
        }

        // Ensure end state is fully invisible.
        for (int i = 0; i < canvasGroups.Count; i++)
        {
            if (canvasGroups[i] != null)
            {
                canvasGroups[i].alpha = 0f;
            }
        }

        for (int i = 0; i < spriteRenderers.Count; i++)
        {
            if (spriteRenderers[i] != null)
            {
                Color c = spriteRenderers[i].color;
                c.a = 0f;
                spriteRenderers[i].color = c;
            }
        }

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer r = renderers[i];
            if (r != null && r.material != null && r.material.HasProperty("_Color"))
            {
                Color c = r.material.color;
                c.a = 0f;
                r.material.color = c;
            }
        }
    }
}
