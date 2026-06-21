using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Configuración")]
    public int scoreToWin = 10;
    public string lobbySceneName = "LobbyScene"; // Cambiado para mayor claridad
    public float matchDurationSeconds = 300f;    // NUEVO: Duración en segundos (ej. 5 min = 300)

    // Claves de Room Properties
    private const string KEY_T1 = "ScoreT1";
    private const string KEY_T2 = "ScoreT2";
    private const string KEY_START_TIME = "StartTime"; // NUEVO: Clave para el tiempo

    private bool matchOver = false;

    public static bool retornarAlMenuSala = false;

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Solo el Master inicializa el marcador y el tiempo de inicio en la red
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(
                new Hashtable
                {
                    [KEY_T1] = 0,
                    [KEY_T2] = 0,
                    [KEY_START_TIME] = PhotonNetwork.Time // Guardamos la hora exacta de inicio
                });
        }

        UIManager.Instance?.ShowHUD();
        UIManager.Instance?.UpdateScoreUI(0, 0);
    }

    // NUEVO: Lógica del temporizador
    void Update()
    {
        if (matchOver) return;

        // Si la propiedad de tiempo de inicio ya existe en la sala
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(KEY_START_TIME, out object startTimeObj))
        {
            double startTime = (double)startTimeObj;
            double elapsedTime = PhotonNetwork.Time - startTime;
            float timeLeft = Mathf.Max(0, matchDurationSeconds - (float)elapsedTime);

            // Actualizamos la UI en todos los clientes
            UIManager.Instance?.UpdateTimerUI(Mathf.CeilToInt(timeLeft));

            // Solo el Master Client decide si el tiempo se acabó
            if (PhotonNetwork.IsMasterClient && timeLeft <= 0)
            {
                EndMatchByTime();
            }
        }
    }

    // ── API pública ──────────────────────────────────────────────

    public void RegisterDeath(int deadPlayerTeam)
    {
        if (matchOver) return;

        if (PhotonNetwork.IsMasterClient)
            SumarPunto(deadPlayerTeam);
        else
            photonView.RPC(nameof(RPC_RegisterDeath), RpcTarget.MasterClient, deadPlayerTeam);
    }

    [PunRPC]
    void RPC_RegisterDeath(int deadPlayerTeam)
    {
        if (matchOver) return;
        SumarPunto(deadPlayerTeam);
    }

    void SumarPunto(int deadPlayerTeam)
    {
        int killerTeam = deadPlayerTeam == 1 ? 2 : 1;
        string key = killerTeam == 1 ? KEY_T1 : KEY_T2;

        int current = GetRoomScore(key);

        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { [key] = current + 1 });
    }

    // ── Callbacks y Fin de Partida ────────────────────────────────

    public override void OnRoomPropertiesUpdate(Hashtable props)
    {
        int score1 = GetRoomScore(KEY_T1);
        int score2 = GetRoomScore(KEY_T2);

        UIManager.Instance?.UpdateScoreUI(score1, score2);

        if (matchOver) return;

        // El Master decide el fin de partida por puntos
        if (PhotonNetwork.IsMasterClient)
        {
            if (score1 >= scoreToWin)
                photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All, 1);
            else if (score2 >= scoreToWin)
                photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All, 2);
        }
    }

    // NUEVO: Fin de partida cuando se agota el tiempo
    void EndMatchByTime()
    {
        int score1 = GetRoomScore(KEY_T1);
        int score2 = GetRoomScore(KEY_T2);

        int winner = 0; // Por defecto es empate
        if (score1 > score2) winner = 1;
        else if (score2 > score1) winner = 2;

        photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All, winner);
    }

    [PunRPC]
    void RPC_EndMatch(int winnerTeam)
    {
        if (matchOver) return;
        matchOver = true;

        int myTeam = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Team")
            ? (int)PhotonNetwork.LocalPlayer.CustomProperties["Team"] : 1;

        UIManager.Instance?.ShowEndScreen(winnerTeam, myTeam);

        StartCoroutine(ReturnToMenu());
    }

    IEnumerator ReturnToMenu()
    {
        // Espera 5 segundos viendo la pantalla de victoria/derrota
        yield return new WaitForSeconds(5f);
        PhotonNetwork.LeaveRoom(); // Esto dispara OnLeftRoom() automáticamente
    }

    public override void OnLeftRoom()
    {
        // Carga la escena del lobby. Asegúrate de que el nombre coincida en Unity.
        SceneManager.LoadScene(lobbySceneName);
    }

    // ── Helper ───────────────────────────────────────────────────

    int GetRoomScore(string key)
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        return props.ContainsKey(key) ? (int)props[key] : 0;
    }
}