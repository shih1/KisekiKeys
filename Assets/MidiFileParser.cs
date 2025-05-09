using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MidiFileParser : MonoBehaviour
{
    [System.Serializable]
    public class MidiNote
    {
        public int note; // MIDI note number (0-127)
        public int velocity; // Note velocity (0-127)
        public float startTime; // Start time in seconds
        public float duration; // Duration in seconds
        public int channel; // MIDI channel
    }

    [System.Serializable]
    public class MidiTrack
    {
        public string name;
        public List<MidiNote> notes = new List<MidiNote>();
    }

    [System.Serializable]
    public class MidiData
    {
        public int division; // Ticks per quarter note
        public List<MidiTrack> tracks = new List<MidiTrack>();
        public float totalDuration; // Total duration in seconds
    }

    // Parse MIDI file from TextAsset
    public MidiData ParseMidiFile(TextAsset midiAsset)
    {
        if (midiAsset == null)
        {
            Debug.LogError("No MIDI file assigned");
            return null;
        }

        return ParseMidiFile(midiAsset.bytes);
    }

    // Main MIDI parsing function
    public MidiData ParseMidiFile(byte[] midiBytes)
    {
        MidiData midiData = new MidiData();
        midiData.tracks = new List<MidiTrack>();

        try
        {
            using (MemoryStream stream = new MemoryStream(midiBytes))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read MIDI header
                string headerChunk = new string(reader.ReadChars(4));
                if (headerChunk != "MThd")
                {
                    Debug.LogError("Not a valid MIDI file - missing MThd header");
                    return null;
                }

                uint headerLength = SwapUInt32(reader.ReadUInt32());
                ushort format = SwapUInt16(reader.ReadUInt16());
                ushort numTracks = SwapUInt16(reader.ReadUInt16());
                ushort division = SwapUInt16(reader.ReadUInt16());

                midiData.division = division;

                Debug.Log($"MIDI Format: {format}, Tracks: {numTracks}, Division: {division}");

                // Read each track
                for (int t = 0; t < numTracks; t++)
                {
                    MidiTrack track = ReadTrack(reader, midiData);
                    if (track != null)
                    {
                        midiData.tracks.Add(track);
                    }
                }

                // Calculate total duration based on the longest track
                midiData.totalDuration = 0;
                foreach (MidiTrack track in midiData.tracks)
                {
                    foreach (MidiNote note in track.notes)
                    {
                        float noteEndTime = note.startTime + note.duration;
                        if (noteEndTime > midiData.totalDuration)
                        {
                            midiData.totalDuration = noteEndTime;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing MIDI file: {e.Message}\n{e.StackTrace}");
            return null;
        }

        Debug.Log(
            $"Successfully parsed MIDI file with {midiData.tracks.Count} tracks and {GetTotalNoteCount(midiData)} notes"
        );
        return midiData;
    }

    // Count total notes across all tracks
    private int GetTotalNoteCount(MidiData midiData)
    {
        int count = 0;
        foreach (MidiTrack track in midiData.tracks)
        {
            count += track.notes.Count;
        }
        return count;
    }

    // Read a single MIDI track
    private MidiTrack ReadTrack(BinaryReader reader, MidiData midiData)
    {
        try
        {
            string trackChunk = new string(reader.ReadChars(4));
            if (trackChunk != "MTrk")
            {
                Debug.LogWarning("Invalid track header: " + trackChunk);
                return null;
            }

            uint trackLength = SwapUInt32(reader.ReadUInt32());
            long trackEnd = reader.BaseStream.Position + trackLength;

            MidiTrack track = new MidiTrack();
            track.name = "Track"; // Default name

            // For tracking note on/off events
            Dictionary<int, Dictionary<int, long>> activeNotes =
                new Dictionary<int, Dictionary<int, long>>();
            for (int channel = 0; channel < 16; channel++)
            {
                activeNotes[channel] = new Dictionary<int, long>();
            }

            // Variables for timing
            long absoluteTicks = 0;
            int tempo = 500000; // Default tempo: 500,000 microseconds per quarter note (120 BPM)
            tempo = 352941; // adjusted tempo to 170 BPM
            // NOTE(yoshih May'25) Some MIDI files may lack tempo data. For now, the tempo is hardcoded to 120 BPM  due to missing tempo in the test data.

            // MIDI files from Ableton and Musescore should include tempo
            // A more flexible solution could allow manual BPM input via the Unity GUI, but we’re keeping it simple for now to avoid unnecessary complexity.

            // Read track events
            while (reader.BaseStream.Position < trackEnd)
            {
                // Read delta time
                long deltaTime = ReadVariableLengthValue(reader);
                absoluteTicks += deltaTime;

                // Read event
                byte statusByte = reader.ReadByte();

                // Meta event
                if (statusByte == 0xFF)
                {
                    byte metaType = reader.ReadByte();
                    int metaLength = (int)ReadVariableLengthValue(reader);

                    switch (metaType)
                    {
                        case 0x03: // Track name
                            track.name = new string(reader.ReadChars(metaLength));
                            break;

                        case 0x51: // Tempo // (NOTE (yoshih) this is untested)
                            if (metaLength == 3)
                            {
                                tempo =
                                    (reader.ReadByte() << 16)
                                    | (reader.ReadByte() << 8)
                                    | reader.ReadByte();
                            }
                            else
                            {
                                reader.BaseStream.Seek(metaLength, SeekOrigin.Current);
                            }
                            break;

                        default:
                            // Skip other meta events
                            reader.BaseStream.Seek(metaLength, SeekOrigin.Current);
                            break;
                    }
                }
                // MIDI event
                else
                {
                    byte statusCode = (byte)(statusByte & 0xF0);
                    byte channel = (byte)(statusByte & 0x0F);

                    switch (statusCode)
                    {
                        case 0x90: // Note On
                            {
                                byte note = reader.ReadByte();
                                byte velocity = reader.ReadByte();

                                if (velocity > 0)
                                {
                                    // Store note on event
                                    activeNotes[channel][note] = absoluteTicks;
                                }
                                else
                                {
                                    // Note on with velocity 0 is actually a note off
                                    HandleNoteOff(
                                        track,
                                        activeNotes,
                                        channel,
                                        note,
                                        absoluteTicks,
                                        tempo,
                                        midiData.division
                                    );
                                }
                            }
                            break;

                        case 0x80: // Note Off
                            {
                                byte note = reader.ReadByte();
                                byte velocity = reader.ReadByte(); // Ignore velocity for note off

                                HandleNoteOff(
                                    track,
                                    activeNotes,
                                    channel,
                                    note,
                                    absoluteTicks,
                                    tempo,
                                    midiData.division
                                );
                            }
                            break;

                        case 0xA0: // Polyphonic Key Pressure (Aftertouch)
                        case 0xB0: // Control Change
                        case 0xE0: // Pitch Wheel Change
                            // Skip 2 bytes
                            reader.BaseStream.Seek(2, SeekOrigin.Current);
                            break;

                        case 0xC0: // Program Change
                        case 0xD0: // Channel Pressure (Aftertouch)
                            // Skip 1 byte
                            reader.BaseStream.Seek(1, SeekOrigin.Current);
                            break;

                        default:
                            // Unknown status byte
                            Debug.LogWarning($"Unknown status byte: {statusByte:X2}");
                            break;
                    }
                }
            }

            // Handle any remaining active notes as if they were turned off at the end of the track
            for (int channel = 0; channel < 16; channel++)
            {
                foreach (var noteEntry in activeNotes[channel])
                {
                    int note = noteEntry.Key;
                    long onTime = noteEntry.Value;

                    HandleNoteOff(
                        track,
                        activeNotes,
                        channel,
                        note,
                        trackEnd,
                        tempo,
                        midiData.division
                    );
                }
            }

            return track;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading track: {e.Message}");
            return null;
        }
    }

    // Process a note off event
    private void HandleNoteOff(
        MidiTrack track,
        Dictionary<int, Dictionary<int, long>> activeNotes,
        int channel,
        int note,
        long offTicks,
        int tempo,
        int division
    )
    {
        if (activeNotes[channel].ContainsKey(note))
        {
            long onTicks = activeNotes[channel][note];
            float startTime = TicksToSeconds(onTicks, tempo, division);
            float endTime = TicksToSeconds(offTicks, tempo, division);

            MidiNote midiNote = new MidiNote
            {
                note = note,
                velocity = 127, // Default to max velocity for now
                startTime = startTime,
                duration = endTime - startTime,
                channel = channel,
            };

            track.notes.Add(midiNote);
            activeNotes[channel].Remove(note);
        }
    }

    // Convert ticks to seconds based on tempo and division
    private float TicksToSeconds(long ticks, int tempo, int division)
    {
        // tempo is in microseconds per quarter note
        // division is in ticks per quarter note
        return (float)(ticks * tempo) / (1000000.0f * division); // Standard MIDI uses 480 ticks per quarter note
    }

    // Read variable-length value from MIDI file
    private long ReadVariableLengthValue(BinaryReader reader)
    {
        long value = 0;
        byte b;

        do
        {
            b = reader.ReadByte();
            value = (value << 7) | (b & 0x7F);
        } while ((b & 0x80) != 0);

        return value;
    }

    // Helper method for byte order conversion
    private uint SwapUInt32(uint value)
    {
        return ((value & 0x000000ff) << 24)
            | ((value & 0x0000ff00) << 8)
            | ((value & 0x00ff0000) >> 8)
            | ((value & 0xff000000) >> 24);
    }

    private ushort SwapUInt16(ushort value)
    {
        return (ushort)(((value & 0x00ff) << 8) | ((value & 0xff00) >> 8));
    }
}
