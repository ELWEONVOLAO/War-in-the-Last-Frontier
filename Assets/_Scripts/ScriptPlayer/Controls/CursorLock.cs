using UnityEngine;

public class CursorLock : MonoBehaviour
{
    // Lock and hide the cursor when the game starts
    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        // Press Escape to unlock and show the cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        // Press Left Mouse Button to lock and hide again
        if (Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    /// <summary>
    /// Locks the cursor to the center of the screen and hides it.
    /// </summary>
    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // Lock to center
        Cursor.visible = false;                   // Hide cursor
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;   // Free movement
        Cursor.visible = true;                    // Show cursor
    }
}
