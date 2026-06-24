using System.Collections;
using UnityEngine;
using Photon.Pun;
using Unity.Mathematics;
using TMPro;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun
{
    [Header("Health Set Up")]
    public int maxHealth;


    [Header("Efecto de Daño")]
    public Image pantallaRojaDaño; // <-- Nueva variable para el flash rojo

    private int health;
    private bool isDead = false;

    void Start()
    {
        health = maxHealth;

    }

 

    [PunRPC]
    public void RPC_TakeDamage(int _damage)
    {
        if (isDead) return;

        health = math.max(0, health - _damage);
        Debug.Log("Vida actual: " + health);

        if (photonView.IsMine)
        {

            // Disparamos el efecto visual
            if (pantallaRojaDaño != null)
            {
                StopCoroutine(EfectoFlashRojo()); // Detenemos el anterior por si te disparan muy rápido
                StartCoroutine(EfectoFlashRojo());
            }
        }

        if (health <= 0)
        {
            isDead = true;
            Die();
        }
    }

    // Esta corrutina hace el efecto de aparecer y desvanecer
    IEnumerator EfectoFlashRojo()
    {
        // 1. Ponemos la imagen roja casi a la mitad de opacidad (Alfa: 0.4f)
        pantallaRojaDaño.color = new Color(1f, 0f, 0f, 0.4f);

        // 2. Esperamos una fracción de segundo para que el impacto se note
        yield return new WaitForSeconds(0.05f);

        // 3. Vamos bajando la opacidad lentamente hasta que vuelva a ser transparente (0)
        float alpha = 0.4f;
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime; // Se desvanece en aprox 0.4 segundos
            pantallaRojaDaño.color = new Color(1f, 0f, 0f, alpha);
            yield return null; // Espera al siguiente frame
        }

        // Nos aseguramos de que quede 100% invisible al final
        pantallaRojaDaño.color = Color.clear;
    }

    void Die()
    {
        Debug.Log("Die() ejecutado en: " + photonView.Owner.NickName);

        if (!photonView.IsMine)
        {
            Debug.Log("No es mi jugador, saliendo");
            return;
        }

        int myTeam = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team")
            ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"] : 1;

        GameManager.Instance?.RegisterDeath(myTeam);

        PlayerSpawner.Instance.RespawnPlayer(2f);
        PhotonNetwork.Destroy(gameObject);
    }
}
