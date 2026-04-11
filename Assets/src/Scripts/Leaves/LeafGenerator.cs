using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class LeafGenerator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The parent box collider that leaves will spawn from.")]
    public BoxCollider2D spawnArea;
    [Tooltip("The child object containing the landing zone collider.")]
    public Collider2D landingZoneArea;
    
    [Header("Leaf Settings")]
    [Tooltip("List of leaf prefabs to spawn.")]
    public List<GameObject> leafPrefabs;
    [Tooltip("How often to spawn a leaf in seconds.")]
    public float spawnInterval = 1f;

    [Header("Perspective Settings")]
    [Tooltip("Size scale of the leaf when it lands at the lowest Y point.")]
    public float scaleAtBottom = 1.0f;
    [Tooltip("Size scale of the leaf when it lands at the highest Y point (further away).")]
    public float scaleAtTop = 0.5f;

    private float spawnTimer;

    void Reset()
    {
        // Try assigning components automatically when added to an object
        spawnArea = GetComponent<BoxCollider2D>();
        if (transform.childCount > 0)
        {
            landingZoneArea = transform.GetChild(0).GetComponent<Collider2D>();
        }
    }

    void Update()
    {
        if (leafPrefabs == null || leafPrefabs.Count == 0 || spawnArea == null || landingZoneArea == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnLeaf();
        }
    }

    void SpawnLeaf()
    {
        // Pick a random leaf prefab
        GameObject prefab = leafPrefabs[Random.Range(0, leafPrefabs.Count)];
        
        // Get start spawn position inside the parent BoxCollider2D
        Vector3 startPos = GetRandomPointInBox(spawnArea);

        // Get target landing position inside the child Collider2D
        Vector3 targetPos = GetRandomPointInCollider(landingZoneArea);

        // Instantiate the leaf
        GameObject leafObj = Instantiate(prefab, startPos, Quaternion.identity, transform);
        
        // Ensure the falling script is attached
        FallingLeaf leafScript = leafObj.GetComponent<FallingLeaf>();
        if (leafScript == null)
        {
            leafScript = leafObj.AddComponent<FallingLeaf>();
        }

        // Calculate perspective scale based on the target Y position within the landing zone
        float scale = CalculatePerspectiveScale(targetPos);

        // Initialize the fall behavior
        leafScript.Initialize(startPos, targetPos, scale);
    }

    private Vector3 GetRandomPointInBox(BoxCollider2D box)
    {
        Bounds bounds = box.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            Random.Range(bounds.min.y, bounds.max.y),
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    private Vector3 GetRandomPointInCollider(Collider2D col)
    {
        Bounds bounds = col.bounds;
        Vector2 randomPoint = Vector2.zero;
        bool pointFound = false;

        // Try up to 50 times to find a point that actually overlaps the 2D collider shape
        for (int i = 0; i < 50; i++)
        {
            randomPoint = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );

            // OverlapPoint checks if the point is within the actual polygon shape, not just the bounding box
            if (col.OverlapPoint(randomPoint))
            {
                pointFound = true;
                break;
            }
        }

        // Fallback to the bounds center if we failed to find a point inside the polygon
        if (!pointFound)
        {
            randomPoint = bounds.center;
        }

        return new Vector3(randomPoint.x, randomPoint.y, bounds.center.z);
    }

    private float CalculatePerspectiveScale(Vector3 targetPos)
    {
        Bounds bounds = landingZoneArea.bounds;
        
        // Return scaleAtBottom if bounds represent a flat 2D plane with zero height
        if (Mathf.Approximately(bounds.min.y, bounds.max.y)) 
            return scaleAtBottom;

        // InverseLerp gets a 0 to 1 value representing where the target Y is within the bounds
        float t = Mathf.InverseLerp(bounds.min.y, bounds.max.y, targetPos.y);
        
        // Lerp between the bottom (closest) and top (furthest) scales based on Y height
        return Mathf.Lerp(scaleAtBottom, scaleAtTop, t);
    }
}
