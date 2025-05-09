# Kiseki Keys 
奇跡 | きせき | ㄑㄧˊ ㄐㄧˋ | qí jì

Kiseki Keys brings nostalgic, hopeful melodies to life through dynamic visualizations.

## 🛠️ Metadata
- **Engine**: Unity `6000.1.2f1`  
- **Developed**: Summer 2025
- **Platform**: M1 Mac

### 📦 Build

Build using the Unity Engine GUI. There is no CLI build option. 


## Script Information

| Script Name               | Description                                                      |
| ------------------------- | ---------------------------------------------------------------- |
| **AudioSource.cs**         | Plays a `.wav` file on start.                                    |
| **MidiFileParser.cs**      | Parses MIDI data (BPM, velocity, note values) in binary format.  |
| **MidiNoteSpawner.cs**     | Spawns NoteObjects based on parsed MIDI data.              |
| **MidiVisualizerController.cs** | Handles keyboard input and manages multiple MIDI files (partial features). |
| **NoteObject.cs**          | Pulses the note and adds a visual trail.                         |

## Developer
yoshih