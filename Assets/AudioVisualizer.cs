using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioVisualizer : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("The audio source to visualize")]
    public AudioSource audioSource;
    
    [Tooltip("How many samples to use from the audio data")]
    public int sampleSize = 256;
    
    [Tooltip("Which frequency band to use (0 = bass, higher = treble)")]
    [Range(0, 7)]
    public int frequencyBand = 0;
    
    [Header("Visual Settings")]
    [Tooltip("The renderer of the plane to change color")]
    public Renderer planeRenderer;
    
    [Tooltip("Base color of the plane")]
    public Color baseColor = new Color(0.5f, 0.5f, 1.0f); // Brighter blue color
    
    [Tooltip("How sensitive the effect is to the audio")]
    [Range(1f, 2000f)]
    public float sensitivity = 500f;
    
    [Tooltip("How quickly the brightness returns to normal")]
    [Range(0.1f, 20f)]
    public float dampening = 10f;
    
    [Tooltip("Maximum brightness multiplier")]
    [Range(1f, 20f)]
    public float maxBrightness = 5f;
    
    [Tooltip("Use emission instead of just color (more dramatic effect)")]
    public bool useEmission = true;
    
    [Header("Debug Settings")]
    [Tooltip("Show detailed logs in the console")]
    public bool showDebugLogs = true;
    
    [Tooltip("Visualize audio data in the scene view")]
    public bool showVisualizer = true;
    
    // Private variables
    private float[] audioSamples;
    private float currentBrightness = 1f;
    private Material planeMaterial;
    
    void Start()
    {
        Debug.Log("AudioVisualizer: Starting initialization...");
        
        // Initialize if not set in inspector
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            Debug.Log("AudioVisualizer: Found AudioSource automatically");
        }
            
        if (planeRenderer == null)
        {
            planeRenderer = GetComponent<Renderer>();
            Debug.Log("AudioVisualizer: Found Renderer automatically");
        }
        
        // Create the samples array
        audioSamples = new float[sampleSize];
        Debug.Log($"AudioVisualizer: Created samples array with {sampleSize} elements");
        
        // Get a reference to the material
        if (planeRenderer != null)
        {
            // Create a material instance to avoid changing shared materials
            planeMaterial = new Material(planeRenderer.sharedMaterial);
            planeRenderer.material = planeMaterial;
            
            // Set initial color
            planeMaterial.color = baseColor;
            Debug.Log($"AudioVisualizer: Set initial color to {baseColor}");
            
            // Set up emission if using it
            if (useEmission)
            {
                if (planeMaterial.HasProperty("_EmissionColor"))
                {
                    planeMaterial.EnableKeyword("_EMISSION");
                    planeMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    planeMaterial.SetColor("_EmissionColor", Color.black);
                    Debug.Log("AudioVisualizer: Emission enabled on material");
                }
                else
                {
                    Debug.LogWarning("AudioVisualizer: Material doesn't support emission! Using a standard shader would work better.");
                    useEmission = false;
                }
            }
        }
        else
        {
            Debug.LogError("AudioVisualizer: No renderer found!");
        }
        
        // Verify the audio source has a clip assigned
        if (audioSource != null && audioSource.clip == null)
        {
            Debug.LogError("AudioVisualizer: AudioSource has no clip assigned!");
        }
        else if (audioSource != null)
        {
            Debug.Log($"AudioVisualizer: Audio clip: {audioSource.clip.name}, Duration: {audioSource.clip.length}s");
            Debug.Log($"AudioVisualizer: Audio is playing: {audioSource.isPlaying}");
        }
        
        // Log initial settings
        Debug.Log($"AudioVisualizer: Initialized with - Frequency Band: {frequencyBand}, Sensitivity: {sensitivity}, Max Brightness: {maxBrightness}");
    }
    
    void Update()
    {
        // Make sure we have the audio source and material
        if (audioSource == null || planeMaterial == null)
        {
            Debug.LogError("AudioVisualizer: Missing audioSource or planeMaterial!");
            return;
        }

        // Check if audio is playing
        if (!audioSource.isPlaying)
        {
            Debug.LogWarning("AudioVisualizer: Audio is not playing!");
            return;
        }
        
        // Get audio spectrum data
        audioSource.GetSpectrumData(audioSamples, 0, FFTWindow.BlackmanHarris);
        
        // Calculate frequency band range
        int sampleRange = sampleSize / 8;
        int startSample = frequencyBand * sampleRange;
        float sum = 0;
        
        // Sum up the values in the selected frequency band
        for (int i = startSample; i < startSample + sampleRange; i++)
        {
            sum += audioSamples[i];
        }
        
        // Calculate average amplitude in the band
        float average = sum / sampleRange;
        
        // average = sum; 
        // Log the audio level periodically (every 20 frames to avoid console spam)
        if (Time.frameCount % 20 == 0)
        {
            Debug.Log($"AudioVisualizer: Frequency band {frequencyBand} level: {average:F6}, Raw sum: {sum:F6}");
        }
        
        // Use amplitude to affect brightness with MUCH higher sensitivity
        // Scale up the small values dramatically using a power function
        float scaledAverage = Mathf.Pow(average * 1000, 2) * sensitivity;
        float targetBrightness = 1f + Mathf.Clamp(scaledAverage, 0f, maxBrightness - 1f);
        
        // Log brightness change (every 20 frames)
        if (Time.frameCount % 20 == 0 && showDebugLogs)
        {
            Debug.Log($"AudioVisualizer: Raw audio: {average:F6}, Scaled: {scaledAverage:F4}, Current brightness: {currentBrightness:F2}, Target: {targetBrightness:F2}");
        }
        
        // Smoothly move towards target brightness
        currentBrightness = Mathf.Lerp(currentBrightness, targetBrightness, Time.deltaTime * dampening);
        
float h, s, v;
Color.RGBToHSV(baseColor, out h, out s, out v);  // Convert base color to HSV
v = Mathf.Lerp(v, targetBrightness, Time.deltaTime * dampening);  // Adjust only the value (brightness)
Color adjustedColor = Color.HSVToRGB(h, s, v);  // Convert back to RGB

// Apply the adjusted color to the material
planeMaterial.color = adjustedColor;
        
        // If using emission, apply an emission color based on brightness
        // This makes the effect much more noticeable
        if (useEmission && planeMaterial.HasProperty("_EmissionColor"))
        {
            // Enable emission
            planeMaterial.EnableKeyword("_EMISSION");
            
            // Make emission more intense as brightness increases
            float emissionIntensity = Mathf.Pow(currentBrightness - 0.9f, 2) * 5f;
            if (emissionIntensity < 0) emissionIntensity = 0;
            
            Color emissionColor = baseColor * emissionIntensity;
            planeMaterial.SetColor("_EmissionColor", emissionColor);
            
            if (Time.frameCount % 20 == 0 && showDebugLogs)
            {
                Debug.Log($"AudioVisualizer: Emission intensity: {emissionIntensity:F2}, Emission color: {emissionColor}");
            }
        }
    }
    
    // Visual debug to see audio levels in Scene view
    void OnDrawGizmos()
    {
        if (audioSamples == null || audioSamples.Length == 0)
            return;
            
        int sampleRange = sampleSize / 8;
        int startSample = frequencyBand * sampleRange;
        
        for (int i = 0; i < sampleRange; i++)
        {
            if (startSample + i < audioSamples.Length)
            {
                float height = audioSamples[startSample + i] * 10f;
                Vector3 pos = transform.position + Vector3.right * (i * 0.1f);
                Gizmos.DrawLine(pos, pos + Vector3.up * height);
            }
        }
    }
}