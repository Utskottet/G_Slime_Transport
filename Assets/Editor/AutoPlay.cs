using UnityEditor;

[InitializeOnLoad]
public class AutoPlay
{
    static AutoPlay()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = true;
            }
        };
    }
}