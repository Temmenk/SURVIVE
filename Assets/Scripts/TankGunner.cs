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
    [Header("Projectile Prefabs")]
    public TankProjectile sabotPrefab;
    public TankProjectile heatPrefab;
    public float sabotSpeed = 1500f;
    public float heatSpeed = 900f;

    [Header("Sight Zoom")]
    public float lowZoomFOV = 21.8f;
    public float highZoomFOV = 6.6f;
    bool highZoom;
    private bool cursorCaptured;

    public AmmoType selectedAmmo = AmmoType.SABOT;
    float nextFireTime;
    public float lastLasedRange { get; private set; }
    public bool hasLased { get; private set; }
    public Vector3 lasedTargetPoint { get; private set; }
    [Header("Sight-to-Bore Alignment")]
    public bool slewGunToLasedPoint = true;
    private bool aligningToLase;
    public float reloadRemaining => Mathf.Max(0f, nextFireTime - Time.time);
    public int selectedRounds => selectedAmmo == AmmoType.SABOT ? sabotRounds : heatRounds;

    void Start()
    {
        if (gunnerCamera != null) gunnerCamera.fieldOfView = lowZoomFOV;
    }

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
        // Manual aiming cancels automatic alignment to the last lased point.
        if (delta.sqrMagnitude > 0.001f)
        {
            aligningToLase = false;
            tankTurret.Aim(delta.x * mouseSensitivity, delta.y * mouseSensitivity);
        }
        else if (aligningToLase)
        {
            tankTurret.SlewTo(lasedTargetPoint);
        }

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            highZoom = !highZoom;
            gunnerCamera.fieldOfView = highZoom ? highZoomFOV : lowZoomFOV;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) selectedAmmo = AmmoType.SABOT;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) selectedAmmo = AmmoType.HEAT;
        if (Mouse.current.rightButton.wasPressedThisFrame) Lase();
        if (Mouse.current.leftButton.wasPressedThisFrame) Fire();
    }

    void Lase()
    {
        if (gunnerCamera == null) return;

        Ray sightRay = new Ray(gunnerCamera.transform.position,
                               gunnerCamera.transform.forward);
        if (Physics.Raycast(sightRay, out RaycastHit hit, maxRange,
                            targetLayers, QueryTriggerInteraction.Ignore))
        {
            lastLasedRange = hit.distance;
            lasedTargetPoint = hit.point;
            hasLased = true;
            aligningToLase = slewGunToLasedPoint;
            Debug.Log($"LASE: {lastLasedRange:F0} m on {hit.collider.name}");
        }
        else
        {
            lastLasedRange = maxRange;
            hasLased = false;
            aligningToLase = false;
            Debug.Log("LASE: NO RETURN");
        }
    }

    void Fire()
    {
        if (Time.time < nextFireTime || muzzle == null) return;
        if (selectedAmmo == AmmoType.SABOT && sabotRounds <= 0) return;
        if (selectedAmmo == AmmoType.HEAT && heatRounds <= 0) return;

        TankProjectile prefabCheck = selectedAmmo == AmmoType.SABOT ? sabotPrefab : heatPrefab;
        if (prefabCheck == null) { Debug.LogWarning("Assign both projectile prefabs first."); return; }

        if (selectedAmmo == AmmoType.SABOT) sabotRounds--;
        else heatRounds--;
        nextFireTime = Time.time + reloadSeconds;

        TankProjectile prefab = selectedAmmo == AmmoType.SABOT ? sabotPrefab : heatPrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"Assign the {selectedAmmo} projectile prefab in TankGunner!");
            return;
        }

        float speed = selectedAmmo == AmmoType.SABOT ? sabotSpeed : heatSpeed;
        TankProjectile projectile = Instantiate(prefab, muzzle.position, muzzle.rotation);
        projectile.Launch(muzzle.forward, speed, selectedAmmo, transform.root, targetLayers);
        Debug.Log($"{selectedAmmo} launched at {speed} m/s");
    }
}
