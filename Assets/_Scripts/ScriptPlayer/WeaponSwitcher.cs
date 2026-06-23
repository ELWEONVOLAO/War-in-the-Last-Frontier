using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class WeaponSwitcher : MonoBehaviourPun
{
    [Header("Armas Equipadas")]
    public GameObject armaPrimaria;
    public GameObject armaSecundaria;

    [Header("Contenedor de la Cámara")]
    public Transform weaponHandler; // <-- Aquí irá tu objeto vacío de la vista

    private int armaActiva = 1;

    void Update()
    {
        if (!photonView.IsMine) return;
        if (UIManager.Instance != null && UIManager.Instance.isGamePaused) return;

        if (Mouse.current != null)
        {
            float scrollValue = Mouse.current.scroll.ReadValue().y;
            if (scrollValue > 0f || scrollValue < 0f)
            {
                AlternarArma();
            }
        }

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

        // Mueve el arma al WeaponHandler y la centra
        if (weaponHandler != null)
        {
            armaPrimaria.transform.SetParent(weaponHandler);
            armaPrimaria.transform.localPosition = Vector3.zero;
            armaPrimaria.transform.localRotation = Quaternion.identity;
        }

        armaPrimaria.SetActive(true);
        if (armaSecundaria != null) armaSecundaria.SetActive(false);
    }

    private void EquiparSecundaria()
    {
        if (armaSecundaria == null) return;
        armaActiva = 2;

        // Mueve el arma al WeaponHandler y la centra
        if (weaponHandler != null)
        {
            armaSecundaria.transform.SetParent(weaponHandler);
            armaSecundaria.transform.localPosition = Vector3.zero;
            armaSecundaria.transform.localRotation = Quaternion.identity;
        }

        armaSecundaria.SetActive(true);
        if (armaPrimaria != null) armaPrimaria.SetActive(false);
    }

    public void ConfigurarLoadout(GameObject primaria, GameObject secundaria)
    {
        armaPrimaria = primaria;
        armaSecundaria = secundaria;

        EquiparPrimaria();
    }
}