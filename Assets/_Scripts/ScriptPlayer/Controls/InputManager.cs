using UnityEngine;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.PlayerActions Player;

    private PlayerMotor Play;
    private PlayerLook look;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = new PlayerInput();
        Player = playerInput.Player;
        Play = GetComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        Player.Jump.performed += ctx => Play.Jump();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Play.ProcessMove(Player.Movement.ReadValue<Vector2>());
    }
    private void LateUpdate()
    {
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
