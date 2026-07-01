using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI pingText; // <-- NUEVO
    [SerializeField] private Button button;

    public void Setup(RoomInfo info)
    {
        // 1. Nombre y Jugadores
        roomNameText.text = info.Name + " (" + info.PlayerCount + "/" + info.MaxPlayers + ")";
        
        // 2. Ping (Usamos tu ping actual al servidor)
        int miPing = PhotonNetwork.GetPing();
        pingText.text = miPing + " ms";

        // 3. Color según calidad del ping
        if (miPing < 80) pingText.color = Color.green;
        else if (miPing < 150) pingText.color = Color.yellow;
        else pingText.color = Color.red;

        // 4. Botón
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => PhotonNetwork.JoinRoom(info.Name));
    }
}