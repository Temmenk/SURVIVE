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
    [Range(0.01f, 2f)] public float mouseSensitivity = 0.5f;
    private bool cursorCaptured;

    public AmmoType selectedAmmo = AmmoType.SABOT;
    float nextFireTime;
    public float lastLasedRange { get; private set; }
    public bool hasLased { get; private set; }
    public float reloadRemaining => Mathf.Max(0f, nextFireTime - Time.time);
    public int selectedRounds => selectedAmmo == AmmoType.SABOT ? sabotRounds : heatRounds;

    void OnEnable()
    {
        if (controlsEnabled) CaptureCursor();
    }

    void OnDisable()
    {
        ReleaseCursor();
    }

    void CaptureCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorCaptured = true;
    }

    void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorCaptured = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReleaseCursor();
            return;
        }

        if (!controlsEnabled)
        {
            if (cursorCaptured) ReleaseCursor();
            return;
        }

        // Click the Game view to recapture the mouse after Escape.
        if (!cursorCaptured && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            CaptureCursor();
            return;
        }

        if (!cursorCaptured) return;

        if (!controlsEnabled || !tankTurret || !gunnerCamera ||
            Mouse.current == null || Keyboard.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        tankTurret.Aim(delta.x * mouseSensitivity, delta.y * mouseSensitivity);

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
        hasLased = true;
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
            TankHitbox hitbox = hit.collider.GetComponent<TankHitbox>();
            if (hitbox != null)
                hitbox.ReceiveHit(selectedAmmo);
            else
                Debug.Log("Hit collider has no TankHitbox: " + hit.collider.name);
        }
        else Debug.Log($"{selectedAmmo} fired: miss");
    }
}
