using UnityEngine;

public class SpotlightController : MonoBehaviour
{
    [Header("Spotlight 1")]
    public Light spotlight1;
    public float panSpeedX1 = 0.2f;
    public float panRangeX1 = 3f;
    public float panSpeedY1 = 0.3f;
    public float panRangeY1 = 30f;
    
    [Header("Spotlight 2")]
    public Light spotlight2;
    public float panSpeedX2 = 0.15f;
    public float panRangeX2 = 3f;
    public float panSpeedY2 = 0.2f;
    public float panRangeY2 = 30f;
    
    [Header("Spotlight 3")]
    public Light spotlight3;
    public float panSpeedX3 = 0.25f;
    public float panRangeX3 = 3f;
    public float panSpeedY3 = 0.25f;
    public float panRangeY3 = 30f;
    
    // Store initial rotations
    private Vector3 startRot1, startRot2, startRot3;
    private float offset1, offset2, offset3;
    
    void Start()
    {
        // Remember where you placed them
        if (spotlight1 != null) startRot1 = spotlight1.transform.localEulerAngles;
        if (spotlight2 != null) startRot2 = spotlight2.transform.localEulerAngles;
        if (spotlight3 != null) startRot3 = spotlight3.transform.localEulerAngles;
        
        offset1 = Random.Range(0f, 10f);
        offset2 = Random.Range(0f, 10f);
        offset3 = Random.Range(0f, 10f);
    }
    
    void Update()
    {
        float time = Time.time;
        
        PanLight(spotlight1, startRot1, time, offset1, panSpeedX1, panRangeX1, panSpeedY1, panRangeY1);
        PanLight(spotlight2, startRot2, time, offset2, panSpeedX2, panRangeX2, panSpeedY2, panRangeY2);
        PanLight(spotlight3, startRot3, time, offset3, panSpeedX3, panRangeX3, panSpeedY3, panRangeY3);
    }
    
    void PanLight(Light light, Vector3 startRot, float time, float offset, float speedX, float rangeX, float speedY, float rangeY)
    {
        if (light == null) return;
        
        // Add oscillation to starting rotation
        float xRotation = startRot.x + Mathf.Sin((time + offset) * speedX) * rangeX;
        float yRotation = startRot.y + Mathf.Sin((time + offset * 1.3f) * speedY) * rangeY;
        
        light.transform.localEulerAngles = new Vector3(xRotation, yRotation, startRot.z);
    }
}