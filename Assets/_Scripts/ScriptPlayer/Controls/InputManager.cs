using UnityEngine;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.PlayerActions Player;

    private PlayerMotor Play;
    private PlayerLook look;

    void Awake()
    {
        playerInput = new PlayerInput();
        Player = playerInput.Player;
        Play = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();

        // Freno para el salto
        Player.Jump.performed += ctx =>
        {
            if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;
            Play.Jump();
        };
    }

    void FixedUpdate()
    {
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused)
        {
            Play.ProcessMove(Vector2.zero);
            return;
        }

        Play.ProcessMove(Player.Movement.ReadValue<Vector2>());
    }

    private void LateUpdate()
    {
        // Freno de pausa para la cámara
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;

        look.ProcessLook(Player.Look.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        Player.Enable();
    }

    private void OnDisable()
    {
        Player.Disable();
    }
}