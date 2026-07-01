using UnityEngine;
using Photon.Pun;

public class PlayerSetup : MonoBehaviourPun
{
    [Header("Componentes a desactivar en enemigos")]
    [Tooltip("La cámara de este jugador")]
    public Camera camaraJugador;
    
    [Tooltip("El Audio Listener (usualmente está en el mismo objeto que la cámara)")]
    public AudioListener audioListenerJugador;

    void Start()
    {
        // Si este jugador NO es el nuestro (es el avatar de alguien más en la red)
        if (!photonView.IsMine)
        {
            // Le apagamos la cámara para no ver a través de sus ojos
            if (camaraJugador != null)
            {
                camaraJugador.enabled = false;
            }

            // Le apagamos los oídos para que no cause el error de "2 Audio Listeners"
            if (audioListenerJugador != null)
            {
                audioListenerJugador.enabled = false;
            }
        }
    }
}