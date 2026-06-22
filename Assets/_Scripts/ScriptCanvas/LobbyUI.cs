using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Realtime;
using System.Collections.Generic;

// ─────────────────────────────────────────────
//  LobbyUI
//  Maneja: PanelMenu, PanelLobby, PanelSalas
//  No sabe nada de Photon directamente,
//  solo llama métodos de NetworkManager
// ─────────────────────────────────────────────
public class LobbyUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelMenu;
    public GameObject panelLobby;
    public GameObject panelSalas;

    [Header("Panel Menu")]
    public TMP_InputField inputNombre;
    public TextMeshProUGUI txtEstado;   // "Conectando..." / "Conectado"

    [Header("Panel Lobby")]
    public TMP_InputField inputNombreSala;

    [Header("Panel Salas")]
    public Transform contenedorSalas;       // Content del ScrollView
    public GameObject prefabItemSala;       // prefab simple: Button + TMP
    public TextMeshProUGUI txtSinSalas;

    // Cache de salas
    private Dictionary<string, RoomInfo> cacheSalas = new Dictionary<string, RoomInfo>();

    // ── Lifecycle ────────────────────────────────────────────────

    void Start()
    {
        // Suscribirse a eventos de red
        NetworkManager.Instance.OnConectado              += AlConectarse;
        NetworkManager.Instance.OnSalaUnida              += AlEntrarSala;
        NetworkManager.Instance.OnSalaAbandonada         += AlSalirSala;
        NetworkManager.Instance.OnListaSalasActualizada  += ActualizarListaSalas;
        NetworkManager.Instance.OnError                  += MostrarError;

        // Estado inicial
        txtEstado.text = "Conectando...";
        MostrarPanel(panelMenu);
    }

    void OnDestroy()
    {
        if (NetworkManager.Instance == null) return;
        NetworkManager.Instance.OnConectado              -= AlConectarse;
        NetworkManager.Instance.OnSalaUnida              -= AlEntrarSala;
        NetworkManager.Instance.OnSalaAbandonada         -= AlSalirSala;
        NetworkManager.Instance.OnListaSalasActualizada  -= ActualizarListaSalas;
        NetworkManager.Instance.OnError                  -= MostrarError;
    }

    // ── Botones del Panel Menu ───────────────────────────────────

    public void BotonJugar()
    {
        NetworkManager.Instance.SetNombre(inputNombre.text);
        MostrarPanel(panelLobby);
    }

    public void BotonVolver() => MostrarPanel(panelMenu);

    public void BotonSalir() => Application.Quit();

    // ── Botones del Panel Lobby ──────────────────────────────────

    public void BotonCrearSala()
        => NetworkManager.Instance.CrearSala(inputNombreSala.text.Trim());

    public void BotonVerSalas()
        => MostrarPanel(panelSalas);

    public void BotonAleatorio()
        => NetworkManager.Instance.UnirseAlAzar();

    public void BotonVolverDesdeListaSalas()
        => MostrarPanel(panelLobby);

    // ── Callbacks de red ────────────────────────────────────────

    void AlConectarse()
    {
        txtEstado.text = "✓ Conectado";
    }

    void AlEntrarSala()
    {
        // La RoomUI se encarga; nosotros solo ocultamos todo
        panelMenu.SetActive(false);
        panelLobby.SetActive(false);
        panelSalas.SetActive(false);
    }

    void AlSalirSala()
    {
        MostrarPanel(panelMenu);
    }

    void MostrarError(string msg)
    {
        txtEstado.text = "✗ " + msg;
        MostrarPanel(panelLobby);
    }

    // ── Lista de salas ───────────────────────────────────────────

    void ActualizarListaSalas(List<RoomInfo> lista)
    {
        // Actualizar cache
        foreach (var info in lista)
        {
            if (info.RemovedFromList) cacheSalas.Remove(info.Name);
            else cacheSalas[info.Name] = info;
        }

        // Redibujar
        foreach (Transform hijo in contenedorSalas) Destroy(hijo.gameObject);

        int visibles = 0;
        foreach (var sala in cacheSalas.Values)
        {
            if (!sala.IsOpen || !sala.IsVisible) continue;
            visibles++;

            GameObject item = Instantiate(prefabItemSala, contenedorSalas);
            item.GetComponentInChildren<TextMeshProUGUI>().text =
                $"{sala.Name}  ({sala.PlayerCount}/{sala.MaxPlayers})";

            string nombre = sala.Name; // captura para el closure
            item.GetComponent<Button>().onClick.AddListener(
                () => NetworkManager.Instance.UnirseASala(nombre));
        }

        if (txtSinSalas != null)
            txtSinSalas.gameObject.SetActive(visibles == 0);
    }

    // ── Helper ───────────────────────────────────────────────────

    void MostrarPanel(GameObject objetivo)
    {
        panelMenu.SetActive(panelMenu   == objetivo);
        panelLobby.SetActive(panelLobby == objetivo);
        panelSalas.SetActive(panelSalas == objetivo);
    }
}
