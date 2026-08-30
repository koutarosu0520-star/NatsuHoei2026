using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Button と同じオブジェクトにアタッチするだけで、クリック時に音を鳴らす。
/// Button の OnClick() に自動で登録するので、Inspectorで手動設定する必要はない。
///
/// SfxManager(シーンをまたいで生き続ける)経由で再生するため、
/// ボタンを押した直後にシーン遷移が起きても音が途切れない。
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float volume = 1f;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        button.onClick.AddListener(PlaySound);
    }

    private void PlaySound()
    {
        if (clickSound == null) return;

        if (SfxManager.Instance != null)
        {
            SfxManager.Instance.PlaySfx(clickSound, volume);
        }
        else
        {
            // SfxManagerが無い場合の保険(この場合はシーン遷移で音が途切れる可能性がある)
            Debug.LogWarning("[ButtonClickSound] SfxManager が見つかりません。タイトル/セレクト画面のシーンに SfxManager を配置してください");
            AudioSource.PlayClipAtPoint(clickSound, Camera.main != null ? Camera.main.transform.position : transform.position, volume);
        }
    }
}
