using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スコアを一元管理するマネージャー。シーンに1つだけ配置する。
/// Ghost.cs の撃破時に AddScore() が呼ばれ、合計スコアを保持・UI表示する。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    /// <summary>スコアが変化するたびに呼ばれる(QuotaManagerなどが購読して使う)</summary>
    public static event Action<int> OnScoreChanged;

    [Header("UI表示(任意)")]
    [SerializeField] private Text scoreText;
    [SerializeField] private string scoreFormat = "Score: {0}";

    public int CurrentScore { get; private set; } = 0;

    private void Awake()
    {
        // シーンに1つだけ存在する想定。念のため重複防止。
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[ScoreManager] シーン内に複数のScoreManagerがあります。後から生成された方は無視します。");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>スコアを加算する(Ghost.cs の撃破時などから呼ばれる)</summary>
    public void AddScore(int amount)
    {
        CurrentScore += amount;
        UpdateText();
        OnScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>スコアをリセットする(ステージ開始時などに使用)</summary>
    public void ResetScore()
    {
        CurrentScore = 0;
        UpdateText();
        OnScoreChanged?.Invoke(CurrentScore);
    }

    private void UpdateText()
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, CurrentScore);
        }
    }
}
