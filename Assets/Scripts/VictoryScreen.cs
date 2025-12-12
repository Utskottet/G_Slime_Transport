using UnityEngine;

public class VictoryScreen : MonoBehaviour
{
    [Header("References")]
    public Shader victoryShader;
    public Texture2D obstacleMap;
    public Texture2D houseTexture;
    
    [Header("Timing")]
    public float fadeInDuration = 0.5f;
    
    [Header("Shader Settings")]
    public float colorSpeed = 1.5f;
    public float pulseSpeed = 3.0f;
    public float pulseIntensity = 0.5f;
    public float swirlScale = 8.0f;
    public float swirlSpeed = 1.0f;
    public float windowGlowWidth = 0.008f;
    public float windowGlowIntensity = 2.0f;
    public float starDensity = 80f;
    public float starSpeed = 1.0f;
    public float starSize = 0.006f;
    public float starBrightness = 1.5f;
    public float brightness = 1.2f;
    
    private GameObject victoryOverlay;
    private Material victoryMaterial;
    private bool isShowing = false;
    private float fadeProgress = 0f;
    
    void Start()
    {
        CreateOverlay();
    }
    
    void OnEnable()
    {
        isShowing = true;
        fadeProgress = 1f; // Start fully visible
        
        if (victoryOverlay != null)
        {
            victoryOverlay.SetActive(true);
        }
    }
    
    void CreateOverlay()
    {
        victoryOverlay = new GameObject("VictoryOverlay");
        victoryOverlay.transform.position = new Vector3(0, 0, -0.5f);
        
        MeshFilter mf = victoryOverlay.AddComponent<MeshFilter>();
        MeshRenderer mr = victoryOverlay.AddComponent<MeshRenderer>();
        
        float worldH = 10.8f;
        float worldW = worldH * 3.555f;
        
        Mesh m = new Mesh();
        m.vertices = new Vector3[] {
            new Vector3(-worldW/2, -worldH/2, 0),
            new Vector3( worldW/2, -worldH/2, 0),
            new Vector3(-worldW/2,  worldH/2, 0),
            new Vector3( worldW/2,  worldH/2, 0)
        };
        m.uv = new Vector2[] { 
            new Vector2(0,0), 
            new Vector2(1,0), 
            new Vector2(0,1), 
            new Vector2(1,1) 
        };
        m.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mf.mesh = m;
        
        if (victoryShader != null)
        {
            victoryMaterial = new Material(victoryShader);
            if (obstacleMap != null)
                victoryMaterial.SetTexture("_ObstacleTex", obstacleMap);
            if (houseTexture != null)
                victoryMaterial.SetTexture("_HouseTex", houseTexture);
            
            mr.material = victoryMaterial;
        }
        else
        {
            Debug.LogError("VictoryScreen: No shader assigned!");
        }
        
        if (isShowing)
        {
            victoryOverlay.SetActive(true);
            fadeProgress = 1f; // Start visible
        }
    }
    
    void Update()
    {
        if (victoryMaterial == null) return;
        
        if (isShowing && fadeProgress < 1f)
        {
            fadeProgress += Time.deltaTime / fadeInDuration;
            fadeProgress = Mathf.Clamp01(fadeProgress);
        }
        
        victoryMaterial.SetFloat("_ColorSpeed", colorSpeed);
        victoryMaterial.SetFloat("_PulseSpeed", pulseSpeed);
        victoryMaterial.SetFloat("_PulseIntensity", pulseIntensity);
        victoryMaterial.SetFloat("_SwirlScale", swirlScale);
        victoryMaterial.SetFloat("_SwirlSpeed", swirlSpeed);
        victoryMaterial.SetFloat("_WindowGlowWidth", windowGlowWidth);
        victoryMaterial.SetFloat("_WindowGlowIntensity", windowGlowIntensity);
        victoryMaterial.SetFloat("_StarDensity", starDensity);
        victoryMaterial.SetFloat("_StarSpeed", starSpeed);
        victoryMaterial.SetFloat("_StarSize", starSize);
        victoryMaterial.SetFloat("_StarBrightness", starBrightness);
        victoryMaterial.SetFloat("_Brightness", brightness * fadeProgress);
    }
    
    void OnDisable()
    {
        if (victoryOverlay != null)
            victoryOverlay.SetActive(false);
        
        isShowing = false;
    }
    
    void OnDestroy()
    {
        if (victoryOverlay != null)
            Destroy(victoryOverlay);
    }
}