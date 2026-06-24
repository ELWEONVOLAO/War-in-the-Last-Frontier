using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
public class ZonaMortal : MonoBehaviourPun
{
    [Header("Configuración de Eventos")]
    [Tooltip("Segundos exactos de la partida donde matará (Ej: 60, 120, 180, 240)")]
    public int[] tiemposDeActivacion = { 60, 120, 180, 240 };

    [Tooltip("Cuántos segundos durará encendida la trampa")]
    public float duracionEncendida = 15f;
    public int dañoInstakill = 9999;

    [Header("Audio")]
    public AudioClip sfxAlarma;
    private AudioSource audioSource;

    private bool zonaActiva = false;

    // --- NUEVOS CRONÓMETROS DEL SERVIDOR ---
    private float tiempoOficialPartida = 0f;
    private float cronometroApagado = 0f;

    private List<int> tiemposYaUsados = new List<int>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Sonido global
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Update()
    {
        // Solo el Host tiene derecho a vigilar el tiempo
        if (!PhotonNetwork.IsMasterClient) return;

        if (!zonaActiva)
        {
            // 1. El Host lleva el conteo exacto de los segundos desde que empezó el mapa
            tiempoOficialPartida += Time.deltaTime;

            // Convertimos los decimales (ej. 60.034f) a un número entero limpio (60)
            int segundoActual = Mathf.FloorToInt(tiempoOficialPartida);

            // 2. Revisamos si el segundo actual coincide con nuestros números mortales
            foreach (int tiempoClave in tiemposDeActivacion)
            {
                if (segundoActual == tiempoClave && !tiemposYaUsados.Contains(tiempoClave))
                {
                    tiemposYaUsados.Add(tiempoClave);
                    photonView.RPC(nameof(RPC_CambiarEstadoZona), RpcTarget.All, true);
                    break;
                }
            }
        }
        else
        {
            // 3. Si está encendida, contamos los 15 segundos para apagarla
            cronometroApagado += Time.deltaTime;

            if (cronometroApagado >= duracionEncendida)
            {
                cronometroApagado = 0f;
                photonView.RPC(nameof(RPC_CambiarEstadoZona), RpcTarget.All, false);
            }
        }
    }

    [PunRPC]
    void RPC_CambiarEstadoZona(bool encender)
    {
        zonaActiva = encender;

        if (encender)
        {
            if (sfxAlarma != null && audioSource != null)
                audioSource.PlayOneShot(sfxAlarma);

            Debug.Log(">>> LA ZONA SE ACTIVÓ POR EL RELOJ DEL HOST <<<");
        }
        else
        {
            Debug.Log(">>> LA ZONA SE APAGÓ <<<");
        }
    }

    void OnTriggerEnter(Collider other) { IntentarMatarJugador(other); }
    void OnTriggerStay(Collider other) { IntentarMatarJugador(other); }

    void IntentarMatarJugador(Collider other)
    {
        if (!zonaActiva) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) ph = other.GetComponentInParent<PlayerHealth>();
        if (ph == null) ph = other.transform.root.GetComponentInChildren<PlayerHealth>();

        if (ph != null && ph.photonView.IsMine && ph.maxHealth > 0)
        {
            ph.photonView.RPC(nameof(ph.RPC_TakeDamage), ph.photonView.Owner, dañoInstakill);
        }
    }
}