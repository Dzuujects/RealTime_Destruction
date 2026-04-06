using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MIConvexHull;
using Parabox.CSG;
using Unity.VisualScripting;

/*  VORONOI FRACTURE CLASS                                                                         */
/*  - Main class for the voronoi fracture                                                          */
/*  - Called at the collision debugger when the impulse equals to or exceeds the minimum threshold */
public class VoronoiFracture : MonoBehaviour
{
    
    /*  FRACTURE FUNCTION */
    /*  - Main function of the fracture */
    /*  - Generate the seed locations inside the mesh */
    /*  - Run Delaunay Triangulation on each seed */
    /*  - For each seed, collect it's Delaunay circumcenters to form a voronoi cell */
    /*  - Build a convex hull from those cells and verify that the mesh is not broken (is watertight, has enough volume) */
    /*  - CSG intersect the cell with the original mesh to get the actual chunk shape */
    /*  - Repair the resulting mesh after CSG intersect */
    /*  - Spawn as a physics object */
    public static void Fracture(GameObject oriObject, Vector3 impactLocation, Vector3 impactVelocity, int seedCount)
    {
        // Get the mesh and material of the original object. If null, end early
        MeshFilter   mf = oriObject.GetComponent<MeshFilter>();
        MeshRenderer mr = oriObject.GetComponent<MeshRenderer>();
        if (mf == null || mr == null) return;

        // Use the bounds from the mesh and generate seed locations inside the mesh 
        Bounds bounds = mf.mesh.bounds;
        List<VoronoiVertex> seeds = VoronoiSeedGenerator.GenerateSeeds(bounds, oriObject.transform, impactLocation, seedCount);

        // Run Delaunay Triangulation on each seed
        var delaunay = DelaunayTriangulation<VoronoiVertex, DefaultTriangulationCell<VoronoiVertex>>
            .Create(seeds, 1e-10);

        float  pad = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        Bounds clampBounds = new Bounds(bounds.center, bounds.size * 3f + Vector3.one * pad);

        // Create a list of chunks game objects
        List<GameObject> chunks = new List<GameObject>();
        int skippedRegions = 0;

        // For each seed
        foreach (var seed in seeds)
        {
            // Get the list of vertices that make up the cells and add it to the list
            List<VoronoiVertex> cellVerts = delaunay.Cells
                .Where(cell => cell.Vertices.Contains(seed))
                .Select(cell => VoronoiSeedGenerator.ComputeCircumcenter(cell.Vertices))
                .Where(v => v != null)
                .Where(v => clampBounds.Contains(new Vector3((float)v.Position[0], (float)v.Position[1], (float)v.Position[2])))
                .GroupBy(v => $"{Math.Round(v.Position[0], 4)},{Math.Round(v.Position[1], 4)},{Math.Round(v.Position[2], 4)}")
                .Select(g => g.First())
                .ToList();

            // If cells is more than 4 than continue, otherwise it would go to infinity
            if (cellVerts.Count < 4) { skippedRegions++; continue; }

            // Create a convex hull mesh with the cells and if there is more than 3 vertex continue
            Mesh rawVoronoiMesh = MeshUtilities.CreateConvexHullMesh(cellVerts);
            if (rawVoronoiMesh == null || rawVoronoiMesh.vertexCount < 3) continue;

            // Check if it is water tight, if not, add to skipped region and continue
            if (!MeshUtilities.IsMeshWatertight(rawVoronoiMesh)) { skippedRegions++; continue; }

            // Get the volume of the bounds, if the chunk is smaller than 0.0001 of the bounds, then skip and continue
            float boundsVolume = bounds.size.x * bounds.size.y * bounds.size.z;
            if (MeshUtilities.GetMeshVolume(rawVoronoiMesh) < boundsVolume * 0.0001f) { skippedRegions++; continue; }

            // After passing all checks, build the raw cell object
            GameObject rawCellObject = BuildRawCellObject(oriObject, mr, rawVoronoiMesh);

            // Create a mesh and try to use CSG Intersect to split the mesh into chunks, if fail return error message
            Mesh finalChunkMesh = null;
            try { finalChunkMesh = (Mesh)CSG.Intersect(oriObject, rawCellObject); }
            catch (Exception e) { Debug.LogWarning($"CSG.Intersect failed: {e.Message}"); }

            // Destroy the raw cells that were used to compare to the final cells
            UnityEngine.Object.Destroy(rawCellObject);

            // If there are final chunks, then cover any holes, spawn the chunk, and add them to the list of game objects
            if (finalChunkMesh != null && finalChunkMesh.vertexCount > 0)
            {
                finalChunkMesh = MeshUtilities.FillOpenBoundaries(finalChunkMesh);
                chunks.Add(ChunkSpawner.SpawnChunk(oriObject, finalChunkMesh, mr.materials, impactVelocity, seedCount));
            }
        }

        // Destroy the final original game object
        Debug.Log($"Fracture complete. Chunks: {chunks.Count}, Skipped: {skippedRegions}");
        UnityEngine.Object.Destroy(oriObject);
    }

    // Creates a temporary game object to get the mesh and   be used to cut out the original mesh later
    private static GameObject BuildRawCellObject(GameObject oriObject, MeshRenderer mr, Mesh mesh)
    {
        GameObject obj = new GameObject("RawCell");
        obj.transform.position   = oriObject.transform.position;
        obj.transform.rotation   = oriObject.transform.rotation;
        obj.transform.localScale = oriObject.transform.localScale;
        obj.AddComponent<MeshFilter>().mesh          = mesh;
        obj.AddComponent<MeshRenderer>().materials   = mr.materials;
        return obj;
    }
}