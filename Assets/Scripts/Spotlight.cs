using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// スポットライト(カメラの照準)をWASDで移動させるスクリプト。
/// ビデオモード中のみ移動でき、写真モード中は SetMovementEnabled(false) で止める想定。
///
/// 前提:
/// ・このオブジェクトに Collider2D(IsTrigger = true)を付け、タグを "Spotlight" にしておく
///   (Ghost側 NormalGhost / SpecialGhost がこのタグを見て「ライトが当たっているか」を判定する)
/// ・SpotLight2D と同じ位置に置く(このスクリプトを SpotLight2D と同じオブジェクトに付けてもよい)
/// </summary>
public class Spotlight : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float speed = 5.0f; // 元コードの50はビューポート換算では速すぎるため調整推奨

    private float inputX;
    private float inputY;

    // モードによって外部(モード管理側)から移動可否を切り替える
    private bool movementEnabled = true;

    private void Update()
    {
        if (!movementEnabled) return;

        ReadInput();
        Move();
    }

    private void ReadInput()
    {
        inputX = 0f;
        inputY = 0f;

        if (Keyboard.current == null) return;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputX -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputX += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputY -= 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputY += 1f;
    }

    private void Move()
    {
        Vector3 velocity = new Vector3(inputX, inputY, 0);
        if (velocity == Vector3.zero) return;

        Vector3 direction = velocity.normalized;
        float distance = speed * Time.deltaTime;

        Vector3 destination = transform.position + direction * distance;

        // 画面内に収まるようにビューポート基準でクランプ
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(destination);
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);

        destination = Camera.main.ViewportToWorldPoint(viewportPos);
        destination.z = transform.position.z; // 奥行きを維持

        transform.position = destination;
    }

    /// <summary>
    /// モード管理側(Tabキーでのモード切替を管理するスクリプト)から呼び出して、
    /// 写真モード中はfalseにして移動を止める。
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        // 移動を止めた瞬間、入力の残りカスで動き続けないようリセット
        if (!enabled)
        {
            inputX = 0f;
            inputY = 0f;
        }
    }
}
