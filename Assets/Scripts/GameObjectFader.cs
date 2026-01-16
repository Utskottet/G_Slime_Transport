using UnityEngine;
using System.Collections;

/// <summary>
/// Attach to any GameObject to fade it in at game start and out during win/lose.
/// Works with SpriteRenderer, CanvasGroup, or multiple child renderers.
/// </summary>
public class GameObjectFader : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The SlimeGameManager to watch for game state")]
    public SlimeGameManager gameManager;

    [Header("Fade Settings")]
    [Tooltip("Seconds to fade in when game starts")]
    public float fadeInDuration = 1f;

    [Tooltip("Delay before fade-in starts (seconds after game begins)")]
    public float fadeInDelay = 0f;

    [Tooltip("Seconds to fade out when win/lose happens")]
    public float fadeOutDuration = 2f;

    [Header("What to Fade")]
    [Tooltip("Automatically finds SpriteRenderer or CanvasGroup. Check this to fade all child renderers too.")]
    public bool fadeAllChildren = true;

    // Runtime
    private SpriteRenderer spriteRenderer;
    private CanvasGroup canvasGroup;
    private SpriteRenderer[] childRenderers;
    private SlimeGameManager.GamePhase lastPhase;
    private bool hasFadedOut = false;

    void Start()
    {
        // Auto-find game manager if not assigned
        if (gameManager == null)
            gameManager = FindObjectOfType<SlimeGameManager>();

        if (gameManager == null)
        {
            Debug.LogWarning("GameObjectFader: No SlimeGameManager found!");
            return;
        }

        // Find what we can fade
        spriteRenderer = GetComponent<SpriteRenderer>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (fadeAllChildren)
            childRenderers = GetComponentsInChildren<SpriteRenderer>();

        // Start invisible
        SetAlpha(0f);

        // Start fade-in
        lastPhase = SlimeGameManager.GamePhase.Playing;
        StartCoroutine(FadeInRoutine());
    }

    void Update()
    {
        if (gameManager == null || hasFadedOut) return;

        // Detect when game phase changes to win/lose
        if (lastPhase == SlimeGameManager.GamePhase.Playing &&
            (gameManager.phase == SlimeGameManager.GamePhase.PlayerWin ||
             gameManager.phase == SlimeGameManager.GamePhase.SlimeWin))
        {
            hasFadedOut = true;
            StartCoroutine(FadeOutRoutine());
        }

        lastPhase = gameManager.phase;
    }

    IEnumerator FadeInRoutine()
    {
        // Wait for delay
        if (fadeInDelay > 0f)
            yield return new WaitForSeconds(fadeInDelay);

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            SetAlpha(t);
            yield return null;
        }

        SetAlpha(1f);
    }

    IEnumerator FadeOutRoutine()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            SetAlpha(1f - t); // Fade from 1 to 0
            yield return null;
        }

        SetAlpha(0f);
    }

    void SetAlpha(float alpha)
    {
        // Fade SpriteRenderer
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        // Fade CanvasGroup
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }

        // Fade all child renderers
        if (fadeAllChildren && childRenderers != null)
        {
            foreach (var renderer in childRenderers)
            {
                if (renderer != null)
                {
                    Color c = renderer.color;
                    c.a = alpha;
                    renderer.color = c;
                }
            }
        }
    }

    // === PUBLIC API ===

    /// <summary>
    /// Manually trigger fade out (useful for testing or custom triggers)
    /// </summary>
    public void TriggerFadeOut()
    {
        if (!hasFadedOut)
        {
            hasFadedOut = true;
            StartCoroutine(FadeOutRoutine());
        }
    }

    /// <summary>
    /// Manually trigger fade in
    /// </summary>
    public void TriggerFadeIn()
    {
        StopAllCoroutines();
        hasFadedOut = false;
        StartCoroutine(FadeInRoutine());
    }

    /// <summary>
    /// Set alpha immediately without animation
    /// </summary>
    public void SetAlphaImmediate(float alpha)
    {
        StopAllCoroutines();
        SetAlpha(alpha);
    }
}