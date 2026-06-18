using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// Gestiona puntuación, fin de partida y regreso al menú.
/// Requiere PhotonView en el mismo GameObject.
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Configuración")]
    public int scoreToWin = 10;
    public string mainMenuSceneName = "TitleScreen";

    // Claves de Room Properties (deben ser únicas)
    private const string KEY_T1 = "ScoreT1";
    private const string KEY_T2 = "ScoreT2";

    private bool matchOver = false;

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // Solo el Master inicializa el marcador en la red
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(
                new Hashtable { [KEY_T1] = 0, [KEY_T2] = 0 });
        }

        // Mostrar HUD desde el inicio
        UIManager.Instance?.ShowHUD();
        UIManager.Instance?.UpdateScoreUI(0, 0);
    }

    // ── API pública ──────────────────────────────────────────────

    /// <summary>
    /// Llamar desde PlayerHealth.Die() pasando el equipo del jugador que MURIÓ.
    /// El punto va al equipo ENEMIGO.
    /// </summary>
    // En GameManager.cs

    public void RegisterDeath(int deadPlayerTeam)
    {
        Debug.Log("RegisterDeath llamado, equipo muerto: " + deadPlayerTeam);

        if (matchOver) return;

        // Si soy Master, sumo directo
        // Si no soy Master, le pido al Master que sume
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

        int current = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key)
            ? (int)PhotonNetwork.CurrentRoom.CustomProperties[key] : 0;

        Debug.Log($"Sumando punto al equipo {killerTeam}, nuevo score: {current + 1}");

        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new Hashtable { [key] = current + 1 });
    }

    // ── Callback Photon: alguien actualizó las Room Properties ──

    public override void OnRoomPropertiesUpdate(Hashtable props)
    {
        int score1 = props.ContainsKey(KEY_T1) ? (int)props[KEY_T1] :
                     GetRoomScore(KEY_T1);
        int score2 = props.ContainsKey(KEY_T2) ? (int)props[KEY_T2] :
                     GetRoomScore(KEY_T2);

        UIManager.Instance?.UpdateScoreUI(score1, score2);

        if (matchOver) return;

        // El Master decide el fin de partida
        if (PhotonNetwork.IsMasterClient)
        {
            if (score1 >= scoreToWin)
                photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All, 1);
            else if (score2 >= scoreToWin)
                photonView.RPC(nameof(RPC_EndMatch), RpcTarget.All, 2);
        }
    }

    // ── RPC fin de partida ───────────────────────────────────────

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
        yield return new WaitForSeconds(5f);
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ── Helper ───────────────────────────────────────────────────

    int GetRoomScore(string key)
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        return props.ContainsKey(key) ? (int)props[key] : 0;
    }
}
