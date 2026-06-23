using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class ClassSelector : MonoBehaviourPun
{
    [Header("Referencias UI (Canvas)")]
    public GameObject panelSeleccionClase; // El panel que ocupa toda la pantalla
    public TextMeshProUGUI textoTemporizador;

    [Header("Armas en el Jugador")]
    public GameObject armaRifle;
    public GameObject armaEscopeta;
    public GameObject armaSniper;
    public GameObject armaPistolaSecundaria;

    private float temporizador = 15f;
    private bool seleccionActiva = true;
    public static ClassSelector LocalInstance;

    void Start()
    {
        if (!photonView.IsMine)
        {
            if (panelSeleccionClase != null) panelSeleccionClase.SetActive(false);
            return;
        }

        LocalInstance = this; // <-- AÑADE ESTO AQUÍ: Le decimos al juego "Yo soy el jugador local"

        DesactivarArmasPrimarias();
        armaPistolaSecundaria.SetActive(false);

        if (panelSeleccionClase != null) panelSeleccionClase.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!photonView.IsMine || !seleccionActiva) return;

        temporizador -= Time.deltaTime;

        if (textoTemporizador != null)
        {
            textoTemporizador.text = "ELIGE TU CLASE: " + Mathf.Ceil(temporizador).ToString() + "s";
        }

        // Si se acaba el tiempo y no eligió, le damos el Rifle por defecto
        if (temporizador <= 0)
        {
            ElegirClaseRifle();
        }
    }

    // Estas funciones las conectarás a los 3 botones de tu Canvas
    public void ElegirClaseRifle() { EquiparClase(armaRifle); }
    public void ElegirClaseEscopeta() { EquiparClase(armaEscopeta); }
    public void ElegirClaseSniper() { EquiparClase(armaSniper); }

    private void EquiparClase(GameObject armaPrimariaSeleccionada)
    {
        seleccionActiva = false;

        // Escondemos el menú
        if (panelSeleccionClase != null) panelSeleccionClase.SetActive(false);

        // Bloqueamos el mouse para jugar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Apagamos todas por seguridad antes de entregarlas
        DesactivarArmasPrimarias();
        armaPistolaSecundaria.SetActive(false);

        // Buscamos el script de cambio de armas en este mismo jugador y le pasamos el Loadout
        WeaponSwitcher switcher = GetComponent<WeaponSwitcher>();
        if (switcher != null)
        {
            switcher.ConfigurarLoadout(armaPrimariaSeleccionada, armaPistolaSecundaria);
        }
        else
        {
            Debug.LogError("¡Falta ponerle el script WeaponSwitcher al jugador!");
        }
    }

    private void DesactivarArmasPrimarias()
    {
        if (armaRifle != null) armaRifle.SetActive(false);
        if (armaEscopeta != null) armaEscopeta.SetActive(false);
        if (armaSniper != null) armaSniper.SetActive(false);
    }
    public void AbrirMenuDeClases()
    {
        if (!photonView.IsMine) return;

        seleccionActiva = true;
        temporizador = 15f; // Le damos 15 segundos de nuevo

        if (panelSeleccionClase != null) panelSeleccionClase.SetActive(true);

        // Volvemos a liberar el cursor para que pueda hacer clic
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}