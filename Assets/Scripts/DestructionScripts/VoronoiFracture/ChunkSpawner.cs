using UnityEngine;

public static class ChunkSpawner
{
    public static GameObject SpawnChunk(GameObject oriObject, Mesh chunkMesh, Material[] materials, Vector3 impactVelocity, int count)
    {
        GameObject chunk = new GameObject("FracturedChunk");
        chunk.transform.position   = oriObject.transform.position;
        chunk.transform.rotation   = oriObject.transform.rotation;
        chunk.transform.localScale = oriObject.transform.localScale;

        Vector3[] vertices      = chunkMesh.vertices;
        Vector3[] localVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            localVertices[i] = chunk.transform.InverseTransformPoint(vertices[i]);

        int[] triangles   = chunkMesh.triangles;
        bool  negativeScale = chunk.transform.lossyScale.x *
                              chunk.transform.lossyScale.y *
                              chunk.transform.lossyScale.z < 0;

        if (negativeScale)
            for (int i = 0; i < triangles.Length; i += 3)
            { int tmp = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = tmp; }

        Mesh localChunkMesh = new Mesh();
        localChunkMesh.vertices  = localVertices;
        localChunkMesh.triangles = triangles;
        localChunkMesh.uv        = chunkMesh.uv;
        localChunkMesh.RecalculateNormals();
        localChunkMesh.RecalculateBounds();

        localChunkMesh = MeshUtilities.EnsureDoubleSidedCaps(localChunkMesh);

        chunk.AddComponent<MeshFilter>().mesh          = localChunkMesh;
        chunk.AddComponent<MeshRenderer>().materials   = materials;
        chunk.AddComponent<MeshCollider>().convex      = true;

        Rigidbody rb  = chunk.AddComponent<Rigidbody>();
        rb.isKinematic = false;

        Rigidbody oriRb = oriObject.GetComponent<Rigidbody>();
        if (oriRb != null) rb.mass = oriRb.mass / count;

        rb.velocity = impactVelocity / 2f;
        return chunk;
    }
}