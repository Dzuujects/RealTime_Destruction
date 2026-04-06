using UnityEngine;

/*  CHUNK SPAWNER CLASS                                                                                                     */
/*  - Handles the physical instansiation of the finished chunks in the game world                                           */
/*  - Takes the mesh that is produced by the CSG and creates game objects of the the finished chunks based off the vertices */
public static class ChunkSpawner
{

    /*  SPAWN CHUNK FUNCTION                                                                    */
    /*  - Takes the mesh created by the CSG and initialises each chunk a game object            */
    /*  - Creates a game object, adding the original game objects position, rotation and scale  */
    /*  - Builds a mesh based on the chunk mesh, inverting the winding order if required        */
    /*  - Then adds all relevent components to the game object such as material, and rigid body */
    public static GameObject SpawnChunk(GameObject oriObject, Mesh chunkMesh, Material[] materials, Vector3 impactVelocity, int count)
    {
        //Creates the game object at the original objects world space
        GameObject chunk = new GameObject("FracturedChunk");
        chunk.transform.position   = oriObject.transform.position;
        chunk.transform.rotation   = oriObject.transform.rotation;
        chunk.transform.localScale = oriObject.transform.localScale;

        // Get's the original chunk mesh's vertices and makes an identical list to transform it to local space
        Vector3[] vertices      = chunkMesh.vertices;
        Vector3[] localVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            localVertices[i] = chunk.transform.InverseTransformPoint(vertices[i]);

        // Makes triangles based on the chunk mesh's list of vertices
        int[] triangles   = chunkMesh.triangles;
        bool  negativeScale = chunk.transform.lossyScale.x *
                              chunk.transform.lossyScale.y *
                              chunk.transform.lossyScale.z < 0;
        
        // If negative scale, then that means the winding order is reversed. So correct it so that the triangle is facing outwards 
        if (negativeScale)
            for (int i = 0; i < triangles.Length; i += 3)
            { int tmp = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = tmp; }

        // Create the mesh in local space so that the chunks do not teleport in a random direction
        Mesh localChunkMesh = new Mesh();
        localChunkMesh.vertices  = localVertices;
        localChunkMesh.triangles = triangles;
        localChunkMesh.uv        = chunkMesh.uv;
        localChunkMesh.RecalculateNormals();
        localChunkMesh.RecalculateBounds();

        // Final check that local mesh is correctly facing outwards
        localChunkMesh = MeshUtilities.EnsureDoubleSidedCaps(localChunkMesh);

        // Add components to the game object
        chunk.AddComponent<MeshFilter>().mesh          = localChunkMesh; // Mesh itself
        chunk.AddComponent<MeshRenderer>().materials   = materials; // Material of the chunk
        chunk.AddComponent<MeshCollider>().convex      = true; // Collider for the model based of the mesh
        Rigidbody rb  = chunk.AddComponent<Rigidbody>(); // Rigidbody to add physics
        
        rb.isKinematic = false; // Enable rigidbody
        Rigidbody oriRb = oriObject.GetComponent<Rigidbody>();
        if (oriRb != null) rb.mass = oriRb.mass / count; // Make the rigidbody be a fraction of the original mesh
        rb.velocity = impactVelocity / 2f; // Add velocity so that it looks like it broke in a direction
        
        // Return the chunk
        return chunk;
    }
}