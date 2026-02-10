using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class StopButton : MonoBehaviour
{
    private RouletteScript rouletteScript;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rouletteScript = FindFirstObjectByType<RouletteScript>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Stop button should be visible only when the game is idle or sweating.
        // It should NOT appear while the spin animation is playing.

        if (rouletteScript == null || (rouletteScript.GameState != 0 && rouletteScript.GameState != 2))
        {
            // Hide button when not in an allowed state (including spinning).
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }

            return;
        }   

        // Show button when game is in an allowed state
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
    }
}
