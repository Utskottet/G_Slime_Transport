using UnityEngine;
using extOSC;

public class OSCBridge : MonoBehaviour
{
    public PlayerInputController playerController;
    
    [Header("OSC Addresses")]
    public string pedal1Address = "/pedal1";
    public string pedal2Address = "/pedal2";
    public string pedal3Address = "/pedal3";
    
    private OSCReceiver receiver;
    
    void Start()
    {
        receiver = GetComponent<OSCReceiver>();
        
        if (receiver == null)
        {
            Debug.LogError("OSCBridge: No OSCReceiver found on this GameObject!");
            return;
        }
        
        if (playerController == null)
        {
            Debug.LogError("OSCBridge: PlayerInputController not assigned!");
            return;
        }
        
        // Bind OSC addresses to callback methods
        receiver.Bind(pedal1Address, OnPedal1Received);
        receiver.Bind(pedal2Address, OnPedal2Received);
        receiver.Bind(pedal3Address, OnPedal3Received);
        
        Debug.Log($"OSCBridge: Listening for {pedal1Address}, {pedal2Address}, {pedal3Address}");
    }
    
    private void OnPedal1Received(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            float value = message.Values[0].FloatValue;
            playerController.SetPedal1(value);
            Debug.Log($"Pedal1: {value}");
        }
    }
    
    private void OnPedal2Received(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            float value = message.Values[0].FloatValue;
            playerController.SetPedal2(value);
            Debug.Log($"Pedal2: {value}");
        }
    }
    
    private void OnPedal3Received(OSCMessage message)
    {
        if (message.Values.Count > 0)
        {
            float value = message.Values[0].FloatValue;
            playerController.SetPedal3(value);
            Debug.Log($"Pedal3: {value}");
        }
    }
}