using UnityEngine;
using Photon.Pun;

public class PasosJugador : MonoBehaviourPun
{
    [Header("Configuración de Pasos")]
    public AudioSource audioSourcePasos;
    public AudioClip[] sonidosPasos; // Pon 3 o 4 sonidos de pasos para que no sea repetitivo
    public float tiempoEntrePasos = 0.5f; 
    
    private float cronometro;
    
    // Asumo que usas CharacterController. Si usas Rigidbody, cámbialo aquí.
    private CharacterController cc; 

    void Start()
    {
        cc = GetComponentInParent<CharacterController>();
        
        if (audioSourcePasos != null)
        {
            // Forzamos el sonido a 3D por código para evitar que se te olvide en el Inspector
            audioSourcePasos.spatialBlend = 1f; 
            audioSourcePasos.rolloffMode = AudioRolloffMode.Linear;
            audioSourcePasos.maxDistance = 30f; // Los pasos se dejan de escuchar a los 30 metros
        }
    }

    void Update()
    {
        // Esto lo ejecutan TODAS las computadoras, no solo el dueño.
        // Si detectan que el cuerpo de este jugador se está moviendo, reproducen el sonido localmente.
        if (cc != null && cc.isGrounded && cc.velocity.magnitude > 0.5f)
        {
            cronometro -= Time.deltaTime;
            
            if (cronometro <= 0f)
            {
                ReproducirPaso();
                cronometro = tiempoEntrePasos;
            }
        }
        else
        {
            cronometro = 0f; // Para que el primer paso suene apenas te muevas
        }
    }

    void ReproducirPaso()
    {
        if (audioSourcePasos != null && sonidosPasos.Length > 0)
        {
            int indice = Random.Range(0, sonidosPasos.Length);
            // El volumen a 0.4f hace que los pasos sean un sonido de fondo y no tapen los disparos
            audioSourcePasos.PlayOneShot(sonidosPasos[indice], 0.4f); 
        }
    }
}