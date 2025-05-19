
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
