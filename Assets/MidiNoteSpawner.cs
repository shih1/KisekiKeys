using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MidiNoteSpawner : MonoBehaviour
{
    [Header("MIDI Configuration")]
    public TextAsset midiFile; // Assign your MIDI file in the inspector
    public bool playOnStart = true;
    public float playbackSpeed = 1.0f;

    [Header("Visual Configuration")]
    public GameObject notePrefab;
    public float noteScale = 1.0f;
    public float xSpacing = 0.5f;
    public float ySpacing = 0.2f;
    public Gradient noteColorGradient;
    public float noteDuration = 30.0f;

    [Header("Physics Configuration")]
    public float initialForce = 2.0f;     // Initial upward force
    public float gravityScale = 1.0f;     // Multiplier for gravity strength
    public bool addRandomSpin = true;     // Add random rotation to notes
    public float spinForce = 2.0f;        // Maximum spin force magnitude
    public float horizontalForce = 1.0f;  // Random horizontal force

    [Header("Space Configuration")]
    public Vector3 spawnOrigin = Vector3.zero;
    public float spawnRadius = 10.0f;
    public bool useSpiral = false;
    public float spiralFactor = 0.1f;

    // MIDI data references
    private MidiFileParser.MidiData midiData;
    private float currentPlayTime = 0.0f;
    private bool isPlaying = false;

    // Reference to the parser
    private MidiFileParser midiParser;

    // Simple spawned note object to track in scene
    private class SpawnedNote
    {
        public GameObject gameObject;
        public float spawnTime;
        public float lifetime;
        public MidiFileParser.MidiNote midiNote;
        public Rigidbody rigidbody;
    }

    private List<SpawnedNote> activeNotes = new List<SpawnedNote>();

    void Start()
    {
        if (notePrefab == null)
        {
            Debug.LogError("Note prefab is not assigned!");
            return;
        }

        // Get reference to the parser
        midiParser = GetComponent<MidiFileParser>();
        if (midiParser == null)
        {
            midiParser = gameObject.AddComponent<MidiFileParser>();
        }

        LoadMidiFile();

        if (playOnStart)
        {
            StartPlayback();
        }
    }

    void Update()
    {
        if (!isPlaying || midiData == null)
            return;

        currentPlayTime += Time.deltaTime * playbackSpeed;

        // Process MIDI notes from all tracks
        foreach (MidiFileParser.MidiTrack track in midiData.tracks)
        {
            foreach (MidiFileParser.MidiNote note in track.notes)
            {
                // Check if note should start now
                if (
                    note.startTime <= currentPlayTime
                    && note.startTime > currentPlayTime - Time.deltaTime * playbackSpeed
                )
                {
                    SpawnNote(note);
                }
            }
        }

        // Loop back to beginning if we've reached the end
        if (currentPlayTime >= midiData.totalDuration)
        {
            if (midiData.totalDuration > 0)
            {
                currentPlayTime = 0f;
                Debug.Log("MIDI playback looping");
            }
        }

        // Update active notes - check lifetimes and remove expired notes
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            SpawnedNote spawnedNote = activeNotes[i];

            // Check if note should be removed
            if (spawnedNote.gameObject != null)
            {
                if (Time.time - spawnedNote.spawnTime >= spawnedNote.lifetime)
                {
                    Destroy(spawnedNote.gameObject);
                    activeNotes.RemoveAt(i);
                }
                else if (spawnedNote.gameObject.transform.position.y < -20f)
                {
                    // Remove notes that have fallen too far
                    Destroy(spawnedNote.gameObject);
                    activeNotes.RemoveAt(i);
                }
            }
            else
            {
                // In case the game object was destroyed by something else
                activeNotes.RemoveAt(i);
            }
        }
    }

    public void LoadMidiFile()
    {
        if (midiFile == null)
        {
            Debug.LogWarning("No MIDI file assigned. Using sample data.");
            return;
        }

        if (midiParser == null)
        {
            Debug.LogError("No MidiFileParser component found!");
            return;
        }

        midiData = midiParser.ParseMidiFile(midiFile);

        if (midiData == null)
        {
            Debug.LogError("Failed to parse MIDI file.");
            return;
        }

        Debug.Log(
            $"Loaded MIDI file with {midiData.tracks.Count} tracks and total duration of {midiData.totalDuration} seconds"
        );
    }

    public void StartPlayback()
    {
        if (midiData == null && midiFile != null)
        {
            LoadMidiFile();
        }

        currentPlayTime = 0.0f;
        isPlaying = true;
        Debug.Log("Starting MIDI playback");

        // Clear any existing notes
        foreach (SpawnedNote note in activeNotes)
        {
            if (note.gameObject != null)
            {
                Destroy(note.gameObject);
            }
        }
        activeNotes.Clear();
    }

    public void StopPlayback()
    {
        isPlaying = false;
        Debug.Log("Stopping MIDI playback");
    }

    private void SpawnNote(MidiFileParser.MidiNote note)
    {
        // Calculate position based on note properties
        Vector3 position;

        if (useSpiral)
        {
            // Spiral placement
            float angle = note.note * Mathf.Deg2Rad * spiralFactor;
            float radius = spawnRadius * (note.note / 127.0f);
            position = new Vector3(
                spawnOrigin.x + Mathf.Cos(angle) * radius,
                spawnOrigin.y + (note.note * ySpacing),
                spawnOrigin.z + Mathf.Sin(angle) * radius
            );
        }
        else
        {
            // Grid-based placement
            position = new Vector3(
                spawnOrigin.x + (note.note - 60) * xSpacing, // Center around middle C (60)
                spawnOrigin.y + (note.velocity / 127.0f) * ySpacing * 5.0f,
                spawnOrigin.z
            );
        }

        // Instantiate note object
        GameObject noteObject = Instantiate(notePrefab, position, Quaternion.identity);
        noteObject.name = $"Note_{note.note}_{note.channel}";

        // Scale based on velocity or duration
        float scale = noteScale * (0.5f + note.velocity / 127.0f);
        noteObject.transform.localScale = new Vector3(scale, scale, scale);

        // Color based on note pitch
        float colorPosition = (note.note % 12) / 12.0f; // Map to octave position (0-1)
        Renderer renderer = noteObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = noteColorGradient.Evaluate(colorPosition);
        }

        // Add physics components if they don't exist
        Rigidbody rb = noteObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = noteObject.AddComponent<Rigidbody>();
        }
        
        // Configure physics properties
        rb.useGravity = true;
        rb.mass = 0.5f + (note.velocity / 127.0f) * 2.0f; // Mass based on velocity
        
        // Adjust gravity if needed
        if (gravityScale != 1.0f)
        {
            // Unity doesn't have a built-in gravity scale like some other engines
            // So we'll modify the physics behavior in ForceMode.Acceleration
            rb.AddForce(Physics.gravity * (gravityScale - 1.0f), ForceMode.Acceleration);
        }
        
        // Add initial vertical force (affected by note velocity)
        float upForce = -1*initialForce * (0.5f + note.velocity / 127.0f);
        rb.AddForce(Vector3.up * upForce, ForceMode.Impulse);
        
        // Add random horizontal force for more interesting movement
        Vector3 randomDir = new Vector3(
            Random.Range(-1f, 1f) * horizontalForce,
            0,
            Random.Range(-1f, 1f) * horizontalForce
        );
        rb.AddForce(randomDir, ForceMode.Impulse);
        
        // Add random spin if enabled
        if (addRandomSpin)
        {
            rb.AddTorque(
                Random.Range(-spinForce, spinForce),
                Random.Range(-spinForce, spinForce),
                Random.Range(-spinForce, spinForce),
                ForceMode.Impulse
            );
        }
        
        // Make sure we have a collider (if the prefab doesn't already have one)
        if (noteObject.GetComponent<Collider>() == null)
        {
            noteObject.AddComponent<SphereCollider>();
        }

        // Add to active notes
        SpawnedNote spawnedNote = new SpawnedNote
        {
            gameObject = noteObject,
            spawnTime = Time.time,
            lifetime = noteDuration > 0 ? noteDuration : note.duration,
            midiNote = note,
            rigidbody = rb
        };

        activeNotes.Add(spawnedNote);
    }

    public float GetPlaybackProgress()
    {
        if (midiData != null && midiData.totalDuration > 0)
        {
            return currentPlayTime / midiData.totalDuration;
        }
        return 0f;
    }

    public void SetPlaybackPosition(float normalizedPosition)
    {
        if (midiData != null)
        {
            currentPlayTime = Mathf.Clamp01(normalizedPosition) * midiData.totalDuration;
        }
    }
}