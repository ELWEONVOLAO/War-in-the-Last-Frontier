using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class WeaponSwitcher : MonoBehaviourPun
{
    [Header("Armas Equipadas (Asignadas automáticamente)")]
    public GameObject armaPrimaria;
    public GameObject armaSecundaria;

    private int armaActiva = 1; // 1 = Primaria, 2 = Secundaria

    void Update()
    {
        // Frenos de seguridad
        if (!photonView.IsMine) return;
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;

        // 1. Cambio con la rueda del ratón (Mouse Scroll)
        if (Mouse.current != null)
        {
            float scrollValue = Mouse.current.scroll.ReadValue().y;
            if (scrollValue > 0f || scrollValue < 0f)
            {
                AlternarArma();
            }
        }

        // 2. Cambio con las teclas alfanuméricas 1 y 2
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame && armaActiva != 1)
            {
                EquiparPrimaria();
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame && armaActiva != 2)
            {
                EquiparSecundaria();
            }
        }
    }

    private void AlternarArma()
    {
        if (armaActiva == 1) EquiparSecundaria();
        else EquiparPrimaria();
    }

    private void EquiparPrimaria()
    {
        if (armaPrimaria == null) return;
        armaActiva = 1;
        armaPrimaria.SetActive(true);
        if (armaSecundaria != null) armaSecundaria.SetActive(false);
    }

    private void EquiparSecundaria()
    {
        if (armaSecundaria == null) return;
        armaActiva = 2;
        armaSecundaria.SetActive(true);
        if (armaPrimaria != null) armaPrimaria.SetActive(false);
    }

    // Esta función es llamada por el menú de clases de los 15 segundos
    public void ConfigurarLoadout(GameObject primaria, GameObject secundaria)
    {
        armaPrimaria = primaria;
        armaSecundaria = secundaria;

        // Al elegir la clase, siempre empezamos con la primaria en la mano
        EquiparPrimaria();
    }
}