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
    
    private Vector3 escalaOriginalArma; 

    [Header("UI Local y Visuales")]
    public TextMeshProUGUI ammoText;
    public GameObject efectoMetal;
    public ParticleSystem casingParticles;
    public Transform cameraTransform;
    public LineRenderer bulletTracer;
    public float tracerDuration = 0.05f;

    [Header("--- HITMARKER ---")]
    public UnityEngine.UI.Image hitmarkerImage;
    public Color colorHit = Color.white;
    public Color colorKill = Color.red;
    public AudioClip sfxHitmarkerNormal; 
    public AudioClip sfxHitmarkerKill;   

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
    [Tooltip("Arrastra aquí el Particle System del fogonazo que ya es hijo de tu arma")]
    public ParticleSystem fogonazoParticulas; 

    [Header("Sonidos (SFX)")]
    public AudioSource audioSourceArma;
    public AudioClip sfxDisparo;
    public AudioClip sfxCasquillo;

    private PhotonView pv;

    void Start()
    {
        pv = GetComponentInParent<PhotonView>();
        currentAmmo = maxAmmo;
        
        if (modeloArma3D != null) escalaOriginalArma = modeloArma3D.transform.localScale;
        
        if (bulletTracer != null) bulletTracer.enabled = false;
        if (miraOverlayUI != null) miraOverlayUI.SetActive(false);
        if (hitmarkerImage != null) hitmarkerImage.gameObject.SetActive(false);
        
        mainCamera = cameraTransform.GetComponent<Camera>();

        if (pv != null && pv.IsMine) UpdateAmmoUI();
    }

    void Update()
    {
        if (pv != null && !pv.IsMine) return;
        if (isReloading) return;
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (esSniper)
        {
            if (Mouse.current.rightButton.wasPressedThisFrame && !estaApuntando)
                StartCoroutine(EfectoZoomSniper(true));
            else if (Mouse.current.rightButton.wasReleasedThisFrame && estaApuntando)
                StartCoroutine(EfectoZoomSniper(false));
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

        if (Keyboard.current.rKey.wasPressedThisFrame) StartReload();
    }

    void Disparar()
    {
        currentAmmo--;
        UpdateAmmoUI();
       
        pv.RPC(nameof(RPC_EfectosArma), RpcTarget.All);

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
                PlayerHealth ph = hit.transform.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    bool esKill = (ph.maxHealth > 0 && (ph.maxHealth - damagePerShot) <= 0); 
                    ph.photonView.RPC(nameof(ph.RPC_TakeDamage), ph.photonView.Owner, (int)damagePerShot);
                    
                    if (pv.IsMine) 
                    {
                        StartCoroutine(MostrarHitmarker(esKill));
                        
                        if (esKill)
                        {
                            int misKills = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Kills") ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Kills"] : 0;
                            misKills++;
                            
                            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
                            hash.Add("Kills", misKills);
                            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
                        }
                    }
                }

                GameObject efectoAInstanciar = impactoPorDefecto;

                if (hit.collider.CompareTag("Metal")) efectoAInstanciar = impactoMetal;
                else if (hit.collider.CompareTag("Madera")) efectoAInstanciar = impactoMadera;
                else if (hit.collider.CompareTag("Piedra")) efectoAInstanciar = impactoPiedra;
                else if (hit.collider.CompareTag("Arena")) efectoAInstanciar = impactoArena;
                else if (hit.collider.CompareTag("Carne") || hit.collider.CompareTag("Player")) efectoAInstanciar = impactoCarne;

                if (efectoAInstanciar != null)
                {
                    Vector3 pos = hit.point + hit.normal * 0.02f;
                    Destroy(Instantiate(efectoAInstanciar, pos, Quaternion.LookRotation(hit.normal)), 5f);
                }
            }
        }
    }

   [PunRPC]
    void RPC_EfectosArma()
    {
        if (audioSourceArma != null && sfxDisparo != null)
            audioSourceArma.PlayOneShot(sfxDisparo);

        // --- ESCUDO ANTI-ERROR ---
        // Solo intentamos emitir si el objeto realmente existe en la escena
        if (casingParticles != null && casingParticles.gameObject.scene.IsValid())
        {
            casingParticles.Emit(1);
        }

        if (sfxCasquillo != null)
            StartCoroutine(SonidoCasquilloDelay());

        if (fogonazoParticulas != null && fogonazoParticulas.gameObject.scene.IsValid())
        {
            fogonazoParticulas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fogonazoParticulas.Play(true);
        }
    }

    IEnumerator MostrarHitmarker(bool esKill)
    {
        if (hitmarkerImage != null)
        {
            hitmarkerImage.color = esKill ? colorKill : colorHit;
            hitmarkerImage.gameObject.SetActive(true);

            if (audioSourceArma != null)
            {
                AudioClip sonidoATocar = esKill ? sfxHitmarkerKill : sfxHitmarkerNormal;
                if (sonidoATocar != null) audioSourceArma.PlayOneShot(sonidoATocar, 0.8f);
            }

            yield return new WaitForSeconds(0.15f);
            hitmarkerImage.gameObject.SetActive(false);
        }
    }

    IEnumerator EfectoZoomSniper(bool apuntar)
    {
        estaApuntando = apuntar;

        if (UIManager.Instance != null && UIManager.Instance.crosshairImage != null)
            UIManager.Instance.crosshairImage.gameObject.SetActive(!apuntar);

        if (apuntar)
        {
            yield return new WaitForSeconds(0.15f);

            if (!estaApuntando) yield break;

            if (miraOverlayUI != null) miraOverlayUI.SetActive(true);
            if (modeloArma3D != null) modeloArma3D.transform.localScale = Vector3.zero;

            mainCamera.fieldOfView = fovApuntando;
        }
        else
        {
            if (miraOverlayUI != null) miraOverlayUI.SetActive(false);
            if (modeloArma3D != null) modeloArma3D.transform.localScale = escalaOriginalArma;

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
            ammoText.text = isReloading ? "Recargando..." : $"{currentAmmo} / {reserveAmmo}";
    }

    void OnDisable()
    {
        if (estaApuntando)
        {
            estaApuntando = false;
            if (miraOverlayUI != null) miraOverlayUI.SetActive(false);
            if (modeloArma3D != null) modeloArma3D.transform.localScale = escalaOriginalArma;
            if (mainCamera != null) mainCamera.fieldOfView = fovNormal;
        }
    }

    IEnumerator SonidoCasquilloDelay()
    {
        yield return new WaitForSeconds(0.4f);
        if (audioSourceArma != null && sfxCasquillo != null)
            audioSourceArma.PlayOneShot(sfxCasquillo, 0.4f);
    }
}