using UnityEngine;
using TMPro;
using Photon.Pun;

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
    public TextMeshProUGUI textTimer; // NUEVO: Texto para el temporizador

    [Header("Pantallas de fin - Textos opcionales")]
    public TextMeshProUGUI txtWinnerName;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        HideAll();
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

    // NUEVO: Función para formatear y mostrar el tiempo
    public void UpdateTimerUI(int timeInSeconds)
    {
        if (textTimer == null) return;

        int minutes = timeInSeconds / 60;
        int seconds = timeInSeconds % 60;

        // Formato "00:00"
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

    // ── Helper ───────────────────────────────────────────────────

    void HideAll()
    {
        if (hudScreen != null) hudScreen.SetActive(false);
        if (victoryScreen != null) victoryScreen.SetActive(false);
        if (defeatScreen != null) defeatScreen.SetActive(false);
        if (tieScreen != null) tieScreen.SetActive(false);
    }
}