using UnityEngine;

public class FallingLeaf : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("How fast the leaf falls downward.")]
    public float fallSpeed = 2f;
    [Tooltip("How fast the leaf sways left and right.")]
    public float swaySpeed = 2f;
    [Tooltip("How far the leaf sways from side to side.")]
    public float swayAmount = 1f;
    [Tooltip("How fast the leaf rotates as it falls.")]
    public float rotationSpeed = 60f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    
    private float timeAlive;
    private bool hasLanded = false;
    
    private float randomTimeOffset;
    private float randomizedSpeed;
    private float rotationDirection;

    public void Initialize(Vector3 start, Vector3 target, float targetScale)
    {
        startPosition = start;
        targetPosition = target;
        
        // Apply the perspective scale immediately, or alternatively you could scale it over time.
        transform.localScale = Vector3.one * targetScale;
        transform.position = start;

        // Add some noise so all leaves don't move in unison
        randomTimeOffset = Random.Range(0f, 100f);
        randomizedSpeed = fallSpeed * Random.Range(0.8f, 1.2f);
        rotationDirection = Random.value > 0.5f ? 1f : -1f;
        swayAmount *= Random.value > 0.5f ? 1f : -1f;

        hasLanded = false;
        timeAlive = 0f;
    }

    void Update()
    {
        if (hasLanded) return;

        timeAlive += Time.deltaTime;

        // Calculate progress based on height (normalized 0 to 1)
        float totalDistanceY = startPosition.y - targetPosition.y;

        // Guard against division by 0 and immediate landing
        if (totalDistanceY <= 0)
        {
            Land();
            return;
        }

        // Move downwards
        float newY = transform.position.y - (randomizedSpeed * Time.deltaTime);

        float distanceCoveredY = startPosition.y - newY;
        float normalizedProgress = Mathf.Clamp01(distanceCoveredY / totalDistanceY);

        // Sine wave for the leaf's side-to-side sway
        float swayX = Mathf.Sin((timeAlive * swaySpeed) + randomTimeOffset) * swayAmount;

        // Fade out the sway effect as it approaches the exact target landing position
        swayX *= (1f - normalizedProgress);

        // Lerp smoothly towards target X and Z, applying the sway to the X axis
        float newX = Mathf.Lerp(startPosition.x, targetPosition.x, normalizedProgress) + swayX;
        float newZ = Mathf.Lerp(startPosition.z, targetPosition.z, normalizedProgress);

        // Apply new position and constant rotation
        transform.position = new Vector3(newX, newY, newZ);
        transform.Rotate(Vector3.forward * rotationSpeed * rotationDirection * Time.deltaTime);

        // Check if we've reached or passed the target height
        if (newY <= targetPosition.y)
        {
            Land();
        }
    }

    private void Land()
    {
        hasLanded = true;
        
        // Snap exactly to the landing spot
        transform.position = targetPosition;

        // Strip the object down to essentially a static PNG to save performance!
        
        // 1. Destroy all 2D Physics Components if they exist
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);

        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach(Collider2D col in colliders)
        {
            Destroy(col);
        }

        // 2. Destroy this script itself so Unity completely stops calling Update()
        Destroy(this);
    }
}
