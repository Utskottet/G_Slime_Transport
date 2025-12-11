using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class SlimeGameManager : MonoBehaviour
{
    [Header("Assets (REQUIRED)")]
    public Texture2D houseImage;
    public Texture2D obstacleMap;
    public Shader slimeShader;

    [Header("Debug")]
    public bool debugAutoGrow = false;
    public bool showObstacleMap = false;
    public bool showDebugGrid = true;

    [Header("Spawn Points")]
    public Transform spawnPointP1;
    public Transform spawnPointP2;
    public Transform spawnPointP3;
    public Transform spawnPointEnemy;

    

    [Header("Settings")]
    public int gridWidth = 400;
    public int gridHeight = 112;
    [Range(1, 60)] public float simulationSpeed = 20f;
    
    // Internal
    [HideInInspector] public byte[,] grid; 
    [HideInInspector] public byte[,] gridThickness; 
    private SpriteRenderer bgRenderer;
    
    public List<SlimeAgent> players = new List<SlimeAgent>();
    public List<SlimeAgent> allAgents = new List<SlimeAgent>();
    
    private float timer;

    void OnValidate() { InitMap(); UpdateBackgroundVisuals(); }
    void OnEnable() { InitMap(); }

    void Start()
    {
        if (Application.isPlaying) {
            InitMap();
            players.Clear();
            allAgents.Clear();

            // Spawn Players (Order matters for Input 1, 2, 3)
            SpawnFromTransform(2, spawnPointP1, Color.blue);      // P1 -> players[0]
            SpawnFromTransform(3, spawnPointP2, Color.magenta);   // P2 -> players[1]
            SpawnFromTransform(4, spawnPointP3, Color.yellow);    // P3 -> players[2]
            
            // Spawn Enemy
            if (spawnPointEnemy != null) SpawnFromTransform(5, spawnPointEnemy, Color.green, true);
            else CreateSlimeAgent(5, Vector2Int.zero, Color.green, true);
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        timer += Time.deltaTime;
        if (timer >= (1f / simulationSpeed))
        {
            timer = 0;
            for (int i = allAgents.Count - 1; i >= 0; i--) {
                if (allAgents[i] == null) allAgents.RemoveAt(i);
                else allAgents[i].GameTick();
            }
        }

        // --- ROBUST INPUT ---
        // We check 3 different keys for each player to be safe.
        
        float p1 = 0f;
        if (debugAutoGrow || Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1) || Input.GetKey(KeyCode.A)) p1 = 1f;

        float p2 = 0f;
        if (debugAutoGrow || Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2) || Input.GetKey(KeyCode.S)) p2 = 1f;

        float p3 = 0f;
        if (debugAutoGrow || Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3) || Input.GetKey(KeyCode.D)) p3 = 1f;

        // Apply to Agents
        if (players.Count > 0 && players[0]) players[0].inputIntensity = p1;
        if (players.Count > 1 && players[1]) players[1].inputIntensity = p2;
        if (players.Count > 2 && players[2]) players[2].inputIntensity = p3;
    }

    // --- ON SCREEN DEBUG GUI ---
    // This draws bars on screen so you KNOW if the key is working.
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;

        // P1 STATUS
        float v1 = (players.Count > 0 && players[0]) ? players[0].inputIntensity : 0;
        GUI.backgroundColor = v1 > 0 ? Color.blue : Color.gray;
        GUI.Box(new Rect(10, 10, 200, 40), $"P1 (Key 1/A): {v1}", style);

        // P2 STATUS
        float v2 = (players.Count > 1 && players[1]) ? players[1].inputIntensity : 0;
        GUI.backgroundColor = v2 > 0 ? Color.magenta : Color.gray;
        GUI.Box(new Rect(10, 60, 200, 40), $"P2 (Key 2/S): {v2}", style);

        // P3 STATUS
        float v3 = (players.Count > 2 && players[2]) ? players[2].inputIntensity : 0;
        GUI.backgroundColor = v3 > 0 ? Color.yellow : Color.gray;
        GUI.Box(new Rect(10, 110, 200, 40), $"P3 (Key 3/D): {v3}", style);
    }

    // --- STANDARD LOGIC ---
    void SpawnFromTransform(int id, Transform t, Color c, bool isEnemy = false)
    {
        if (t == null) return;
        Vector2Int gridPos = WorldToGrid(t.position);
        gridPos.x = Mathf.Clamp(gridPos.x, 0, gridWidth - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, gridHeight - 1);
        CreateSlimeAgent(id, gridPos, c, isEnemy);
    }

    void InitMap()
    {
        if (obstacleMap == null) return;
        grid = new byte[gridWidth, gridHeight];
        gridThickness = new byte[gridWidth, gridHeight];

        if (!obstacleMap.isReadable) return;

        float rX = (float)obstacleMap.width / gridWidth;
        float rY = (float)obstacleMap.height / gridHeight;

        for (int y = 0; y < gridHeight; y++) {
            for (int x = 0; x < gridWidth; x++) {
                int sx = Mathf.Clamp((int)(x * rX), 0, obstacleMap.width - 1);
                int sy = Mathf.Clamp((int)(y * rY), 0, obstacleMap.height - 1);
                Color p = obstacleMap.GetPixel(sx, sy);
                bool isRedWall = (p.r > 0.5f && p.g < 0.4f && p.b < 0.4f);
                grid[x, y] = isRedWall ? (byte)1 : (byte)0;
            }
        }
    }

    void UpdateBackgroundVisuals()
    {
        GameObject bg = GameObject.Find("Background_House");
        if (bg == null) {
            bg = new GameObject("Background_House");
            bg.transform.position = new Vector3(0, 0, 1);
            bgRenderer = bg.AddComponent<SpriteRenderer>();
        } else {
            bgRenderer = bg.GetComponent<SpriteRenderer>();
        }

        Texture2D texToShow = showObstacleMap ? obstacleMap : houseImage;
        if (texToShow != null) {
            bgRenderer.sprite = Sprite.Create(texToShow, new Rect(0,0, texToShow.width, texToShow.height), new Vector2(0.5f, 0.5f), 100f);
            float targetHeight = 10.8f; 
            float scaleY = targetHeight / (texToShow.height / 100f);
            float targetWidth = (float)texToShow.width / texToShow.height * targetHeight;
            float scaleX = targetWidth / (texToShow.width / 100f);
            bg.transform.localScale = new Vector3(scaleX, scaleY, 1);
        }
    }

    void CreateSlimeAgent(int id, Vector2Int startSeed, Color c, bool isEnemy)
    {
        GameObject go = new GameObject(isEnemy ? "Enemy" : "Player_ID" + id);
        SlimeRenderer rend = go.AddComponent<SlimeRenderer>();
        float aspect = 1.77f;
        if(bgRenderer && bgRenderer.bounds.size.y > 0) aspect = bgRenderer.bounds.size.x / bgRenderer.bounds.size.y;
        
        rend.Init(this, c, slimeShader, aspect);
        SlimeAgent agent = go.AddComponent<SlimeAgent>();
        agent.Init(this, rend, id, startSeed, isEnemy);
        allAgents.Add(agent);
        if (!isEnemy) players.Add(agent);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos) {
        if (!bgRenderer) return Vector2Int.zero;
        Bounds b = bgRenderer.bounds;
        float nx = (worldPos.x - b.min.x) / b.size.x;
        float ny = (worldPos.y - b.min.y) / b.size.y;
        return new Vector2Int(Mathf.FloorToInt(nx * gridWidth), Mathf.FloorToInt(ny * gridHeight));
    }

    void OnDrawGizmos() {
        if (!bgRenderer) return;
        Bounds b = bgRenderer.bounds;
        if (showDebugGrid && grid != null) {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            float cellW = b.size.x / gridWidth;
            float cellH = b.size.y / gridHeight;
            for (int y = 0; y < gridHeight; y+=2) {
                for (int x = 0; x < gridWidth; x+=2) {
                    if (grid[x, y] == 1) {
                        float wx = b.min.x + (x * cellW) + (cellW * 0.5f);
                        float wy = b.min.y + (y * cellH) + (cellH * 0.5f);
                        Gizmos.DrawCube(new Vector3(wx, wy, -0.1f), new Vector3(cellW*2, cellH*2, 0.1f));
                    }
                }
            }
        }
        CheckSpawnGizmo(spawnPointP1, Color.blue);
        CheckSpawnGizmo(spawnPointP2, Color.magenta);
        CheckSpawnGizmo(spawnPointP3, Color.yellow);
    }
    void CheckSpawnGizmo(Transform t, Color c) {
        if (t == null) return;
        Vector2Int g = WorldToGrid(t.position);
        // IsValid checks if inside bounds AND not a wall
        bool isSafe = g.x >= 0 && g.x < gridWidth && g.y >= 0 && g.y < gridHeight && (grid == null || grid[g.x, g.y] == 0);
        Gizmos.color = isSafe ? c : Color.red;
        Gizmos.DrawSphere(t.position, 0.3f);
        if (!isSafe) Gizmos.DrawWireSphere(t.position, 0.6f);
    }
}