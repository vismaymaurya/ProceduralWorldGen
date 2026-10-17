# How To Use: Procedural Fairytale Terrain Generator

Welcome to the **Fairytale Procedural Terrain Generation & Sculpting System** for Unity (URP), inspired by the game **Brushland**.

---

## 1. Quick Start

1. Open the project in **Unity 6**.
2. In the Project window, navigate to `Assets/Scenes/` and double-click **`ProceduralWorld.unity`**.
3. Press the **Play** button at the top of the Unity Editor.
4. The system will immediately generate a procedural diorama island with lush meadows, rocky cliffs, sandy beaches, winding downhill rivers, cliff waterfalls, and clusters of fairytale trees.

---

## 2. Controls & Keybindings

### Camera Controls (Diorama Tabletop)
| Action | Input |
| :--- | :--- |
| **Orbit Camera** | Hold **Right Mouse Button** (or **Alt + Left Click**) and drag |
| **Pan Camera** | Hold **Middle Mouse Button** and drag, or use **W / A / S / D** / Arrow keys |
| **Zoom In / Out** | Scroll the **Mouse Wheel** |
| **Focus Center** | Moves focus dynamically with panning |

### Sculpting & Painting Controls
| Action | Input |
| :--- | :--- |
| **Paint / Sculpt Terrain** | **Left Click** on the terrain surface |
| **Switch to Mountain Tool** | Key **1** (Raises land into peaks and rocky cliffs) |
| **Switch to River Tool** | Key **2** (Carves land below water level into lakes and rivers) |
| **Switch to Plateau Tool** | Key **3** (Flattens and levels terrain to clicked height) |
| **Switch to Smooth Tool** | Key **4** (Relaxes steep, sharp bumps) |
| **Switch to Foliage Tool** | Key **5** (Plants fairytale trees on clicked surface) |
| **Switch to Erase Tool** | Key **6** (Removes trees and foliage within brush radius) |
| **Resize Brush Radius** | Hold **Left Shift** + Scroll **Mouse Wheel** |
| **Randomize New Island** | Press **Spacebar** |
| **Toggle UI Visibility** | Press **H** (Hide UI for clean cinematic view or screenshots) |

---

## 3. The Bottom Toolbar

When running in Play mode, a cozy diorama dock appears at the bottom of the screen:

- **[1 Mountain]**: Left-click and drag to raise peaks and mountain ranges.
- **[2 River]**: Left-click and drag to carve river valleys, bays, and lakes.
- **[3 Plateau]**: Left-click and drag to level the ground into flat building plateaus.
- **[4 Smooth]**: Left-click and drag over uneven terrain to smooth the slopes.
- **[5 Foliage]**: Left-click to plant deciduous, pine, or blossom trees.
- **[6 Erase]**: Left-click to clear foliage in the brush circle.
- **[🎲 New Island]**: Generates a completely fresh random island with new rivers, mountains, and trees.
- **[⚙ Settings]**: Toggles the parameter adjustment sidebar.

---

## 4. Customizing Generation Parameters

In the **⚙ Settings** panel (or on the `ProceduralWorldManager` component in the Inspector), you can customize:

- **Seed**: Change the numerical seed to reproduce specific island layouts.
- **Island Radius**: Controls how much of the map is land vs ocean.
- **Mountain Height**: Maximum elevation for central mountain peaks.
- **Water Level**: Height of the ocean plane (affects beaches, bays, and rivers).
- **River Count**: Number of downhill rivers carved from mountain ridges.
- **Coastline Warping**: Strength of domain warping for organic coastal bays.
- **Low-Poly Flat Shading**: Toggle between faceted papercraft style and smooth shaded normals.

---

## 5. Working in the Unity Editor (Without Play Mode)

You can generate and preview islands directly in the Scene view:

1. Select the **`ProceduralWorld`** GameObject in the Hierarchy.
2. In the Inspector:
   - Click **`🎲 Randomize Seed & Generate`** to create a fresh variation.
   - Click **`🔄 Re-Generate Island`** to rebuild with current parameters.
   - Click **`Rebuild Mesh Only (Instant)`** to refresh vertex colors and heights.
3. You can also trigger **`Fairytale World > Setup Complete Scene`** from Unity's top menu bar at any time to re-verify materials, lighting, and camera setup.

---

## 6. Shaders & Visual Tuning

Materials are located in `Assets/ProceduralTerrain/Materials/`:

- **`Mat_StylizedTerrain`**:
  - `Grass Tint`: Main meadow albedo.
  - `Cliff Tint`: Rock cliff color on steep slopes (>38°).
  - `Sand Tint`: Warm shoreline beach color.
  - `Snow Tint`: High-elevation mountain cap color.
  - `Shadow Wrap`: Softness of directional light shadows.
- **`Mat_StylizedWater`**:
  - `Shallow Color`: Turquoise / cyan coastal water tone.
  - `Deep Color`: Deep ocean sapphire blue.
  - `Foam Width`: Width of the shoreline foam rim.
  - `Wave Speed / Frequency`: Surface animation speed.
- **`Mat_StylizedWaterfall`**:
  - `Vertical Flow Speed`: Scrolling speed of waterfall foam.
  - `Foam Density`: Frequency of foam streaks.
