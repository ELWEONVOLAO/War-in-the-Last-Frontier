using UnityEngine;
using Photon.Pun;
using System.Collections; // Necesario para el IEnumerator

public class PlayerSetup : MonoBehaviourPun
{
    public Camera camaraJugador;
    public AudioListener audioListenerJugador;

    // Cambiamos Start por un IEnumerator para esperar a que Photon esté listo
    IEnumerator Start()
    {
        // Esperamos un frame para asegurar que PhotonView tenga los datos de red
        yield return new WaitForSeconds(0.1f);

        if (photonView.IsMine)
        {
            // Eres tú: mantén todo encendido
            if (camaraJugador != null) camaraJugador.enabled = true;
            if (audioListenerJugador != null) audioListenerJugador.enabled = true;
        }
        else
        {
            // Eres un enemigo: apaga cámaras y oídos
            if (camaraJugador != null) camaraJugador.enabled = false;
            if (audioListenerJugador != null) audioListenerJugador.enabled = false;
        }
    }
}