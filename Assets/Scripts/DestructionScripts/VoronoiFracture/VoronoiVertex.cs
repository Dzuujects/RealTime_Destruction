using UnityEngine;
using MIConvexHull;

public class VoronoiVertex : IVertex
{
    public double[] Position { get; set; }
    public Vector3 toVector3() => new Vector3((float)Position[0], (float)Position[1], (float)Position[2]);
    public VoronoiVertex(double x, double y, double z)
    {
        Position = new double[3] { x, y, z };
    }
}
