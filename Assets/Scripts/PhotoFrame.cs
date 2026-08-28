using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 写真モードの撮影枠1つ分。
/// 常に画面上の固定位置に表示され、Collider2D(Trigger)の中に
/// 特別な幽霊(Ghost.GhostType.Special)が入っているかどうかを検知する。
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

    private readonly List<Ghost> specialGhostsInside = new List<Ghost>();

    private void Awake()
    {
        originalScale = transform.localScale;
        SetSelected(false); // 開始時は必ず非選択の見た目にしておく
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Ghost ghost = other.GetComponent<Ghost>();
        if (ghost != null && ghost.Type == Ghost.GhostType.Special)
        {
            if (!specialGhostsInside.Contains(ghost))
            {
                specialGhostsInside.Add(ghost);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Ghost ghost = other.GetComponent<Ghost>();
        if (ghost != null)
        {
            specialGhostsInside.Remove(ghost);
        }
    }

    private void Update()
    {
        // 消滅済み(Destroy済み)の参照を掃除
        specialGhostsInside.RemoveAll(g => g == null);
    }

    /// <summary>
    /// 現在この枠の中にいる特別な幽霊を1体取得する(いなければfalse)。
    /// </summary>
    public bool TryGetSpecialGhostInside(out Ghost ghost)
    {
        specialGhostsInside.RemoveAll(g => g == null);

        if (specialGhostsInside.Count > 0)
        {
            ghost = specialGhostsInside[0];
            return true;
        }

        ghost = null;
        return false;
    }

    /// <summary>
    /// 現在この枠の中にいる特別な幽霊を全員取得する(コピーを返すので、
    /// 呼び出し側でKillByPhoto()などにより中身が変化しても安全)。
    /// </summary>
    public List<Ghost> GetAllSpecialGhostsInside()
    {
        specialGhostsInside.RemoveAll(g => g == null);
        return new List<Ghost>(specialGhostsInside);
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
}
