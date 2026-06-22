using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────
//  Datos de un mapa (configurar en Inspector)
// ─────────────────────────────────────────────
[System.Serializable]
public class DatosMapa
{
    public string nombreAMostrar;   // "Desierto", "Bosque", etc.
    public string nombreEscena;     // nombre EXACTO en Build Settings
    public Sprite imagenMapa;       // sprite para la votación
}

// ─────────────────────────────────────────────
//  NetworkManager
//  - Singleton que vive toda la partida
//  - Solo habla con Photon, no toca la UI
//  - LobbyUI y RoomUI se suscriben a sus eventos
// ─────────────────────────────────────────────
public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Configuración")]
    public DatosMapa[] mapas;           // Arrastra tus 3 mapas aquí
    public byte maxJugadores = 10;

    // Eventos que escuchan las UIs
    public event System.Action             OnConectado;
    public event System.Action             OnSalaUnida;
    public event System.Action             OnSalaAbandonada;
    public event System.Action<List<RoomInfo>> OnListaSalasActualizada;
    public event System.Action<string>     OnError;

    // Índices de los 2 mapas sorteados (guardados en Room Properties)
    public const string KEY_MAPA1 = "MapaOp1";
    public const string KEY_MAPA2 = "MapaOp2";
    public const string KEY_VOTO  = "VotoMapa";
    public const string KEY_EQUIPO = "Team";
    public const string KEY_PING  = "Ping";

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        PhotonNetwork.NickName = "Jugador_" + Random.Range(1000, 9999);
        PhotonNetwork.ConnectUsingSettings();
    }

    // ── Callbacks Photon ─────────────────────────────────────────

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        OnConectado?.Invoke();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
        => OnListaSalasActualizada?.Invoke(roomList);

    public override void OnJoinedRoom()
    {
        // Solo el Host sortea los mapas (una sola vez)
        if (PhotonNetwork.IsMasterClient &&
            !PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_MAPA1))
            SortearMapas();

        OnSalaUnida?.Invoke();
    }

    public override void OnLeftRoom()          => OnSalaAbandonada?.Invoke();
    public override void OnCreateRoomFailed(short c, string msg) => OnError?.Invoke("No se pudo crear: " + msg);
    public override void OnJoinRoomFailed(short c, string msg)   => OnError?.Invoke("No se pudo unir: " + msg);
    public override void OnJoinRandomFailed(short c, string msg)
        => CrearSala("Sala_" + Random.Range(1000, 9999)); // crea sala si no hay ninguna

    // ── Acciones públicas ────────────────────────────────────────

    public void SetNombre(string nombre)
    {
        if (!string.IsNullOrWhiteSpace(nombre))
            PhotonNetwork.NickName = nombre;
    }

    public void CrearSala(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) { OnError?.Invoke("Escribe un nombre de sala."); return; }
        PhotonNetwork.CreateRoom(nombre, new RoomOptions { MaxPlayers = maxJugadores });
    }

    public void UnirseASala(string nombre) => PhotonNetwork.JoinRoom(nombre);
    public void UnirseAlAzar()             => PhotonNetwork.JoinRandomRoom();
    public void AbandonarSala()            { if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom(); }

    public void VotarMapa(int opcion)      // opcion: 1 o 2
    {
        var props = new ExitGames.Client.Photon.Hashtable { [KEY_VOTO] = opcion };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void CambiarEquipo(int equipo)  // equipo: 1 o 2
    {
        var props = new ExitGames.Client.Photon.Hashtable { [KEY_EQUIPO] = equipo };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void PublicarPing()
    {
        var props = new ExitGames.Client.Photon.Hashtable { [KEY_PING] = PhotonNetwork.GetPing() };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // Solo el Master Client llama esto
    public void IniciarPartida()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_CuentaRegresiva), RpcTarget.All);
    }

    // ── Lógica interna ───────────────────────────────────────────

    void SortearMapas()
    {
        if (mapas.Length < 2) return;
        int i1 = Random.Range(0, mapas.Length);
        int i2;
        do { i2 = Random.Range(0, mapas.Length); } while (i2 == i1);

        PhotonNetwork.CurrentRoom.SetCustomProperties(
            new ExitGames.Client.Photon.Hashtable { [KEY_MAPA1] = i1, [KEY_MAPA2] = i2 });
    }

    [PunRPC]
    void RPC_CuentaRegresiva() => StartCoroutine(CuentaRegresiva());

    IEnumerator CuentaRegresiva()
    {
        for (int t = 5; t > 0; t--)
        {
            // Avisamos a la RoomUI cuántos segundos quedan
            RoomUI.Instance?.MostrarCuenta(t);
            yield return new WaitForSeconds(1f);
        }
        RoomUI.Instance?.MostrarCuenta(0);
        if (PhotonNetwork.IsMasterClient) CargarMapaGanador();
    }

    void CargarMapaGanador()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (!props.ContainsKey(KEY_MAPA1) || !props.ContainsKey(KEY_MAPA2)) return;

        int idx1 = (int)props[KEY_MAPA1];
        int idx2 = (int)props[KEY_MAPA2];

        int votos1 = 0, votos2 = 0;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.ContainsKey(KEY_VOTO)) continue;
            if ((int)p.CustomProperties[KEY_VOTO] == 1) votos1++; else votos2++;
        }

        int ganador = votos1 >= votos2 ? idx1 : idx2;
        PhotonNetwork.LoadLevel(mapas[ganador].nombreEscena);
    }
}
