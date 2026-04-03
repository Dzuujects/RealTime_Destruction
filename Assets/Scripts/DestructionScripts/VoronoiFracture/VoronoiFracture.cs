using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MIConvexHull;
using Parabox.CSG;

public class VoronoiFracture : MonoBehaviour
{
    public static void Fracture(GameObject oriObject, Vector3 impactLocation, Vector3 impactVelocity, int seedCount)
    {
        MeshFilter   mf = oriObject.GetComponent<MeshFilter>();
        MeshRenderer mr = oriObject.GetComponent<MeshRenderer>();
        if (mf == null || mr == null) return;

        Bounds bounds = mf.mesh.bounds;
        List<VoronoiVertex> seeds = VoronoiSeedGenerator.GenerateSeeds(bounds, oriObject.transform, impactLocation, seedCount);

        var delaunay = DelaunayTriangulation<VoronoiVertex, DefaultTriangulationCell<VoronoiVertex>>
            .Create(seeds, 1e-10);

        float  pad         = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        Bounds clampBounds = new Bounds(bounds.center, bounds.size * 3f + Vector3.one * pad);

        List<GameObject> chunks         = new List<GameObject>();
        int              skippedRegions = 0;

        foreach (var seed in seeds)
        {
            List<VoronoiVertex> cellVerts = delaunay.Cells
                .Where(cell => cell.Vertices.Contains(seed))
                .Select(cell => VoronoiSeedGenerator.ComputeCircumcenter(cell.Vertices))
                .Where(v => v != null)
                .Where(v => clampBounds.Contains(new Vector3((float)v.Position[0], (float)v.Position[1], (float)v.Position[2])))
                .GroupBy(v => $"{Math.Round(v.Position[0], 4)},{Math.Round(v.Position[1], 4)},{Math.Round(v.Position[2], 4)}")
                .Select(g => g.First())
                .ToList();

            if (cellVerts.Count < 4) { skippedRegions++; continue; }

            Mesh rawVoronoiMesh = MeshUtilities.CreateConvexHullMesh(cellVerts);
            if (rawVoronoiMesh == null || rawVoronoiMesh.vertexCount < 3) continue;

            if (!MeshUtilities.IsMeshWatertight(rawVoronoiMesh)) { skippedRegions++; continue; }

            float boundsVolume = bounds.size.x * bounds.size.y * bounds.size.z;
            if (MeshUtilities.GetMeshVolume(rawVoronoiMesh) < boundsVolume * 0.0001f) { skippedRegions++; continue; }

            GameObject rawCellObject = BuildRawCellObject(oriObject, mr, rawVoronoiMesh);

            Mesh finalChunkMesh = null;
            try { finalChunkMesh = (Mesh)CSG.Intersect(oriObject, rawCellObject); }
            catch (Exception e) { Debug.LogWarning($"CSG.Intersect failed: {e.Message}"); }

            UnityEngine.Object.Destroy(rawCellObject);

            if (finalChunkMesh != null && finalChunkMesh.vertexCount > 0)
            {
                finalChunkMesh = MeshUtilities.FillOpenBoundaries(finalChunkMesh);
                chunks.Add(ChunkSpawner.SpawnChunk(oriObject, finalChunkMesh, mr.materials, impactVelocity, seedCount));
            }
        }

        Debug.Log($"Fracture complete. Chunks: {chunks.Count}, Skipped: {skippedRegions}");
        UnityEngine.Object.Destroy(oriObject);
    }

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