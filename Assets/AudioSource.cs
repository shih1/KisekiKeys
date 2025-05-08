using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public AudioSource audioSource;

    void Start()
    {
        audioSource.Play();  // Start playing the audio when the scene loads
    }
}
