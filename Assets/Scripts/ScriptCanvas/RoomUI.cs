using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;

// ─────────────────────────────────────────────
//  RoomUI
//  Maneja: sala de espera, equipos, votación,
//          ping en vivo y cuenta regresiva
// ─────────────────────────────────────────────
public class RoomUI : MonoBehaviourPunCallbacks
{
    public static RoomUI Instance { get; private set; }

    [Header("Panel raíz")]
    public GameObject panelSala;

    [Header("Info sala")]
    public TextMeshProUGUI txtNombreSala;
    public TextMeshProUGUI txtCuenta;       // "Iniciando en 3..." / vacío

    [Header("Votación mapas")]
    public Image     imagenMapa1;
    public Image     imagenMapa2;
    public TextMeshProUGUI txtNombreMapa1;
    public TextMeshProUGUI txtNombreMapa2;
    public TextMeshProUGUI txtVotosMapa1;
    public TextMeshProUGUI txtVotosMapa2;

    [Header("Jugadores (prefab simple)")]
    public Transform contenedorEquipo1;
    public Transform contenedorEquipo2;
    public GameObject prefabJugador;        // prefab: solo un TextMeshProUGUI

    [Header("Botones")]
    public Button btnIniciar;               // solo visible para el Host
    public Button btnSalir;

    // Internos
    private Dictionary<int, TextMeshProUGUI> labelesPing = new Dictionary<int, TextMeshProUGUI>();
    private Coroutine rutinaPing;

    // ── Lifecycle ────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        panelSala.SetActive(false);

        NetworkManager.Instance.OnSalaUnida      += AlEntrarSala;
        NetworkManager.Instance.OnSalaAbandonada += AlSalirSala;

        btnIniciar.onClick.AddListener(NetworkManager.Instance.IniciarPartida);
        btnSalir.onClick.AddListener(NetworkManager.Instance.AbandonarSala);
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnSalaUnida      -= AlEntrarSala;
        NetworkManager.Instance.OnSalaAbandonada -= AlSalirSala;
    }

    // ── Entrada / Salida de sala ─────────────────────────────────

    void AlEntrarSala()
    {
        panelSala.SetActive(true);
        txtNombreSala.text = PhotonNetwork.CurrentRoom.Name;
        txtCuenta.text = "";

        RefrescarMapas();
        RefrescarJugadores();
        RefrescarVotos();

        // El jugador nuevo se asigna al equipo con menos gente
        AsignarEquipoAutomatico();

        if (rutinaPing != null) StopCoroutine(rutinaPing);
        rutinaPing = StartCoroutine(LoopPing());
    }

    void AlSalirSala()
    {
        panelSala.SetActive(false);
        labelesPing.Clear();
        if (rutinaPing != null) StopCoroutine(rutinaPing);
    }

    // ── Callbacks Photon ─────────────────────────────────────────

    public override void OnPlayerEnteredRoom(Player newPlayer)  => RefrescarJugadores();
    public override void OnPlayerLeftRoom(Player otherPlayer)   { RefrescarJugadores(); RefrescarVotos(); }
    public override void OnMasterClientSwitched(Player newMaster) => RefrescarJugadores();

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable props)
    {
        if (props.ContainsKey(NetworkManager.KEY_MAPA1) ||
            props.ContainsKey(NetworkManager.KEY_MAPA2))
            RefrescarMapas();
    }

    public override void OnPlayerPropertiesUpdate(Player jugador, ExitGames.Client.Photon.Hashtable props)
    {
        if (props.ContainsKey(NetworkManager.KEY_VOTO))   RefrescarVotos();
        if (props.ContainsKey(NetworkManager.KEY_EQUIPO)) RefrescarJugadores();
        if (props.ContainsKey(NetworkManager.KEY_PING))   ActualizarPingLabel(jugador);
    }

    // ── Botones de votación y equipo (llamar desde los botones UI) ──

    public void VotarMapa1() => NetworkManager.Instance.VotarMapa(1);
    public void VotarMapa2() => NetworkManager.Instance.VotarMapa(2);
    public void UnirseEquipo1() => NetworkManager.Instance.CambiarEquipo(1);
    public void UnirseEquipo2() => NetworkManager.Instance.CambiarEquipo(2);

    // ── Cuenta regresiva (llamado por NetworkManager vía RPC) ────

    public void MostrarCuenta(int segundos)
    {
        txtCuenta.text = segundos > 0 ? $"Iniciando en {segundos}..." : "¡Cargando mapa!";
    }

    // ── Refrescos de UI ──────────────────────────────────────────

    void RefrescarMapas()
    {
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (!props.ContainsKey(NetworkManager.KEY_MAPA1)) return;

        int idx1 = (int)props[NetworkManager.KEY_MAPA1];
        int idx2 = (int)props[NetworkManager.KEY_MAPA2];
        var mapas = NetworkManager.Instance.mapas;

        if (imagenMapa1)    imagenMapa1.sprite    = mapas[idx1].imagenMapa;
        if (imagenMapa2)    imagenMapa2.sprite    = mapas[idx2].imagenMapa;
        if (txtNombreMapa1) txtNombreMapa1.text   = mapas[idx1].nombreAMostrar;
        if (txtNombreMapa2) txtNombreMapa2.text   = mapas[idx2].nombreAMostrar;
    }

    void RefrescarVotos()
    {
        int v1 = 0, v2 = 0;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.ContainsKey(NetworkManager.KEY_VOTO)) continue;
            if ((int)p.CustomProperties[NetworkManager.KEY_VOTO] == 1) v1++; else v2++;
        }
        if (txtVotosMapa1) txtVotosMapa1.text = $"Votos: {v1}";
        if (txtVotosMapa2) txtVotosMapa2.text = $"Votos: {v2}";
    }

    void RefrescarJugadores()
    {
        labelesPing.Clear();
        LimpiarContenedor(contenedorEquipo1);
        LimpiarContenedor(contenedorEquipo2);

        var eq1 = new List<Player>();
        var eq2 = new List<Player>();

        foreach (var p in PhotonNetwork.PlayerList)
        {
            int equipo = p.CustomProperties.ContainsKey(NetworkManager.KEY_EQUIPO)
                ? (int)p.CustomProperties[NetworkManager.KEY_EQUIPO] : 1;
            (equipo == 2 ? eq2 : eq1).Add(p);
        }

        foreach (var p in eq1) CrearFilaJugador(p, contenedorEquipo1);
        foreach (var p in eq2) CrearFilaJugador(p, contenedorEquipo2);

        // Botón iniciar solo para el Host
        btnIniciar.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        // Revisar si la sala está llena → iniciar automáticamente
        var sala = PhotonNetwork.CurrentRoom;
        if (sala.PlayerCount == sala.MaxPlayers && PhotonNetwork.IsMasterClient)
            NetworkManager.Instance.IniciarPartida();
    }

    void CrearFilaJugador(Player jugador, Transform contenedor)
    {
        GameObject fila = Instantiate(prefabJugador, contenedor);
        var labels = fila.GetComponentsInChildren<TextMeshProUGUI>();

        // El prefab tiene 2 TMP: [0] nombre, [1] ping
        if (labels.Length >= 1)
        {
            string nombre = jugador.NickName +
                (jugador.IsMasterClient ? " <color=#FFD700>(Host)</color>" : "");
            labels[0].text = nombre;
        }
        if (labels.Length >= 2)
        {
            int ping = ObtenerPing(jugador);
            labels[1].text  = ping >= 0 ? ping + " ms" : "-- ms";
            labels[1].color = ColorPing(ping);
            labelesPing[jugador.ActorNumber] = labels[1];
        }
    }

    void ActualizarPingLabel(Player jugador)
    {
        if (!labelesPing.TryGetValue(jugador.ActorNumber, out var label) || label == null) return;
        int ping = ObtenerPing(jugador);
        label.text  = ping >= 0 ? ping + " ms" : "-- ms";
        label.color = ColorPing(ping);
    }

    // ── Ping ─────────────────────────────────────────────────────

    IEnumerator LoopPing()
    {
        while (PhotonNetwork.InRoom)
        {
            NetworkManager.Instance.PublicarPing();
            yield return new WaitForSeconds(2f);
        }
    }

    int ObtenerPing(Player p)
    {
        if (p.IsLocal) return PhotonNetwork.GetPing();
        return p.CustomProperties.ContainsKey(NetworkManager.KEY_PING)
            ? (int)p.CustomProperties[NetworkManager.KEY_PING] : -1;
    }

    Color ColorPing(int ping)
    {
        if (ping < 0)   return new Color(0.45f, 0.45f, 0.55f);
        if (ping < 80)  return new Color(0.30f, 1.00f, 0.40f);
        if (ping < 150) return new Color(1.00f, 0.85f, 0.20f);
        return                 new Color(1.00f, 0.30f, 0.20f);
    }

    // ── Helpers ──────────────────────────────────────────────────

    void LimpiarContenedor(Transform t) { foreach (Transform h in t) Destroy(h.gameObject); }

    void AsignarEquipoAutomatico()
    {
        int e1 = 0, e2 = 0;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            if (!p.CustomProperties.ContainsKey(NetworkManager.KEY_EQUIPO)) continue;
            if ((int)p.CustomProperties[NetworkManager.KEY_EQUIPO] == 1) e1++; else e2++;
        }
        NetworkManager.Instance.CambiarEquipo(e1 <= e2 ? 1 : 2);
    }
}
