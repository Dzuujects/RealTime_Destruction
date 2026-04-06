using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MIConvexHull;
using Unity.VisualScripting;

/*  MESH UTILITES CLASS                                                                                                     */
/*  - Script filled with static classes for mesh operations when creating meshes after generating voronoi seeds and diagram */
public static class MeshUtilities
{

    /*  CREATE CONVEX HULL MESH FUNCTION                                                                      */
    /*  - Takes region vertices and wraps them in a convex hull mesh                                          */
    /*  - Runs MICovexHull algorithm on the voronoi cells circumcenter vertices                               */
    /*  - Computes the everage position of all hull points and wound to make it face outwards from the center */    
    public static Mesh CreateConvexHullMesh(IEnumerable<VoronoiVertex> regionVertices)
    {

        List<VoronoiVertex> hullVertices = regionVertices.ToList(); // adds the regionVertices to a list of hullVertices
        
        // Runs MIConvexHull algorithm on voronoi cell's circumcenter points, if fails: return null
        var convexHull = ConvexHull.Create(hullVertices);
        if (convexHull == null || convexHull.Result == null) return null;

        // Computes average position of hull
        Vector3 centroid = Vector3.zero;
        var pts = convexHull.Result.Points.ToList();
        foreach (var p in pts)
            centroid += new Vector3((float)p.Position[0], (float)p.Position[1], (float)p.Position[2]);
        centroid /= Mathf.Max(1, pts.Count);

        Mesh mesh = new Mesh();
        List<Vector3> vertices  = new List<Vector3>();
        List<int>     triangles = new List<int>();

        // For each triangular face, check which was it is facing adn make it face outwards from the center
        foreach (var face in convexHull.Result.Faces)
        {
            // Early break if face is bad
            if (face?.Vertices == null || face.Vertices.Length < 3) continue;

            Vector3 v0 = face.Vertices[0].toVector3();
            Vector3 v1 = face.Vertices[1].toVector3();
            Vector3 v2 = face.Vertices[2].toVector3();

            Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);
            Vector3 faceMid    = (v0 + v1 + v2) / 3f;

            int startIndex = vertices.Count;
            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2);

            // If vector dot is negative, the normal is facing inwards so flip
            if (Vector3.Dot(faceNormal, faceMid - centroid) < 0f)
            { triangles.Add(startIndex); triangles.Add(startIndex + 2); triangles.Add(startIndex + 1); }
            else
            { triangles.Add(startIndex); triangles.Add(startIndex + 1); triangles.Add(startIndex + 2); }
        }
        
        // Vertices there are no vertices, return null. Otherwise, create the mesh and return it
        if (vertices.Count == 0) return null;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    /*  IS MESH WATER-TIGHT FUNCTION                                                             */
    /*  - Checks that every edge in the mesh is connected to exactly 2 triangles                 */
    /*  - If all edges are connected to exactly 2 triangles, then return true, else return false */
    public static bool IsMeshWatertight(Mesh mesh)
    {
        // Get list of mesh vertices, triangles, and numbe rof edges
        Vector3[] verts = mesh.vertices;
        int[]     tris  = mesh.triangles;
        Dictionary<(long, long), int> edgeCount = new Dictionary<(long, long), int>(); // Map of edge to how many times the edge appears

        // For each edge in each triangle, create an unordered pair where the smaller key is first
        // If the same edge is shared between two triangles, it will still map to the same dictionary key
        for (int i = 0; i < tris.Length; i += 3)
            for (int e = 0; e < 3; e++)
            {
                long keyA = PositionKey(verts[tris[i + e]]);
                long keyB = PositionKey(verts[tris[i + (e + 1) % 3]]);
                var  key  = keyA < keyB ? (keyA, keyB) : (keyB, keyA);
                edgeCount[key] = edgeCount.TryGetValue(key, out int c) ? c + 1 : 1;
            }
        
        // If all edge counts is shared by exactly two triangles, then return true. Otherwise, return false
        return edgeCount.Values.All(c => c == 2);
    }

    /*  GET MESH VOLUME FUNCTION                                                                                             */
    /*  - Uses the divergence theorem to compute teh volume of the mesh from it's triangle                                   */
    /*  - Filters out degenerate cells that passed the water-tight check but is still too small to spawn as a physics object */
    public static float GetMeshVolume(Mesh mesh)
    {   
        // Get list of mesh vertices and triangles. Also initialise volume variable
        float    volume = 0f;
        Vector3[] verts = mesh.vertices;
        int[]     tris  = mesh.triangles;

        // For each triangle, calculate the volume
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];
            volume += Vector3.Dot(v0, Vector3.Cross(v1, v2));
        }
        // Returns absolute volume to ignore winding direction
        return Mathf.Abs(volume) / 6f;
    }

    /*  FILL OPEN BOUNDARIES FUNCTION                                                                                       */
    /*  - Fill in holes once CSG intersection has run                                                                       */
    /*  - Checks for mapping where edge A->B exists but not B->A and builds a linked list of all open boundary vertex       */
    /*  - Adds vertex at the center of the bounding loop to create triangles to fill the gap                                */
    /*  - Then checks the normals of the face to see if it is facing from the center. Flip triangles if not facing outwards */
    public static Mesh FillOpenBoundaries(Mesh mesh)
    {
        // Get list of vertices, triangles and uv of the mesh
        Vector3[] origVerts = mesh.vertices;
        int[]     origTris  = mesh.triangles;
        Vector2[] origUVs   = mesh.uv;

        // Stores every edge of every triangle in order
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

        // If there is no boundary vertices, return mesh
        if (boundaryNext.Count == 0) return mesh;

        // Boundary loops list and visited list to keep track of bounding
        List<List<int>> loops   = new List<List<int>>();
        HashSet<long>   visited = new HashSet<long>();

        //For each boundary vertex
        foreach (long startKey in boundaryNext.Keys)
        {

            // Bypass if already visited, if not add list and set current vertex to current
            if (visited.Contains(startKey)) continue;
            List<int> loop    = new List<int>();
            long      current = startKey;
            int       safety  = 0;

            // While not visited: add to visited and loop then go to the next one
            while (!visited.Contains(current) && safety++ < 10000)
            {
                visited.Add(current);
                loop.Add(keyToVertIndex[current]);
                if (!boundaryNext.TryGetValue(current, out long next)) break;
                current = next;
            }
            if (loop.Count >= 3) loops.Add(loop);
        }

        // Create a new list of vertices and triangles using the original vertices and triangles
        List<Vector3> newVerts = new List<Vector3>(origVerts);
        List<int>     newTris  = new List<int>(origTris);

        // Initialise a center vertex
        Vector3 meshCentroid = Vector3.zero;
        foreach (var v in origVerts) meshCentroid += v;
        meshCentroid /= Mathf.Max(1, origVerts.Length);

        // Calculate the ceneter of the boundary loop
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

        // Initialised new UVs and return the filled in mesh
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

    /*  ENSURE DOUBLE SIDED CAPS FUNCTION                                          */
    /*  - Final winding order checks that runs on spawned chunks                   */
    /*  - Re-examines all triangles on spawned chunks and flip any that are inward */
    public static Mesh EnsureDoubleSidedCaps(Mesh mesh)
    {
        // Gets the vertices, triangles, and UVs of the original mesh
        Vector3[] origVerts   = mesh.vertices;
        int[]     origTris    = mesh.triangles;
        Vector2[] origUVs     = mesh.uv;

        // Gets the center vertex of the chunk
        Vector3 centroid = Vector3.zero;
        foreach (var v in origVerts) centroid += v;
        centroid /= origVerts.Length;

        // Checks each triangle and if it points towards the centroid: add to bad triangles list
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

        // Then for each bad triangle, flip the triangles and add to a new 'fixed' list
        int[] fixedTris = (int[])origTris.Clone();
        foreach (int i in badTris)
        {
            int tmp = fixedTris[i + 1];
            fixedTris[i + 1] = fixedTris[i + 2];
            fixedTris[i + 2] = tmp;
        }

        // Create a mesh that uses the fixed mesh
        Mesh fixedMesh = new Mesh();
        fixedMesh.vertices  = origVerts;
        fixedMesh.triangles = fixedTris;
        fixedMesh.uv        = origUVs;
        fixedMesh.RecalculateNormals();
        fixedMesh.RecalculateBounds();
        return fixedMesh;
    }

    /*  POSITION KEY FUNCTION                                                  */
    /*  - Converts a Vector3 into a long integer                               */
    /*  - Used to compare vertex position by value instead of object reference */
    public static long PositionKey(Vector3 v)
    {
        int x = Mathf.RoundToInt(v.x * 10000f);
        int y = Mathf.RoundToInt(v.y * 10000f);
        int z = Mathf.RoundToInt(v.z * 10000f);
        return ((long)(x + 100000) * 1000000L + (y + 100000)) * 1000000L + (z + 100000);
    }
}