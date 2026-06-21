using System.Collections;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public Transform[] team1SpawnPoints;
    public Transform[] team2SpawnPoints;

    public static PlayerSpawner Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SpawnMyPlayer();
    }

    public void SpawnMyPlayer()
    {
        Debug.Log("Team1 spawn count: " + team1SpawnPoints.Length);  // ← agregar
        Debug.Log("Team2 spawn count: " + team2SpawnPoints.Length);  // ← agregar

        int myTeam = 1;
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team"))
            myTeam = (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"];

        Transform[] spawnArray = myTeam == 2 ? team2SpawnPoints : team1SpawnPoints;

        // Verificación antes de usar el array
        if (spawnArray == null || spawnArray.Length == 0)
        {
            Debug.LogError("No hay SpawnPoints para el equipo " + myTeam);
            return;
        }

        Transform spawnPoint = spawnArray[Random.Range(0, spawnArray.Length)];

        PhotonNetwork.Instantiate("PlayerPrefab", spawnPoint.position, spawnPoint.rotation);
    }

    public void RespawnPlayer(float delay)
    {
        StartCoroutine(RespawnRoutine(delay));
    }

    private IEnumerator RespawnRoutine(float delay)
    {
        Debug.Log("Esperando " + delay + "s para respawnear...");
        yield return new WaitForSeconds(delay);
        Debug.Log("Respawneando ahora");
        SpawnMyPlayer();
    }
}