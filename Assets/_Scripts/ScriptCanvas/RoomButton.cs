using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class RoomButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText; 
    [SerializeField] private TextMeshProUGUI pingText; 
    [SerializeField] private Button button;

    public void Setup(RoomInfo info)
    {
        roomNameText.text = info.Name + " (" + info.PlayerCount + "/" + info.MaxPlayers + ")";
        
        int miPing = PhotonNetwork.GetPing();
        pingText.text = miPing + " ms";

        if (miPing < 80) pingText.color = Color.green;
        else if (miPing < 150) pingText.color = Color.yellow;
        else pingText.color = Color.red;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => PhotonNetwork.JoinRoom(info.Name));
    }
}