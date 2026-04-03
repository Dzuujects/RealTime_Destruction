using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireProjectile : MonoBehaviour
{
    public Camera playerCamera;
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SpawnProjectile();
        }
    }

    void SpawnProjectile()
    {
        // Spawn position slightly in front of camera to avoid collision
        Vector3 spawnPos = playerCamera.transform.position + 
                           playerCamera.transform.forward * 0.5f;

        GameObject projectile = Instantiate(
            projectilePrefab,
            spawnPos,
            Quaternion.LookRotation(playerCamera.transform.forward)
        );

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        rb.velocity = playerCamera.transform.forward * projectileSpeed;
    }
}

