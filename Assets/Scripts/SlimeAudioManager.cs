using UnityEngine;

public class SlimeAudioManager : MonoBehaviour
{
    [Header("Slime Drone (loop - during gameplay)")]
    public AudioClip slimeDrone;
    [Range(0f, 1f)] public float slimeDroneVolume = 0.5f;
    
    [Header("Player Expanding (loop - player growing)")]
    public AudioClip playerExpanding;
    [Range(0f, 1f)] public float playerExpandingVolume = 0.5f;
    
    [Header("Player Retreating (loop - player shrinking)")]
    public AudioClip playerRetreating;
    [Range(0f, 1f)] public float playerRetreatingVolume = 0.5f;
    
    [Header("Player Pushing Enemy (loop - claiming enemy cells)")]
    public AudioClip playerPushingEnemy;
    [Range(0f, 1f)] public float playerPushingEnemyVolume = 0.5f;
    
    [Header("Player Pushing Player (loop - claiming player cells)")]
    public AudioClip playerPushingPlayer;
    [Range(0f, 1f)] public float playerPushingPlayerVolume = 0.5f;
    
    [Header("Player Hit Enemy (one-shot - first contact)")]
    public AudioClip playerHitEnemy;
    [Range(0f, 1f)] public float playerHitEnemyVolume = 0.7f;
    
    [Header("Player Hit Player (one-shot - first contact)")]
    public AudioClip playerHitPlayer;
    [Range(0f, 1f)] public float playerHitPlayerVolume = 0.7f;
    
    [Header("Game Start (one-shot)")]
    public AudioClip gameStart;
    [Range(0f, 1f)] public float gameStartVolume = 1f;
    
    [Header("Game Win (one-shot)")]
    public AudioClip gameWin;
    [Range(0f, 1f)] public float gameWinVolume = 1f;
    
    [Header("Game Lose (one-shot)")]
    public AudioClip gameLose;
    [Range(0f, 1f)] public float gameLoseVolume = 1f;
    
    [Header("References")]
    public SlimeGameManager gameManager;
    
    // Audio sources - loops
    private AudioSource droneSource;
    private AudioSource expandingSource;
    private AudioSource retreatingSource;
    private AudioSource pushEnemySource;
    private AudioSource pushPlayerSource;
    
    // Audio source for one-shots
    private AudioSource oneShotSource;
    
    // Track previous phase for win/lose detection
    private SlimeGameManager.GamePhase lastPhase;
    private bool startSoundPlayed = false;
    
    void Start()
    {
        // Find game manager if not assigned
        if (gameManager == null)
            gameManager = FindObjectOfType<SlimeGameManager>();
        
        // Create loop sources
        droneSource = CreateLoopSource(slimeDrone, slimeDroneVolume);
        expandingSource = CreateLoopSource(playerExpanding, playerExpandingVolume);
        retreatingSource = CreateLoopSource(playerRetreating, playerRetreatingVolume);
        pushEnemySource = CreateLoopSource(playerPushingEnemy, playerPushingEnemyVolume);
        pushPlayerSource = CreateLoopSource(playerPushingPlayer, playerPushingPlayerVolume);
        
        // Create one-shot source
        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.spatialBlend = 0f;
        oneShotSource.playOnAwake = false;
        
        // Play game start sound
        if (gameStart != null)
        {
            oneShotSource.PlayOneShot(gameStart, gameStartVolume);
            startSoundPlayed = true;
        }
        
        lastPhase = SlimeGameManager.GamePhase.Playing;
    }
    
    AudioSource CreateLoopSource(AudioClip clip, float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        return source;
    }
    
    void Update()
    {
        if (gameManager == null) return;
        
        bool isPlaying = gameManager.phase == SlimeGameManager.GamePhase.Playing;
        
        // === LOOPS ===
        
        // Drone: plays during gameplay
        UpdateLoop(droneSource, isPlaying, slimeDroneVolume);
        
        // Expanding: any player expanding
        UpdateLoop(expandingSource, isPlaying && gameManager.anyPlayerExpanding, playerExpandingVolume);
        
        // Retreating: any player retreating
        UpdateLoop(retreatingSource, isPlaying && gameManager.anyPlayerRetreating, playerRetreatingVolume);
        
        // Push enemy: player claiming enemy cells
        UpdateLoop(pushEnemySource, isPlaying && gameManager.playerPushingEnemy, playerPushingEnemyVolume);
        
        // Push player: player claiming other player cells
        UpdateLoop(pushPlayerSource, isPlaying && gameManager.playerPushingPlayer, playerPushingPlayerVolume);
        
        // === ONE-SHOTS ===
        
        // Hit enemy (first contact)
        if (gameManager.playerHitEnemy && playerHitEnemy != null)
            oneShotSource.PlayOneShot(playerHitEnemy, playerHitEnemyVolume);
        
        // Hit player (first contact)
        if (gameManager.playerHitPlayer && playerHitPlayer != null)
            oneShotSource.PlayOneShot(playerHitPlayer, playerHitPlayerVolume);
        
        // === GAME END ===
        
        // Win
        if (lastPhase == SlimeGameManager.GamePhase.Playing && 
            gameManager.phase == SlimeGameManager.GamePhase.PlayerWin)
        {
            if (gameWin != null)
                oneShotSource.PlayOneShot(gameWin, gameWinVolume);
        }
        
        // Lose
        if (lastPhase == SlimeGameManager.GamePhase.Playing && 
            gameManager.phase == SlimeGameManager.GamePhase.SlimeWin)
        {
            if (gameLose != null)
                oneShotSource.PlayOneShot(gameLose, gameLoseVolume);
        }
        
        lastPhase = gameManager.phase;
    }
    
    void UpdateLoop(AudioSource source, bool shouldPlay, float volume)
    {
        if (source == null || source.clip == null) return;
        
        if (shouldPlay && !source.isPlaying)
            source.Play();
        else if (!shouldPlay && source.isPlaying)
            source.Stop();
        
        source.volume = volume;
    }
}