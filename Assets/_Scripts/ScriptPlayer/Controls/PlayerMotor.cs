using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerMotor : MonoBehaviourPun
{
    private CharacterController controller;
    private Vector3 playerVelocity;

    [Header("Configuración de Movimiento")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8.5f; // <-- Velocidad al correr
    private float currentSpeed;      // <-- Velocidad actual del jugador

    private bool isGrounded;
    public float gravity = -9.8f;
    public float jumpHeight = 1.2f;  // <-- Reducido de 3f a 1.2f para un salto rápido y táctico

    [Header("Photon & UI")]
    public GameObject fpCamera;
    public GameObject minimapCamera;
    public TextMeshPro nameText;

    [Header("UI del Jugador")]
    public GameObject hudCombate;
    void Start()
    {
        if (hudCombate != null)
        {
            hudCombate.SetActive(photonView.IsMine);
        }
        controller = GetComponent<CharacterController>();
        currentSpeed = walkSpeed; // Empezamos caminando por defecto

        //Camera
        fpCamera.SetActive(photonView.IsMine);

        //Movimiento
        enabled = photonView.IsMine;

        //Nombre Flotante
        nameText.gameObject.SetActive(!photonView.IsMine);
        nameText.text = photonView.Owner != null ? photonView.Owner.NickName : "Jugador Offline";

        if (minimapCamera != null)
        {
            minimapCamera.SetActive(photonView.IsMine);
        }
    }

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

        // Multiplicamos por currentSpeed en lugar de una velocidad estática
        controller.Move(
            transform.TransformDirection(moveDirection) * currentSpeed * Time.deltaTime
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

    // ---> NUEVA FUNCIÓN PARA EL NEW INPUT SYSTEM <---
    public void Sprint(bool isSprinting)
    {
        if (!isGrounded) return; // Opcional: Evita que empiece a correr estando en el aire

        if (isSprinting)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
    }
}
