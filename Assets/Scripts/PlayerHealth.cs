using System.Collections;
using UnityEngine;
using Photon.Pun;
using Unity.Mathematics;
using TMPro;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviourPun
{
    [Header("health set up")]
    public int maxHealth;

    [Header("ui set up")]
    public TextMeshProUGUI healthText;
    public Image healthFillImage;

    private int health;

    // Agregar en Start()
    void Start()
    {
        health = maxHealth;

        if (photonView.IsMine)
        {
            // Solo activa la UI para el jugador local
            UpdateUi();
        }
        else
        {
            // Oculta todo el canvas del jugador remoto
            healthText.gameObject.SetActive(false);
            healthFillImage.gameObject.SetActive(false);
        }
    }

    private void UpdateUi()
    {
        if (healthText != null)
            healthText.text = $"{health} / {maxHealth}";

        if (healthFillImage != null)
            healthFillImage.fillAmount = (float)health / maxHealth;
    }

    private bool isDead = false;  // ← agregar variable

    [PunRPC]
    public void RPC_TakeDamage(int _damage)
    {
        if (isDead) return;  // ← evita múltiples llamadas a Die()

        health = math.max(0, health - _damage);
        Debug.Log("Vida actual: " + health);

        if (photonView.IsMine)
            UpdateUi();

        if (health <= 0)
        {
            isDead = true;
            Die();
        }
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

        Debug.Log("Mi equipo: " + myTeam);
        Debug.Log("GameManager existe: " + (GameManager.Instance != null));

        GameManager.Instance?.RegisterDeath(myTeam);

        PlayerSpawner.Instance.RespawnPlayer(2f);
        PhotonNetwork.Destroy(gameObject);
    }
    /*
    void Die()
    {
        // Avisar al GameManager quién mató
        // (necesitas saber el equipo del que mató, se hace desde Weapon)
            Debug.Log(photonView.Owner.NickName + " murió");

            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);

                Invoke(nameof(Respawn), 2f);
            }
    }
    */
    /*
    void Die()
    {
        Debug.Log(photonView.Owner.NickName + " murió");

        if (!photonView.IsMine)
            return;

        StartCoroutine(RespawnRoutine());
    }
    */
    /*
    void Respawn()
    {
        FindFirstObjectByType<PlayerSpawner>().SendMessage("SpawnMyPlayer");
    }
    */
    IEnumerator RespawnRoutine()
    {
        PhotonNetwork.Destroy(gameObject);

        yield return new WaitForSeconds(2f);

        PlayerSpawner.Instance.SpawnMyPlayer();
    }
}
