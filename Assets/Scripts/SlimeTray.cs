using UnityEngine;

public class SlimeTray : MonoBehaviour
{
    [Header("Movement Settings")]
    public float leftLimit = -17f;
    public float rightLimit = 17f;
    public float speed = 0.3f;
    public float heightOffset = 0f;
    
    [Header("Path Smoothing")]
    public bool smoothPath = true;
    public int smoothSamples = 5;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    [Header("Detection Settings")]
    [Tooltip("Search from top to bottom instead of bottom to top")]
    public bool searchFromTop = false;

    [HideInInspector] public SlimeAgent owner;
    private SlimeGameManager manager;
    private float[] heightCache;
    private SpriteRenderer bgRenderer;
    
    float t = 0f;
    bool frozen = false;

    void Start()
    {
        manager = FindObjectOfType<SlimeGameManager>();
        
        GameObject bg = GameObject.Find("Background_House");
        if (bg != null)
            bgRenderer = bg.GetComponent<SpriteRenderer>();
        
        t = Random.Range(0f, 100f);
        
        if (smoothPath)
            heightCache = new float[smoothSamples];
            
        // Debug: Show what we're working with
        if (showDebug && manager != null && manager.obstacleMap != null)
        {
            Debug.Log($"Obstacle map size: {manager.obstacleMap.width}x{manager.obstacleMap.height}");
            Debug.Log($"Background bounds: min={bgRenderer.bounds.min}, max={bgRenderer.bounds.max}");
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (frozen) return;

        t += Time.deltaTime * speed;

        float halfRange = (rightLimit - leftLimit) * 0.5f;
        float center = (rightLimit + leftLimit) * 0.5f;
        float x = center + Mathf.Sin(t) * halfRange;
        x = Mathf.Clamp(x, leftLimit, rightLimit);

        float y = GetGreenLineY(x);

        transform.position = new Vector3(x, y + heightOffset, transform.position.z);
    }

    float GetGreenLineY(float worldX)
    {
        if (manager == null || manager.obstacleMap == null || !manager.obstacleMap.isReadable) 
            return transform.position.y;
        
        if (bgRenderer == null)
            return transform.position.y;

        Bounds bounds = bgRenderer.bounds;
        float normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldX);
        int texX = Mathf.RoundToInt(normalizedX * (manager.obstacleMap.width - 1));
        texX = Mathf.Clamp(texX, 0, manager.obstacleMap.width - 1);

        // Search for green pixel
        int foundTexY = -1;
        
        if (searchFromTop)
        {
            // Search from top down
            for (int y = manager.obstacleMap.height - 1; y >= 0; y--)
            {
                Color pixel = manager.obstacleMap.GetPixel(texX, y);
                
                if (pixel.g > 0.5f && pixel.r < 0.4f && pixel.b < 0.4f)
                {
                    foundTexY = y;
                    break;
                }
            }
        }
        else
        {
            // Search from bottom up
            for (int y = 0; y < manager.obstacleMap.height; y++)
            {
                Color pixel = manager.obstacleMap.GetPixel(texX, y);
                
                if (pixel.g > 0.5f && pixel.r < 0.4f && pixel.b < 0.4f)
                {
                    foundTexY = y;
                    break;
                }
            }
        }

        if (foundTexY == -1)
        {
            if (showDebug && Time.frameCount % 60 == 0) // Log once per second
                Debug.LogWarning($"No green pixel found at worldX={worldX:F2}, texX={texX}");
            return transform.position.y;
        }

        // Convert texture Y to world Y
        float normalizedY = (float)foundTexY / (manager.obstacleMap.height - 1);
        float foundY = Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY);

        if (showDebug && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Found green: worldX={worldX:F2} -> texX={texX}, texY={foundTexY} -> worldY={foundY:F2}");
        }

        // Smooth the path
        if (smoothPath && heightCache != null)
        {
            for (int i = heightCache.Length - 1; i > 0; i--)
            {
                heightCache[i] = heightCache[i - 1];
            }
            heightCache[0] = foundY;

            float sum = 0;
            int count = 0;
            foreach (float h in heightCache)
            {
                if (h != 0)
                {
                    sum += h;
                    count++;
                }
            }

            return count > 0 ? sum / count : foundY;
        }

        return foundY;
    }

    public void SetFrozen(bool value)
    {
        frozen = value;
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || manager == null || bgRenderer == null) return;

        float step = (rightLimit - leftLimit) / 50f;
        
        Vector3 lastPos = Vector3.zero;
        bool first = true;
        
        for (float x = leftLimit; x <= rightLimit; x += step)
        {
            float y = GetGreenLineY(x);
            Vector3 pos = new Vector3(x, y + heightOffset, transform.position.z);
            
            // Color code the spheres
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pos, 0.1f);
            
            if (!first)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(lastPos, pos);
            }
            
            lastPos = pos;
            first = false;
        }
        
        // Draw the actual green line from obstacle map for comparison
        Gizmos.color = Color.red;
        Bounds bounds = bgRenderer.bounds;
        float texStep = (float)manager.obstacleMap.width / 50f;
        
        Vector3 lastGreenPos = Vector3.zero;
        bool firstGreen = true;
        
        for (int texX = 0; texX < manager.obstacleMap.width; texX += Mathf.Max(1, (int)texStep))
        {
            // Find green in this column
            for (int texY = 0; texY < manager.obstacleMap.height; texY++)
            {
                Color pixel = manager.obstacleMap.GetPixel(texX, texY);
                
                if (pixel.g > 0.5f && pixel.r < 0.4f && pixel.b < 0.4f)
                {
                    // Convert to world space
                    float normalizedX = (float)texX / (manager.obstacleMap.width - 1);
                    float normalizedY = (float)texY / (manager.obstacleMap.height - 1);
                    
                    float worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedX);
                    float worldY = Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY);
                    
                    Vector3 greenPos = new Vector3(worldX, worldY, transform.position.z);
                    
                    Gizmos.DrawSphere(greenPos, 0.08f);
                    
                    if (!firstGreen)
                    {
                        Gizmos.DrawLine(lastGreenPos, greenPos);
                    }
                    
                    lastGreenPos = greenPos;
                    firstGreen = false;
                    break;
                }
            }
        }
    }
}