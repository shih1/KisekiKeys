using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MidiVisualizerController : MonoBehaviour
{
    [Header("References")]
    public MidiNoteSpawner noteSpawner;
    public MidiFileParser midiParser;
    
    [Header("MIDI Files")]
    public TextAsset[] midiFiles;
    public int currentMidiFileIndex = 0;
    
    [Header("Camera Settings")]
    public Camera mainCamera;
    public float cameraMovementSpeed = 5.0f;
    public float cameraTurnSpeed = 2.0f;
    
    [Header("UI References")]
    public Text currentSongText;
    public Slider timelineSlider;
    public Button playButton;
    public Button stopButton;
    public Button nextSongButton;
    public Button prevSongButton;
    
    private bool isPlaying = false;
    private float totalDuration = 0f;
    
    void Start()
    {
        if (noteSpawner == null)
        {
            noteSpawner = FindObjectOfType<MidiNoteSpawner>();
        }
        
        if (midiParser == null)
        {
            midiParser = FindObjectOfType<MidiFileParser>();
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        // Set up UI listeners
        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayCurrentSong);
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(StopPlayback);
        }
        
        if (nextSongButton != null)
        {
            nextSongButton.onClick.AddListener(NextSong);
        }
        
        if (prevSongButton != null)
        {
            prevSongButton.onClick.AddListener(PreviousSong);
        }
        
        // Load initial MIDI file
        if (midiFiles != null && midiFiles.Length > 0)
        {
            LoadMidiFile(currentMidiFileIndex);
        }
    }
    
    void Update()
    {
        // Handle keyboard input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayback();
        }
        
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.N))
        {
            NextSong();
        }
        
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.P))
        {
            PreviousSong();
        }
        
        // Camera movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        if (mainCamera != null)
        {
            // Move camera position
            Vector3 movement = new Vector3(horizontal, 0, vertical) * cameraMovementSpeed * Time.deltaTime;
            mainCamera.transform.position += mainCamera.transform.TransformDirection(movement);
            
            // Camera look around with mouse
            if (Input.GetMouseButton(1)) // Right mouse button held
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                
                mainCamera.transform.RotateAround(mainCamera.transform.position, Vector3.up, mouseX * cameraTurnSpeed);
                mainCamera.transform.RotateAround(mainCamera.transform.position, mainCamera.transform.right, -mouseY * cameraTurnSpeed);
            }
            
            // Zoom with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            mainCamera.transform.position += mainCamera.transform.forward * scroll * 10f;
        }
        
        // Update UI
        UpdateUI();
    }
    
    private void LoadMidiFile(int index)
    {
        if (midiFiles == null || midiFiles.Length == 0)
        {
            Debug.LogWarning("No MIDI files available to load");
            return;
        }
        
        if (index < 0 || index >= midiFiles.Length)
        {
            Debug.LogWarning("MIDI file index out of range");
            return;
        }
        
        TextAsset midiFile = midiFiles[index];
        currentMidiFileIndex = index;
        
        if (midiFile != null)
        {
            // For a complete implementation, you would:
            // 1. Parse the MIDI file using the midiParser
            // 2. Pass the parsed data to the noteSpawner
            // 3. Set up visualization parameters
            
            // For this demonstration, we'll just set the file directly
            noteSpawner.midiFile = midiFile;
            
            // Update UI to show current song
            if (currentSongText != null)
            {
                currentSongText.text = midiFile.name;
            }
            
            Debug.Log($"Loaded MIDI file: {midiFile.name}");
        }
    }
    
    public void PlayCurrentSong()
    {
        if (noteSpawner != null)
        {
            noteSpawner.StartPlayback();
            isPlaying = true;
        }
    }
    
    public void StopPlayback()
    {
        if (noteSpawner != null)
        {
            noteSpawner.StopPlayback();
            isPlaying = false;
        }
    }
    
    public void TogglePlayback()
    {
        if (isPlaying)
        {
            StopPlayback();
        }
        else
        {
            PlayCurrentSong();
        }
    }
    
    public void NextSong()
    {
        StopPlayback();
        currentMidiFileIndex = (currentMidiFileIndex + 1) % midiFiles.Length;
        LoadMidiFile(currentMidiFileIndex);
    }
    
    public void PreviousSong()
    {
        StopPlayback();
        currentMidiFileIndex = (currentMidiFileIndex - 1 + midiFiles.Length) % midiFiles.Length;
        LoadMidiFile(currentMidiFileIndex);
    }
    
    private void UpdateUI()
    {
        // Update timeline slider if we have one
        if (timelineSlider != null && noteSpawner != null)
        {
            // Implementation would depend on how you track playback progress
            // This is just a placeholder
            // timelineSlider.value = noteSpawner.currentPlayTime / totalDuration;
        }
    }
}