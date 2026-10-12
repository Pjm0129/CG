using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LobbyManager : MonoBehaviour
{
    private bool isStarting = false;

    private void Update()
    {
        if (isStarting)
            return;

        bool mouseClicked =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        bool keyPressed =
            Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame;

        if (mouseClicked || keyPressed)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        isStarting = true;
        SceneManager.LoadScene("MainGame");
    }
}