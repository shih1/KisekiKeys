using UnityEngine;

public class NoteObject : MonoBehaviour
{
    [Header("Visual Effects")]
    public bool pulseOnBeat = true;
    public float pulseScale = 1.2f;
    public float pulseSpeed = 5.0f;

    [Header("Trail")]
    public bool leaveTrail = true;
    public float trailDuration = 1.0f;

    private Vector3 originalScale;
    private float pulseTime = 0f;
    private TrailRenderer trail;

    void Start()
    {
        originalScale = transform.localScale;

        if (leaveTrail)
        {
            // Add a trail renderer if it doesn't exist
            trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = trailDuration;
                trail.startWidth = transform.localScale.x * 0.5f;
                trail.endWidth = 0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.startColor = GetComponent<Renderer>()?.material.color ?? Color.white;
                trail.endColor = new Color(
                    trail.startColor.r,
                    trail.startColor.g,
                    trail.startColor.b,
                    0
                );
            }
        }
    }

    void Update()
    {
        if (pulseOnBeat)
        {
            // Create a pulsing effect
            pulseTime += Time.deltaTime * pulseSpeed;
            float pulseFactor = 1.0f + Mathf.Sin(pulseTime) * (pulseScale - 1.0f) * 0.5f;
            transform.localScale = originalScale * pulseFactor;
        }

        // You could add rotation or other visual effects here
        transform.Rotate(Vector3.up * Time.deltaTime * 30.0f);
    }
}
