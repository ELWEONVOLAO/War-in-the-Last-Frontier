using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class WeaponSwitcher : MonoBehaviourPun
{
    [Header("Armas (objetos hijos, inactivos al inicio salvo el primero)")]
    public GameObject[] weapons;

    private int currentWeapon = 0;

    void Start()
    {
        // Solo el jugador local controla sus armas
        if (!photonView.IsMine) return;

        EquipWeapon(0);
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) EquipWeapon(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) EquipWeapon(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) EquipWeapon(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) EquipWeapon(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) EquipWeapon(4);
    }

    void EquipWeapon(int index)
    {
        if (index >= weapons.Length) return;

        for (int i = 0; i < weapons.Length; i++)
            weapons[i].SetActive(i == index);

        currentWeapon = index;
    }
}