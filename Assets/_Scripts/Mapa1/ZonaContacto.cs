using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(Collider))]
public class ZonaContacto : MonoBehaviourPun
{
    [Header("Configuración")]
    public int daño = 9999;         // 9999 = instakill, o pon un valor normal
    public bool soloUnaVez = false; // true = se desactiva tras el primer contacto

    [Header("Audio")]
    public AudioClip sfxContacto;
    private AudioSource audioSource;

    private bool activa = true;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
            audioSource.spatialBlend = 0f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!activa) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>()
                       ?? other.GetComponentInParent<PlayerHealth>()
                       ?? other.transform.root.GetComponentInChildren<PlayerHealth>();

        if (ph == null) return;
        if (!ph.photonView.IsMine) return;

        // Sonido
        if (sfxContacto != null && audioSource != null)
            audioSource.PlayOneShot(sfxContacto);

        // Daño instantáneo
        ph.photonView.RPC(nameof(ph.RPC_TakeDamage), ph.photonView.Owner, daño);

        // Si es de un solo uso, desactivar
        if (soloUnaVez) activa = false;
    }
}