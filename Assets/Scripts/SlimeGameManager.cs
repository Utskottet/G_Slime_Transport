using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class SlimeGameManager : MonoBehaviour
{
    [Header("Assets (REQUIRED)")]
    public Texture2D houseImage;
    public Texture2D obstacleMap;
    public Material slimeMaterial;


    [Header("Debug")]
    public bool debugAutoGrow = false;
    public bool showObstacleMap = false;
    public bool showDebugGrid = true;

    [Header("Visuals")]
    public bool showBackground = true;

    [Header("Spawn Points (Enemy only uses this)")]
    public Transform spawnPointP1;
    public Transform spawnPointP2;
    public Transform spawnPointP3;
    public Transform spawnPointEnemy;

    [Header("Trays (Players spawn from these)")]
    public SlimeTray trayP1;
    public SlimeTray trayP2;
    public SlimeTray trayP3;

    [Header("Settings")]
    public int gridWidth = 400;
    public int gridHeight = 112;
    [Range(1, 60)] public float simulationSpeed = 20f;

    [Header("Game Rules")]
    [Tooltip("If enemy slime covers this fraction of non-wall cells, slime wins.")]
    public float slimeWinPercent = 0.95f;  // 95% = lose

    [Tooltip("If enemy slime is pushed down below this fraction, players win.")]
    public float playerWinPercent = 0.05f; // 5% = win

    [Tooltip("ID used for enemy slime in the grid.")]
    public int enemyId = 5;

    [Tooltip("Seconds after start before win/lose can trigger.")]
    public float winCheckDelay = 8f;       // tweak in Inspector

    [Tooltip("Seconds to show WIN FX before restart.")]
    public float playerWinDelay = 6f;

    [Tooltip("Seconds to show LOSE FX before restart.")]
    public float slimeWinDelay = 4f;

    [Header("Win/Lose FX (optional)")]
    public GameObject playerWinFx;   // coinrain, WIN-text
    public GameObject slimeWinFx;    // SLIME WINS-text


    [Header("Controllers")]
    public PlayerInputController playerController;
    public EnemyAIController enemyController;

    public enum GamePhase { Playing, PlayerWin, SlimeWin }
    [HideInInspector] public GamePhase phase = GamePhase.Playing;

    // =========================================================
    // AUDIO FLAGS - read by SlimeAudioManager
    // =========================================================
    [Header("Audio Flags (read-only, for debugging)")]
    [SerializeField] private bool _anyPlayerExpanding;
    [SerializeField] private bool _anyPlayerRetreating;
    [SerializeField] private bool _playerPushingEnemy;
    [SerializeField] private bool _playerPushingPlayer;
    [SerializeField] private bool _playerHitEnemy;
    [SerializeField] private bool _playerHitPlayer;

    // Public accessors
    public bool anyPlayerExpanding => _anyPlayerExpanding;
    public bool anyPlayerRetreating => _anyPlayerRetreating;
    public bool playerPushingEnemy => _playerPushingEnemy;
    public bool playerPushingPlayer => _playerPushingPlayer;
    public bool playerHitEnemy => _playerHitEnemy;
    public bool playerHitPlayer => _playerHitPlayer;

    // Tracking for one-shot triggers
    private bool wasPlayerTouchingEnemy = false;
    private bool wasPlayerTouchingPlayer = false;

    // Internal
    [HideInInspector] public byte[,] grid;
    [HideInInspector] public byte[,] gridThickness;
    private SpriteRenderer bgRenderer;

    public List<SlimeAgent> players = new List<SlimeAgent>();
    public List<SlimeAgent> allAgents = new List<SlimeAgent>();

    private float timer;
    private float gameTime;


    void OnValidate()
    {
        InitMap();
        UpdateBackgroundVisuals();
    }

    void OnEnable()
    {
        InitMap();
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            InitMap();
            players.Clear();
            allAgents.Clear();

            // Players spawn from trays
            SpawnPlayerFromTray(2, trayP1, Color.blue);      // P1 -> players[0]
            SpawnPlayerFromTray(3, trayP2, Color.magenta);   // P2 -> players[1]
            SpawnPlayerFromTray(4, trayP3, Color.yellow);    // P3 -> players[2]

            // Enemy spawn (pattern handled inside SlimeAgent.Init)
            if (spawnPointEnemy != null)
                SpawnFromTransform(enemyId, spawnPointEnemy, Color.green, true);
            else
                CreateSlimeAgent(enemyId, Vector2Int.zero, Color.green, true);

            phase = GamePhase.Playing;
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (phase != GamePhase.Playing) return;

        // Track how long the game has been running
        gameTime += Time.deltaTime;

        // --- SIMULATION TICKING ---
        timer += Time.deltaTime;
        if (timer >= (1f / simulationSpeed))
        {
            timer = 0f;
            
            // Reset per-tick push flags (these get set by SlimeAgent during tick)
            _playerPushingEnemy = false;
            _playerPushingPlayer = false;
            
            for (int i = allAgents.Count - 1; i >= 0; i--)
            {
                if (allAgents[i] == null)
                    allAgents.RemoveAt(i);
                else
                    allAgents[i].GameTick();
            }
            
            // Update audio flags after all agents have ticked
            UpdateAudioFlags();
        }

        // --- INPUT FROM CONTROLLERS ---
        if (playerController != null)
        {
            float[] inputs = playerController.GetPlayerIntensities();
            for (int i = 0; i < Mathf.Min(players.Count, inputs.Length); i++)
            {
                if (players[i] != null)
                    players[i].inputIntensity = inputs[i];
            }
        }

        // Set enemy intensity from AI controller
        if (enemyController != null)
{
        // Find the enemy agent (it's the one that's not in players list)
        foreach (var agent in allAgents)
        {
            if (agent != null && !players.Contains(agent))
            {
                float intensity = enemyController.GetEnemyIntensity();
                agent.inputIntensity = intensity;
                Debug.Log("Setting enemy agent intensity to: " + intensity);
                break; // Only one enemy
        }
    }
}

        // --- GAME RULES: WIN/LOSE ---

        // Do NOT check win/lose until the game has had time to "start"
        if (gameTime < winCheckDelay)
            return;

        float coverage = GetEnemyCoverage();

        // Ignorera mikro-slim (<1%)
        if (coverage < 0.01f)
            return;

        // Slime wins
        if (coverage >= slimeWinPercent)
        {
            SlimeWins();
        }
        // Players win (else if så båda inte triggar samma frame)
        else if (coverage <= playerWinPercent)
        {
            PlayersWin();
        }
    }

    // =========================================================
    // AUDIO FLAG UPDATES
    // =========================================================
    void UpdateAudioFlags()
    {
        // --- Expanding / Retreating ---
        _anyPlayerExpanding = false;
        _anyPlayerRetreating = false;

        foreach (var player in players)
        {
            if (player == null) continue;
            
            if (player.inputIntensity > 0 && PlayerHasCells(player))
                _anyPlayerExpanding = true;
            
            if (player.inputIntensity == 0 && PlayerHasCells(player))
                _anyPlayerRetreating = true;
        }

        // --- Contact detection ---
        bool playerTouchingEnemy = false;
        bool playerTouchingPlayer = false;

        // Scan grid for frontier contacts
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                byte cell = grid[x, y];
                
                // Only check player cells (2, 3, 4)
                if (cell < 2 || cell > 4) continue;

                // Check neighbors
                if (x > 0) CheckNeighbor(cell, grid[x - 1, y], ref playerTouchingEnemy, ref playerTouchingPlayer);
                if (x < gridWidth - 1) CheckNeighbor(cell, grid[x + 1, y], ref playerTouchingEnemy, ref playerTouchingPlayer);
                if (y > 0) CheckNeighbor(cell, grid[x, y - 1], ref playerTouchingEnemy, ref playerTouchingPlayer);
                if (y < gridHeight - 1) CheckNeighbor(cell, grid[x, y + 1], ref playerTouchingEnemy, ref playerTouchingPlayer);
            }
        }

        // --- One-shot hit detection (first frame of contact) ---
        _playerHitEnemy = playerTouchingEnemy && !wasPlayerTouchingEnemy;
        _playerHitPlayer = playerTouchingPlayer && !wasPlayerTouchingPlayer;

        // Store for next frame
        wasPlayerTouchingEnemy = playerTouchingEnemy;
        wasPlayerTouchingPlayer = playerTouchingPlayer;
    }

    void CheckNeighbor(byte playerCell, byte neighborCell, ref bool touchingEnemy, ref bool touchingPlayer)
    {
        // Neighbor is enemy
        if (neighborCell == enemyId)
            touchingEnemy = true;
        
        // Neighbor is another player (not same player, not wall, not empty, not enemy)
        if (neighborCell >= 2 && neighborCell <= 4 && neighborCell != playerCell)
            touchingPlayer = true;
    }

    bool PlayerHasCells(SlimeAgent player)
    {
        if (player == null || grid == null) return false;
        
        // Get player id by checking which player index this is
        int playerId = players.IndexOf(player) + 2; // players[0]=id2, players[1]=id3, players[2]=id4
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y] == playerId)
                    return true;
            }
        }
        return false;
    }

    // Called by SlimeAgent when it claims a cell that belonged to enemy
    public void NotifyPlayerPushedEnemy()
    {
        _playerPushingEnemy = true;
    }

    // Called by SlimeAgent when it claims a cell that belonged to another player
    public void NotifyPlayerPushedPlayer()
    {
        _playerPushingPlayer = true;
    }

    // --- ON SCREEN DEBUG GUI ---
void OnGUI()
{
    if (!Application.isPlaying) return;

    GUIStyle style = new GUIStyle(GUI.skin.box);
    style.fontSize = 20;
    style.fontStyle = FontStyle.Bold;
    style.normal.textColor = Color.white;

    float v1 = (players.Count > 0 && players[0]) ? players[0].inputIntensity : 0;
    GUI.backgroundColor = v1 > 0 ? Color.blue : Color.gray;
    GUI.Box(new Rect(10, 10, 200, 40), "P1 (Key 1/A): " + v1, style);

    float v2 = (players.Count > 1 && players[1]) ? players[1].inputIntensity : 0;
    GUI.backgroundColor = v2 > 0 ? Color.magenta : Color.gray;
    GUI.Box(new Rect(10, 60, 200, 40), "P2 (Key 2/S): " + v2, style);

    float v3 = (players.Count > 2 && players[2]) ? players[2].inputIntensity : 0;
    GUI.backgroundColor = v3 > 0 ? Color.yellow : Color.gray;
    GUI.Box(new Rect(10, 110, 200, 40), "P3 (Key 3/D): " + v3, style);

    float coverage = GetEnemyCoverage();
    GUI.backgroundColor = Color.black;
    GUI.Box(new Rect(10, 160, 260, 40), "Enemy coverage: " + (coverage * 100f).ToString("0.0") + "% (" + phase + ")", style);

    // Game Timer
    int minutes = Mathf.FloorToInt(gameTime / 60f);
    int seconds = Mathf.FloorToInt(gameTime % 60f);
    GUI.backgroundColor = new Color(0.2f, 0.5f, 0.8f);
    GUI.Box(new Rect(10, 210, 200, 40), "Time: " + string.Format("{0:00}:{1:00}", minutes, seconds), style);

    // Audio flags debug
    GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
    string audioState = "EXP:" + (_anyPlayerExpanding ? "1" : "0") + " RET:" + (_anyPlayerRetreating ? "1" : "0") + " " +
                       "PvE:" + (_playerPushingEnemy ? "1" : "0") + " PvP:" + (_playerPushingPlayer ? "1" : "0");
    GUI.Box(new Rect(10, 260, 320, 40), audioState, style);
}

    // --- GAME RULE HELPERS ---
    public float GetEnemyCoverage()
    {
        if (grid == null) return 0f;

        int totalPlayable = 0;
        int enemyCells = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                byte cell = grid[x, y];

                if (cell == 1) // wall
                    continue;

                totalPlayable++;

                if (cell == enemyId)
                    enemyCells++;
            }
        }

        if (totalPlayable == 0) return 0f;
        return (float)enemyCells / totalPlayable;
    }

    IEnumerator RestartSceneRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    void SlimeWins()
    {
        if (phase != GamePhase.Playing) return;

        phase = GamePhase.SlimeWin;
        Debug.Log($"SLIME WINS – coverage: {GetEnemyCoverage() * 100f:0.0}%");

        // visa lose-FX
        if (slimeWinFx != null)
            slimeWinFx.SetActive(true);

        StartCoroutine(RestartSceneRoutine(slimeWinDelay));
    }

    void PlayersWin()
    {
        if (phase != GamePhase.Playing) return;

        phase = GamePhase.PlayerWin;
        Debug.Log($"PLAYERS WIN – coverage: {GetEnemyCoverage() * 100f:0.0}%");

        // visa win-FX (coinrain, text osv)
        if (playerWinFx != null)
            playerWinFx.SetActive(true);

        StartCoroutine(RestartSceneRoutine(playerWinDelay));
    }

    // --- SPAWNING ---
    void SpawnFromTransform(int id, Transform t, Color c, bool isEnemy = false)
    {
        if (t == null) return;
        Vector2Int gridPos = WorldToGrid(t.position);
        gridPos.x = Mathf.Clamp(gridPos.x, 0, gridWidth - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, gridHeight - 1);
        CreateSlimeAgent(id, gridPos, c, isEnemy);
    }

    void SpawnPlayerFromTray(int id, SlimeTray tray, Color c)
    {
        if (tray == null)
        {
            Debug.LogWarning($"No tray assigned for player id {id}");
            return;
        }

        Vector2Int gridPos = WorldToGrid(tray.transform.position);
        gridPos.x = Mathf.Clamp(gridPos.x, 0, gridWidth - 1);
        gridPos.y = Mathf.Clamp(gridPos.y, 0, gridHeight - 1);

        SlimeAgent agent = CreateSlimeAgent(id, gridPos, c, false);

        agent.tray = tray;
        tray.owner = agent;
    }

    void InitMap()
    {
        if (obstacleMap == null) return;
        grid = new byte[gridWidth, gridHeight];
        gridThickness = new byte[gridWidth, gridHeight];

        if (!obstacleMap.isReadable) return;

        float rX = (float)obstacleMap.width / gridWidth;
        float rY = (float)obstacleMap.height / gridHeight;

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
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
        if (bg == null)
        {
            bg = new GameObject("Background_House");
            bg.transform.position = new Vector3(0, 0, 1);
            bgRenderer = bg.AddComponent<SpriteRenderer>();
        }
        else
        {
            bgRenderer = bg.GetComponent<SpriteRenderer>();
        }

        // Select texture: obstacle map OR house
        Texture2D texToShow = showObstacleMap ? obstacleMap : houseImage;

        if (texToShow != null)
        {
            bgRenderer.sprite = Sprite.Create(
                texToShow,
                new Rect(0, 0, texToShow.width, texToShow.height),
                new Vector2(0.5f, 0.5f),
                100f);

            float targetHeight = 10.8f;
            float scaleY = targetHeight / (texToShow.height / 100f);
            float targetWidth = (float)texToShow.width / texToShow.height * targetHeight;
            float scaleX = targetWidth / (texToShow.width / 100f);

            bg.transform.localScale = new Vector3(scaleX, scaleY, 1);
        }

        // NEW: only change alpha, never disable object
        if (bgRenderer != null)
        {
            Color c = bgRenderer.color;
            c.a = showBackground ? 1f : 0f;   // 1 = visible, 0 = invisible
            bgRenderer.color = c;
        }
    }

    SlimeAgent CreateSlimeAgent(int id, Vector2Int startSeed, Color c, bool isEnemy)
{
    GameObject go = new GameObject(isEnemy ? "Enemy" : "Player_ID" + id);
    SlimeRenderer rend = go.AddComponent<SlimeRenderer>();
    float aspect = 1.77f;
    if (bgRenderer && bgRenderer.bounds.size.y > 0)
        aspect = bgRenderer.bounds.size.x / bgRenderer.bounds.size.y;

    rend.Init(this, c, slimeMaterial, aspect);
    SlimeAgent agent = go.AddComponent<SlimeAgent>();
    agent.Init(this, rend, id, startSeed, isEnemy);
    
    // Set growth rates based on agent type
    if (isEnemy)
    {
        agent.minGrowthRate = 0;   // Enemy minimum speed
        agent.maxGrowthRate = 40;  // Enemy maximum speed
    }
    else
    {
        agent.minGrowthRate = 0;   // Players can stop completely
        agent.maxGrowthRate = 30;  // Players maximum speed
    }
    
    allAgents.Add(agent);
    if (!isEnemy) players.Add(agent);

    return agent;
}

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (!bgRenderer) return Vector2Int.zero;
        Bounds b = bgRenderer.bounds;
        float nx = (worldPos.x - b.min.x) / b.size.x;
        float ny = (worldPos.y - b.min.y) / b.size.y;
        return new Vector2Int(Mathf.FloorToInt(nx * gridWidth), Mathf.FloorToInt(ny * gridHeight));
    }

    void OnDrawGizmos()
    {
        if (!bgRenderer) return;
        Bounds b = bgRenderer.bounds;
        if (showDebugGrid && grid != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            float cellW = b.size.x / gridWidth;
            float cellH = b.size.y / gridHeight;
            for (int y = 0; y < gridHeight; y += 2)
            {
                for (int x = 0; x < gridWidth; x += 2)
                {
                    if (grid[x, y] == 1)
                    {
                        float wx = b.min.x + (x * cellW) + (cellW * 0.5f);
                        float wy = b.min.y + (y * cellH) + (cellH * 0.5f);
                        Gizmos.DrawCube(new Vector3(wx, wy, -0.1f),
                            new Vector3(cellW * 2, cellH * 2, 0.1f));
                    }
                }
            }
        }
        CheckSpawnGizmo(spawnPointP1, Color.blue);
        CheckSpawnGizmo(spawnPointP2, Color.magenta);
        CheckSpawnGizmo(spawnPointP3, Color.yellow);
    }

    void CheckSpawnGizmo(Transform t, Color c)
    {
        if (t == null) return;
        Vector2Int g = WorldToGrid(t.position);
        bool isSafe = g.x >= 0 && g.x < gridWidth &&
                      g.y >= 0 && g.y < gridHeight &&
                      (grid == null || grid[g.x, g.y] == 0);
        Gizmos.color = isSafe ? c : Color.red;
        Gizmos.DrawSphere(t.position, 0.3f);
        if (!isSafe) Gizmos.DrawWireSphere(t.position, 0.6f);
    }
}