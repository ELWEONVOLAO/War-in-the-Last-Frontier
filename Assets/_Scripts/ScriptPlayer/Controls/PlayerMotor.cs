using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerMotor : MonoBehaviourPun
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private float speed = 5f;
    private bool isGrounded;
    public float gravity = -9.8f;
    public float jumpHeight = 3f;

    //Photon
    public GameObject fpCamera;
    public GameObject minimapCamera; 
    public TextMeshPro nameText;
    void Start()
    {
        controller = GetComponent<CharacterController>();

        //Camera
        fpCamera.SetActive(photonView.IsMine);

        //Movimiento
        enabled = photonView.IsMine;

        //Nombre Flotante
        nameText.gameObject.SetActive(!photonView.IsMine);
        nameText.text = photonView.Owner != null ? photonView.Owner.NickName : "Jugador Offline";

        if (UIManager.Instance != null && UIManager.Instance.isGamePaused)
        {
            return;
        }
        if (minimapCamera != null)
        {
            minimapCamera.SetActive(photonView.IsMine);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine) return;

        isGrounded = controller.isGrounded;
    }

    public void ProcessMove(Vector2 input)
    {
        if (!photonView.IsMine) return;

        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        controller.Move(
            transform.TransformDirection(moveDirection) * speed * Time.deltaTime
        );

        playerVelocity.y += gravity * Time.deltaTime;

        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;

        controller.Move(playerVelocity * Time.deltaTime);
    }

    public void Jump()
    {
        if (!photonView.IsMine) return;

        if (isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }
}
