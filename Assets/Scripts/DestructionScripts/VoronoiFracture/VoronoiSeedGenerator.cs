using System;
using System.Collections.Generic;
using UnityEngine;

public static class VoronoiSeedGenerator
{
    public static List<VoronoiVertex> GenerateSeeds(Bounds bounds, Transform objTransform, Vector3 impactLocation, int count)
    {
        List<VoronoiVertex> locations = new List<VoronoiVertex>();
        Vector3 localImpact = objTransform.InverseTransformPoint(impactLocation);

        for (int i = 0; i < count; i++)
        {
            Vector3 randomLocation = new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );
            Vector3 biasedLocation = Vector3.Lerp(randomLocation, localImpact, UnityEngine.Random.Range(0f, 0.8f));
            locations.Add(new VoronoiVertex(biasedLocation.x, biasedLocation.y, biasedLocation.z));
        }
        return locations;
    }

    public static VoronoiVertex ComputeCircumcenter(VoronoiVertex[] verts)
    {
        if (verts.Length < 4) return null;

        double ax = verts[1].Position[0] - verts[0].Position[0];
        double ay = verts[1].Position[1] - verts[0].Position[1];
        double az = verts[1].Position[2] - verts[0].Position[2];

        double bx = verts[2].Position[0] - verts[0].Position[0];
        double by = verts[2].Position[1] - verts[0].Position[1];
        double bz = verts[2].Position[2] - verts[0].Position[2];

        double cx = verts[3].Position[0] - verts[0].Position[0];
        double cy = verts[3].Position[1] - verts[0].Position[1];
        double cz = verts[3].Position[2] - verts[0].Position[2];

        double d = 2.0 * (ax * (by * cz - bz * cy)
                        - ay * (bx * cz - bz * cx)
                        + az * (bx * cy - by * cx));

        if (Math.Abs(d) < 1e-10) return null;

        double a2 = ax*ax + ay*ay + az*az;
        double b2 = bx*bx + by*by + bz*bz;
        double c2 = cx*cx + cy*cy + cz*cz;

        double ux = (a2 * (by*cz - bz*cy) - b2 * (ay*cz - az*cy) + c2 * (ay*bz - az*by)) / d;
        double uy = (a2 * (bz*cx - bx*cz) - b2 * (az*cx - ax*cz) + c2 * (az*bx - ax*bz)) / d;
        double uz = (a2 * (bx*cy - by*cx) - b2 * (ax*cy - ay*cx) + c2 * (ax*by - ay*bx)) / d;

        return new VoronoiVertex(
            verts[0].Position[0] + ux,
            verts[0].Position[1] + uy,
            verts[0].Position[2] + uz
        );
    }
}