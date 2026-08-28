using UnityEngine;

/// <summary>
/// シーンをまたいで音楽を流し続けるマネージャー。
/// タイトル/セレクト画面のシーンに1つだけ置いておけば、
/// DontDestroyOnLoad により以降どのシーンに移動しても生き続ける。
///
/// 使い方:
/// ・各シーンに SceneMusicTrigger を置き、そこから PlayMusic() を呼び出す想定
/// ・同じ曲が既に再生中なら何もしない(=シーンを行き来しても曲が頭から再スタートしない)
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("再生設定")]
    [SerializeField] private float defaultVolume = 0.6f;
    [SerializeField] private float fadeDuration = 0.5f; // 曲を切り替える時のフェード時間

    private AudioSource audioSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        // シーンをまたいでも1つだけに保つ
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
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = defaultVolume;
    }

    /// <summary>
    /// 指定した曲を再生する。既に同じ曲が再生中なら何もしない(流し続ける)。
    /// 違う曲の場合はフェードしながら切り替える。
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        // 既に同じ曲が流れているなら、何もせず継続する
        if (audioSource.clip == clip && audioSource.isPlaying)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = StartCoroutine(CrossfadeTo(clip, loop));
    }

    /// <summary>音楽を止める(フェードアウト)</summary>
    public void StopMusic()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = StartCoroutine(FadeOutAndStop());
    }

    private System.Collections.IEnumerator CrossfadeTo(AudioClip newClip, bool loop)
    {
        // フェードアウト
        yield return StartCoroutine(FadeVolume(audioSource.volume, 0f));

        audioSource.clip = newClip;
        audioSource.loop = loop;
        audioSource.Play();

        // フェードイン
        yield return StartCoroutine(FadeVolume(0f, defaultVolume));
    }

    private System.Collections.IEnumerator FadeOutAndStop()
    {
        yield return StartCoroutine(FadeVolume(audioSource.volume, 0f));
        audioSource.Stop();
    }

    private System.Collections.IEnumerator FadeVolume(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        audioSource.volume = to;
    }
}
