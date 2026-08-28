using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 写真モードの撮影枠1つ分。
/// 常に画面上の固定位置に表示され、Collider2D(Trigger)の中に
/// 「今、写真で撃てる幽霊」(Ghost.IsVulnerableToPhoto が true の幽霊)が
/// 入っているかどうかを検知する。
/// (特別な幽霊は常にtrue。ボスはスタン中のみtrue。普通の幽霊は対象外)
///
/// 前提:
/// ・このオブジェクトに Collider2D(IsTrigger = true)を付けておくこと
/// ・見た目(枠の絵)は SpriteRenderer などで別途用意し、常時表示しておく
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PhotoFrame : MonoBehaviour
{
    [Header("見た目(任意)")]
    [SerializeField] private SpriteRenderer frameRenderer; // 選択中/非選択の色分けに使う(未設定でも可)
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private float selectedScaleMultiplier = 1.15f; // 選択中に枠を少し拡大して強調する

    private Vector3 originalScale;

    // 枠の中にいる幽霊は種類を問わず全て記録しておき、
    // 実際に狙えるかどうか(IsVulnerableToPhoto)は取得時に判定する。
    // (ボスはライトフェーズ中は対象外、スタン中だけ対象になるなど、状態が変化するため)
    private readonly List<Ghost> ghostsInside = new List<Ghost>();

    private void Awake()
    {
        originalScale = transform.localScale;
        SetSelected(false); // 開始時は必ず非選択の見た目にしておく
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ghost ghost = other.GetComponent<Ghost>();
        if (ghost != null && !ghostsInside.Contains(ghost))
        {
            ghostsInside.Add(ghost);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Ghost ghost = other.GetComponent<Ghost>();
        if (ghost != null)
        {
            ghostsInside.Remove(ghost);
        }
    }

    private void Update()
    {
        // 消滅済み(Destroy済み)の参照を掃除
        ghostsInside.RemoveAll(g => g == null);
    }

    /// <summary>
    /// 現在この枠の中にいて、かつ写真で撃てる幽霊を1体取得する(いなければfalse)。
    /// </summary>
    public bool TryGetSpecialGhostInside(out Ghost ghost)
    {
        ghostsInside.RemoveAll(g => g == null);

        foreach (Ghost g in ghostsInside)
        {
            if (g.IsVulnerableToPhoto)
            {
                ghost = g;
                return true;
            }
        }

        ghost = null;
        return false;
    }

    /// <summary>
    /// 現在この枠の中にいて、かつ写真で撃てる幽霊を全員取得する(コピーを返すので、
    /// 呼び出し側でKillByPhoto()などにより中身が変化しても安全)。
    /// </summary>
    public List<Ghost> GetAllSpecialGhostsInside()
    {
        ghostsInside.RemoveAll(g => g == null);

        List<Ghost> result = new List<Ghost>();
        foreach (Ghost g in ghostsInside)
        {
            if (g.IsVulnerableToPhoto)
            {
                result.Add(g);
            }
        }
        return result;
    }

    /// <summary>選択中かどうかの見た目を切り替える(PhotoModeControllerから呼ばれる)</summary>
    public void SetSelected(bool isSelected)
    {
        if (frameRenderer != null)
        {
            frameRenderer.color = isSelected ? selectedColor : normalColor;
        }

        transform.localScale = isSelected ? originalScale * selectedScaleMultiplier : originalScale;
    }

    /// <summary>
    /// 枠自体の表示/非表示を切り替える。ビデオモード中は false にして、
    /// 見た目だけでなく判定(Collider)も止める。
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (frameRenderer != null)
        {
            frameRenderer.enabled = visible;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = visible;
        }

        if (!visible)
        {
            // 非表示にする瞬間、中に幽霊が入っている記録が残らないようにする
            ghostsInside.Clear();
        }
    }
}
