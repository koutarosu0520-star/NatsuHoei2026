using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel; 
    private bool isPaused = false;

    void Update()
    {
        if (Keyboard.current == null) return;

        // Escキーが押されたら TogglePause() を実行する
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    // 【追加】ボタンからも呼び出せるように public（公開）にします
    public void TogglePause()
    {
        isPaused = !isPaused; 

        if (isPaused)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    // 【追加】ポーズ画面内の「再開ボタン」専用（確実にポーズを解除するため）
    public void ResumeGame()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }
}