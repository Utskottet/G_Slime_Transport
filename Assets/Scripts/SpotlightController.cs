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
    
    private Vector3 startRot1, startRot2, startRot3;
    private float offset1, offset2, offset3;
    
    void Start()
    {
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
        
        // Send spotlight data to all slime shaders
        SendSpotlightData();
    }
    
    void PanLight(Light light, Vector3 startRot, float time, float offset, float speedX, float rangeX, float speedY, float rangeY)
    {
        if (light == null) return;
        
        float xRotation = startRot.x + Mathf.Sin((time + offset) * speedX) * rangeX;
        float yRotation = startRot.y + Mathf.Sin((time + offset * 1.3f) * speedY) * rangeY;
        
        light.transform.localEulerAngles = new Vector3(xRotation, yRotation, startRot.z);
    }
    
    void SendSpotlightData()
    {
        // Send to global shader properties (all materials receive this)
        if (spotlight1 != null)
        {
            Shader.SetGlobalVector("_Spot1Pos", spotlight1.transform.position);
            Shader.SetGlobalVector("_Spot1Dir", spotlight1.transform.forward);
            Shader.SetGlobalFloat("_Spot1Angle", spotlight1.spotAngle);
            Shader.SetGlobalFloat("_Spot1Intensity", spotlight1.intensity);
            Shader.SetGlobalColor("_Spot1Color", spotlight1.color);
        }
        
        if (spotlight2 != null)
        {
            Shader.SetGlobalVector("_Spot2Pos", spotlight2.transform.position);
            Shader.SetGlobalVector("_Spot2Dir", spotlight2.transform.forward);
            Shader.SetGlobalFloat("_Spot2Angle", spotlight2.spotAngle);
            Shader.SetGlobalFloat("_Spot2Intensity", spotlight2.intensity);
            Shader.SetGlobalColor("_Spot2Color", spotlight2.color);
        }
        
        if (spotlight3 != null)
        {
            Shader.SetGlobalVector("_Spot3Pos", spotlight3.transform.position);
            Shader.SetGlobalVector("_Spot3Dir", spotlight3.transform.forward);
            Shader.SetGlobalFloat("_Spot3Angle", spotlight3.spotAngle);
            Shader.SetGlobalFloat("_Spot3Intensity", spotlight3.intensity);
            Shader.SetGlobalColor("_Spot3Color", spotlight3.color);
        }
    }
}