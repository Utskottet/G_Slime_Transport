using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    [Header("References")]
    public SlimeGameManager gameManager;
    
    [Header("Base Speed")]
    [Range(0f, 1f)]
    [Tooltip("Constant growth speed for enemy")]
    public float baseSpeed = 0.3f;
    
    [Header("Time-Based Speed Curve")]
    public bool useTimeCurve = false;
    public AnimationCurve speedOverTime = AnimationCurve.Linear(0, 0.3f, 180, 0.8f);
    
    [Header("Coverage-Based Adjustments")]
    public bool useCoverageBoost = false;
    
    [Tooltip("Enemy speeds up when it has less than this coverage")]
    [Range(0f, 0.5f)]
    public float lowCoverageThreshold = 0.2f;
    
    [Tooltip("Speed multiplier when below threshold")]
    [Range(1f, 3f)]
    public float lowCoverageBoost = 1.5f;
    
    [Header("Game Phase Adjustments")]
    public bool adjustByPhase = false;
    public float playingPhaseSpeed = 0.3f;
    
    private float currentIntensity = 0f;
    
    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<SlimeGameManager>();
            if (gameManager == null)
            {
                Debug.LogError("EnemyAIController: No SlimeGameManager found!");
            }
        }
    }
    
    void Update()
    {
        if (gameManager == null) return;
        
        // Only update during playing phase
        if (gameManager.phase != SlimeGameManager.GamePhase.Playing)
        {
            currentIntensity = 0f;
            return;
        }
        
        // Start with base speed
        float speed = baseSpeed;
        
        // Apply time curve if enabled
        if (useTimeCurve)
        {
            float gameTime = Time.timeSinceLevelLoad;
            speed = speedOverTime.Evaluate(gameTime);
        }
        
        // Apply coverage boost if enabled
        if (useCoverageBoost)
        {
            float coverage = gameManager.GetEnemyCoverage();
            
            if (coverage < lowCoverageThreshold)
            {
                // Enemy is losing - boost its speed!
                speed *= lowCoverageBoost;
            }
        }
        
        // Phase-based override
        if (adjustByPhase)
        {
            if (gameManager.phase == SlimeGameManager.GamePhase.Playing)
            {
                speed = playingPhaseSpeed;
            }
        }
        
        currentIntensity = Mathf.Clamp01(speed);
        
        Debug.Log("Enemy intensity: " + currentIntensity + ", base: " + baseSpeed);
    }

    
    
    public float GetEnemyIntensity()
    {
        return currentIntensity;
    }


    
    // Debug visualization
    void OnGUI()
    {
        if (!Application.isPlaying || gameManager == null) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.normal.textColor = Color.green;
        
        GUI.backgroundColor = new Color(0.1f, 0.3f, 0.1f, 0.8f);
        GUI.Box(new Rect(10, 390, 280, 50), "", style);
        
        style.fontSize = 14;
        float coverage = gameManager.GetEnemyCoverage();
        GUI.Label(new Rect(20, 400, 260, 25), $"Enemy AI: {currentIntensity:F2} (Cov: {coverage:P0})", style);
    }
}