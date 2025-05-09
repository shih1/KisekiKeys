# Kiseki Keys 
奇跡 | きせき | ㄑㄧˊ ㄐㄧˋ | qí jì

Kiseki Keys brings nostalgic, hopeful melodies to life through dynamic visualizations.

This visulalizer was designed to visualize piano MIDI and drum audio in a 3D environment. 

## 🛠️ Metadata
- **Engine**: Unity `6000.1.2f1`  
- **Developed**: Summer 2025
- **Platform**: M1 Mac

### 📦 Build

Build using the Unity Engine GUI. There is no CLI build option. 

## Unfamiliar with Unity? 

If you're unfamiliar with Unity, just focus on the /Assets folder — that's where all the custom src is. The rest is Unity-generated and can mostly be ignored.

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

## AI Note

Code in this repository was substantially generated with assistance from Claude 3.7 Sonnet (February 2025) and with prompts found in https://github.com/x1xhlol/system-prompts-and-models-of-ai-tools/tree/main.