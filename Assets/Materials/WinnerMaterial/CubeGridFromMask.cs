using UnityEngine;

public class CubeGridFromMask : MonoBehaviour
{
    [Header("Mask")]
    public Texture2D mask;
    
    [Header("Grid")]
    public int gridX = 40;
    public int gridY = 15;
    public float cubeSize = 0.9f;
    
    [Header("Size")]
    public float width = 10f;
    public float height = 4f;
    
    [Header("Animation")]
    public float speed = 1f;
    public float noiseScale = 0.1f;
    public float maxHeight = 0.5f;
    
    [Header("Look")]
    public Material cubeMaterial;
    
    private Transform[] cubes;
    private Vector2[] gridUVs;
    private int cubeCount;
    
    void Start()
    {
        BuildGrid();
    }
    
    public void BuildGrid()
    {
        // Clear old
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        cubes = new Transform[gridX * gridY];
        gridUVs = new Vector2[gridX * gridY];
        cubeCount = 0;
        
        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                // UV position (0-1)
                float u = (x + 0.5f) / gridX;
                float v = (y + 0.5f) / gridY;
                
                // Sample mask at this position
                Color pixel = mask.GetPixelBilinear(u, v);
                
                // Skip if red (window) - red > 0.5 and green < 0.5
                if (pixel.r > 0.5f && pixel.g < 0.5f)
                    continue;
                
                // World position
                float posX = (u - 0.5f) * width;
                float posY = (v - 0.5f) * height;
                
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = transform;
                cube.transform.localPosition = new Vector3(posX, posY, 0);
                cube.transform.localScale = new Vector3(
                    width / gridX * cubeSize,
                    height / gridY * cubeSize,
                    width / gridX * cubeSize
                );
                
                if (cubeMaterial != null)
                    cube.GetComponent<Renderer>().material = cubeMaterial;
                
                cubes[cubeCount] = cube.transform;
                gridUVs[cubeCount] = new Vector2(u, v);
                cubeCount++;
            }
        }
        
        Debug.Log($"Created {cubeCount} cubes");
    }
    
    void Update()
    {
        if (cubes == null) return;
        
        float time = Time.time * speed;
        
        for (int i = 0; i < cubeCount; i++)
        {
            if (cubes[i] == null) continue;
            
            Vector2 uv = gridUVs[i];
            
            float noise = Mathf.PerlinNoise(
                uv.x * gridX * noiseScale + time,
                uv.y * gridY * noiseScale + time * 0.7f
            );
            
            Vector3 pos = cubes[i].localPosition;
            pos.z = noise * maxHeight;
            cubes[i].localPosition = pos;
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0.1f));
    }
}