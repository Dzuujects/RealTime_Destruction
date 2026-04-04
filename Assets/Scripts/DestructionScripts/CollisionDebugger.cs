using UnityEngine;

/*                        COLLISION DEBUGGER                       */
/*  - Component attached to the model                              */
/*  - Used to debug collision and activate the fracture algorithm  */

public class CollisionDebugger : MonoBehaviour
{
    // Debugging Variables for Visualising Force direction and magnitude
    [Header("Debugging")]
    public bool drawContactNormals = true;
    public bool drawImpactForce = true;
    public float drawDuration = 5f;

    // Threshold Variables to change according to materials and size
    [Header("Impact Checks")]
    public float minSpeed = 1f;
    public float impulseThreshold = 75f;

    // Break Setting to adjust for material and type of break
    [Header("Break settings")]
    public bool Cuttable = false;
    public int seedCount;

    // Private Variables to get rigidbody of model and to not break again
    private Rigidbody rb;
    private bool hasBeenSliced = false;

    // Awake function to get the rigidbody of the model
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
   
    /*                             COLLISION DETECTION FUNCTION                               */
    /*  - Uses Unity Engine's built in collision detection (Nvidia PhysX Physics Engine)      */
    /*  - On collision, get the impulse force of the incoming projectile                      */
    /*  - If the force is greater than the minimum speed and the impulse threshold then break */
    private void OnCollisionEnter(Collision coll)
    {
        // If true, stops mutiple collision detections leading to overflows
        if (hasBeenSliced) return;

        // Gets the maginitude and direction of projectile velocity
        Vector3 relativeVelocity = coll.relativeVelocity;
        float speedOnImpact = relativeVelocity.magnitude;

        // Stops low speed projectiles with high masses from fracturing the model
        if (speedOnImpact < minSpeed) return;

        // Calculate impulse
        float impulseMagnitude = rb.mass * speedOnImpact;
        
        // Gets contact locations on the mesh
        ContactPoint contact = coll.contacts[0];
        Vector3 contactLocation = contact.point;

        // Debugging checks, if true then draw lines on screen
        if (drawContactNormals)
            Debug.DrawRay(contactLocation, contact.normal, Color.red, drawDuration);
        if (drawImpactForce)
            Debug.DrawRay(contactLocation, relativeVelocity.normalized * speedOnImpact, Color.yellow, drawDuration);

        // If impulse is greater than threshold, break system
        if (impulseMagnitude >= impulseThreshold)
        {
            Debug.Log("Impact Threshold Met. Breaking object");
            hasBeenSliced = true;
            
            // Cuttable is a check on impacts working. Would create a horizontal cut at contact location if true
            if (Cuttable)
            MeshSlicer.Slice(gameObject,contactLocation,Vector3.up);

            // If not, run voronoi fracture algorithm
            else
            VoronoiFracture.Fracture(gameObject, contactLocation, relativeVelocity, seedCount);
        }
    }
}
