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
    
    // Track valid positions
    private float lastValidY;
    private float lastValidX;
    private bool hasValidPosition = false;
    
    // Detected green line bounds (auto-calculated)
    private float greenLineMinX = float.MaxValue;
    private float greenLineMaxX = float.MinValue;
    private bool boundsDetected = false;

    void Start()
    {
        manager = FindObjectOfType<SlimeGameManager>();
        
        GameObject bg = GameObject.Find("Background_House");
        if (bg != null)
            bgRenderer = bg.GetComponent<SpriteRenderer>();
        
        t = Random.Range(0f, 100f);
        
        if (smoothPath)
            heightCache = new float[smoothSamples];
        
        // Detect where the green line actually exists
        DetectGreenLineBounds();
        
        // Initialize position on the line
        InitializeOnGreenLine();
            
        if (showDebug && manager != null && manager.obstacleMap != null)
        {
            Debug.Log($"Obstacle map size: {manager.obstacleMap.width}x{manager.obstacleMap.height}");
            Debug.Log($"Background bounds: min={bgRenderer.bounds.min}, max={bgRenderer.bounds.max}");
            Debug.Log($"Green line X bounds: {greenLineMinX:F2} to {greenLineMaxX:F2}");
        }
    }
    
    void DetectGreenLineBounds()
    {
        if (manager == null || manager.obstacleMap == null || !manager.obstacleMap.isReadable)
            return;
        if (bgRenderer == null)
            return;
            
        Bounds bounds = bgRenderer.bounds;
        
        // Scan across the texture to find where green exists
        for (int texX = 0; texX < manager.obstacleMap.width; texX++)
        {
            bool foundGreenInColumn = false;
            
            for (int y = 0; y < manager.obstacleMap.height; y++)
            {
                Color pixel = manager.obstacleMap.GetPixel(texX, y);
                
                if (pixel.g > 0.5f && pixel.r < 0.4f && pixel.b < 0.4f)
                {
                    foundGreenInColumn = true;
                    break;
                }
            }
            
            if (foundGreenInColumn)
            {
                float normalizedX = (float)texX / (manager.obstacleMap.width - 1);
                float worldX = Mathf.Lerp(bounds.min.x, bounds.max.x, normalizedX);
                
                if (worldX < greenLineMinX) greenLineMinX = worldX;
                if (worldX > greenLineMaxX) greenLineMaxX = worldX;
            }
        }
        
        // Add small margin to avoid edge issues
        float margin = 0.2f;
        greenLineMinX += margin;
        greenLineMaxX -= margin;
        
        boundsDetected = (greenLineMinX < greenLineMaxX);
        
        if (showDebug)
        {
            Debug.Log($"[SlimeTray] Detected green line from X={greenLineMinX:F2} to X={greenLineMaxX:F2}");
        }
    }
    
    void InitializeOnGreenLine()
    {
        if (!boundsDetected) return;
        
        // Start in the middle of the green line
        float startX = (greenLineMinX + greenLineMaxX) * 0.5f;
        float startY = GetGreenLineYInternal(startX);
        
        if (startY != float.MinValue)
        {
            lastValidX = startX;
            lastValidY = startY;
            hasValidPosition = true;
            transform.position = new Vector3(startX, startY + heightOffset, transform.position.z);
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (frozen) return;

        t += Time.deltaTime * speed;

        // Use detected bounds if available, otherwise fall back to manual limits
        float effectiveLeftLimit = boundsDetected ? Mathf.Max(leftLimit, greenLineMinX) : leftLimit;
        float effectiveRightLimit = boundsDetected ? Mathf.Min(rightLimit, greenLineMaxX) : rightLimit;

        float halfRange = (effectiveRightLimit - effectiveLeftLimit) * 0.5f;
        float center = (effectiveRightLimit + effectiveLeftLimit) * 0.5f;
        float x = center + Mathf.Sin(t) * halfRange;
        x = Mathf.Clamp(x, effectiveLeftLimit, effectiveRightLimit);

        float y = GetGreenLineY(x);

        transform.position = new Vector3(x, y + heightOffset, transform.position.z);
    }
    
    // Internal version that returns MinValue if not found (for initialization)
    float GetGreenLineYInternal(float worldX)
    {
        if (manager == null || manager.obstacleMap == null || !manager.obstacleMap.isReadable) 
            return float.MinValue;
        
        if (bgRenderer == null)
            return float.MinValue;

        Bounds bounds = bgRenderer.bounds;
        float normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldX);
        int texX = Mathf.RoundToInt(normalizedX * (manager.obstacleMap.width - 1));
        texX = Mathf.Clamp(texX, 0, manager.obstacleMap.width - 1);

        int foundTexY = -1;
        
        if (searchFromTop)
        {
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
            return float.MinValue;

        float normalizedY = (float)foundTexY / (manager.obstacleMap.height - 1);
        return Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY);
    }

    float GetGreenLineY(float worldX)
    {
        if (manager == null || manager.obstacleMap == null || !manager.obstacleMap.isReadable) 
            return hasValidPosition ? lastValidY : transform.position.y;
        
        if (bgRenderer == null)
            return hasValidPosition ? lastValidY : transform.position.y;

        Bounds bounds = bgRenderer.bounds;
        float normalizedX = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldX);
        int texX = Mathf.RoundToInt(normalizedX * (manager.obstacleMap.width - 1));
        texX = Mathf.Clamp(texX, 0, manager.obstacleMap.width - 1);

        int foundTexY = -1;
        
        if (searchFromTop)
        {
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

        // No green found - return last valid position
        if (foundTexY == -1)
        {
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.LogWarning($"No green pixel found at worldX={worldX:F2}, texX={texX} - using last valid Y={lastValidY:F2}");
            
            return hasValidPosition ? lastValidY : transform.position.y;
        }

        // Convert texture Y to world Y
        float normalizedY = (float)foundTexY / (manager.obstacleMap.height - 1);
        float foundY = Mathf.Lerp(bounds.min.y, bounds.max.y, normalizedY);

        // Store as last valid position
        lastValidX = worldX;
        lastValidY = foundY;
        hasValidPosition = true;

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

        // Draw detected green line bounds
        if (boundsDetected)
        {
            Gizmos.color = Color.cyan;
            Vector3 leftBound = new Vector3(greenLineMinX, transform.position.y, transform.position.z);
            Vector3 rightBound = new Vector3(greenLineMaxX, transform.position.y, transform.position.z);
            Gizmos.DrawLine(leftBound + Vector3.down * 2, leftBound + Vector3.up * 2);
            Gizmos.DrawLine(rightBound + Vector3.down * 2, rightBound + Vector3.up * 2);
        }

        float step = (rightLimit - leftLimit) / 50f;
        
        Vector3 lastPos = Vector3.zero;
        bool first = true;
        
        for (float x = leftLimit; x <= rightLimit; x += step)
        {
            float y = GetGreenLineYInternal(x);
            if (y == float.MinValue) continue; // Skip positions with no green
            
            Vector3 pos = new Vector3(x, y + heightOffset, transform.position.z);
            
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
            for (int texY = 0; texY < manager.obstacleMap.height; texY++)
            {
                Color pixel = manager.obstacleMap.GetPixel(texX, texY);
                
                if (pixel.g > 0.5f && pixel.r < 0.4f && pixel.b < 0.4f)
                {
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