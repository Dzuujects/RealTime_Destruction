using UnityEngine;

public class CollisionDebugger : MonoBehaviour
{
    [Header("Debugging")]
    public bool drawContactNormals = true;
    public bool drawImpactForce = true;
    public float drawDuration = 5f;

    [Header("Impact Checks")]
    public float minSpeed = 1f;
    public float impulseThreshold = 75f;

    [Header("Break settings")]
    public bool Cuttable = true;
    public int seedCount;
    private Rigidbody rb;

    private bool hasBeenSliced = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
   
    private void OnCollisionEnter(Collision coll)
    {
        if (hasBeenSliced) return;

        Vector3 relativeVelocity = coll.relativeVelocity;
        float speedOnImpact = relativeVelocity.magnitude;

        if (speedOnImpact < minSpeed) return;

        float impulseMagnitude = rb.mass * speedOnImpact;
        
        ContactPoint contact = coll.contacts[0];
        Vector3 contactLocation = contact.point;

        if (drawContactNormals)
            Debug.DrawRay(contactLocation, contact.normal, Color.red, drawDuration);
        
        if (drawImpactForce)
            Debug.DrawRay(contactLocation, relativeVelocity.normalized * speedOnImpact, Color.yellow, drawDuration);

        if (impulseMagnitude >= impulseThreshold)
        {
            Debug.Log("Impact Threshold Met. Breaking object");
            hasBeenSliced = true;
            
            if (Cuttable)
            MeshSlicer.Slice(gameObject,contactLocation,Vector3.up);

            else
            VoronoiFracture.Fracture(gameObject, contactLocation, relativeVelocity, seedCount);
        }
    }
}
