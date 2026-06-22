using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class LeaveRoomButton : MonoBehaviourPunCallbacks
{
    [SerializeField] private string menuSceneName = "MainMenu";

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        SceneManager.LoadScene(menuSceneName);
    }
}