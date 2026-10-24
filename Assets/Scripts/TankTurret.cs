using UnityEngine;

public class TankTurret : MonoBehaviour
{
    public Transform turret;
    public Transform gun;
    public Transform gunnerSightPivot; // Child of Turret, at GPS doghouse
    public float traverseSpeed = 45f;
    public float elevationSpeed = 25f;
    public float minElevation = -10f, maxElevation = 20f;
    float elevation;

    public void Aim(float yawInput, float pitchInput)
    {
        if (!turret || !gun) return;
        turret.Rotate(0f, yawInput * traverseSpeed * Time.deltaTime, 0f, Space.Self);
        elevation = Mathf.Clamp(elevation - pitchInput * elevationSpeed * Time.deltaTime,
                                minElevation, maxElevation);
        SyncSight();
    }

    // Slew turret and elevate gun so the muzzle points toward the lased world point.
    // This handles geometric sight-to-bore offset; ballistic drop comes later.
    public void SlewTo(Vector3 worldPoint)
    {
        if (!turret || !gun) return;

        Vector3 flatDirection = worldPoint - turret.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.001f) return;

        // Convert world heading to local turret yaw, respecting Abrams rotation.
        Quaternion worldYaw = Quaternion.LookRotation(flatDirection, Vector3.up);
        Quaternion parentRotation = turret.parent ? turret.parent.rotation : Quaternion.identity;
        Quaternion localYaw = Quaternion.Inverse(parentRotation) * worldYaw;
        turret.localRotation = Quaternion.RotateTowards(
            turret.localRotation, localYaw, traverseSpeed * Time.deltaTime);

        // Recalculate pitch after turret rotation, from the muzzle's actual position.
        // Gun local +Z is the barrel's forward direction.
        Vector3 source = gun.position;
        Vector3 localTarget = turret.InverseTransformDirection(worldPoint - source);
        float desiredElevation = -Mathf.Atan2(localTarget.y,
            Mathf.Sqrt(localTarget.x * localTarget.x + localTarget.z * localTarget.z))
            * Mathf.Rad2Deg;
        elevation = Mathf.MoveTowards(elevation,
            Mathf.Clamp(desiredElevation, minElevation, maxElevation),
            elevationSpeed * Time.deltaTime);
        SyncSight();
    }

    void SyncSight()
    {
        gun.localRotation = Quaternion.Euler(elevation, 0f, 0f);
        if (gunnerSightPivot != null)
            gunnerSightPivot.localRotation = Quaternion.Euler(elevation, 0f, 0f);
    }
}
