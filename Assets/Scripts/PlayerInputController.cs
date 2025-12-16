using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerSpeedSettings
    {
        public string playerName = "Player 1";
        
        [Header("Growth Speeds")]
        [Range(0f, 1f)]
        public float slowSpeed = 0.5f;
        
        [Range(0f, 1f)]
        public float fastSpeed = 1.0f;
        
        [Header("Retreat")]
        [Range(0f, 2f)]
        public float retreatSpeed = 1.0f;
    }
    
    public enum InputMode
    {
        Keyboard,
        OSC,
        Both
    }
    
    [Header("Input Method")]
    public InputMode inputMode = InputMode.Keyboard;
    
    [Header("Player Settings")]
    public PlayerSpeedSettings player1 = new PlayerSpeedSettings { playerName = "Player 1" };
    public PlayerSpeedSettings player2 = new PlayerSpeedSettings { playerName = "Player 2" };
    public PlayerSpeedSettings player3 = new PlayerSpeedSettings { playerName = "Player 3" };
    
    [Header("OSC Settings")]
    public string oscAddress1 = "/pedal1";
    public string oscAddress2 = "/pedal2";
    public string oscAddress3 = "/pedal3";
    
    [Range(0f, 0.5f)]
    [Tooltip("Below this value = stop/retreat")]
    public float oscStopThreshold = 0.25f;
    
    [Range(0.5f, 1f)]
    [Tooltip("Above this value = fast speed")]
    public float oscFastThreshold = 0.75f;
    
    // Current intensities for each player
    private float[] playerIntensities = new float[3];
    
    // OSC values (0-1 from pedals)
    private float oscValue1 = 0f;
    private float oscValue2 = 0f;
    private float oscValue3 = 0f;
    
    // Keyboard input states (0=stop, 1=slow, 2=fast)
    private int keyboardState1 = 0;
    private int keyboardState2 = 0;
    private int keyboardState3 = 0;
    
    void Start()
    {
        // extOSC setup will be done manually in Inspector
        // We'll bind OSC receivers to call our SetPedal methods
        Debug.Log("PlayerInputController started. Configure extOSC receivers to call SetPedal1/2/3 methods.");
    }
    
    void Update()
    {
        // Update keyboard input
        if (inputMode == InputMode.Keyboard || inputMode == InputMode.Both)
        {
            UpdateKeyboardInput();
        }
        
        // Calculate final intensities based on input mode
        CalculateIntensities();
    }
    
    void UpdateKeyboardInput()
    {
        // Player 1: Keys 1, 2, 3
        if (Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Keypad1))
            keyboardState1 = 0; // Stop
        else if (Input.GetKey(KeyCode.Alpha2) || Input.GetKey(KeyCode.Keypad2))
            keyboardState1 = 1; // Slow
        else if (Input.GetKey(KeyCode.Alpha3) || Input.GetKey(KeyCode.Keypad3))
            keyboardState1 = 2; // Fast
        else
            keyboardState1 = 0; // Nothing pressed = stop
        
        // Player 2: Keys 4, 5, 6
        if (Input.GetKey(KeyCode.Alpha4) || Input.GetKey(KeyCode.Keypad4))
            keyboardState2 = 0;
        else if (Input.GetKey(KeyCode.Alpha5) || Input.GetKey(KeyCode.Keypad5))
            keyboardState2 = 1;
        else if (Input.GetKey(KeyCode.Alpha6) || Input.GetKey(KeyCode.Keypad6))
            keyboardState2 = 2;
        else
            keyboardState2 = 0;
        
        // Player 3: Keys 7, 8, 9
        if (Input.GetKey(KeyCode.Alpha7) || Input.GetKey(KeyCode.Keypad7))
            keyboardState3 = 0;
        else if (Input.GetKey(KeyCode.Alpha8) || Input.GetKey(KeyCode.Keypad8))
            keyboardState3 = 1;
        else if (Input.GetKey(KeyCode.Alpha9) || Input.GetKey(KeyCode.Keypad9))
            keyboardState3 = 2;
        else
            keyboardState3 = 0;
    }
    
    void CalculateIntensities()
    {
        playerIntensities[0] = CalculatePlayerIntensity(0, keyboardState1, oscValue1, player1);
        playerIntensities[1] = CalculatePlayerIntensity(1, keyboardState2, oscValue2, player2);
        playerIntensities[2] = CalculatePlayerIntensity(2, keyboardState3, oscValue3, player3);
    }
    
    float CalculatePlayerIntensity(int playerIndex, int keyState, float oscValue, PlayerSpeedSettings settings)
    {
        float intensity = 0f;
        
        // Determine intensity based on input mode
        switch (inputMode)
        {
            case InputMode.Keyboard:
                intensity = KeyboardToIntensity(keyState, settings);
                break;
                
            case InputMode.OSC:
                intensity = OSCToIntensity(oscValue, settings);
                break;
                
            case InputMode.Both:
                // Use the higher of the two inputs (so either keyboard OR OSC can control)
                float keyIntensity = KeyboardToIntensity(keyState, settings);
                float oscIntensity = OSCToIntensity(oscValue, settings);
                intensity = Mathf.Max(keyIntensity, oscIntensity);
                break;
        }
        
        return intensity;
    }
    
    float KeyboardToIntensity(int keyState, PlayerSpeedSettings settings)
    {
        switch (keyState)
        {
            case 0: return 0f;                    // Stop
            case 1: return settings.slowSpeed;    // Slow
            case 2: return settings.fastSpeed;    // Fast
            default: return 0f;
        }
    }
    
    float OSCToIntensity(float oscValue, PlayerSpeedSettings settings)
    {
        if (oscValue <= oscStopThreshold)
        {
            return 0f; // Stop/retreat
        }
        else if (oscValue < oscFastThreshold)
        {
            return settings.slowSpeed; // Slow speed
        }
        else
        {
            return settings.fastSpeed; // Fast speed
        }
    }
    
    // Public method for GameManager to read intensities
    public float[] GetPlayerIntensities()
    {
        return playerIntensities;
    }
    
    // Public methods to receive OSC data
    // These will be called by extOSC receivers
    public void SetPedal1(float value)
    {
        oscValue1 = Mathf.Clamp01(value);
    }
    
    public void SetPedal2(float value)
    {
        oscValue2 = Mathf.Clamp01(value);
    }
    
    public void SetPedal3(float value)
    {
        oscValue3 = Mathf.Clamp01(value);
    }
    
    // Debug GUI
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.Box(new Rect(10, 260, 280, 120), "", style);
        
        style.fontSize = 14;
        GUI.Label(new Rect(20, 270, 260, 25), $"Input Mode: {inputMode}", style);
        GUI.Label(new Rect(20, 295, 260, 25), $"P1: {playerIntensities[0]:F2} (KB:{keyboardState1} OSC:{oscValue1:F2})", style);
        GUI.Label(new Rect(20, 320, 260, 25), $"P2: {playerIntensities[1]:F2} (KB:{keyboardState2} OSC:{oscValue2:F2})", style);
        GUI.Label(new Rect(20, 345, 260, 25), $"P3: {playerIntensities[2]:F2} (KB:{keyboardState3} OSC:{oscValue3:F2})", style);
    }
}