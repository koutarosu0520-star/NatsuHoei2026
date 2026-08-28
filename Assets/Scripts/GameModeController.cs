using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tabキーでビデオモード⇔写真モードを切り替える司令塔。
/// ・ビデオモード中:Spotlightの移動(WASD)を有効化
/// ・写真モード中:Spotlightの移動を無効化し、PhotoModeControllerを有効化(WASDが枠選択になる)
/// </summary>
public class GameModeController : MonoBehaviour
{
    public enum CameraMode { Video, Photo }

    [SerializeField] private Spotlight spotlight;
    [SerializeField] private PhotoModeController photoModeController;
    [SerializeField] private CameraMode startMode = CameraMode.Video;

    public CameraMode CurrentMode { get; private set; }

    private void Start()
    {
        SetMode(startMode);
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            Debug.LogWarning("[GameModeController] Keyboard.current が null です(Input Systemの設定を確認してください)");
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            Debug.Log("[GameModeController] Tabキーを検知しました。現在のモード: " + CurrentMode);
            ToggleMode();
            Debug.Log("[GameModeController] 切り替え後のモード: " + CurrentMode);
        }
    }

    private void ToggleMode()
    {
        SetMode(CurrentMode == CameraMode.Video ? CameraMode.Photo : CameraMode.Video);
    }

    private void SetMode(CameraMode mode)
    {
        CurrentMode = mode;

        bool isVideo = mode == CameraMode.Video;

        if (spotlight != null)
        {
            spotlight.SetMovementEnabled(isVideo);
        }

        if (photoModeController != null)
        {
            photoModeController.SetActive(!isVideo);
        }
    }
}