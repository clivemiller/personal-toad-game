using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class SpinButton : MonoBehaviour
{
    [Header("Click Detection")]
    [SerializeField]
    private Camera clickCamera;

    [Header("Hover Effect")]
    [SerializeField]
    private float hoverScaleMultiplier = 1.1f;
    private Vector3 normalScale;
    private bool isHovering = false;

    private Collider2D col2D;
    private RouletteScript rouletteScript;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        col2D = GetComponent<Collider2D>();
        rouletteScript = FindFirstObjectByType<RouletteScript>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (clickCamera == null)
        {
            clickCamera = Camera.main;
        }

        normalScale = transform.localScale;
    }

    private void Update()
    {
        if (rouletteScript == null || rouletteScript.GameState != 0)
        {
            // hide button if game is not in initial state
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
                transform.localScale = normalScale;
                isHovering = false;
            }

            return;
        }

        // show button when game is in initial state
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        DetectHover();
        DetectClick();
    }

    private void DetectHover()
    {
        if (clickCamera == null || col2D == null || spriteRenderer == null)
        {
            return;
        }

        if (Mouse.current == null)
        {
            return;
        }

        Vector3 mousePos = Mouse.current.position.ReadValue();
        mousePos.z = clickCamera.WorldToScreenPoint(transform.position).z;
        Vector2 mouseWorldPos = clickCamera.ScreenToWorldPoint(mousePos);

        bool wasHovering = isHovering;
        isHovering = col2D.OverlapPoint(mouseWorldPos);

        if (isHovering && !wasHovering)
        {
            transform.localScale = normalScale * hoverScaleMultiplier;
        }
        else if (!isHovering && wasHovering)
        {
            transform.localScale = normalScale;
        }
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
        rouletteScript.GameState = 1;
        rouletteScript.ActionUponGameState();
    }
}
