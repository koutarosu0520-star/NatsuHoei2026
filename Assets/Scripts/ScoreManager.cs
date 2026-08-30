using System;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; } 
    public static event Action<int> OnScoreChanged;

    [Header("種類別のノルマ設定")]
    [SerializeField] private int normalGhostQuota = 10;
    [SerializeField] private int specialGhostQuota = 5;
    [SerializeField] private int bossGhostQuota = 1;

    private int normalCount = 0;
    private int specialCount = 0;
    private int bossCount = 0;

    [Header("UI設定")]
    [SerializeField] private TextMeshProUGUI scoreText; 
    
    // ▼ 追加：Unityのインスペクターから画像名を直接入力できるようにしました
    [Header("アイコンの画像名")]
    [SerializeField] private string normalSpriteName = "ghost2";
    [SerializeField] private string specialSpriteName = "ghost2_red";
    [SerializeField] private string bossSpriteName = "ghost3_spark";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        Ghost.OnGhostDefeated += HandleGhostDefeated;
    }

    private void OnDisable()
    {
        Ghost.OnGhostDefeated -= HandleGhostDefeated;
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    private void HandleGhostDefeated(Ghost ghost)
    {
        if (ghost.Type == Ghost.GhostType.Normal) normalCount++;
        else if (ghost.Type == Ghost.GhostType.Special) specialCount++;
        else if (ghost.Type == Ghost.GhostType.Boss) bossCount++;
        
        UpdateScoreUI();

        if (normalCount >= normalGhostQuota && 
            specialCount >= specialGhostQuota && 
            bossCount >= bossGhostQuota)
        {
            OnQuotaAchieved();
        }
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount; 
        OnScoreChanged?.Invoke(CurrentScore);
    }

    private void UpdateScoreUI()
    {
        if (scoreText == null) return;

        string uiText = "";

        // ▼ 変更：index=0 等ではなく、指定した画像名(normalSpriteName等)で呼び出す
        if (normalGhostQuota > 0)
        {
            uiText += $"<sprite name=\"{normalSpriteName}\"> x {normalGhostQuota} : {normalCount}\n";
        }
        if (specialGhostQuota > 0)
        {
            uiText += $"<sprite name=\"{specialSpriteName}\"> x {specialGhostQuota} : {specialCount}\n";
        }
        if (bossGhostQuota > 0)
        {
            uiText += $"<sprite name=\"{bossSpriteName}\"> x {bossGhostQuota} : {bossCount}\n";
        }

        scoreText.text = uiText.TrimEnd();
    }

    public void SetQuotas(int normal, int special, int boss)
    {
        normalGhostQuota = normal;
        specialGhostQuota = special;
        bossGhostQuota = boss;
        UpdateScoreUI();
    }

    private void OnQuotaAchieved()
    {
        // 全ノルマ達成時の処理
    }
}