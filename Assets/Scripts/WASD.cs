using UnityEngine;
using UnityEngine.InputSystem;

public class WASD : MonoBehaviour {
    // 速度を一般的な数値に戻します
    private float _speed = 50.0f; 

    private float _input_x;
    private float _input_y;

    void Update() {
        _input_x = 0f;
        _input_y = 0f;

        if (Keyboard.current != null) {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) _input_x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) _input_x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) _input_y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) _input_y += 1f;
        }

        Vector3 velocity = new Vector3(_input_x, _input_y, 0);
        
        if (velocity == Vector3.zero) return;

        Vector3 direction = velocity.normalized;
        float distance = _speed * Time.deltaTime;
        
        // 仮の移動先
        Vector3 destination = transform.position + direction * distance;

        // ----------------------------------------------------
        // 【修正ポイント】カメラの画面枠を使って自動で制限する
        // 1. 移動先の座標を、画面左下を(0,0)、右上を(1,1)とする「割合」に変換
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(destination);
        
        // 2. 割合を 0.05(5%) ～ 0.95(95%) の間に制限する
        // （0 と 1 にするとキャラクターが半分画面外にはみ出るため、少し余裕を持たせています）
        viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
        viewportPos.y = Mathf.Clamp(viewportPos.y, 0.05f, 0.95f);

        // 3. 制限した割合を、実際のゲーム内の座標に戻す
        destination = Camera.main.ViewportToWorldPoint(viewportPos);
        
        // 4. Z座標（奥行き）がカメラと同じ位置になってしまうのを防ぐため、元のZを維持する
        destination.z = transform.position.z;
        // ----------------------------------------------------

        // キャラクターの向き反転
        if (_input_x > 0) {
            transform.localScale = new Vector3(1, 1, 1);
        } else if (_input_x < 0) {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        transform.position = destination;
    }
}