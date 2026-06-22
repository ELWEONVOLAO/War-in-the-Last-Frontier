using UnityEngine;
using Photon.Pun;

public class PlayerLook : MonoBehaviourPun
{
    public Camera Cam;
    private float xRotation = 0f;

    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    void Update()
    {
        // SI EL JUEGO ESTÁ EN PAUSA, IGNORAMOS EL MOVIMIENTO DEL RATÓN Y LOS CLICS
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused)
        {
            return;
        }

        // ... [Aquí abajo va todo tu código normal de mover la cámara o disparar] ...
    }

    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        Cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
    }
}
