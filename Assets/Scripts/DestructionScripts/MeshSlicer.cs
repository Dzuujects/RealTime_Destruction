using System.Collections.Generic;
using UnityEngine;

/*  MESH SLICER                                                                    */
/*  - Runs when collision debugger calls Slice() function if model is cuttable     */
/*  - Creates a horizontal cut through the mesh spltting the model into two pieces */
public class MeshSlicer : MonoBehaviour
{
    /*  MESH BUILDER                                                                  */
    /*  - A private helper class                                                      */
    /*  - accumulates raw mesh data into lists before converting it into a Unity Mesh */
    private class MeshBuilder
    {
        //List of Verticeis, Normals, and UVs to make Triangles as well as a list of Triangles to make the mesh
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector2> UVs = new List<Vector2>();
        public List<int> Triangles = new List<int>();

        // Appends one triangle's worth of data. 
        // baseIndex records where in the vertex list this triangle starts, so the triangle indices correctly point to the three new vertices just added.
        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 n1, Vector3 n2, Vector3 n3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            int baseIndex = Vertices.Count;
            Vertices.Add(v1); Vertices.Add(v2); Vertices.Add(v3);
            Normals.Add(n1); Normals.Add(n2); Normals.Add(n3);
            UVs.Add(uv1); UVs.Add(uv2); UVs.Add(uv3);
            Triangles.Add(baseIndex); Triangles.Add(baseIndex + 1); Triangles.Add(baseIndex + 2);
        }

        // Converts the accumulated lists into an actual Unity Mesh object. 
        // RecalculateBounds updates the mesh's bounding box, which Unity needs for rendering culling and physics.
        public Mesh ToMesh()
        {
            Mesh m = new Mesh();
            m.vertices = Vertices.ToArray();
            m.normals = Normals.ToArray();
            m.uv = UVs.ToArray();
            m.triangles = Triangles.ToArray();
            m.RecalculateBounds();
            return m;
        }
    }

    /* SLICE FUNCTION                                                    */
    /*  - Main Slicing Algorithm                                         */
    /*  - Creates a plane at contact location                            */
    /*  - Finds which side of the plane the vertices are on              */
    /*  - Generate two new meshes according to vertices position         */
    public static void Slice(GameObject target, Vector3 slicePoint, Vector3 sliceNormal)
    {
        MeshFilter sourceFilter = target.GetComponent<MeshFilter>();
        if (sourceFilter == null) return;
        
        Mesh sourceMesh = sourceFilter.mesh;
        
        // Convert plane to Local Space so we don't have to transform every vertex
        Vector3 localPoint = target.transform.InverseTransformPoint(slicePoint);
        Vector3 localNormal = target.transform.InverseTransformDirection(sliceNormal).normalized;

        MeshBuilder posSide = new MeshBuilder(); // Top
        MeshBuilder negSide = new MeshBuilder(); // Bottom
        
        // Loop through the original triangles
        int[] triangles = sourceMesh.triangles;
        Vector3[] verts = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;
        Vector2[] uvs = sourceMesh.uv;

        // Cap data
        List<Vector3> capVerts = new List<Vector3>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Get triangle data
            Vector3 v1 = verts[triangles[i]];
            Vector3 v2 = verts[triangles[i+1]];
            Vector3 v3 = verts[triangles[i+2]];
            
            // Determine which side of the plane each vertex is on
            bool s1 = GetSide(v1, localPoint, localNormal);
            bool s2 = GetSide(v2, localPoint, localNormal);
            bool s3 = GetSide(v3, localPoint, localNormal);

            // CASE 1: All vertices on one side
            if (s1 == s2 && s2 == s3)
            {
                MeshBuilder targetSide = s1 ? posSide : negSide;
                targetSide.AddTriangle(v1, v2, v3, normals[triangles[i]], normals[triangles[i+1]], normals[triangles[i+2]], uvs[triangles[i]], uvs[triangles[i+1]], uvs[triangles[i+2]]);
                continue;
            }

            // CASE 2: Intersection - cut the triangle
            // Arrange vertices so that A is on one side, B and C are on the other side
            Vector3 A, B, C;
            Vector3 nA, nB, nC;
            Vector2 uvA, uvB, uvC;
            MeshBuilder sideA, sideBC;

            if (s1 != s2 && s1 != s3) // v1 is alone, v1 = A
            { 
                A = v1; B = v2; C = v3; 
                nA = normals[triangles[i]]; nB = normals[triangles[i+1]]; nC = normals[triangles[i+2]];
                uvA = uvs[triangles[i]]; uvB = uvs[triangles[i+1]]; uvC = uvs[triangles[i+2]];
                sideA = s1 ? posSide : negSide; sideBC = s1 ? negSide : posSide;
            }
            else if (s2 != s1 && s2 != s3) // v2 is alone, v2 = A
            { 
                A = v2; B = v3; C = v1;
                nA = normals[triangles[i+1]]; nB = normals[triangles[i+2]]; nC = normals[triangles[i]];
                uvA = uvs[triangles[i+1]]; uvB = uvs[triangles[i+2]]; uvC = uvs[triangles[i]];
                sideA = s2 ? posSide : negSide; sideBC = s2 ? negSide : posSide;
            }
            else // v3 is alone, v1 = A
            { 
                A = v3; B = v1; C = v2;
                nA = normals[triangles[i+2]]; nB = normals[triangles[i]]; nC = normals[triangles[i+1]];
                uvA = uvs[triangles[i+2]]; uvB = uvs[triangles[i]]; uvC = uvs[triangles[i+1]];
                sideA = s3 ? posSide : negSide; sideBC = s3 ? negSide : posSide;
            }

            // Calculate intersection points AB and AC
            float t1 = GetT(A, B, localPoint, localNormal);
            float t2 = GetT(A, C, localPoint, localNormal);

            Vector3 I1 = Vector3.Lerp(A, B, t1); // Intersection on AB edge
            Vector3 I2 = Vector3.Lerp(A, C, t2); // Intersection on AC edge
            
            // Interpolate normals and UVs for the new points
            Vector3 nI1 = Vector3.Lerp(nA, nB, t1);
            Vector3 nI2 = Vector3.Lerp(nA, nC, t2);
            Vector2 uvI1 = Vector2.Lerp(uvA, uvB, t1);
            Vector2 uvI2 = Vector2.Lerp(uvA, uvC, t2);

            // Add the "Tip" triangle (A -> I1 -> I2) to side A
            sideA.AddTriangle(A, I1, I2, nA, nI1, nI2, uvA, uvI1, uvI2);

            // Add the "Base" quad (split into 2 triangles) to side BC
            // Tri 1: B -> C -> I1
            sideBC.AddTriangle(B, C, I1, nB, nC, nI1, uvB, uvC, uvI1);
            // Tri 2: C -> I2 -> I1
            sideBC.AddTriangle(C, I2, I1, nC, nI2, nI1, uvC, uvI2, uvI1);

            // Store intersection points for the Cap
            capVerts.Add(I1);
            capVerts.Add(I2);
        }

        // Generate the new Objects
        ReplaceObject(target, posSide, "Top");
        ReplaceObject(target, negSide, "Bottom");
        
        // Destroy the original
        Object.Destroy(target);
    }

    // Dot product > 0 means we are on the side the normal is pointing to
    private static bool GetSide(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
    {

        return Vector3.Dot(point - planePoint, planeNormal) >= 0;
    }

    // Calculate the interpolation factor 'T' (0 to 1) where line crosses plane
    private static float GetT(Vector3 p1, Vector3 p2, Vector3 planePoint, Vector3 planeNormal)
    {
        float d1 = Vector3.Dot(p1 - planePoint, planeNormal);
        float d2 = Vector3.Dot(p2 - planePoint, planeNormal);
        return d1 / (d1 - d2);
    }

    // Recreates the meshes two halfs the and adds all required components
    private static void ReplaceObject(GameObject original, MeshBuilder builder, string suffix)
    {
        if (builder.Vertices.Count == 0) return;

        Mesh newMesh = builder.ToMesh();
        GameObject obj = new GameObject(original.name + "_" + suffix);
        obj.transform.position = original.transform.position;
        obj.transform.rotation = original.transform.rotation;
        obj.transform.localScale = original.transform.localScale;

        obj.AddComponent<MeshFilter>().mesh = newMesh;
        
        // Copy materials
        MeshRenderer originalRenderer = original.GetComponent<MeshRenderer>();
        MeshRenderer newRenderer = obj.AddComponent<MeshRenderer>();
        newRenderer.materials = originalRenderer.materials;

        // Add Physics - use a MeshCollider for accurate fragment shape
        MeshCollider mc = obj.AddComponent<MeshCollider>();
        mc.sharedMesh = newMesh;
        mc.convex = true; // required for MeshCollider with Rigidbody

        Rigidbody rb = obj.AddComponent<Rigidbody>();
        var origRb = original.GetComponent<Rigidbody>();
        if (origRb != null)
        {
            rb.mass = origRb.mass * 0.5f;
            rb.velocity = origRb.velocity;
            rb.angularVelocity = origRb.angularVelocity;
            rb.interpolation = origRb.interpolation;
        }
        else
        {
            rb.mass = 1f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        CollisionDebugger collDebug = obj.AddComponent<CollisionDebugger>();
    }   
}
