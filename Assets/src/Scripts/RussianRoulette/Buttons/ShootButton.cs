using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ShootButton : MouseInteractable2D
{
    [Header("Hover Effect")]
    [SerializeField]
    private float hoverScaleMultiplier = 1.1f;

    private Vector3 normalScale;
    private RouletteScript rouletteScript;
    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        rouletteScript = FindFirstObjectByType<RouletteScript>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalScale = transform.localScale;
    }

    private void Update()
    {
        if (rouletteScript == null || rouletteScript.GameState != 2)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            ClearHoverState();
            transform.localScale = normalScale;
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        ProcessMouseInteraction();
    }

    protected override void OnHoverEntered()
    {
        transform.localScale = normalScale * hoverScaleMultiplier;
    }

    protected override void OnHoverExited()
    {
        transform.localScale = normalScale;
    }

    protected override void OnMouseClicked()
    {
        rouletteScript.GameState = 3;
        rouletteScript.ActionUponGameState();
    }
}
