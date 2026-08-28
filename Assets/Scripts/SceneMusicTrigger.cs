using UnityEngine;

/// <summary>
/// シーン開始時に、そのシーン用のBGMをMusicManagerに再生させる。
/// タイトル/セレクト画面、ゲームプレイ画面など、それぞれのシーンに1つずつ置いて、
/// 対応する曲(sceneMusic)を設定しておく。
/// </summary>
public class SceneMusicTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip sceneMusic;
    [SerializeField] private bool loop = true;

    private void Start()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic(sceneMusic, loop);
        }
        else
        {
            Debug.LogWarning("[SceneMusicTrigger] MusicManager が見つかりません。タイトル/セレクト画面のシーンに MusicManager を配置してください");
        }
    }
}
