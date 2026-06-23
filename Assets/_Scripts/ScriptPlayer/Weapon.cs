using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Photon.Pun;
using Unity.Mathematics;
using TMPro;

public class Weapon : MonoBehaviour
{
    [Header("Atributos Básicos")]
    public float fireRate = 10f;
    public float damagePerShot = 25f;
    public float hitScanDistance = 500f;

    [Header("Munición")]
    public int maxAmmo = 30;
    public int reserveAmmo = 90;
    public float reloadTime = 2f;

    [Header("--- TIPO DE ARMA ---")]
    public bool esEscopeta = false;
    public int cantidadPerdigones = 8; // Solo se usa si es escopeta
    public float dispersion = 0.05f;   // Nivel de apertura de las balas

    [Header("--- SNIPER SCOPE ---")]
    public bool esSniper = false;
    public GameObject modeloArma3D;    // Para ocultar el arma al apuntar
    public GameObject miraOverlayUI;   // La imagen negra de la mira telescópica (Canvas)
    public float fovNormal = 60f;
    public float fovApuntando = 15f;
    private bool estaApuntando = false;

    [Header("Visuales")]
    public GameObject efectoMetal;
    public ParticleSystem casingParticles;
    public Transform cameraTransform;
    public TextMeshProUGUI ammoText;
    public LineRenderer bulletTracer;
    public float tracerDuration = 0.05f;

    private int currentAmmo;
    private float timeUntilAllowNextShot;
    private bool isReloading;
    private Camera mainCamera;

    void Start()
    {
        currentAmmo = maxAmmo;
        if (bulletTracer != null) bulletTracer.enabled = false;
        if (miraOverlayUI != null) miraOverlayUI.SetActive(false);
        mainCamera = cameraTransform.GetComponent<Camera>();
        UpdateAmmoUI();
    }

    void Update()
    {
        if (isReloading) return;
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // --- Lógica del Sniper Scope ---
        if (esSniper && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ToggleScope();
        }

        // --- Lógica de Disparo ---
        timeUntilAllowNextShot = math.max(0, timeUntilAllowNextShot - Time.deltaTime);

        // Si no es automática (Sniper o Escopeta), disparamos por clic. Si es automática (Rifle), mantenemos presionado.
        bool intentoDisparo = (esSniper || esEscopeta) ? Mouse.current.leftButton.wasPressedThisFrame : Mouse.current.leftButton.isPressed;

        if (intentoDisparo && timeUntilAllowNextShot <= 0)
        {
            if (currentAmmo > 0)
            {
                Disparar();
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

    void Disparar()
    {
        currentAmmo--;
        UpdateAmmoUI();
        if (casingParticles != null) casingParticles.Emit(1);

        // Si es escopeta disparamos varios rayos, si no, solo 1.
        int rayosADisparar = esEscopeta ? cantidadPerdigones : 1;

        for (int i = 0; i < rayosADisparar; i++)
        {
            Vector3 direccionDisparo = cameraTransform.forward;

            // Aplicamos dispersión (Spread) si la tiene
            if (dispersion > 0)
            {
                direccionDisparo += new Vector3(
                    UnityEngine.Random.Range(-dispersion, dispersion),
                    UnityEngine.Random.Range(-dispersion, dispersion),
                    UnityEngine.Random.Range(-dispersion, dispersion)
                );
                direccionDisparo.Normalize();
            }

            Ray ray = new Ray(cameraTransform.position, direccionDisparo);
            bool golpeo = Physics.Raycast(ray, out RaycastHit hit, hitScanDistance);

            Vector3 endPoint = golpeo ? hit.point : ray.origin + ray.direction * hitScanDistance;

            if (bulletTracer != null) StartCoroutine(ShowTracer(ray.origin, endPoint));

            if (golpeo)
            {
                PlayerHealth ph = hit.transform.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.photonView.RPC(nameof(ph.RPC_TakeDamage), ph.photonView.Owner, (int)damagePerShot);
                }

                if (efectoMetal != null)
                {
                    Vector3 pos = hit.point + hit.normal * 0.02f;
                    Destroy(Instantiate(efectoMetal, pos, Quaternion.LookRotation(hit.normal)), 5f);
                }
            }
        }
    }

    void ToggleScope()
    {
        estaApuntando = !estaApuntando;

        // Ocultamos/Mostramos la interfaz del HUD normal (para que no estorbe)
        if (UIManager.Instance != null && UIManager.Instance.crosshairImage != null)
            UIManager.Instance.crosshairImage.gameObject.SetActive(!estaApuntando);

        if (estaApuntando)
        {
            StartCoroutine(EfectoZoomSniper());
        }
        else
        {
            if (miraOverlayUI != null) miraOverlayUI.SetActive(false);
            if (modeloArma3D != null) modeloArma3D.SetActive(true);
            mainCamera.fieldOfView = fovNormal;
        }
    }

    IEnumerator EfectoZoomSniper()
    {
        // Un pequeño retraso para que la animación se vea suave
        yield return new WaitForSeconds(0.15f);
        if (miraOverlayUI != null) miraOverlayUI.SetActive(true);
        if (modeloArma3D != null) modeloArma3D.SetActive(false); // Ocultamos el arma de la pantalla
        mainCamera.fieldOfView = fovApuntando; // Hacemos zoom
    }

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
        // Si estábamos apuntando con el sniper, quitamos la mira para recargar
        if (estaApuntando) ToggleScope();

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

    // Para el gestor de clases, necesitamos una función que apague la mira si cambiamos de arma rápido
    void OnDisable()
    {
        if (estaApuntando) ToggleScope();
    }
}