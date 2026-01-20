using UnityEngine;

public class SlimeRenderer : MonoBehaviour
{
    private SlimeGameManager manager;
    private Texture2D maskTex;
    private Color32[] colors;
    private int myId;
    private Material mat;

    public void Init(SlimeGameManager mgr, Color c, Material baseMaterial, float aspect)
    {
        manager = mgr;
        int w = manager.gridWidth;
        int h = manager.gridHeight;

        maskTex = new Texture2D(w, h, TextureFormat.R8, false);
        maskTex.filterMode = FilterMode.Bilinear;
        maskTex.wrapMode = TextureWrapMode.Clamp;
        
        colors = new Color32[w * h];
        
        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        
        float worldH = 10.8f; 
        float worldW = worldH * aspect; 
        Mesh m = new Mesh();
        m.vertices = new Vector3[] {
            new Vector3(-worldW/2, -worldH/2, 0),
            new Vector3( worldW/2, -worldH/2, 0),
            new Vector3(-worldW/2,  worldH/2, 0),
            new Vector3( worldW/2,  worldH/2, 0)
        };
        m.uv = new Vector2[] { new Vector2(0,0), new Vector2(1,0), new Vector2(0,1), new Vector2(1,1) };
        m.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        m.normals = new Vector3[] {
        -Vector3.forward,
        -Vector3.forward,
        -Vector3.forward,
        -Vector3.forward
        };
        mf.mesh = m;

        // Create instance from base material
        mat = new Material(baseMaterial);
        if (baseMaterial != mgr.slimeMaterial)
        {
            mat.SetColor("_Color", c);
            mat.SetColor("_ColorDark", c * 0.6f);
        }
        mat.SetTexture("_MainTex", maskTex);
        
        // Pass obstacle map to shader
        if (manager.obstacleMap != null)
        {
            mat.SetTexture("_ObstacleTex", manager.obstacleMap);
        }
        
        mr.material = mat;
    }

    public void SetId(int id) { myId = id; }

    public void UpdateTexture()
    {
        byte[,] grid = manager.grid;
        byte[,] thick = manager.gridThickness;
        int w = manager.gridWidth;
        int h = manager.gridHeight;

        for (int i = 0; i < colors.Length; i++)
        {
            int x = i % w;
            int y = i / w;

            if (grid[x, y] == myId)
            {
                int val = thick[x,y] + 50;
                if (val > 255) val = 255;
                colors[i] = new Color32((byte)val, 0, 0, 255);
            }
            else
            {
                colors[i] = new Color32(0, 0, 0, 0);
            }
        }
        maskTex.SetPixels32(colors);
        maskTex.Apply();
    }
}