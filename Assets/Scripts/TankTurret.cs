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
    float sightElevation;
    float ballisticOffset; // Retained until the next successful lase or commander designation.

    public void Aim(float yawInput, float pitchInput)
    {
        if (!turret || !gun) return;
        turret.Rotate(0f, yawInput * traverseSpeed * Time.deltaTime, 0f, Space.Self);
        sightElevation = Mathf.Clamp(sightElevation - pitchInput * elevationSpeed * Time.deltaTime,
                                     minElevation, maxElevation);
        elevation = Mathf.Clamp(sightElevation + ballisticOffset, minElevation, maxElevation);
        SyncSight();
    }

    // Slew turret and elevate gun so the muzzle points toward the lased world point.
    // This handles geometric sight-to-bore offset; ballistic drop comes later.
    public void SlewTo(Vector3 worldPoint)
    {
        if (!turret || !gun) return;
        ballisticOffset = 0f; // Commander designation overrides the stored correction.

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

    // Adjust barrel pitch only; turret yaw and GPS direction remain unchanged.
    public bool SetBallisticElevation(Vector3 target, Vector3 muzzlePosition, float speed, float gravity)
    {
        if (!turret || !gun || speed <= 0f) return false;
        Vector3 difference = target - muzzlePosition;
        float forwardDistance = Vector3.Dot(difference, turret.forward);
        if (forwardDistance <= 0.01f) return false;

        float height = difference.y;
        float angle;
        if (gravity <= 0.0001f)
        {
            angle = Mathf.Atan2(height, forwardDistance);
        }
        else
        {
            float v2 = speed * speed;
            float discriminant = v2 * v2 - gravity * (gravity * forwardDistance * forwardDistance + 2f * height * v2);
            if (discriminant < 0f) return false;
            angle = Mathf.Atan((v2 - Mathf.Sqrt(discriminant)) / (gravity * forwardDistance));
        }

        float desiredPitch = -angle * Mathf.Rad2Deg;
        if (desiredPitch < minElevation || desiredPitch > maxElevation) return false;
        ballisticOffset = desiredPitch - sightElevation;
        elevation = desiredPitch;
        gun.localRotation = Quaternion.Euler(elevation, 0f, 0f);
        return true;
    }

    public void ClearBallisticSolution()
    {
        ballisticOffset = 0f;
        elevation = sightElevation;
        if (gun != null) gun.localRotation = Quaternion.Euler(elevation, 0f, 0f);
    }

    void SyncSight()
    {
        gun.localRotation = Quaternion.Euler(elevation, 0f, 0f);
        if (gunnerSightPivot != null)
            gunnerSightPivot.localRotation = Quaternion.Euler(sightElevation, 0f, 0f);
    }
}
