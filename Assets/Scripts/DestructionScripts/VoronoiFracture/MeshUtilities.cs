using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MIConvexHull;

public static class MeshUtilities
{
    public static Mesh CreateConvexHullMesh(IEnumerable<VoronoiVertex> regionVertices)
    {
        List<VoronoiVertex> hullVertices = regionVertices.ToList();
        var convexHull = ConvexHull.Create(hullVertices);
        if (convexHull == null || convexHull.Result == null) return null;

        Vector3 centroid = Vector3.zero;
        var pts = convexHull.Result.Points.ToList();
        foreach (var p in pts)
            centroid += new Vector3((float)p.Position[0], (float)p.Position[1], (float)p.Position[2]);
        centroid /= Mathf.Max(1, pts.Count);

        Mesh mesh = new Mesh();
        List<Vector3> vertices  = new List<Vector3>();
        List<int>     triangles = new List<int>();

        foreach (var face in convexHull.Result.Faces)
        {
            if (face?.Vertices == null || face.Vertices.Length < 3) continue;

            Vector3 v0 = face.Vertices[0].toVector3();
            Vector3 v1 = face.Vertices[1].toVector3();
            Vector3 v2 = face.Vertices[2].toVector3();

            Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);
            Vector3 faceMid    = (v0 + v1 + v2) / 3f;

            int startIndex = vertices.Count;
            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2);

            if (Vector3.Dot(faceNormal, faceMid - centroid) < 0f)
            { triangles.Add(startIndex); triangles.Add(startIndex + 2); triangles.Add(startIndex + 1); }
            else
            { triangles.Add(startIndex); triangles.Add(startIndex + 1); triangles.Add(startIndex + 2); }
        }

        if (vertices.Count == 0) return null;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    public static bool IsMeshWatertight(Mesh mesh)
    {
        Vector3[] verts = mesh.vertices;
        int[]     tris  = mesh.triangles;
        Dictionary<(long, long), int> edgeCount = new Dictionary<(long, long), int>();

        for (int i = 0; i < tris.Length; i += 3)
            for (int e = 0; e < 3; e++)
            {
                long keyA = PositionKey(verts[tris[i + e]]);
                long keyB = PositionKey(verts[tris[i + (e + 1) % 3]]);
                var  key  = keyA < keyB ? (keyA, keyB) : (keyB, keyA);
                edgeCount[key] = edgeCount.TryGetValue(key, out int c) ? c + 1 : 1;
            }

        return edgeCount.Values.All(c => c == 2);
    }

    public static float GetMeshVolume(Mesh mesh)
    {
        float    volume = 0f;
        Vector3[] verts = mesh.vertices;
        int[]     tris  = mesh.triangles;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];
            volume += Vector3.Dot(v0, Vector3.Cross(v1, v2));
        }
        return Mathf.Abs(volume) / 6f;
    }

    public static Mesh FillOpenBoundaries(Mesh mesh)
    {
        Vector3[] origVerts = mesh.vertices;
        int[]     origTris  = mesh.triangles;
        Vector2[] origUVs   = mesh.uv;

        Dictionary<(long, long), bool> directedEdges = new Dictionary<(long, long), bool>();
        for (int i = 0; i < origTris.Length; i += 3)
            for (int e = 0; e < 3; e++)
            {
                long ka = PositionKey(origVerts[origTris[i + e]]);
                long kb = PositionKey(origVerts[origTris[i + (e + 1) % 3]]);
                directedEdges[(ka, kb)] = true;
            }

        Dictionary<long, long> boundaryNext   = new Dictionary<long, long>();
        Dictionary<long, int>  keyToVertIndex = new Dictionary<long, int>();

        for (int i = 0; i < origTris.Length; i += 3)
            for (int e = 0; e < 3; e++)
            {
                int  ia = origTris[i + e];
                int  ib = origTris[i + (e + 1) % 3];
                long ka = PositionKey(origVerts[ia]);
                long kb = PositionKey(origVerts[ib]);

                if (!directedEdges.ContainsKey((kb, ka)))
                {
                    boundaryNext[ka]   = kb;
                    keyToVertIndex[ka] = ia;
                    keyToVertIndex[kb] = ib;
                }
            }

        if (boundaryNext.Count == 0) return mesh;

        List<List<int>> loops   = new List<List<int>>();
        HashSet<long>   visited = new HashSet<long>();

        foreach (long startKey in boundaryNext.Keys)
        {
            if (visited.Contains(startKey)) continue;
            List<int> loop    = new List<int>();
            long      current = startKey;
            int       safety  = 0;

            while (!visited.Contains(current) && safety++ < 10000)
            {
                visited.Add(current);
                loop.Add(keyToVertIndex[current]);
                if (!boundaryNext.TryGetValue(current, out long next)) break;
                current = next;
            }
            if (loop.Count >= 3) loops.Add(loop);
        }

        List<Vector3> newVerts = new List<Vector3>(origVerts);
        List<int>     newTris  = new List<int>(origTris);

        Vector3 meshCentroid = Vector3.zero;
        foreach (var v in origVerts) meshCentroid += v;
        meshCentroid /= Mathf.Max(1, origVerts.Length);

        foreach (List<int> loop in loops)
        {
            Vector3 loopCentroid = Vector3.zero;
            foreach (int idx in loop) loopCentroid += origVerts[idx];
            loopCentroid /= loop.Count;

            int centroidIdx = newVerts.Count;
            newVerts.Add(loopCentroid);

            for (int i = 0; i < loop.Count; i++)
            {
                int     ia     = loop[i];
                int     ib     = loop[(i + 1) % loop.Count];
                Vector3 va     = origVerts[ia];
                Vector3 vb     = origVerts[ib];
                Vector3 normal = Vector3.Cross(vb - loopCentroid, va - loopCentroid);

                if (Vector3.Dot(normal, loopCentroid - meshCentroid) >= 0f)
                { newTris.Add(centroidIdx); newTris.Add(ia); newTris.Add(ib); }
                else
                { newTris.Add(centroidIdx); newTris.Add(ib); newTris.Add(ia); }
            }
        }

        Vector2[] newUVs = new Vector2[newVerts.Count];
        for (int i = 0; i < origUVs.Length && i < newUVs.Length; i++)
            newUVs[i] = origUVs[i];

        Mesh filled = new Mesh();
        filled.vertices  = newVerts.ToArray();
        filled.triangles = newTris.ToArray();
        filled.uv        = newUVs;
        filled.RecalculateNormals();
        filled.RecalculateBounds();
        return filled;
    }

    public static Mesh EnsureDoubleSidedCaps(Mesh mesh)
    {
        Vector3[] origVerts   = mesh.vertices;
        int[]     origTris    = mesh.triangles;
        Vector2[] origUVs     = mesh.uv;

        Vector3 centroid = Vector3.zero;
        foreach (var v in origVerts) centroid += v;
        centroid /= origVerts.Length;

        List<int> badTris = new List<int>();
        for (int i = 0; i < origTris.Length; i += 3)
        {
            Vector3 v0     = origVerts[origTris[i]];
            Vector3 v1     = origVerts[origTris[i + 1]];
            Vector3 v2     = origVerts[origTris[i + 2]];
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
            Vector3 mid    = (v0 + v1 + v2) / 3f;
            if (Vector3.Dot(normal, mid - centroid) < 0) badTris.Add(i);
        }

        int[] fixedTris = (int[])origTris.Clone();
        foreach (int i in badTris)
        {
            int tmp = fixedTris[i + 1];
            fixedTris[i + 1] = fixedTris[i + 2];
            fixedTris[i + 2] = tmp;
        }

        Mesh fixedMesh = new Mesh();
        fixedMesh.vertices  = origVerts;
        fixedMesh.triangles = fixedTris;
        fixedMesh.uv        = origUVs;
        fixedMesh.RecalculateNormals();
        fixedMesh.RecalculateBounds();
        return fixedMesh;
    }

    public static long PositionKey(Vector3 v)
    {
        int x = Mathf.RoundToInt(v.x * 10000f);
        int y = Mathf.RoundToInt(v.y * 10000f);
        int z = Mathf.RoundToInt(v.z * 10000f);
        return ((long)(x + 100000) * 1000000L + (y + 100000)) * 1000000L + (z + 100000);
    }
}