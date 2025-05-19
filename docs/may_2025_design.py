
# === CONFIGURATION ===

# Radial Bass Membrane System PsuedoCode. 

#The Radial Bass Membrane System visualizes the low end of a mix using a vibrating planar membrane. It merges two radial heightmaps—a kick pulse and a low-end radial FFT

# Kick pulse parameters
pulse_height = 1.0       # Amplitude of each radial pulse
pulse_width = 0.1        # How wide the pulse front appears
pulse_speed = 2.0        # Base speed of radial propagation
pulse_lambda = lambda t: pulse_speed  # Optional dynamic speed modifier

# FFT parameters
fft_low = 0              # Low cutoff frequency (Hz)
fft_high = 200           # High cutoff frequency (Hz)
fft_bins = 32            # Number of FFT bins to represent

# Terrain grid parameters
grid_size = 128          # 2D grid size: grid_size x grid_size
origin = (grid_size // 2, grid_size // 2)  # Center of grid

# === SYSTEM STATE ===

kick_pulses = []         # List of active kick pulses with timestamp and properties
z_kick = create_2d_grid(grid_size)  # Heightmap buffer for kick
z_fft = create_2d_grid(grid_size)   # Heightmap buffer for mix FFT
z_total = create_2d_grid(grid_size) # Final combined terrain

# === FRAME UPDATE LOOP ===

def update_frame(dt, mix_audio_frame, kick_triggered, current_time):
    """
    Called every frame with time delta, new audio buffer, and kick trigger status
    """

    # --- 1. KICK PASS ---

    # If a kick is detected, spawn a new radial pulse
    if kick_triggered:
        kick_pulses.append({
            "time": current_time,
            "height": pulse_height,
            "width": pulse_width,
            "speed_fn": pulse_lambda
        })

    # Reset kick terrain buffer
    clear_grid(z_kick)

    # For every active pulse, update the heightmap
    for pulse in kick_pulses:
        t_elapsed = current_time - pulse["time"]
        radius = pulse["speed_fn"](t_elapsed) * t_elapsed

        for x in range(grid_size):
            for y in range(grid_size):
                dx = x - origin[0]
                dy = y - origin[1]
                dist = compute_distance(dx, dy)

                # Apply falloff based on distance from pulse front
                # e.g. z = height * falloff(dist - radius)
                z_kick[x][y] += apply_radial_falloff(
                    dist, radius, pulse["width"], pulse["height"]
                )

    # Optionally prune old pulses if outside grid

    # --- 2. FFT PASS ---

    # Compute FFT of mix audio buffer
    fft_result = compute_fft(mix_audio_frame, fft_low, fft_high, fft_bins)

    # Map 1D FFT bins into radial terrain
    clear_grid(z_fft)

    for bin_index, amplitude in enumerate(fft_result):
        # Map bin_index to a radial band or angular region on grid
        # Then assign amplitude to corresponding cells
        apply_fft_bin_to_grid(bin_index, amplitude, z_fft, grid_size)

    # --- 3. MERGE PASS ---

    for x in range(grid_size):
        for y in range(grid_size):
            z_total[x][y] = z_kick[x][y] + z_fft[x][y]

    # --- 4. RENDER PASS ---

    # Pass z_total to Unity mesh/plane rendering logic
    render_terrain(z_total)

# === UTILITY FUNCTIONS ===

def create_2d_grid(size):
    return [[0.0 for _ in range(size)] for _ in range(size)]

def clear_grid(grid):
    for x in range(len(grid)):
        for y in range(len(grid[0])):
            grid[x][y] = 0.0

def compute_distance(dx, dy):
    # Euclidean distance from center
    return sqrt(dx * dx + dy * dy)

def apply_radial_falloff(dist, radius, width, height):
    # Return a value based on how close dist is to the pulse front
    # Example: Gaussian or smoothstep falloff
    # return height * smoothstep(dist - radius, width)
    return "value based on pulse falloff shape"

def compute_fft(audio_frame, low, high, bins):
    # Take FFT of the audio frame and return amplitudes in desired band
    return [0.0 for _ in range(bins)]  # Placeholder

def apply_fft_bin_to_grid(bin_index, amplitude, z_fft, grid_size):
    # Project bin_index into spatial region (e.g. ring or sector)
    # Add amplitude to corresponding region in z_fft
    pass

def render_terrain(z_grid):
    # Unity-native rendering logic using the Z values
    pass



## == doc 2 ===claude generated so its not great.
===Spherical Vocal Visualizer ===
=== CONFIGURATION ===
Spherical Vocal Visualizer System PseudoCode.
The Spherical Vocal Visualizer represents vocal audio as a dynamic sphere where radius is modulated by low-frequency content
and surface color is determined by phonetic/spectral features
Sphere parameters
base_radius = 1.0        # Base radius of sphere before modulation
vertex_density = 64      # Resolution of sphere (vertices per longitude/latitude)
max_displacement = 0.5   # Maximum radial displacement from base
FFT parameters for radius modulation
fft_low = 80             # Low cutoff frequency (Hz)
fft_high = 700           # High cutoff frequency (Hz)
fft_bins = 24            # Number of FFT bins to represent
Color mapping parameters
color_smoothing = 0.3    # Smoothing factor for color transitions (0-1)
color_intensity = 0.8    # Overall intensity of color mapping
Color mappings for phonetic features
color_map = {
'a': (1.0, 0.2, 0.2),    # Red
'e': (0.2, 1.0, 0.2),    # Green
'i': (0.2, 0.2, 1.0),    # Blue
'o': (1.0, 0.6, 0.2),    # Orange
'u': (0.8, 0.2, 1.0),    # Purple
'default': (0.8, 0.8, 0.8)  # Light gray
}
=== SYSTEM STATE ===
sphere_vertices = []     # Base vertex positions on unit sphere
vertex_displacements = []  # Current displacement values for each vertex
current_color = (0.8, 0.8, 0.8)  # Current interpolated color (r,g,b)
target_color = (0.8, 0.8, 0.8)   # Target color we're transitioning toward
last_phonetic = 'default'  # Last detected phonetic feature
=== FRAME UPDATE LOOP ===


def update_frame(dt, vocal_audio_frame, current_time):
"""
Called every frame with time delta and new audio buffer
"""
# --- 1. SPECTRAL ANALYSIS ---

# Compute FFT of vocal audio
fft_result = compute_fft(vocal_audio_frame, fft_low, fft_high, fft_bins)

# Extract phonetic feature (formant analysis, vowel detection, etc.)
phonetic_feature = extract_phonetic_feature(vocal_audio_frame)

# --- 2. RADIUS MODULATION PASS ---

# Map FFT bins to spherical regions
for vertex_idx, vertex in enumerate(sphere_vertices):
    # Convert vertex to spherical coordinates
    theta, phi = cartesian_to_spherical(vertex)
    
    # Map spherical coordinates to FFT bin indices
    bin_index = map_to_fft_bin(theta, phi, fft_bins)
    
    # Get amplitude for corresponding frequency bin
    amplitude = fft_result[bin_index]
    
    # Apply smoothing and scaling
    target_displacement = amplitude * max_displacement
    current_displacement = vertex_displacements[vertex_idx]
    
    # Smooth displacement transition
    vertex_displacements[vertex_idx] = lerp(
        current_displacement,
        target_displacement,
        min(1.0, dt * 10.0)
    )

# --- 3. COLOR MAPPING PASS ---

# Update target color based on phonetic feature
if phonetic_feature in color_map:
    target_color = color_map[phonetic_feature]
    last_phonetic = phonetic_feature
else:
    target_color = color_map['default']

# Smoothly interpolate current color toward target
current_color = (
    lerp(current_color[0], target_color[0], dt / color_smoothing),
    lerp(current_color[1], target_color[1], dt / color_smoothing),
    lerp(current_color[2], target_color[2], dt / color_smoothing)
)

# --- 4. RENDER PASS ---

# Construct the final sphere mesh
final_vertices = []
for idx, base_vertex in enumerate(sphere_vertices):
    # Apply radial displacement to base vertex
    displacement = vertex_displacements[idx]
    final_pos = scale_vector(base_vertex, base_radius + displacement)
    final_vertices.append(final_pos)

# Update mesh with new vertices and color
update_sphere_mesh(final_vertices, current_color)
=== UTILITY FUNCTIONS ===
def generate_unit_sphere(density):
# Generate vertices for a unit sphere with given density
return vertices  # List of (x,y,z) coordinates
def cartesian_to_spherical(vertex):
# Convert 3D cartesian coordinates to spherical coordinates
x, y, z = vertex
r = sqrt(xx + yy + z*z)
theta = atan2(y, x)
phi = acos(z / r)
return theta, phi
def map_to_fft_bin(theta, phi, num_bins):
# Map spherical coordinates to an FFT bin index
normalized_phi = phi / PI  # Now in range 0-1
bin_index = int(normalized_phi * num_bins)
return max(0, min(num_bins - 1, bin_index))
def compute_fft(audio_frame, low, high, bins):
# Take FFT of the audio frame and return amplitudes in desired band
return [0.0] * bins  # Placeholder
def extract_phonetic_feature(audio_frame):
# Extract phonetic feature from audio frame
return 'default'  # Placeholder
def scale_vector(vec, scalar):
# Multiply a vector by a scalar
return (vec[0] * scalar, vec[1] * scalar, vec[2] * scalar)
def lerp(a, b, t):
# Linear interpolation between a and b by factor t
return a + (b - a) * t
def update_sphere_mesh(vertices, color):
# Update the Unity mesh with new vertices and color
pass
=== ALTERNATIVE COLOR MAPPING SYSTEMS ===
Spectral Centroid: Map brightness of sound (centroid) to hue and spread to saturation
Formant-Based: Map F1 to red-green axis and F2 to blue axis for vowel space visualization
Prosodic Features: Map pitch to hue, energy to value, and speaking rate to saturation
Emotional Content: Map detected emotion to predefined color scheme (happy=yellow, sad=blue)
Voice Characteristics: Map voice breathiness, pitch and clarity to different color channels
##
