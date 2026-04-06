using System;
using System.Collections.Generic;
using UnityEngine;

/*  VORONOI SEED GENERATOR CLASS                                                                                                */
/*  - Generate Seeds and Circumcenters on the mesh to be sliced later                                                           */
/*  - It firsts generates seeds inside the models bounding box being biased near the contact location                           */
/*  - Then it uses Delaunay Triangulation to create vertices around the seed locations leading to a voronoi diagram             */
public static class VoronoiSeedGenerator
{
    /*  GENERATE SEED FUNCTION                                                                                                  */
    /*  - Creates a new list to store all the seed locations                                                                    */
    /*  - For each number of seeds, find a random location in the bounding box of the model adding bias to the contact location */
    /*  - Add to the list of locations, then return the list                                                                    */
    public static List<VoronoiVertex> GenerateSeeds(Bounds bounds, Transform objTransform, Vector3 impactLocation, int count)
    {
        List<VoronoiVertex> locations = new List<VoronoiVertex>(); // List to store seed locations
        Vector3 localImpact = objTransform.InverseTransformPoint(impactLocation); // get impact location to add bias

        // Create a seed location for the number of seeds you want and add to the list of locations
        for (int i = 0; i < count; i++)
        {
            // Find random x,y,z locations
            Vector3 randomLocation = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );
            // Add bias to the contact location
            Vector3 biasedLocation = Vector3.Lerp(randomLocation, localImpact, UnityEngine.Random.Range(0f, 0.8f));

            // Add to list of seed locataions
            locations.Add(new VoronoiVertex(biasedLocation.x, biasedLocation.y, biasedLocation.z));
        }
        return locations;
    }

    /*  COMPUTE CIRCUMCENTER FUNCTION                                                               */
    /*  - Uses Delaunay Triangulation to compute vertices that would be used to create the cells    */
    /*  - Checks if tetrahedron has less that 4 vertices. If true: return null                      */
    /*  - Translate problem so that vert[0] is origin making calculations easier                    */
    /*  - Calculate the numerator and denominator and use them to calculate the circumcenter        */
    /*  - Return circumcenter as a VoronoiVertex                                                    */
    public static VoronoiVertex ComputeCircumcenter(VoronoiVertex[] verts)
    {
        // Verts < 4 return null, else continue
        if (verts.Length < 4) return null;

        // Make verts[0] be origin for coordinates a, b, and c,
        double ax = verts[1].Position[0] - verts[0].Position[0];
        double ay = verts[1].Position[1] - verts[0].Position[1];
        double az = verts[1].Position[2] - verts[0].Position[2];

        double bx = verts[2].Position[0] - verts[0].Position[0];
        double by = verts[2].Position[1] - verts[0].Position[1];
        double bz = verts[2].Position[2] - verts[0].Position[2];

        double cx = verts[3].Position[0] - verts[0].Position[0];
        double cy = verts[3].Position[1] - verts[0].Position[1];
        double cz = verts[3].Position[2] - verts[0].Position[2];

        // Calculate the denominator, d, being doubled the volume of the tetraherdron
        double d = 2.0 * (ax * (by * cz - bz * cy)
                        - ay * (bx * cz - bz * cx)
                        + az * (bx * cy - by * cx));

        // If d is nearly 0, return null.
        if (Math.Abs(d) < 1e-10) return null;

        // Calculate the squared magnitude of each three vectors
        double a2 = ax*ax + ay*ay + az*az;
        double b2 = bx*bx + by*by + bz*bz;
        double c2 = cx*cx + cy*cy + cz*cz;

        // Use d and sqaured magnitude to calculate circumcenter
        double ux = (a2 * (by*cz - bz*cy) - b2 * (ay*cz - az*cy) + c2 * (ay*bz - az*by)) / d;
        double uy = (a2 * (bz*cx - bx*cz) - b2 * (az*cx - ax*cz) + c2 * (az*bx - ax*bz)) / d;
        double uz = (a2 * (bx*cy - by*cx) - b2 * (ax*cy - ay*cx) + c2 * (ax*by - ay*bx)) / d;

        // Return the location as a VoronoiVertex
        return new VoronoiVertex(
            verts[0].Position[0] + ux,
            verts[0].Position[1] + uy,
            verts[0].Position[2] + uz
        );
    }
}