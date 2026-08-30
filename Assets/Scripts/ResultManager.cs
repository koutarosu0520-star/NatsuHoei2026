using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameController がタイムアップで PlayState.Finish になったタイミングを検知し、
/// QuotaManager の達成状況(IsCleared)を見て「クリア」か「ゲームオーバー」かを判定、
/// スコアや種類別の撃破数と合わせてリザルト画面に表示するマネージャー。
///
/// GameController.cs は変更していないため、CurrentState の変化を毎フレーム監視する形で実装している。
///
/// セットアップ:
/// ・Result Text は、GameController の Time Up Panel の中に置いたTextを指定するとよい
///   (タイムアップ時にパネルごと表示されるタイミングと合わせられるため)
/// ・LV3(ボスクリア)のステージでは Quota Manager を設定しなくてよい
///   (ボス撃破時は GhostSpawner 側で別途クリア処理をしているため、
///    このスクリプトは「タイムアップで終わった場合」のみを扱う)
/// ・種類別の撃破数(普通/特別/ボス)は、プレイ中ずっとカウントしておき、
///   リザルト表示のタイミングでまとめてテキストに反映する
/// </summary>
public class ResultManager : MonoBehaviour
{
    [Header("ゲーム進行(GameController.csと連携)")]
    [SerializeField] private GameController gameController;

    [Header("ノルマ(LV1/LV2用。未設定なら常にゲームオーバー扱い)")]
    [SerializeField] private QuotaManager quotaManager;

    [Header("リザルトパネル(タイムアップ/クリアの瞬間だけ表示する)")]
    [SerializeField] private GameObject resultPanel; // 5つのTextをまとめて入れておくパネル。最初は非アクティブにしておく

    [Header("クリア/ゲームオーバー表示")]
    [SerializeField] private Text resultText;
    [SerializeField] private string clearMessage = "GAME CLEAR";
    [SerializeField] private string gameOverMessage = "GAME OVER";

    [Header("サウンド(任意)")]
    [SerializeField] private AudioClip clearSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private float resultSoundVolume = 1f;

    [Header("集計表示(任意・未設定の項目は表示しなくてもOK)")]
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text normalGhostCountText;   // 白いお化け(普通の幽霊)
    [SerializeField] private Text specialGhostCountText;  // 赤いお化け(特別な幽霊)
    [SerializeField] private Text bossGhostCountText;      // 大きいお化け(ボス)
    [SerializeField] private string finalScoreFormat = "スコア: {0}";
    [SerializeField] private string normalGhostCountFormat = "白いお化け: {0}";
    [SerializeField] private string specialGhostCountFormat = "赤いお化け: {0}";
    [SerializeField] private string bossGhostCountFormat = "大きいお化け: {0}";

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true;

    private GameController.PlayState previousState = GameController.PlayState.None;
    private bool resultShown = false;

    // 種類別の撃破数(プレイ中ずっとカウントし続ける)
    private int normalDefeatedCount = 0;
    private int specialDefeatedCount = 0;
    private int bossDefeatedCount = 0;

    private void Awake()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
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

    private void HandleGhostDefeated(Ghost ghost)
    {
        switch (ghost.Type)
        {
            case Ghost.GhostType.Normal:
                normalDefeatedCount++;
                break;
            case Ghost.GhostType.Special:
                specialDefeatedCount++;
                break;
            case Ghost.GhostType.Boss:
                bossDefeatedCount++;
                break;
        }
    }

    private void Update()
    {
        if (gameController == null)
        {
            if (debugLog)
            {
                Debug.LogWarning("[ResultManager] Game Controller が未設定です(参照がnull)");
            }
            return;
        }

        GameController.PlayState current = gameController.CurrentState;

        // PlayState.Finish に「今まさに切り替わった瞬間」だけ処理する
        if (!resultShown && previousState != GameController.PlayState.Finish && current == GameController.PlayState.Finish)
        {
            ShowResult();
            resultShown = true;
        }

        previousState = current;
    }

    private void ShowResult()
    {
        // ノルマ達成(LV1/LV2)、またはボス撃破(LV3)のどちらかでクリア扱いにする
        bool cleared = (quotaManager != null && quotaManager.IsCleared) || bossDefeatedCount > 0;

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling(); // 他のUIより手前に表示する
        }

        if (debugLog)
        {
            Debug.Log($"[ResultManager] リザルト表示。ノルマ達成: {cleared} → {(cleared ? "CLEAR" : "GAME OVER")}" +
                       $" / 白:{normalDefeatedCount} 赤:{specialDefeatedCount} ボス:{bossDefeatedCount}");
        }

        if (resultText != null)
        {
            resultText.text = cleared ? clearMessage : gameOverMessage;
        }

        AudioClip soundToPlay = cleared ? clearSound : gameOverSound;
        if (soundToPlay != null)
        {
            // Time.timeScale が0の状態でも、音自体は通常通り再生される
            AudioSource.PlayClipAtPoint(soundToPlay, Camera.main != null ? Camera.main.transform.position : transform.position, resultSoundVolume);
        }

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;

        if (finalScoreText != null)
        {
            finalScoreText.text = string.Format(finalScoreFormat, finalScore);
        }
        if (normalGhostCountText != null)
        {
            normalGhostCountText.text = string.Format(normalGhostCountFormat, normalDefeatedCount);
        }
        if (specialGhostCountText != null)
        {
            specialGhostCountText.text = string.Format(specialGhostCountFormat, specialDefeatedCount);
        }
        if (bossGhostCountText != null)
        {
            bossGhostCountText.text = string.Format(bossGhostCountFormat, bossDefeatedCount);
        }
    }
}
