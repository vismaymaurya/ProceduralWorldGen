# Daily Progress Report

**Project:** Procedural World Generation (Fairytale Diorama Terrain)  
**Inspired By:** *Brushland* (Transhuman Design)  
**Date:** October 17, 2026  
**Status:** Core Procedural Engine & Interactive Sculpting Completed  

---

## 1. Executive Summary

Today's milestone focused on designing, implementing, and optimizing a complete procedural terrain generation and interactive sculpting system in Unity 6 (Universal Render Pipeline). The system recreates the aesthetic and mechanics featured in the *Brushland* announcement trailer: organic diorama islands, steep cliff transitions, dynamic downhill rivers, cascading cliff waterfalls, stylized foliage scatter, custom URP shaders, and a real-time tile/brush painting toolset.

---

## 2. Implemented Features & Architecture

### A. Procedural Noise & Island Shaping
- **Multi-Octave Simplex & Ridge Noise:** Integrated [`FastNoiseLite.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/FastNoiseLite.cs) for layered elevation generation.
- **Organic Coastline Domain Warping:** Distorts island boundaries using continuous domain warp noise to prevent circular or geometric island shapes, producing natural coves, peninsulas, and bays.
- **Island Distance Falloff:** Implemented smoothstep radial attenuation that blends the terrain smoothly into the ocean floor.

### B. Procedural Mesh Generation
- **Low-Poly Faceted (Flat-Shaded) Mesh:** [`IslandMeshGenerator.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/IslandMeshGenerator.cs) computes independent face normals per triangle to achieve a crisp, stylized papercraft diorama aesthetic.
- **Smooth Mesh Option:** Built-in toggle to switch dynamically between low-poly faceted shading and smooth terrain normals.
- **Baked Vertex Color Palette:** Calculates slope and height-based color weights directly into vertices (sand shorelines, lush meadow greens, dark stone cliffs, and snowy mountain peaks).

### C. Downhill Rivers & Cascading Waterfalls
- **Steepest-Descent Tracing:** [`RiverGenerator.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/RiverGenerator.cs) identifies high elevation candidates and carves smooth riverbeds downhill towards sea level using cosine falloffs.
- **Waterfall Detection:** Analyzes elevation drops along river paths; whenever height drops sharply over cliff edges, waterfall nodes are spawned.
- **Waterfall Ribbon Meshes:** [`ProceduralWater.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralWater.cs) creates dynamic curved quad ribbons along cliff drops with splash foam disks at the base.

### D. Procedural Foliage & Nature Scatter
- **Static Prototype Mesh Reuse:** [`ProceduralFoliage.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralFoliage.cs) procedurally builds and caches low-poly 3D models for:
  - **Pine / Conifer Trees:** Multi-tiered conical needles for upper hill groves.
  - **Deciduous Oak Trees:** Clustered spherical canopies for lowland meadows.
  - **Cherry Blossom Trees:** Soft pastel pink floral trees.
  - **River Rocks / Boulders:** Faceted stones placed along shores and steep slopes.
  - **Wildflower Clumps:** Clustered yellow, red, and lavender floral dots.
- **Biome & Density Rules:** Noise-driven placement prevents trees on cliffs, underwater, or above the mountain snowline, while bounding total instances for performance.

### E. Custom Stylized URP Shaders
1. **[`StylizedTerrain.shader`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Shaders/StylizedTerrain.shader):**
   - Half-Lambert wrapped lighting for soft, warm diorama illumination.
   - Tint controls for grass, sand, stone cliffs, and snow.
   - Full support for Universal Render Pipeline directional lighting and soft shadow reception.
2. **[`StylizedWater.shader`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Shaders/StylizedWater.shader):**
   - Depth-based color ramp (shallow turquoise/cyan to deep sapphire blue).
   - Dynamic shoreline foam calculated via camera scene depth buffer (`_CameraDepthTexture`).
   - Undulating wave vertices and sun specular reflection.
3. **[`StylizedWaterfall.shader`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Shaders/StylizedWaterfall.shader):**
   - Vertical UV scrolling with procedural foam streak noise and soft side edge fade.

### F. Interactive Sculpting & Camera System
- **Interactive Brush Tool:** [`TerrainBrushTool.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Interaction/TerrainBrushTool.cs) provides real-time cursor projection and sculpting modes:
  - **Raise Mountain:** Creates mountain ridges and cliff faces.
  - **Lower River:** Carves waterbeds, lakes, and channels.
  - **Flatten Plateau:** Levels ground for building settlement foundations.
  - **Smooth Terrain:** Relaxes steep, jagged edges.
  - **Foliage Tool:** Interactively places trees or removes foliage with brush radius.
- **Diorama Camera:** [`DioramaCameraController.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Interaction/DioramaCameraController.cs) provides smooth orbit, pan, and zoom controls.
- **Runtime UI Dock:** [`TerrainUIController.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Interaction/TerrainUIController.cs) provides a bottom toolbar matching the trailer (buttons 1–6, dice randomize button, collapsible parameter tuning panel).

---

## 3. Bug Fixes & Critical Memory Optimization

### Issue Encountered
During runtime testing and continuous brush sculpting, Unity crashed with:
```text
Could not allocate memory: System out of memory!
Trying to allocate: 2599216B with 32 alignment. MemoryLabel: VertexData
Allocation happened at: Line:261 in ...\Runtime\Graphics\Mesh\VertexData.cpp
[ ALLOC_DEFAULT ] used: 3875448760B (~3.87 GB)
```

### Root Cause
1. `ApplyBrush()` was calling `new Mesh()` on every frame of dragging the mouse (60–120 times/sec). Each 96×96 flat-shaded mesh is 54,150 vertices (exactly ~2.6 MB of native unmanaged C++ memory).
2. Unmanaged C++ mesh memory is not freed immediately by .NET garbage collection, causing 3.87 GB of orphaned meshes to accumulate in memory.
3. `meshCollider.sharedMesh` was being reassigned every frame, triggering repeated PhysX cooking and memory pressure.
4. Old water and waterfall meshes were abandoned on GameObjects without explicit `Destroy(mesh)` calls.
5. In-editor mesh generation was embedding 5+ MB of raw mesh vertex data into the scene file.

### Remediation & Solution
1. **In-Place Buffer Reuse:** Updated [`IslandMeshGenerator.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/IslandMeshGenerator.cs) to reuse cached vertex, normal, UV, and color arrays.
2. **Dynamic Mesh Marking:** Applied `mesh.MarkDynamic()` on the terrain mesh for fast GPU buffer streaming.
3. **Decoupled Physics Baking:** Visual mesh updates run in-place at 120 FPS; `meshCollider.sharedMesh` baking only executes when the user releases the mouse button (`Input.GetMouseButtonUp(0)`).
4. **Explicit Cleanup:** Added `SafeDestroyMesh` and `SafeDestroyGO` routines to [`ProceduralWater.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralWater.cs) and [`ProceduralWorldManager.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralWorldManager.cs) to properly destroy previous native mesh assets.
5. **`HideFlags.DontSave` Integration:** Marked all dynamic runtime meshes with `HideFlags.DontSave`, preventing raw vertex data from bloating the `.unity` scene asset.

---

## 4. File Inventory

| Path | Purpose |
| :--- | :--- |
| [`Assets/ProceduralTerrain/Scripts/Core/FastNoiseLite.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/FastNoiseLite.cs) | Procedural noise and domain warping algorithms |
| [`Assets/ProceduralTerrain/Scripts/Core/ProceduralTerrainData.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralTerrainData.cs) | Settings, structs, and enums |
| [`Assets/ProceduralTerrain/Scripts/Core/IslandMeshGenerator.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/IslandMeshGenerator.cs) | Mesh topology and vertex color generation |
| [`Assets/ProceduralTerrain/Scripts/Core/RiverGenerator.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/RiverGenerator.cs) | Downhill river pathing and waterfall detection |
| [`Assets/ProceduralTerrain/Scripts/Core/ProceduralWater.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralWater.cs) | Water plane and waterfall ribbon builder |
| [`Assets/ProceduralTerrain/Scripts/Core/ProceduralFoliage.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralFoliage.cs) | Procedural tree, rock, and flower spawner |
| [`Assets/ProceduralTerrain/Scripts/Core/ProceduralWorldManager.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Core/ProceduralWorldManager.cs) | Core manager orchestrating generation and sculpting |
| [`Assets/ProceduralTerrain/Scripts/Interaction/TerrainBrushTool.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Interaction/TerrainBrushTool.cs) | Real-time sculpting tool and cursor visualizer |
| [`Assets/ProceduralTerrain/Scripts/Interaction/DioramaCameraController.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Interaction/DioramaCameraController.cs) | Diorama orbit, pan, and zoom camera |
| [`Assets/ProceduralTerrain/Scripts/Interaction/TerrainUIController.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Interaction/TerrainUIController.cs) | On-screen bottom dock and parameter panel |
| [`Assets/ProceduralTerrain/Scripts/Editor/ProceduralWorldEditor.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Editor/ProceduralWorldEditor.cs) | Inspector editor with one-click generation buttons |
| [`Assets/ProceduralTerrain/Scripts/Editor/FairytaleWorldSetup.cs`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Scripts/Editor/FairytaleWorldSetup.cs) | Editor setup script for scene, lighting, and materials |
| [`Assets/ProceduralTerrain/Shaders/StylizedTerrain.shader`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Shaders/StylizedTerrain.shader) | URP stylized terrain shader |
| [`Assets/ProceduralTerrain/Shaders/StylizedWater.shader`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Shaders/StylizedWater.shader) | URP stylized water shader with shoreline foam |
| [`Assets/ProceduralTerrain/Shaders/StylizedWaterfall.shader`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Shaders/StylizedWaterfall.shader) | URP scrolling waterfall shader |
| [`Assets/ProceduralTerrain/Materials/Mat_StylizedTerrain.mat`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Materials/Mat_StylizedTerrain.mat) | Terrain material asset |
| [`Assets/ProceduralTerrain/Materials/Mat_StylizedWater.mat`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Materials/Mat_StylizedWater.mat) | Water material asset |
| [`Assets/ProceduralTerrain/Materials/Mat_StylizedWaterfall.mat`](file:///g:/Unity/Unity%20Project/ProceduralWorldGen/Assets/ProceduralTerrain/Materials/Mat_StylizedWaterfall.mat) | Waterfall material asset |

---

## 5. Next Steps
- Implement procedural roads, paths, and bridges crossing riverbeds.
- Add procedural village building generation that adapts organically to painted tiles (houses, churches, windmills, and castle towers).
- Add water particle splash effects at the base of waterfalls.
