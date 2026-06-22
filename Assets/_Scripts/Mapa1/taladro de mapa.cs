using UnityEngine;
using Photon.Pun;
using System.Collections;
public class taladrodemapa : MonoBehaviourPunCallbacks
{
    [Header("Configuración del Taladro")]
    public float tiempoApagado = 30f;   // Cada cuánto se activa
    public float tiempoEncendido = 5f;  // Cuánto tiempo permanece letal
    public Collider zonaLetal;          // El Trigger Collider que matará al jugador

    [Header("Efectos Visuales")]
    public GameObject particulasTaladro; // Opcional: Para encender partículas de Unity

    private bool taladroActivo = false;

    void Start()
    {
        // El estado inicial debe ser apagado
        ActualizarEstadoTaladro(false);

        // Solo el Master Client lleva la cuenta del tiempo para evitar desincronización
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(CicloDelTaladro());
        }
    }

    // Corrutina que se ejecuta en bucle en el Master Client
    IEnumerator CicloDelTaladro()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoApagado);

            // Avisa a todos los clientes (incluido el Master) que enciendan el taladro
            photonView.RPC("RPC_SincronizarTaladro", RpcTarget.All, true);

            yield return new WaitForSeconds(tiempoEncendido);

            // Avisa a todos los clientes que lo apaguen
            photonView.RPC("RPC_SincronizarTaladro", RpcTarget.All, false);
        }
    }

    [PunRPC]
    void RPC_SincronizarTaladro(bool estado)
    {
        ActualizarEstadoTaladro(estado);
    }

    private void ActualizarEstadoTaladro(bool estado)
    {
        taladroActivo = estado;
        zonaLetal.enabled = estado;

        // Aquí activas/desactivas las animaciones o sistemas de partículas
        if (particulasTaladro != null)
        {
            particulasTaladro.SetActive(estado);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!taladroActivo) return;

        PhotonView playerView = other.GetComponent<PhotonView>();

        if (playerView != null && playerView.IsMine)
        {
            // Obtener equipo
            int miEquipo = 1;
            if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
                miEquipo = (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"];

            // Matar al jugador
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.photonView.RPC(nameof(ph.RPC_TakeDamage),
                                  ph.photonView.Owner,
                                  9999); // daño instantáneo

            // Sumar punto al equipo enemigo
            GameManager.Instance?.RegisterDeath(miEquipo);
        }
    }
}

