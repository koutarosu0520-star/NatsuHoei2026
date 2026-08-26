using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    [Tooltip("スタートボタンを押した時に読み込むゲームシーンの名前")]
    [SerializeField] private string gameSceneName = "game";

    // スタートボタンのOnClickイベントに登録する関数
    public void OnStartButtonPressed()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}