using UnityEngine;
using Photon.Pun;
using System.Collections;

public class ZonaDanio : MonoBehaviourPun
{
    [Header("Configuración")]
    public float radio = 5f;
    public int danioPorSegundo = 10;
    public float intervalo = 1f;        // cada cuánto aplica el daño
    public LayerMask capasJugador;      // asigna la layer "Player" en el Inspector

    private void Start()
    {
        StartCoroutine(LoopDanio());
    }

    IEnumerator LoopDanio()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalo);
            AplicarDanio();
        }
    }

    void AplicarDanio()
    {
        Collider[] coliders = Physics.OverlapSphere(transform.position, radio);

        Debug.Log("Colliders detectados: " + coliders.Length);

        foreach (Collider col in coliders)
        {
            Debug.Log("Collider encontrado: " + col.name);

            PlayerHealth ph = col.GetComponentInParent<PlayerHealth>();

            if (ph == null)
            {
                Debug.Log(col.name + " no tiene PlayerHealth");
                continue;
            }

            Debug.Log("PlayerHealth encontrado en: " + col.name + " | IsMine: " + ph.photonView.IsMine);

            if (!ph.photonView.IsMine)
            {
                Debug.Log("No es mi jugador, saltando");
                continue;
            }

            Debug.Log("Aplicando " + danioPorSegundo + " de daño a " + ph.photonView.Owner.NickName);

            ph.photonView.RPC(nameof(ph.RPC_TakeDamage),
                              ph.photonView.Owner,
                              danioPorSegundo);
        }
    }

    // Dibuja el radio en la Scene View para ajustarlo fácilmente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}