using UnityEngine;

public class MaskGrid : MonoBehaviour
{
    public Texture2D mask;
    public int gridX = 40;
    public int gridY = 15;
    public float width = 10f;
    public float height = 4f;
    public float cubeSize = 0.9f;
    public float speed = 1f;
    public float noiseScale = 0.1f;
    public float maxHeight = 0.5f;
    public Material cubeMaterial;

    Transform[] cubes;
    Vector2[] uvs;
    int count;

    void Start()
    {
        Build();
    }

    void Build()
    {
        cubes = new Transform[gridX * gridY];
        uvs = new Vector2[gridX * gridY];
        count = 0;

        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                float u = (x + 0.5f) / gridX;
                float v = (y + 0.5f) / gridY;

                if (mask != null)
                {
                    Color c = mask.GetPixelBilinear(u, v);
                    if (c.r > 0.5f && c.g < 0.5f) continue;
                }

                float px = (u - 0.5f) * width;
                float py = (v - 0.5f) * height;

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = transform;
                cube.transform.localPosition = new Vector3(px, py, 0);
                cube.transform.localScale = new Vector3(
                    width / gridX * cubeSize,
                    height / gridY * cubeSize,
                    width / gridX * cubeSize
                );

                if (cubeMaterial != null)
                    cube.GetComponent<Renderer>().material = cubeMaterial;

                cubes[count] = cube.transform;
                uvs[count] = new Vector2(u, v);
                count++;
            }
        }
    }

    void Update()
    {
        if (cubes == null) return;

        float t = Time.time * speed;

        for (int i = 0; i < count; i++)
        {
            if (cubes[i] == null) continue;

            float n = Mathf.PerlinNoise(
                uvs[i].x * gridX * noiseScale + t,
                uvs[i].y * gridY * noiseScale + t * 0.7f
            );

            Vector3 p = cubes[i].localPosition;
            p.z = n * maxHeight;
            cubes[i].localPosition = p;
        }
    }
}