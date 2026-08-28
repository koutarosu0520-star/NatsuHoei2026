using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 写真モード中の「WASDで枠を選ぶ→スペースでシャッター」を管理するスクリプト。
///
/// 枠とキーの対応はステージごとに変わる仕様なので、
/// Inspectorで各枠に割り当てるキーを設定できるようにしてある。
/// ステージ管理スクリプト側から SetFrameKeyMapping() を呼べば、
/// ステージ開始時に対応を差し替えられる。
///
/// GameModeController から SetActive(true/false) で有効/無効を切り替える想定。
/// </summary>
public class PhotoModeController : MonoBehaviour
{
    public enum WasdKey { W, A, S, D }

    [System.Serializable]
    private class FrameKeyBinding
    {
        public PhotoFrame frame;
        public WasdKey key;
    }

    [Header("枠とキーの対応(ステージごとに差し替え可能)")]
    [SerializeField] private FrameKeyBinding[] bindings = new FrameKeyBinding[3];

    [Header("サウンド(任意)")]
    [SerializeField] private AudioClip shutterSound;
    [SerializeField] private AudioClip shutterMissSound; // 枠内に何もいなかった時の音(任意)
    [SerializeField] private float shutterSoundVolume = 1f;

    [Header("クールタイム")]
    [SerializeField] private float shutterCooldown = 1f; // シャッターを切ってから次に切れるまでの秒数

    [Header("演出(任意)")]
    [SerializeField] private PhotoFlashEffect flashEffect;

    private float cooldownTimer = 0f;

    private int selectedIndex = -1;
    private bool isActive = false;

    private void Update()
    {
        if (!isActive) return;
        if (Keyboard.current == null) return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        ReadFrameSelectionInput();

        if (Keyboard.current.spaceKey.wasPressedThisFrame && cooldownTimer <= 0f)
        {
            TryShutter();
        }
    }

    private void ReadFrameSelectionInput()
    {
        for (int i = 0; i < bindings.Length; i++)
        {
            if (IsKeyPressed(bindings[i].key))
            {
                Debug.Log($"[PhotoModeController] キー入力を検知、枠 {i} を選択します");
                SelectFrame(i);
                break; // 同フレームで複数押されても最初の1つを優先
            }
        }
    }

    private bool IsKeyPressed(WasdKey key)
    {
        switch (key)
        {
            case WasdKey.W: return Keyboard.current.wKey.wasPressedThisFrame;
            case WasdKey.A: return Keyboard.current.aKey.wasPressedThisFrame;
            case WasdKey.S: return Keyboard.current.sKey.wasPressedThisFrame;
            case WasdKey.D: return Keyboard.current.dKey.wasPressedThisFrame;
            default: return false;
        }
    }

    private void SelectFrame(int index)
    {
        if (selectedIndex == index) return;

        // 見た目を更新(前の選択を解除、新しい選択をハイライト)
        if (selectedIndex >= 0 && selectedIndex < bindings.Length && bindings[selectedIndex].frame != null)
        {
            bindings[selectedIndex].frame.SetSelected(false);
        }

        selectedIndex = index;

        if (bindings[selectedIndex].frame != null)
        {
            bindings[selectedIndex].frame.SetSelected(true);
        }
    }

    private void TryShutter()
    {
        if (selectedIndex < 0 || selectedIndex >= bindings.Length) return;

        PhotoFrame frame = bindings[selectedIndex].frame;
        if (frame == null) return;

        // シャッターを切った(成功/失敗にかかわらず)のでクールタイム開始
        cooldownTimer = shutterCooldown;

        if (flashEffect != null)
        {
            flashEffect.Flash();
        }

        List<Ghost> ghostsInFrame = frame.GetAllSpecialGhostsInside();

        if (ghostsInFrame.Count > 0)
        {
            foreach (Ghost ghost in ghostsInFrame)
            {
                ghost.KillByPhoto();
            }
            PlaySound(shutterSound);
        }
        else
        {
            PlaySound(shutterMissSound);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[PhotoModeController] AudioClipが未設定のため再生できません");
            return;
        }
        Debug.Log("[PhotoModeController] 効果音を再生: " + clip.name);
        AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : transform.position, shutterSoundVolume);
    }

    /// <summary>
    /// GameModeControllerから呼び出す。写真モードに入った/出たときの有効・無効切り替え。
    /// </summary>
    public void SetActive(bool active)
    {
        Debug.Log($"[PhotoModeController] SetActive({active}) が呼ばれました");
        isActive = active;

        // モードが切り替わったら、選択状態を一旦すべて解除する。
        // (写真モードに入った直後は、まだキーが押されていないので何も強調されない)
        foreach (FrameKeyBinding binding in bindings)
        {
            if (binding.frame != null)
            {
                binding.frame.SetSelected(false);
            }
        }
        selectedIndex = -1;
    }

    /// <summary>
    /// ステージ開始時などに、枠とキーの対応を差し替える。
    /// bindings配列と同じ並び順で新しいキーを渡す。
    /// </summary>
    public void SetFrameKeyMapping(WasdKey[] newKeys)
    {
        int count = Mathf.Min(newKeys.Length, bindings.Length);
        for (int i = 0; i < count; i++)
        {
            bindings[i].key = newKeys[i];
        }
    }
}
