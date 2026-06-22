using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class MenuManager : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class DatosMapa
    {
        public string nombreAMostrar;
        public string nombreEscena;
        public Sprite imagenMapa;
    }

    [Header("--- NAVEGACION DE PANELES ---")]
    public GameObject panelMenu;
    public GameObject panelConfiguraciones;
    public GameObject panelJuego;
    public GameObject panelSalas;
    public GameObject panelSalaJuego;

    [Header("--- UI REFERENCIAS (Inputs y Textos) ---")]
    public TMP_InputField inputNombreJugador;
    public TMP_InputField inputNombreSala;
    public TextMeshProUGUI textoEstadoGlobal;

    // ---> ¡AQUÍ ESTÁ TU NUEVA VARIABLE! <---
    public TextMeshProUGUI textoNombreSalaActual;

    [Header("--- LISTA DINAMICA DE SALAS ---")]
    public GameObject roomItemPrefab;
    public Transform roomListContent;
    private Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    [Header("--- LOBBY: EQUIPOS Y JUGADORES ---")]
    public Transform contentEquipo1;
    public Transform contentEquipo2;
    public Button botonIniciarPartida;

    [Header("--- CONFIGURACIÓN DE MAPAS ---")]
    public DatosMapa[] todosLosMapas;

    [Header("UI Votación - Opción 1")]
    public Image uiImagenOpcion1;
    public TextMeshProUGUI uiNombreOpcion1;
    public TextMeshProUGUI textoVotosMapa1;

    [Header("UI Votación - Opción 2")]
    public Image uiImagenOpcion2;
    public TextMeshProUGUI uiNombreOpcion2;
    public TextMeshProUGUI textoVotosMapa2;

    private int indiceMapa1 = -1;
    private int indiceMapa2 = -1;
    private int slotsPorEquipo = 5;
    private Dictionary<int, TextMeshProUGUI> pingLabels = new Dictionary<int, TextMeshProUGUI>();
    private Coroutine pingCoroutine;

    private static readonly Color colorSlotOcupado = new Color(0.15f, 0.15f, 0.20f, 1f);
    private static readonly Color colorSlotVacio = new Color(0.08f, 0.08f, 0.10f, 0.8f);
    private static readonly Color colorNombreJugador = new Color(0.90f, 0.90f, 1.00f, 1f);
    private static readonly Color colorNombreEspera = new Color(0.45f, 0.45f, 0.55f, 1f);
    private static readonly Color colorPingBueno = new Color(0.30f, 1.00f, 0.40f, 1f);
    private static readonly Color colorPingMedio = new Color(1.00f, 0.85f, 0.20f, 1f);
    private static readonly Color colorPingMalo = new Color(1.00f, 0.30f, 0.20f, 1f);

    void Awake()
    {
        // 1. Configuramos Photon lo antes posible
        PhotonNetwork.AutomaticallySyncScene = true;

        // 2. Nos conectamos en el Awake si no estamos conectados
        if (!PhotonNetwork.IsConnected)
        {
            // Cargamos el nombre guardado súper rápido
            if (PlayerPrefs.HasKey("NombreJugadorGuardado"))
            {
                PhotonNetwork.NickName = PlayerPrefs.GetString("NombreJugadorGuardado");
            }
            else
            {
                PhotonNetwork.NickName = "Jugador_" + Random.Range(1000, 9999);
            }

            // Lanzamos la conexión al Master Server
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    void Start()
    {
        // El Start se encarga EXCLUSIVAMENTE de la interfaz gráfica y los paneles,
        // porque aquí ya estamos 100% seguros de que el Canvas y los botones existen.

        if (GameManager.retornarAlMenuSala)
        {
            panelMenu.SetActive(false);
            panelConfiguraciones.SetActive(false);
            panelJuego.SetActive(false);
            panelSalaJuego.SetActive(false);

            panelSalas.SetActive(true);
            GameManager.retornarAlMenuSala = false;
        }
        else
        {
            VolverAlMenu();
        }

        // Actualizamos los textos visuales según el estado de Photon
        if (PhotonNetwork.IsConnected)
        {
            if (textoEstadoGlobal != null) textoEstadoGlobal.text = "Conectado. Buscando partidas...";
            if (inputNombreJugador != null) inputNombreJugador.text = PhotonNetwork.NickName;

            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
        }
        else
        {
            if (textoEstadoGlobal != null) textoEstadoGlobal.text = "Conectando al servidor...";
        }
    }

    public void cambiarMenu()
    {
        if (inputNombreJugador != null && !string.IsNullOrEmpty(inputNombreJugador.text))
        {
            // Asignamos el nombre a Photon
            PhotonNetwork.NickName = inputNombreJugador.text;

            // GUARDAMOS EL NOMBRE EN LA MEMORIA DEL PC
            PlayerPrefs.SetString("NombreJugadorGuardado", inputNombreJugador.text);
            PlayerPrefs.Save();
        }

        panelMenu.SetActive(false);
        panelJuego.SetActive(true);
    }

    public void configuraciones() { panelConfiguraciones.SetActive(true); panelMenu.SetActive(false); }

    public void VolverAlMenu()
    {
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();

        panelMenu.SetActive(true);
        panelConfiguraciones.SetActive(false);
        panelSalas.SetActive(false);
        panelJuego.SetActive(false);
        panelSalaJuego.SetActive(false);
    }

    public void Salir() => Application.Quit();

    public void CrearSala()
    {
        if (string.IsNullOrEmpty(inputNombreSala.text)) return;
        RoomOptions opciones = new RoomOptions { MaxPlayers = 10 };
        PhotonNetwork.CreateRoom(inputNombreSala.text, opciones);
    }

    public void UnirseASalaEspecifica() { panelJuego.SetActive(false); panelSalas.SetActive(true); }
    public void UnirseASalaAleatoria()
    {
        // 1. Evitamos bugs si el jugador ya está en una sala y apretó el botón por error
        if (PhotonNetwork.InRoom) return;

        // 2. Comprobamos que Photon esté listo para recibir el comando
        if (PhotonNetwork.IsConnectedAndReady)
        {
            if (textoEstadoGlobal != null) textoEstadoGlobal.text = "Buscando sala aleatoria...";
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            if (textoEstadoGlobal != null) textoEstadoGlobal.text = "Espera, conectando...";
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        // Si no encontró ninguna sala aleatoria disponible, creamos una nueva.
        if (textoEstadoGlobal != null) textoEstadoGlobal.text = "No hay salas disponibles. Creando una nueva...";

        RoomOptions opciones = new RoomOptions { MaxPlayers = 10 };
        PhotonNetwork.CreateRoom("Sala_" + Random.Range(1000, 10000), opciones);
    }

    public override void OnConnectedToMaster()
    {
        if (textoEstadoGlobal != null) textoEstadoGlobal.text = "Conectado. Buscando partidas...";
        PhotonNetwork.JoinLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo info in roomList)
        {
            if (info.RemovedFromList) cachedRoomList.Remove(info.Name);
            else cachedRoomList[info.Name] = info;
        }
        ActualizarListaSalasUI();
    }

    void ActualizarListaSalasUI()
    {
        foreach (Transform child in roomListContent) Destroy(child.gameObject);
        foreach (var room in cachedRoomList.Values)
        {
            GameObject item = Instantiate(roomItemPrefab, roomListContent);
            item.GetComponentInChildren<TextMeshProUGUI>().text = room.Name + " (" + room.PlayerCount + "/" + room.MaxPlayers + ")";
            item.GetComponent<Button>().onClick.AddListener(() => PhotonNetwork.JoinRoom(room.Name));
        }
    }

    public override void OnJoinedRoom()
    {
        panelMenu.SetActive(false);
        panelJuego.SetActive(false);
        panelSalas.SetActive(false);
        panelSalaJuego.SetActive(true);

        // ---> ¡NUEVO: AQUÍ MOSTRAMOS EL NOMBRE DE LA SALA! <---
        if (textoNombreSalaActual != null)
        {
            textoNombreSalaActual.text = "SALA: " + PhotonNetwork.CurrentRoom.Name;
        }

        slotsPorEquipo = Mathf.Max(1, PhotonNetwork.CurrentRoom.MaxPlayers / 2);

        if (PhotonNetwork.IsMasterClient)
        {
            if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("MapaOp1"))
            {
                SeleccionarMapasAlAzar();
            }
        }

        ActualizarMapasEnPantalla();
        ActualizarListaJugadoresUI();
        ContarVotosUI();
        RevisarSalaLlena();
        AsignarEquipoAutomatico();

        if (pingCoroutine != null) StopCoroutine(pingCoroutine);
        pingCoroutine = StartCoroutine(RefrescarPingPeriodico());
    }

    void SeleccionarMapasAlAzar()
    {
        if (todosLosMapas.Length < 2) return;

        int index1 = Random.Range(0, todosLosMapas.Length);
        int index2;

        do
        {
            index2 = Random.Range(0, todosLosMapas.Length);
        } while (index1 == index2);

        ExitGames.Client.Photon.Hashtable mapasProperties = new ExitGames.Client.Photon.Hashtable();
        mapasProperties.Add("MapaOp1", index1);
        mapasProperties.Add("MapaOp2", index2);

        PhotonNetwork.CurrentRoom.SetCustomProperties(mapasProperties);
    }

    void ActualizarMapasEnPantalla()
    {
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("MapaOp1"))
        {
            indiceMapa1 = (int)PhotonNetwork.CurrentRoom.CustomProperties["MapaOp1"];
            if (uiImagenOpcion1 != null) uiImagenOpcion1.sprite = todosLosMapas[indiceMapa1].imagenMapa;
            if (uiNombreOpcion1 != null) uiNombreOpcion1.text = todosLosMapas[indiceMapa1].nombreAMostrar;
        }

        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("MapaOp2"))
        {
            indiceMapa2 = (int)PhotonNetwork.CurrentRoom.CustomProperties["MapaOp2"];
            if (uiImagenOpcion2 != null) uiImagenOpcion2.sprite = todosLosMapas[indiceMapa2].imagenMapa;
            if (uiNombreOpcion2 != null) uiNombreOpcion2.text = todosLosMapas[indiceMapa2].nombreAMostrar;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ActualizarListaJugadoresUI();
        RevisarSalaLlena();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ActualizarListaJugadoresUI();
        ContarVotosUI();
    }

    public override void OnLeftRoom()
    {
        if (pingCoroutine != null)
        {
            StopCoroutine(pingCoroutine);
            pingCoroutine = null;
        }
        pingLabels.Clear();
        indiceMapa1 = -1;
        indiceMapa2 = -1;
    }

    void AsignarEquipoAutomatico()
    {
        int eq1 = 0, eq2 = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Team"))
            {
                int e = (int)p.CustomProperties["Team"];
                if (e == 1) eq1++;
                else if (e == 2) eq2++;
            }
        }
        CambiarEquipo(eq1 <= eq2 ? 1 : 2);
    }

    public void CambiarEquipo(int numeroEquipo)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["Team"] = numeroEquipo;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    void ActualizarListaJugadoresUI()
    {
        if (contentEquipo1 == null || contentEquipo2 == null) return;
        pingLabels.Clear();

        List<Player> eq1 = new List<Player>();
        List<Player> eq2 = new List<Player>();

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int equipo = 1;
            if (p.CustomProperties.ContainsKey("Team"))
                equipo = (int)p.CustomProperties["Team"];

            if (equipo == 2) eq2.Add(p);
            else eq1.Add(p);
        }

        ConstruirSlotsEquipo(contentEquipo1, eq1);
        ConstruirSlotsEquipo(contentEquipo2, eq2);

        if (botonIniciarPartida != null)
            botonIniciarPartida.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    void ConstruirSlotsEquipo(Transform contenedor, List<Player> jugadores)
    {
        foreach (Transform child in contenedor) Destroy(child.gameObject);

        VerticalLayoutGroup vlg = contenedor.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = contenedor.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f; vlg.childControlHeight = false; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true; vlg.padding = new RectOffset(4, 4, 4, 4);
        }

        for (int i = 0; i < slotsPorEquipo; i++)
        {
            bool tieneJugador = i < jugadores.Count;
            Player jugador = tieneJugador ? jugadores[i] : null;

            GameObject fila = new GameObject("Slot_" + i, typeof(RectTransform));
            fila.transform.SetParent(contenedor, false);
            fila.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36f);

            Image fondoFila = fila.AddComponent<Image>();
            fondoFila.color = tieneJugador ? colorSlotOcupado : colorSlotVacio;

            HorizontalLayoutGroup hlg = fila.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft; hlg.spacing = 6f; hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childControlHeight = true; hlg.childControlWidth = false; hlg.childForceExpandHeight = true; hlg.childForceExpandWidth = false;

            GameObject objNombre = new GameObject("LabelNombre", typeof(RectTransform));
            objNombre.transform.SetParent(fila.transform, false);
            LayoutElement leNombre = objNombre.AddComponent<LayoutElement>();
            leNombre.flexibleWidth = 1f; leNombre.preferredWidth = 120f;

            TextMeshProUGUI tmNombre = objNombre.AddComponent<TextMeshProUGUI>();
            tmNombre.fontSize = 14f; tmNombre.alignment = TextAlignmentOptions.MidlineLeft; tmNombre.overflowMode = TextOverflowModes.Ellipsis;

            if (tieneJugador)
            {
                tmNombre.text = jugador.NickName + (jugador.IsMasterClient ? " <color=#FFD700>(Host)</color>" : "");
                tmNombre.color = colorNombreJugador;
            }
            else
            {
                tmNombre.text = "Esperando jugador...";
                tmNombre.color = colorNombreEspera; tmNombre.fontStyle = FontStyles.Italic;
            }

            GameObject objPing = new GameObject("LabelPing", typeof(RectTransform));
            objPing.transform.SetParent(fila.transform, false);
            LayoutElement lePing = objPing.AddComponent<LayoutElement>();
            lePing.preferredWidth = 60f; lePing.flexibleWidth = 0f;

            TextMeshProUGUI tmPing = objPing.AddComponent<TextMeshProUGUI>();
            tmPing.fontSize = 13f; tmPing.alignment = TextAlignmentOptions.MidlineRight;

            if (tieneJugador)
            {
                int ping = ObtenerPingJugador(jugador);
                tmPing.text = ping + " ms"; tmPing.color = ColorSegunPing(ping);
                pingLabels[jugador.ActorNumber] = tmPing;
            }
            else
            {
                tmPing.text = ""; tmPing.color = Color.clear;
            }
        }
    }

    int ObtenerPingJugador(Player p)
    {
        if (p.IsLocal) return PhotonNetwork.GetPing();
        if (p.CustomProperties.ContainsKey("Ping")) return (int)p.CustomProperties["Ping"];
        return -1;
    }

    Color ColorSegunPing(int ping)
    {
        if (ping < 0) return colorNombreEspera;
        if (ping < 80) return colorPingBueno;
        if (ping < 150) return colorPingMedio;
        return colorPingMalo;
    }

    IEnumerator RefrescarPingPeriodico()
    {
        while (PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props["Ping"] = PhotonNetwork.GetPing();
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (pingLabels.TryGetValue(p.ActorNumber, out TextMeshProUGUI label) && label != null)
                {
                    int ping = ObtenerPingJugador(p);
                    label.text = ping >= 0 ? ping + " ms" : "-- ms";
                    label.color = ColorSegunPing(ping);
                }
            }
            yield return new WaitForSeconds(2f);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("MapaOp1") || propertiesThatChanged.ContainsKey("MapaOp2"))
        {
            ActualizarMapasEnPantalla();
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("VotoMapa")) ContarVotosUI();
        if (changedProps.ContainsKey("Team")) ActualizarListaJugadoresUI();
        if (changedProps.ContainsKey("Ping") && !changedProps.ContainsKey("Team"))
        {
            if (pingLabels.TryGetValue(targetPlayer.ActorNumber, out TextMeshProUGUI label) && label != null)
            {
                int ping = (int)changedProps["Ping"];
                label.text = ping + " ms";
                label.color = ColorSegunPing(ping);
            }
        }
    }

    public void VotarPorMapa(int indiceVoto)
    {
        ExitGames.Client.Photon.Hashtable voto = new ExitGames.Client.Photon.Hashtable();
        voto["VotoMapa"] = indiceVoto;
        PhotonNetwork.LocalPlayer.SetCustomProperties(voto);
    }

    void ContarVotosUI()
    {
        if (textoVotosMapa1 == null || textoVotosMapa2 == null) return;
        int votos1 = 0, votos2 = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("VotoMapa"))
            {
                if ((int)p.CustomProperties["VotoMapa"] == 1) votos1++;
                else if ((int)p.CustomProperties["VotoMapa"] == 2) votos2++;
            }
        }
        textoVotosMapa1.text = "Votos: " + votos1;
        textoVotosMapa2.text = "Votos: " + votos2;
    }

    void RevisarSalaLlena()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC("RPC_IniciarCuentaRegresiva", RpcTarget.All);
    }

    public void BotonHostIniciarPartida()
    {
        photonView.RPC("RPC_IniciarCuentaRegresiva", RpcTarget.All);
    }

    [PunRPC]
    void RPC_IniciarCuentaRegresiva() => StartCoroutine(RutinaCuentaRegresiva());

    IEnumerator RutinaCuentaRegresiva()
    {
        float tiempo = 5f;
        while (tiempo > 0)
        {
            if (textoEstadoGlobal != null) textoEstadoGlobal.text = "Iniciando partida en " + tiempo;
            yield return new WaitForSeconds(1f);
            tiempo--;
        }
        if (textoEstadoGlobal != null) textoEstadoGlobal.text = "¡Cargando mapa!";
        if (PhotonNetwork.IsMasterClient) CargarMapaGanador();
    }

    void CargarMapaGanador()
    {
        int votosOpcion1 = 0, votosOpcion2 = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("VotoMapa"))
            {
                if ((int)p.CustomProperties["VotoMapa"] == 1) votosOpcion1++;
                else if ((int)p.CustomProperties["VotoMapa"] == 2) votosOpcion2++;
            }
        }

        int indiceGanador = (votosOpcion1 >= votosOpcion2) ? indiceMapa1 : indiceMapa2;
        string nombreEscenaCargar = todosLosMapas[indiceGanador].nombreEscena;

        PhotonNetwork.LoadLevel(nombreEscenaCargar);

    }
}