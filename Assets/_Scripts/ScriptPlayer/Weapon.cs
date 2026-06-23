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
    public int cantidadPerdigones = 8;
    public float dispersion = 0.05f;

    [Header("--- SNIPER SCOPE ---")]
    public bool esSniper = false;
    public GameObject modeloArma3D;
    public GameObject miraOverlayUI;
    public float fovNormal = 60f;
    public float fovApuntando = 15f;
    private bool estaApuntando = false;

    [Header("UI Local y Visuales")]
    public TextMeshProUGUI ammoText; // <-- Vuelve a estar en el arma
    public GameObject efectoMetal;
    public ParticleSystem casingParticles;
    public Transform cameraTransform;
    public LineRenderer bulletTracer;
    public float tracerDuration = 0.05f;

    private int currentAmmo;
    private float timeUntilAllowNextShot;
    private bool isReloading;
    private Camera mainCamera;

    [Header("Efectos de Impacto")]
    public GameObject impactoMetal;
    public GameObject impactoMadera;
    public GameObject impactoPiedra;
    public GameObject impactoArena;
    public GameObject impactoCarne;
    public GameObject impactoPorDefecto;

    [Header("Efectos del Arma")]
    public GameObject muzzleFlashPrefab;
    public Transform puntoDisparo;

    // El freno de seguridad multijugador
    private PhotonView pv;

    void Start()
    {
        // Buscamos a quién le pertenece este brazo
        pv = GetComponentInParent<PhotonView>();

        currentAmmo = maxAmmo;
        if (bulletTracer != null) bulletTracer.enabled = false;
        if (miraOverlayUI != null) miraOverlayUI.SetActive(false);
        mainCamera = cameraTransform.GetComponent<Camera>();

        // Solo la actualizamos si somos nosotros
        if (pv != null && pv.IsMine) UpdateAmmoUI();
    }

    void Update()
    {
        // ---> FRENO MULTIJUGADOR CRÍTICO <---
        // Si el arma es de otro jugador, ignoramos todo su Update
        if (pv != null && !pv.IsMine) return;

        if (isReloading) return;
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // --- LÓGICA DEL SNIPER (MANTENER Y SOLTAR) ---
        if (esSniper)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame && !estaApuntando)
            {
                StartCoroutine(EfectoZoomSniper(true));
            }
            else if (Mouse.current.rightButton.wasReleasedThisFrame && estaApuntando)
            {
                StartCoroutine(EfectoZoomSniper(false));
            }
        }

        timeUntilAllowNextShot = math.max(0, timeUntilAllowNextShot - Time.deltaTime);

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

        if (muzzleFlashPrefab != null && puntoDisparo != null)
        {
            // Lo creamos exactamente en la punta del cañón
            GameObject flash = Instantiate(muzzleFlashPrefab, puntoDisparo.position, puntoDisparo.rotation);

            // Lo hacemos "hijo" del cañón para que, si te mueves rápido, el fuego viaje pegado al arma
            flash.transform.SetParent(puntoDisparo);

            // Lo destruimos automáticamente medio segundo después para no saturar la memoria
            Destroy(flash, 0.5f);
        }

        int rayosADisparar = esEscopeta ? cantidadPerdigones : 1;

        for (int i = 0; i < rayosADisparar; i++)
        {
            Vector3 direccionDisparo = cameraTransform.forward;

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
                // 1. Lógica de Daño
                PlayerHealth ph = hit.transform.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.photonView.RPC(nameof(ph.RPC_TakeDamage), ph.photonView.Owner, (int)damagePerShot);
                }

                // 2. Lógica de Efectos Visuales (Materiales)
                GameObject efectoAInstanciar = impactoPorDefecto; // Efecto base

                if (hit.collider.CompareTag("Metal")) efectoAInstanciar = impactoMetal;
                else if (hit.collider.CompareTag("Madera")) efectoAInstanciar = impactoMadera;
                else if (hit.collider.CompareTag("Piedra")) efectoAInstanciar = impactoPiedra;
                else if (hit.collider.CompareTag("Arena")) efectoAInstanciar = impactoArena;
                else if (hit.collider.CompareTag("Carne") || hit.collider.CompareTag("Player")) efectoAInstanciar = impactoCarne;

                // 3. Instanciar el efecto en el punto exacto del choque
                if (efectoAInstanciar != null)
                {
                    // Lo separamos 0.02f de la pared para que no se superponga (Z-Fighting)
                    Vector3 pos = hit.point + hit.normal * 0.02f;
                    Destroy(Instantiate(efectoAInstanciar, pos, Quaternion.LookRotation(hit.normal)), 5f);
                }
            }
        }
    }

    // La función ToggleScope fue eliminada, ahora todo lo hace EfectoZoomSniper directamente

    IEnumerator EfectoZoomSniper(bool apuntar)
    {
        estaApuntando = apuntar;

        if (UIManager.Instance != null && UIManager.Instance.crosshairImage != null)
            UIManager.Instance.crosshairImage.gameObject.SetActive(!apuntar);

        if (apuntar)
        {
            yield return new WaitForSeconds(0.15f);

            // Freno de seguridad: si el jugador soltó el clic rapidísimo, cancelamos
            if (!estaApuntando) yield break;

            if (miraOverlayUI != null) miraOverlayUI.SetActive(true);

            // Truco maestro: Achicamos el arma a cero en vez de apagarla para no matar el script
            if (modeloArma3D != null) modeloArma3D.transform.localScale = Vector3.zero;

            mainCamera.fieldOfView = fovApuntando;
        }
        else
        {
            if (miraOverlayUI != null) miraOverlayUI.SetActive(false);

            // Devolvemos el arma a su tamaño normal
            if (modeloArma3D != null) modeloArma3D.transform.localScale = Vector3.one;

            mainCamera.fieldOfView = fovNormal;
        }
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
        if (estaApuntando) StartCoroutine(EfectoZoomSniper(false));

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
        if (ammoText != null)
        {
            // Ahora lo actualiza de forma autónoma, sin depender de la escena
            ammoText.text = isReloading ? "Recargando..." : $"{currentAmmo} / {reserveAmmo}";
        }
    }

    void OnDisable()
    {
        // Si el jugador cambia de arma mientras apunta, restauramos la vista
        if (estaApuntando)
        {
            estaApuntando = false;
            if (miraOverlayUI != null) miraOverlayUI.SetActive(false);
            if (modeloArma3D != null) modeloArma3D.transform.localScale = Vector3.one;
            if (mainCamera != null) mainCamera.fieldOfView = fovNormal;
        }
    }
}