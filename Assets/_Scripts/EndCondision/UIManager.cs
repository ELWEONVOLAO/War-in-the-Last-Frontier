using Photon.Pun;
using Photon.Realtime;
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
    public GameObject prefabFilaEncabezado;
    public GameObject playerRowPrefab;
    public GameObject scoreboardPanel;

    [Header("Menú de Pausa y Mira")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public UnityEngine.UI.Image crosshairImage;
    public Sprite[] crosshairSprites;
    public bool isGamePaused = false;

    // ---> NUEVO: MUSICA DE FIN DE PARTIDA <---
    [Header("Música de Fin de Partida")]
    public AudioSource audioSourceMusica;
    public AudioClip musicaVictoria;
    public AudioClip musicaDerrota;
    
    // El "candado" que bloquea la interfaz al terminar
    private bool partidaTerminada = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        ShowHUD();
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CargarCrosshairGuardada();
    }

    void Update()
    {
        // ---> NUEVO: Si la partida terminó, ignoramos todo para que no abran menús <---
        if (partidaTerminada) return;

        if (Input.GetKeyDown(KeyCode.Tab)) ToggleScoreboard(true);
        else if (Input.GetKeyUp(KeyCode.Tab)) ToggleScoreboard(false);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGamePaused && settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
                pausePanel.SetActive(true);
            }
            else TogglePause();
        }

        if (isGamePaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

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

    // ---> LA MAGIA OCURRE AQUÍ <---
    public void ShowEndScreen(int winnerTeam, int myTeam)
    {
        // 1. Bloqueamos cualquier interacción de UI externa (pausa, scoreboard)
        partidaTerminada = true; 

        HideAll(); // Apaga el hudScreen

        // 2. APAGADO FORZADO DE TODO LO DEMÁS
        if (scoreboardPanel != null) scoreboardPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (crosshairImage != null) crosshairImage.gameObject.SetActive(false);

        // 3. Liberamos el cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Detenemos cualquier música anterior por seguridad
        if (audioSourceMusica != null) audioSourceMusica.Stop();

        // 5. Mostramos la pantalla correcta y reproducimos su música
        if (winnerTeam == 0)
        {
            if (tieScreen != null) tieScreen.SetActive(true);
            if (txtWinnerName != null) txtWinnerName.text = "¡EMPATE!";
            
            // Sonará la música de derrota por defecto para un empate
            if (audioSourceMusica != null && musicaDerrota != null) 
                audioSourceMusica.PlayOneShot(musicaDerrota);
        }
        else if (winnerTeam == myTeam)
        {
            if (victoryScreen != null) victoryScreen.SetActive(true);
            if (txtWinnerName != null) txtWinnerName.text = $"¡EQUIPO {winnerTeam} GANA!";
            
            // ---> MÚSICA VICTORIA <---
            if (audioSourceMusica != null && musicaVictoria != null) 
                audioSourceMusica.PlayOneShot(musicaVictoria);
        }
        else
        {
            if (defeatScreen != null) defeatScreen.SetActive(true);
            if (txtWinnerName != null) txtWinnerName.text = $"¡EQUIPO {winnerTeam} GANA!";
            
            // ---> MÚSICA DERROTA <---
            if (audioSourceMusica != null && musicaDerrota != null) 
                audioSourceMusica.PlayOneShot(musicaDerrota);
        }
    }

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
        foreach (Transform child in team1Container) Destroy(child.gameObject);
        foreach (Transform child in team2Container) Destroy(child.gameObject);

        if (prefabFilaEncabezado != null)
        {
            Instantiate(prefabFilaEncabezado, team1Container);
            Instantiate(prefabFilaEncabezado, team2Container);
        }

        foreach (Photon.Realtime.Player p in PhotonNetwork.PlayerList)
        {
            int team = p.CustomProperties.ContainsKey("Team") ? (int)p.CustomProperties["Team"] : 1;
            int kills = p.CustomProperties.ContainsKey("Kills") ? (int)p.CustomProperties["Kills"] : 0;
            int deaths = p.CustomProperties.ContainsKey("Deaths") ? (int)p.CustomProperties["Deaths"] : 0;

            int ping = p.CustomProperties.ContainsKey("Ping") ? (int)p.CustomProperties["Ping"] : 0;
            if (p.IsLocal) ping = PhotonNetwork.GetPing();

            Transform targetContainer = (team == 1) ? team1Container : team2Container;
            GameObject row = Instantiate(playerRowPrefab, targetContainer);

            TextMeshProUGUI[] textos = row.GetComponentsInChildren<TextMeshProUGUI>();

            if (textos.Length >= 4)
            {
                textos[0].text = p.NickName;
                textos[1].text = kills.ToString();
                textos[2].text = deaths.ToString();
                textos[3].text = ping.ToString() + " ms";

                if (ping < 80) textos[3].color = Color.green;
                else if (ping < 150) textos[3].color = Color.yellow;
                else textos[3].color = Color.red;
            }
        }
    }

    void HideAll()
    {
        if (hudScreen != null) hudScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (defeatScreen != null) defeatScreen.SetActive(false);
        if (tieScreen != null) tieScreen.SetActive(false);
    }

    public void TogglePause()
    {
        isGamePaused = !isGamePaused;

        if (pausePanel != null) pausePanel.SetActive(isGamePaused);

        if (isGamePaused)
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (crosshairImage != null) crosshairImage.gameObject.SetActive(false);
            ToggleScoreboard(false);
        }
        else
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (crosshairImage != null) crosshairImage.gameObject.SetActive(true);
        }
    }

    public void BotonSalirDePartida()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void BotonCambiarClase()
    {
        if (isGamePaused) TogglePause();
    }

    public void CargarCrosshairGuardada()
    {
        int index = PlayerPrefs.GetInt("CrosshairIndex", 0);
        if (crosshairImage != null && crosshairSprites.Length > 0 && index < crosshairSprites.Length)
        {
            crosshairImage.sprite = crosshairSprites[index];
        }
    }
}