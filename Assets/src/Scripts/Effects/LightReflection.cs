using UnityEngine;

public class LightReflection : MonoBehaviour
{
    [Header("Wobble & Shake Settings")]
    [Tooltip("How fast the reflection moves on the X axis.")]
    public float wobbleSpeedX = 1.5f;
    [Tooltip("How fast the reflection moves on the Y axis.")]
    public float wobbleSpeedY = 2.5f;
    [Tooltip("How far the reflection sways left and right.")]
    public float wobbleAmountX = 0.08f;
    [Tooltip("How far the reflection sways up and down.")]
    public float wobbleAmountY = 0.03f;

    [Header("Fake Blur Effect")]
    [Tooltip("Generates slightly offset, semi-transparent duplicates of child sprites to simulate a blur.")]
    public bool enableFakeBlur = true;
    [Tooltip("How many ghost layers to generate per sprite for the blur.")]
    public int blurLayers = 4;
    [Tooltip("How far out the blur spreads from the center.")]
    public float blurSpread = 0.05f;
    [Tooltip("Opacity of the blurred layers.")]
    [Range(0f, 1f)] public float blurAlpha = 0.15f;

    private Vector3 initialLocalPosition;
    private float noiseOffsetX;
    private float noiseOffsetY;

    void Start()
    {
        initialLocalPosition = transform.localPosition;
        
        // Randomize the noise starting points so multiple reflections don't sync up perfectly
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetY = Random.Range(0f, 100f);

        if (enableFakeBlur)
        {
            CreateFakeBlurOnChildren();
        }
    }

    void Update()
    {
        // Use Perlin noise for a smooth, organic, fluid wobble (resembling water/light reflection)
        float noiseX = Mathf.PerlinNoise(Time.time * wobbleSpeedX + noiseOffsetX, 0f) * 2f - 1f;
        float noiseY = Mathf.PerlinNoise(0f, Time.time * wobbleSpeedY + noiseOffsetY) * 2f - 1f;

        transform.localPosition = initialLocalPosition + new Vector3(noiseX * wobbleAmountX, noiseY * wobbleAmountY, 0f);
    }

    private void CreateFakeBlurOnChildren()
    {
        // Wait! We only want original children, not ghosts if this is called multiple times.
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (SpriteRenderer sr in renderers)
        {
            // Skip ghost objects so we don't infinitely duplicate them
            if (sr.gameObject.name.Contains("_BlurGhost")) continue;

            for (int i = 0; i < blurLayers; i++)
            {
                // Create a duplicate object to act as a blurry layer
                GameObject ghost = new GameObject(sr.gameObject.name + "_BlurGhost");
                ghost.transform.SetParent(sr.transform);
                
                // Offset the ghost slightly
                Vector2 randomOffset = Random.insideUnitCircle * blurSpread;
                ghost.transform.localPosition = new Vector3(randomOffset.x, randomOffset.y, 0.01f); // push slightly behind
                
                ghost.transform.localRotation = Quaternion.identity;
                ghost.transform.localScale = Vector3.one;

                // Copy over the SpriteRenderer properties
                SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
                ghostSr.sprite = sr.sprite;
                
                // Keep original color but drop the opacity way down
                ghostSr.color = new Color(sr.color.r, sr.color.g, sr.color.b, blurAlpha);
                
                // Put them right behind the main object
                ghostSr.sortingLayerID = sr.sortingLayerID;
                ghostSr.sortingOrder = sr.sortingOrder - 1;
            }
        }
    }
}
