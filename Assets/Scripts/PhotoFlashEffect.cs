using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// シャッターを切った瞬間に、画面全体を白くフラッシュさせる演出。
///
/// セットアップ:
/// ・Canvas(Screen Space - Overlay)の下に、画面いっぱいに広げた Image を1つ作る
/// ・その Image に本スクリプトをアタッチ
/// ・Image の色は白、初期状態はアルファ0(透明)にしておく
/// ・Raycast Target はOFFにしておく(他のUI操作を妨げないため)
/// </summary>
[RequireComponent(typeof(Image))]
public class PhotoFlashEffect : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.25f; // 白くなってから消えるまでの時間
    [SerializeField] private Color flashColor = Color.white;

    private Image image;
    private Coroutine flashRoutine;

    private void Awake()
    {
        image = GetComponent<Image>();
        SetAlpha(0f);
    }

    /// <summary>シャッターを切った瞬間に呼び出す</summary>
    public void Flash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetAlpha(1f); // 一瞬で真っ白に

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / flashDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0f);
        flashRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color c = flashColor;
        c.a = alpha;
        image.color = c;
    }
}
