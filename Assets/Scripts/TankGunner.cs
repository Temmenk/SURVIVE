using UnityEngine;
using UnityEngine.InputSystem;

public class TankGunner : MonoBehaviour
{
    public enum AmmoType { SABOT, HEAT }
    public TankTurret tankTurret;
    public Camera gunnerCamera;
    public Transform muzzle;
    public LayerMask targetLayers = ~0;
    public float maxRange = 4000f;
    public float reloadSeconds = 5f;
    public int sabotRounds = 20;
    public int heatRounds = 15;
    public bool controlsEnabled = true;

    public AmmoType selectedAmmo = AmmoType.SABOT;
    float nextFireTime;
    public float lastLasedRange { get; private set; }

    void Update()
    {
        if (!controlsEnabled || !tankTurret || !gunnerCamera ||
            Mouse.current == null || Keyboard.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        tankTurret.Aim(delta.x * 0.01f, delta.y * 0.01f);

        if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedAmmo = AmmoType.SABOT;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) selectedAmmo = AmmoType.HEAT;
        if (Mouse.current.rightButton.wasPressedThisFrame) Lase();
        if (Mouse.current.leftButton.wasPressedThisFrame) Fire();
    }

    void Lase()
    {
        Ray ray = new Ray(gunnerCamera.transform.position, gunnerCamera.transform.forward);
        lastLasedRange = Physics.Raycast(ray, out RaycastHit hit, maxRange, targetLayers)
            ? hit.distance : maxRange;
        Debug.Log($"LASE: {lastLasedRange:F0} m");
    }

    void Fire()
    {
        if (Time.time < nextFireTime || muzzle == null) return;
        if (selectedAmmo == AmmoType.SABOT && sabotRounds <= 0) return;
        if (selectedAmmo == AmmoType.HEAT && heatRounds <= 0) return;

        if (selectedAmmo == AmmoType.SABOT) sabotRounds--;
        else heatRounds--;
        nextFireTime = Time.time + reloadSeconds;

        // Prototype: instant hit along bore axis, no ballistic drop.
        Ray ray = new Ray(muzzle.position, muzzle.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRange, targetLayers))
        {
            Debug.Log($"{selectedAmmo} hit {hit.collider.name}");
            hit.collider.SendMessage("OnTankHit", selectedAmmo.ToString(),
                                     SendMessageOptions.DontRequireReceiver);
        }
        else Debug.Log($"{selectedAmmo} fired: miss");
    }
}
