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
| **Invert Raise / Lower Mode** | Hold **Shift** or **Ctrl** while sculpting (instantly inverts between raising peaks and lowering valleys) |
| **Switch to 1 Raise Tool** | Key **1** (Raises land into peaks and rocky cliffs) |
| **Switch to 2 Lower Tool** | Key **2** (Lowers land into valleys, canyons, bays, and depressions) |
| **Switch to 3 River Tool** | Key **3** (Sculpts shallow riverbeds and places conformed flowing water; cliffs >50° spawn waterfalls) |
| **Switch to 4 Plateau Tool** | Key **4** (Flattens and levels terrain to clicked height) |
| **Switch to 5 Smooth Tool** | Key **5** (Relaxes steep, sharp bumps) |
| **Switch to 6 Foliage Tool** | Key **6** (Plants fairytale trees on clicked surface) |
| **Switch to 7 Erase Tool** | Key **7** (Removes trees and foliage within brush radius) |
| **Resize Brush Radius** | Keys **[** / **]** or on-screen slider |
| **Adjust Brush Power** | On-screen **Power** slider |
| **Randomize New Island** | Press **Spacebar** or click **`🎲 New Island`** |
| **Toggle UI Visibility** | Press **H** (Hide UI for clean cinematic view or screenshots) |

> [!TIP]
> **Dynamic Nature & River Conforming:** Whenever you raise or lower terrain height, all nearby trees, rocks, and flowers automatically ride up and down smoothly with the ground! Submerged trees are cleanly removed, and all rivers and cliff waterfalls dynamically re-conform their elevation to the new terrain bed in real time!

---

## 3. The On-Screen Docks

### Active Brush Settings Dock (Above Toolbar)
- **Tool Display**: Shows currently selected sculpting mode (Raise Mountain, Lower Valley, River Stream, Plateau, Smooth, Plant Foliage, Erase Foliage).
- **Size Slider**: Live slider to dynamically adjust brush circle radius (`1.5` to `20.0`).
- **Power Slider**: Live slider to dynamically adjust brush strength/speed (`0.2` to `3.5`).

### The Bottom Toolbar
When running in Play mode, a diorama dock appears at the bottom of the screen:
- **[1 Raise]**: Left-click and drag to raise peaks and mountain ranges.
- **[2 Lower]**: Left-click and drag to carve valleys, gorges, bays, and beaches.
- **[3 River]**: Left-click and drag down hills to carve a shallow river channel and place flowing water without erasing mountains into an abyss.
- **[4 Plateau]**: Left-click and drag to level the ground into flat building plateaus.
- **[5 Smooth]**: Left-click and drag over uneven terrain to smooth the slopes.
- **[6 Foliage]**: Left-click to plant deciduous, pine, or blossom trees.
- **[7 Erase]**: Left-click to clear foliage in the brush circle.
- **[🎲 New Island]**: Instantly rolls a random seed, recreates terrain, rivers, waterfalls, and trees with distinct mountain layouts.
- **[⚙ Settings]**: Toggles the parameter adjustment sidebar.

---

## 4. Customizing Generation Parameters

In the **⚙ Settings** panel (or on the `ProceduralWorldManager` component in the Inspector), you can customize:

- **`🎲 Generate Random Island`**: Generates a completely new island with a fresh random seed and distinct topography.
- **Island Seed**: Type any seed number in the input box, or click **`🎲 Roll`** to pick a random seed instantly!
- **Terrain Height**: Maximum elevation for central mountain peaks (10m to 50m).
- **River Depth**: Controls depth of carved riverbeds (0.10 to 1.80 units; shallow channels look best).
- **River Width**: Width of procedural and painted water ribbons (1.5 to 6.0 units).
- **Water Level**: Height of the ocean plane (affects beaches, bays, and rivers).
- **River Count**: Number of procedural downhill rivers carved from mountain ridges.
- **Island Radius**: Controls how much of the map is land vs ocean.
- **Coastline Warping**: Strength of domain warping for organic coastal bays.
- **Low-Poly Flat Shading**: Toggle between faceted papercraft style and smooth shaded normals.
- **🔄 Re-generate Island**: Rebuilds the entire world with current settings.
- **🌊 Clear Custom Rivers**: Clears player-painted rivers and restores procedural rivers.

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
