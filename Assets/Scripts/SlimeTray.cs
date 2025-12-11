using UnityEngine;

public class SlimeTray : MonoBehaviour
{
    [Header("Movement Range (World Space)")]
    public float leftLimit = -10f;
    public float rightLimit = 10f;

    [Header("Movement Settings")]
    public float speed = 0.3f;

    [HideInInspector] public SlimeAgent owner;

    float t = 0f;
    bool frozen = false;

    void Start()
    {
        // Starta på en slumpad position inom intervallet
        float startX = Mathf.Clamp(
            transform.position.x,
            leftLimit,
            rightLimit
        );

        transform.position = new Vector3(startX, transform.position.y, transform.position.z);

        // Slumpa fas så brickorna inte synkar
        t = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!Application.isPlaying) return;
        if (frozen) return;

        t += Time.deltaTime * speed;

        float halfRange = (rightLimit - leftLimit) * 0.5f;
        float center = (rightLimit + leftLimit) * 0.5f;

        float x = center + Mathf.Sin(t) * halfRange;
        x = Mathf.Clamp(x, leftLimit, rightLimit);

        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    public void SetFrozen(bool value)
    {
        frozen = value;
    }
}
