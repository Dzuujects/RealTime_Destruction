using UnityEngine;
using MIConvexHull;

/*  VORONOI VERTEX INTERFACE CLASS                                                                                      */
/*  - Data container representing a 3D point in space                                                                   */
/*  - Used to bridge between MIConvexHUll and Unity differing coordinate representations                                */
/*  - Implements IVertex interface from MIConvexHull and uses a toVector3() to allow Unity to handle without issues     */
public class VoronoiVertex : IVertex
{
    public double[] Position { get; set; }
    public Vector3 toVector3() => new Vector3((float)Position[0], (float)Position[1], (float)Position[2]);
    public VoronoiVertex(double x, double y, double z)
    {
        Position = new double[3] { x, y, z };
    }
}
