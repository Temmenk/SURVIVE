using UnityEngine;

public class TankTurret : MonoBehaviour
{
    public Transform turret;
    public Transform gun;
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
        gun.localRotation = Quaternion.Euler(elevation, 0f, 0f);
    }

    public void SlewTo(Vector3 worldPoint)
    {
        if (!turret || !gun) return;
        Vector3 flatDirection = worldPoint - turret.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.01f) return;
        Quaternion target = Quaternion.LookRotation(flatDirection, Vector3.up);
        turret.rotation = Quaternion.RotateTowards(turret.rotation, target,
                                                   traverseSpeed * Time.deltaTime);
        Vector3 localTarget = turret.InverseTransformPoint(worldPoint);
        float desiredElevation = -Mathf.Atan2(localTarget.y, localTarget.z) * Mathf.Rad2Deg;
        elevation = Mathf.MoveTowards(elevation, Mathf.Clamp(desiredElevation,
                                    minElevation, maxElevation), elevationSpeed * Time.deltaTime);
        gun.localRotation = Quaternion.Euler(elevation, 0f, 0f);
    }
}
