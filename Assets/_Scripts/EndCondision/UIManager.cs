using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Pantallas")]
    public GameObject hudScreen;
    public GameObject victoryScreen;
    public GameObject defeatScreen;
    public GameObject tieScreen;

    [Header("HUD - Marcador y Tiempo")]
    public TextMeshProUGUI textTeam1Score;
    public TextMeshProUGUI textTeam2Score;
    public TextMeshProUGUI textTimer;

    [Header("Pantallas de fin - Textos opcionales")]
    public TextMeshProUGUI txtWinnerName;

    [Header("Scoreboard Settings")]
    public Transform team1Container;
    public Transform team2Container;
    public GameObject playerRowPrefab;

    // Panel general del scoreboard que activaremos/desactivaremos
    public GameObject scoreboardPanel;

    [Header("Menú de Pausa y Mira")]
    public GameObject pausePanel;
    public UnityEngine.UI.Image crosshairImage; // Asegúrate de usar UnityEngine.UI
    public Sprite[] crosshairSprites; // Aquí pondrás tus diferentes diseños de mira
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        HideAll();
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Bloqueamos el cursor en el centro y lo ocultamos al iniciar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CargarCrosshairGuardada();
    }
    void Update()
    {
        // 2. REEMPLAZA EL UPDATE CON ESTO:

        // Verificamos que haya un teclado conectado para evitar errores
        if (Keyboard.current != null)
        {
            // Detecta cuando PRESIONAS la tecla Tab
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ToggleScoreboard(true);
            }
            // Detecta cuando SUELTAS la tecla Tab
            else if (Keyboard.current.tabKey.wasReleasedThisFrame)
            {
                ToggleScoreboard(false);
            }
            // Lógica del Scoreboard (Tab)
            
            if (Keyboard.current.tabKey.wasPressedThisFrame)
                ToggleScoreboard(true);
            else if (Keyboard.current.tabKey.wasReleasedThisFrame)
                ToggleScoreboard(false);

            // NUEVO: Lógica del Menú de Pausa (Escape)
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    // ── Llamados desde GameManager ───────────────────────────────

    public void ShowHUD()
    {
        HideAll();
        if (hudScreen != null) hudScreen.SetActive(true);
    }

    public void UpdateScoreUI(int score1, int score2)
    {
        if (textTeam1Score != null) textTeam1Score.text = score1.ToString();
        if (textTeam2Score != null) textTeam2Score.text = score2.ToString();
    }

    public void UpdateTimerUI(int timeInSeconds)
    {
        if (textTimer == null) return;

        int minutes = timeInSeconds / 60;
        int seconds = timeInSeconds % 60;

        textTimer.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void ShowEndScreen(int winnerTeam, int myTeam)
    {
        HideAll();

        if (winnerTeam == 0)
        {
            if (tieScreen != null) tieScreen.SetActive(true);
            if (txtWinnerName != null) txtWinnerName.text = "¡EMPATE!";
        }
        else if (winnerTeam == myTeam)
        {
            if (victoryScreen != null) victoryScreen.SetActive(true);
            if (txtWinnerName != null) txtWinnerName.text = $"¡EQUIPO {winnerTeam} GANA!";
        }
        else
        {
            if (defeatScreen != null) defeatScreen.SetActive(true);
            if (txtWinnerName != null) txtWinnerName.text = $"¡EQUIPO {winnerTeam} GANA!";
        }
    }

    // ── Lógica del Scoreboard ────────────────────────────────────

    public void ToggleScoreboard(bool show)
    {
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(show);
            if (show) ActualizarScoreboard();
        }
    }

    private void ActualizarScoreboard()
    {
        // 1. Limpiamos los contenedores para no duplicar la lista
        foreach (Transform child in team1Container) Destroy(child.gameObject);
        foreach (Transform child in team2Container) Destroy(child.gameObject);

        // 2. Recorremos a todos los jugadores en la sala
        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            int team = p.CustomProperties.ContainsKey("Team") ? (int)p.CustomProperties["Team"] : 1;
            int kills = p.CustomProperties.ContainsKey("Kills") ? (int)p.CustomProperties["Kills"] : 0;
            int deaths = p.CustomProperties.ContainsKey("Deaths") ? (int)p.CustomProperties["Deaths"] : 0;

            // Leemos el ping (si es el jugador local, lo tomamos directo de Photon para mayor precisión)
            int ping = p.CustomProperties.ContainsKey("Ping") ? (int)p.CustomProperties["Ping"] : 0;
            if (p.IsLocal) ping = PhotonNetwork.GetPing();

            Transform targetContainer = (team == 1) ? team1Container : team2Container;
            GameObject row = Instantiate(playerRowPrefab, targetContainer);

            // 3. Asignamos los textos
            TextMeshProUGUI[] textos = row.GetComponentsInChildren<TextMeshProUGUI>();

            // Requerimos que el prefab tenga 4 textos: Nombre, Kills, Deaths, Ping
            if (textos.Length >= 4)
            {
                textos[0].text = p.NickName;
                textos[1].text = kills.ToString();
                textos[2].text = deaths.ToString();
                textos[3].text = ping.ToString() + " ms";

                // Opcional: Cambiar color del ping (verde si es bueno, rojo si es malo)
                if (ping < 80) textos[3].color = Color.green;
                else if (ping < 150) textos[3].color = Color.yellow;
                else textos[3].color = Color.red;
            }
        }
    }

    // ── Helper ───────────────────────────────────────────────────

    void HideAll()
    {
        if (hudScreen != null) hudScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (defeatScreen != null) defeatScreen.SetActive(false);
        if (tieScreen != null) tieScreen.SetActive(false);
    }
    // ── Lógica de Pausa y Mira ───────────────────────────────────

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pausePanel != null) pausePanel.SetActive(isPaused);

        if (isPaused)
        {
            // Pausado: liberamos el mouse para usar los botones y ocultamos el HUD rápido
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (crosshairImage != null) crosshairImage.gameObject.SetActive(false);
            ToggleScoreboard(false); // Cerramos scoreboard por si acaso
        }
        else
        {
            // Jugando: volvemos a bloquear el mouse y mostramos la mira
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (crosshairImage != null) crosshairImage.gameObject.SetActive(true);
        }
    }

    public void BotonSalirDePartida()
    {
        // Esto le avisa a Photon que te vas. 
        // Tu GameManager detectará esto (OnLeftRoom) y cargará la lobby automáticamente.
        PhotonNetwork.LeaveRoom();
    }

    public void CargarCrosshairGuardada()
    {
        // PlayerPrefs nos permite guardar datos en el PC del jugador.
        // Aquí leemos qué índice de mira guardó (por defecto 0).
        int index = PlayerPrefs.GetInt("CrosshairIndex", 0);

        if (crosshairImage != null && crosshairSprites.Length > 0 && index < crosshairSprites.Length)
        {
            crosshairImage.sprite = crosshairSprites[index];
        }
    }
}