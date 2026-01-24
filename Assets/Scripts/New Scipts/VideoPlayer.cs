using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoPlayer1 : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public RawImage displayImage;
    public VideoClip videoClip;
    
    private RenderTexture renderTexture;

    void Start()
    {
        renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        renderTexture.Create();
        
        videoPlayer.clip = videoClip;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.isLooping = false;
        
        displayImage.texture = renderTexture;
        displayImage.enabled = true;
        videoPlayer.Play();
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
        displayImage.enabled = false;
    }

    void OnDestroy()
    {
        if (renderTexture != null)
            renderTexture.Release();
    }
}