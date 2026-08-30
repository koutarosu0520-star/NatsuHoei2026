using UnityEngine;

/// <summary>
/// 単発の効果音(ボタンクリック音など)を再生するマネージャー。
/// MusicManager と同様に DontDestroyOnLoad でシーンをまたいで生き続けるため、
/// 「ボタンを押した直後にシーン遷移が起きて音が途切れる」問題を防げる。
///
/// タイトル/セレクト画面のシーンに1つだけ置いておく(MusicManagerと同じオブジェクトでもよい)。
/// </summary>
public class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [SerializeField] private float defaultVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    /// <summary>
    /// 単発の効果音を再生する。シーン遷移が起きても再生中の音は途切れない。
    /// </summary>
    public void PlaySfx(AudioClip clip, float volume = -1f)
    {
        if (clip == null) return;

        float v = volume >= 0f ? volume : defaultVolume;
        audioSource.PlayOneShot(clip, v);
    }
}
