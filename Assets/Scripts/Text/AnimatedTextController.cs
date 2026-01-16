using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Animated text controller with support for multiple text groups.
/// Attach to a container (like GameStartFX) that gets enabled/disabled by SlimeGameManager.
/// Each group can have multiple TMP objects that animate together with shared settings.
/// </summary>
public class AnimatedTextController : MonoBehaviour
{
    public enum TransitionEffect
    {
        None,
        Fade,
        Dissolve
    }

    [System.Serializable]
    public class TextGroup
    {
        [Header("Text Objects")]
        [Tooltip("Drag multiple TMP objects here - they all animate together")]
        public List<TMP_Text> textObjects = new List<TMP_Text>();

        [Header("Timing")]
        [Tooltip("Seconds after container enables before this group starts")]
        public float delay = 0f;
        
        [Tooltip("How long text stays visible (0 = forever until container disables)")]
        public float duration = 0f;

        [Header("Transition Effect")]
        [Tooltip("Controls fade in/out animation")]
        public TransitionEffect transition = TransitionEffect.None;

        [Header("Fade/Dissolve Settings")]
        [Tooltip("Duration of fade/dissolve IN (0 = instant appear)")]
        public float effectInDuration = 0.5f;
        
        [Tooltip("Duration of fade/dissolve OUT (0 = instant disappear)")]
        public float effectOutDuration = 0.5f;

        [Header("Blink Settings")]
        [Tooltip("Enable blinking during visible period")]
        public bool enableBlink = false;
        
        [Tooltip("Use smooth sine wave instead of hard on/off")]
        public bool smoothBlink = false;
        
        [Tooltip("Blinks per second")]
        public float blinkSpeed = 3f;
        
        [Tooltip("Minimum alpha during blink")]
        [Range(0f, 1f)] public float blinkMinAlpha = 0f;
        
        [Header("Text Change (Optional)")]
        [Tooltip("If set, text changes to this after changeDelay seconds")]
        public string changeTextTo = "";
        [Tooltip("Seconds after group starts before text changes")]
        public float changeDelay = 0f;

        // Runtime state
        [HideInInspector] public bool isActive = false;
        [HideInInspector] public float fadeMultiplier = 1f; // Current fade multiplier (0-1)
        [HideInInspector] public List<Material> originalMaterials = new List<Material>();
        [HideInInspector] public List<Color> originalColors = new List<Color>();
    }

    [Header("Text Groups")]
    public List<TextGroup> textGroups = new List<TextGroup>();

    [Header("Dissolve Material")]
    [Tooltip("Material with _Dissolve property (uses your TextDissolveShader)")]
    public Material dissolveMaterialTemplate;

    // Runtime
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    void OnEnable()
    {
        // Cache original states and start all groups
        foreach (var group in textGroups)
        {
            CacheOriginalState(group);
            
            // Initially hide all text objects
            foreach (var tmp in group.textObjects)
            {
                if (tmp != null)
                    SetAlpha(tmp, 0f);
            }
        }

        // Start the sequence
        foreach (var group in textGroups)
        {
            Coroutine c = StartCoroutine(RunTextGroup(group));
            activeCoroutines.Add(c);
        }
    }

    void OnDisable()
    {
        // Stop all coroutines
        foreach (var c in activeCoroutines)
        {
            if (c != null)
                StopCoroutine(c);
        }
        activeCoroutines.Clear();

        // Reset all groups to original state
        foreach (var group in textGroups)
        {
            RestoreOriginalState(group);
            group.isActive = false;
        }
    }

    void CacheOriginalState(TextGroup group)
    {
        group.originalMaterials.Clear();
        group.originalColors.Clear();

        foreach (var tmp in group.textObjects)
        {
            if (tmp != null)
            {
                group.originalMaterials.Add(tmp.fontMaterial);
                group.originalColors.Add(tmp.color);
            }
        }
    }

    void RestoreOriginalState(TextGroup group)
    {
        for (int i = 0; i < group.textObjects.Count; i++)
        {
            var tmp = group.textObjects[i];
            if (tmp != null)
            {
                if (i < group.originalMaterials.Count && group.originalMaterials[i] != null)
                    tmp.fontMaterial = group.originalMaterials[i];
                
                if (i < group.originalColors.Count)
                    tmp.color = group.originalColors[i];
            }
        }
    }

    IEnumerator RunTextGroup(TextGroup group)
    {
        // Wait for initial delay
        if (group.delay > 0f)
            yield return new WaitForSeconds(group.delay);

        group.isActive = true;
        
        // Initialize fade multiplier based on whether we're fading in
        if (group.transition == TransitionEffect.Fade && group.effectInDuration > 0f)
        {
            group.fadeMultiplier = 0f; // Start invisible for fade-in
        }
        else
        {
            group.fadeMultiplier = 1f; // Start visible
        }

        // === START BLINK IMMEDIATELY (runs entire time if enabled) ===
        Coroutine blinkCoroutine = null;
        if (group.enableBlink)
        {
            blinkCoroutine = StartCoroutine(BlinkEffect(group));
        }
        else
        {
            // No blink - set base alpha to fade multiplier
            foreach (var tmp in group.textObjects)
            {
                if (tmp != null)
                    SetAlpha(tmp, group.fadeMultiplier);
            }
        }

        // === TRANSITION IN (multiplies on top of blink) ===
        Coroutine fadeInCoroutine = null;
        if (group.transition == TransitionEffect.Fade && group.effectInDuration > 0f)
        {
            fadeInCoroutine = StartCoroutine(FadeMultiplierEffect(group, 0f, 1f, group.effectInDuration));
        }
        else if (group.transition == TransitionEffect.Dissolve && group.effectInDuration > 0f)
        {
            yield return DissolveEffect(group, 1f, 0f, group.effectInDuration);
        }

        // Wait for transition in to complete
        if (fadeInCoroutine != null)
            yield return fadeInCoroutine;

        // === TEXT CHANGE (if configured) ===
        Coroutine textChangeCoroutine = null;
        if (!string.IsNullOrEmpty(group.changeTextTo))
        {
            textChangeCoroutine = StartCoroutine(HandleTextChange(group));
        }

        // === WAIT FOR DURATION ===
        if (group.duration > 0f)
        {
            yield return new WaitForSeconds(group.duration);

            // Stop text change
            if (textChangeCoroutine != null)
                StopCoroutine(textChangeCoroutine);

            // === TRANSITION OUT (multiplies on top of blink) ===
            if (group.transition == TransitionEffect.Fade && group.effectOutDuration > 0f)
            {
                yield return FadeMultiplierEffect(group, 1f, 0f, group.effectOutDuration);
            }
            else if (group.transition == TransitionEffect.Dissolve && group.effectOutDuration > 0f)
            {
                yield return DissolveEffect(group, 0f, 1f, group.effectOutDuration);
            }
            else
            {
                // Instant hide
                group.fadeMultiplier = 0f;
                foreach (var tmp in group.textObjects)
                {
                    if (tmp != null)
                        SetAlpha(tmp, 0f);
                }
            }

            // Stop blink
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            group.isActive = false;
        }
        // If duration is 0, text stays visible until container disables
    }

    IEnumerator HandleTextChange(TextGroup group)
    {
        // Wait for change delay
        yield return new WaitForSeconds(group.changeDelay);

        if (!group.isActive) yield break;

        // Quick fade out
        if (group.transition == TransitionEffect.Fade)
        {
            yield return FadeEffect(group, 1f, 0f, 0.2f);
        }

        // Change the text
        foreach (var tmp in group.textObjects)
        {
            if (tmp != null)
                tmp.text = group.changeTextTo;
        }

        // Quick fade back in
        if (group.transition == TransitionEffect.Fade)
        {
            yield return FadeEffect(group, 0f, 1f, 0.2f);
        }
    }

    IEnumerator BlinkEffect(TextGroup group)
    {
        float elapsed = 0f;

        while (group.isActive)
        {
            elapsed += Time.deltaTime;

            float blinkAlpha;
            if (group.smoothBlink)
            {
                // Smooth sine wave blink
                float wave = (Mathf.Sin(elapsed * group.blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                blinkAlpha = Mathf.Lerp(group.blinkMinAlpha, 1f, wave);
            }
            else
            {
                // Hard on/off blink
                bool on = Mathf.FloorToInt(elapsed * group.blinkSpeed * 2f) % 2 == 0;
                blinkAlpha = on ? 1f : group.blinkMinAlpha;
            }

            // Apply fade multiplier on top of blink
            float finalAlpha = blinkAlpha * group.fadeMultiplier;

            foreach (var tmp in group.textObjects)
            {
                if (tmp != null)
                    SetAlpha(tmp, finalAlpha);
            }

            yield return null;
        }
    }

    IEnumerator DissolveEffect(TextGroup group, float from, float to, float duration)
    {
        if (dissolveMaterialTemplate == null)
        {
            Debug.LogWarning("AnimatedTextController: Dissolve requires dissolveMaterialTemplate to be assigned!");
            // Fallback to fade
            yield return FadeEffect(group, from == 1f ? 0f : 1f, to == 1f ? 0f : 1f, duration);
            yield break;
        }

        // Create material instances for each text object
        List<Material> dissolveMats = new List<Material>();
        foreach (var tmp in group.textObjects)
        {
            if (tmp != null)
            {
                Material mat = new Material(dissolveMaterialTemplate);
                // Copy the font atlas from original material
                if (tmp.fontMaterial != null && tmp.fontMaterial.HasProperty("_MainTex"))
                {
                    mat.SetTexture("_MainTex", tmp.fontMaterial.GetTexture("_MainTex"));
                }
                mat.SetFloat("_Dissolve", from);
                tmp.fontMaterial = mat;
                dissolveMats.Add(mat);
            }
        }

        // Animate dissolve
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float dissolveValue = Mathf.Lerp(from, to, t);

            foreach (var mat in dissolveMats)
            {
                if (mat != null)
                    mat.SetFloat("_Dissolve", dissolveValue);
            }

            yield return null;
        }

        // Ensure final value
        foreach (var mat in dissolveMats)
        {
            if (mat != null)
                mat.SetFloat("_Dissolve", to);
        }
    }

    IEnumerator FadeMultiplierEffect(TextGroup group, float fromMultiplier, float toMultiplier, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            group.fadeMultiplier = Mathf.Lerp(fromMultiplier, toMultiplier, t);

            // If no blink is active, we need to manually apply the fade
            if (!group.enableBlink)
            {
                foreach (var tmp in group.textObjects)
                {
                    if (tmp != null)
                        SetAlpha(tmp, group.fadeMultiplier);
                }
            }
            // If blink IS active, it will read fadeMultiplier automatically

            yield return null;
        }

        // Ensure final multiplier
        group.fadeMultiplier = toMultiplier;
        
        if (!group.enableBlink)
        {
            foreach (var tmp in group.textObjects)
            {
                if (tmp != null)
                    SetAlpha(tmp, toMultiplier);
            }
        }
    }

    IEnumerator FadeEffect(TextGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;

        // Set initial alpha
        foreach (var tmp in group.textObjects)
        {
            if (tmp != null)
                SetAlpha(tmp, from);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);

            foreach (var tmp in group.textObjects)
            {
                if (tmp != null)
                    SetAlpha(tmp, alpha);
            }

            yield return null;
        }

        // Ensure final value
        foreach (var tmp in group.textObjects)
        {
            if (tmp != null)
                SetAlpha(tmp, to);
        }
    }

    void SetAlpha(TMP_Text tmp, float alpha)
    {
        Color c = tmp.color;
        c.a = alpha;
        tmp.color = c;
    }

    // === PUBLIC API ===

    /// <summary>
    /// Manually trigger a specific group by index
    /// </summary>
    public void TriggerGroup(int groupIndex)
    {
        if (groupIndex >= 0 && groupIndex < textGroups.Count)
        {
            StartCoroutine(RunTextGroup(textGroups[groupIndex]));
        }
    }

    /// <summary>
    /// Change text on all objects in a group
    /// </summary>
    public void SetGroupText(int groupIndex, string newText)
    {
        if (groupIndex >= 0 && groupIndex < textGroups.Count)
        {
            foreach (var tmp in textGroups[groupIndex].textObjects)
            {
                if (tmp != null)
                    tmp.text = newText;
            }
        }
    }

    /// <summary>
    /// Hide a specific group immediately
    /// </summary>
    public void HideGroup(int groupIndex)
    {
        if (groupIndex >= 0 && groupIndex < textGroups.Count)
        {
            var group = textGroups[groupIndex];
            group.isActive = false;
            
            foreach (var tmp in group.textObjects)
            {
                if (tmp != null)
                    SetAlpha(tmp, 0f);
            }
        }
    }
}