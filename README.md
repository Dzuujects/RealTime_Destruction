# RealTime Destruction

## Overview

RealTime Destruction is a Unity project that demonstrates advanced real-time mesh destruction and fracturing techniques. The system allows objects to be fractured dynamically at runtime using Constructive Solid Geometry (CSG) and Voronoi-based algorithms, enabling realistic destruction effects for physics-based gameplay.

## Features

- **Projectile Firing:** Fire projectiles from the player camera to interact with destructible objects.
- **Collision Debugging:** Visualize collision normals and impact forces for debugging and tuning destruction thresholds.
- **Mesh Slicing:** Perform horizontal mesh cuts at collision points for objects marked as cuttable.
- **Voronoi Fracture:** Procedurally fracture meshes into multiple chunks using Voronoi diagrams and Delaunay triangulation.
- **Physics Integration:** All fractured chunks are spawned as independent physics objects with correct materials and colliders.
- **CSG Operations:** Boolean mesh operations (Union, Subtraction, Intersection) using a C# port of the CSG.js library.

## Project Structure

- `Assets/Scripts/PlayerControls/FireProjectile.cs`: Handles player input for firing projectiles and scene reset.
- `Assets/Scripts/DestructionScripts/CollisionDebugger.cs`: Detects collisions, visualizes impact, and triggers destruction.
- `Assets/Scripts/DestructionScripts/MeshSlicer.cs`: Slices meshes horizontally at the point of impact.
- `Assets/Scripts/DestructionScripts/VoronoiFracture/`: Contains Voronoi-based mesh fracturing logic and utilities.
- `Assets/CSG/CSG.cs`: CSG Boolean operations for mesh manipulation.

## How It Works

1. **Firing Projectiles:**
   - Left/Right mouse buttons fire projectiles at different speeds.
   - Projectiles interact with destructible objects in the scene.
2. **Collision Detection:**
   - On collision, the `CollisionDebugger` checks impact speed and impulse.
   - If thresholds are met, the object is fractured.
3. **Destruction Algorithms:**
   - If the object is marked as cuttable, a horizontal mesh slice is performed.
   - Otherwise, Voronoi fracturing splits the mesh into multiple chunks.
   - Chunks are spawned as new physics objects.

## Requirements

- **Unity Version:** 2022.3.62f3 or later
- **Dependencies:**
  - Unity Physics, Mesh, and Visual Scripting modules
  - [MIConvexHull](https://github.com/DesignEngrLab/MIConvexHull) (for Voronoi/Delaunay)
  - Parabox CSG (C# port of CSG.js)

## Getting Started

1. **Clone or Download the Repository.**
2. **Open the Project in Unity 2022.3.62f3 or later.**
3. **Import Required Packages:**
   - Ensure all dependencies in `Packages/manifest.json` are installed.
   - Add MIConvexHull if not present.
4. **Run the Scene:**
   - Open a demo scene.
   - Enter Play mode and fire projectiles at destructible objects.

## Customization

- **Adjust Destruction Thresholds:**
  - Tune `minSpeed` and `impulseThreshold` in `CollisionDebugger` for different materials.
- **Change Fracture Type:**
  - Set `Cuttable` to true for mesh slicing, false for Voronoi fracture.
- **Seed Count:**
  - Increase `seedCount` for more fracture pieces.

## Credits

- CSG.js by Evan Wallace (MIT License)
- C++ port by Tomasz Dabrowski
- C# port by Karl Henkel (Parabox)
- MIConvexHull by DesignEngrLab

## License

This project is licensed under the MIT License. See source files for details.
