using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Photon.Pun;
using Unity.Mathematics;
using TMPro;

public class Weapon : MonoBehaviour
{
    [Header("Weapon Stats")]
    public float fireRate = 10f;
    public float damagePerShot = 25f;
    public float hitScanDistance = 500f;

    [Header("Munición")]
    public int maxAmmo = 30;
    public int reserveAmmo = 90;
    public float reloadTime = 2f;

    [Header("Partículas")]
    public GameObject efectoMetal;
    public ParticleSystem casingParticles;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    [Header("Tracer")]
    public LineRenderer bulletTracer;
    public float tracerDuration = 0.05f;

    private int currentAmmo;
    private float timeUntilAllowNextShot;
    private bool isReloading;

    void Start()
    {
        currentAmmo = maxAmmo;
        if (bulletTracer != null) bulletTracer.enabled = false;
        UpdateAmmoUI();
    }

    void Update()
    {
        // 1. Freno si recargamos
        if (isReloading) return;

        // 2. Freno de pausa (no hacer nada si el menú está abierto)
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;

        // 3. Freno de UI (no disparar si el ratón está sobre un botón del canvas)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // --- Aquí empieza tu código de disparo normal ---
        timeUntilAllowNextShot = math.max(0, timeUntilAllowNextShot - Time.deltaTime);

        if (Mouse.current.leftButton.isPressed && timeUntilAllowNextShot <= 0)
        {
            if (currentAmmo > 0)
            {
                HitScanShoot();
                timeUntilAllowNextShot = 1f / fireRate;
            }
            else
            {
                StartReload();
            }
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
            StartReload();
    }
    void HitScanShoot()
    {
        currentAmmo--;
        UpdateAmmoUI();

        if (casingParticles != null)
            casingParticles.Emit(1);

        // Un solo Raycast
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        bool golpeo = Physics.Raycast(ray, out RaycastHit hit, hitScanDistance);

        // Endpoint del tracer
        Vector3 endPoint = golpeo
            ? hit.point
            : ray.origin + ray.direction * hitScanDistance;

        if (bulletTracer != null)
            StartCoroutine(ShowTracer(ray.origin, endPoint));

        if (golpeo)
        {
            Debug.Log("Golpeaste: " + hit.transform.name);

            PlayerHealth ph = hit.transform.GetComponentInParent<PlayerHealth>();
            if (ph != null)
                ph.photonView.RPC(nameof(ph.RPC_TakeDamage),
                                  ph.photonView.Owner,
                                  (int)damagePerShot);

            if (efectoMetal != null)
            {
                Vector3 pos = hit.point + hit.normal * 0.02f;
                Destroy(Instantiate(efectoMetal, pos, Quaternion.LookRotation(hit.normal)), 5f);
            }
        }
    }

    // ShowTracer va aquí afuera, como método de la clase
    IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        bulletTracer.enabled = true;
        bulletTracer.SetPosition(0, start);
        bulletTracer.SetPosition(1, end);
        yield return new WaitForSeconds(tracerDuration);
        bulletTracer.enabled = false;
    }

    void StartReload()
    {
        if (isReloading || currentAmmo == maxAmmo || reserveAmmo <= 0) return;
        isReloading = true;
        UpdateAmmoUI();
        Invoke(nameof(FinishReload), reloadTime);
    }

    void FinishReload()
    {
        int needed = maxAmmo - currentAmmo;
        int taken = math.min(needed, reserveAmmo);
        currentAmmo += taken;
        reserveAmmo -= taken;
        isReloading = false;
        UpdateAmmoUI();
    }

    void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        ammoText.text = isReloading ? "Recargando..." : $"{currentAmmo} / {reserveAmmo}";
    }
}