using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ノルマ(「スコア」または「倒した数」 + 特別な幽霊の撮影数)の達成状況を追跡するマネージャー。
/// ボスを使わないステージ(LV1/LV2など、GhostSpawner の Boss Enabled = OFF)用。
///
/// 【重要】ノルマを達成しても、その場でゲームを終了させることはしない。
/// あくまで「達成したかどうか」を記録するだけで、実際の終了は
/// GameController のタイムアップ処理に任せる。タイムアップした時に、
/// このスクリプトの IsCleared を見て「クリア」か「ゲームオーバー」かを判定する
/// (判定・表示は ResultManager が行う)。
///
/// ボスを使うステージ(LV3など)では、このスクリプト自体を使わない
/// (または Use Quota For Clear を OFF にする)ことで、クリア判定は
/// GhostSpawner 側(ボス撃破でクリア)に任せる。
/// </summary>
public class QuotaManager : MonoBehaviour
{
    /// <summary>ノルマの片方(スコア枠)を、スコアで見るか倒した数で見るかの選択肢</summary>
    public enum QuotaMode
    {
        Score,       // 合計スコアで判定
        DefeatCount  // 倒した幽霊の合計数(普通+特別)で判定
    }

    [Header("クリア条件として使うかどうか")]
    [SerializeField] private bool useQuotaForClear = true; // OFFならこのスクリプトは判定を行わない(ボスクリアのステージ用)

    [Header("ノルマの内容")]
    [SerializeField] private QuotaMode quotaMode = QuotaMode.Score; // スコアで見るか、倒した数で見るか
    [SerializeField] private int requiredScore = 200;               // 必要スコア(仮の値。QuotaMode = Score の時に使用)
    [SerializeField] private int requiredDefeatCount = 20;           // 必要な合計撃破数(仮の値。QuotaMode = DefeatCount の時に使用)
    [SerializeField] private int requiredSpecialGhostPhotos = 3;     // 必要な特別な幽霊の撮影数(仮の値)

    [Header("UI表示(任意)")]
    [SerializeField] private Text scoreProgressText; // QuotaModeに応じて「スコア」か「撃破数」どちらかを表示する
    [SerializeField] private Text specialPhotoProgressText;
    [SerializeField] private string scoreProgressFormat = "Score: {0} / {1}";
    [SerializeField] private string defeatCountProgressFormat = "撃破数: {0} / {1}";
    [SerializeField] private string specialPhotoProgressFormat = "Special Ghost: {0} / {1}";

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true;

    private int specialGhostsPhotographed = 0;
    private int totalDefeatCount = 0; // 普通+特別の合計撃破数(QuotaMode = DefeatCount の時に使用)

    /// <summary>ノルマを達成しているかどうか(ResultManagerがタイムアップ時に参照する)</summary>
    public bool IsCleared { get; private set; } = false;

    private void OnEnable()
    {
        Ghost.OnGhostDefeated += HandleGhostDefeated;
        ScoreManager.OnScoreChanged += HandleScoreChanged;
    }

    private void OnDisable()
    {
        Ghost.OnGhostDefeated -= HandleGhostDefeated;
        ScoreManager.OnScoreChanged -= HandleScoreChanged;
    }

    private void Start()
    {
        UpdateScoreOrDefeatText();
        UpdateSpecialPhotoText();
    }

    private void HandleGhostDefeated(Ghost ghost)
    {
        // 撃破数モードで使う合計カウント(普通+特別)
        if (ghost.Type == Ghost.GhostType.Normal || ghost.Type == Ghost.GhostType.Special)
        {
            totalDefeatCount++;

            if (quotaMode == QuotaMode.DefeatCount)
            {
                UpdateScoreOrDefeatText();

                if (debugLog)
                {
                    Debug.Log($"[QuotaManager] 合計撃破数: {totalDefeatCount} / {requiredDefeatCount}");
                }
            }
        }

        if (ghost.Type != Ghost.GhostType.Special)
        {
            CheckClear();
            return;
        }

        specialGhostsPhotographed++;
        UpdateSpecialPhotoText();

        if (debugLog)
        {
            Debug.Log($"[QuotaManager] 特別な幽霊の撮影数: {specialGhostsPhotographed} / {requiredSpecialGhostPhotos}");
        }

        CheckClear();
    }

    private void HandleScoreChanged(int newScore)
    {
        if (quotaMode == QuotaMode.Score)
        {
            UpdateScoreOrDefeatText();
        }
        CheckClear();
    }

    /// <summary>
    /// 達成状況を更新するだけ。ゲームの終了処理は一切行わない。
    /// </summary>
    private void CheckClear()
    {
        if (!useQuotaForClear || IsCleared) return;

        bool mainConditionOk;
        if (quotaMode == QuotaMode.Score)
        {
            int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            mainConditionOk = currentScore >= requiredScore;
        }
        else
        {
            mainConditionOk = totalDefeatCount >= requiredDefeatCount;
        }

        bool specialPhotoOk = specialGhostsPhotographed >= requiredSpecialGhostPhotos;

        if (mainConditionOk && specialPhotoOk)
        {
            IsCleared = true;

            if (debugLog)
            {
                Debug.Log("[QuotaManager] ノルマ達成(まだゲームは終了しません。タイムアップ時にクリア扱いになります)");
            }
        }
    }

    private void UpdateScoreOrDefeatText()
    {
        if (scoreProgressText == null) return;

        if (quotaMode == QuotaMode.Score)
        {
            int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            scoreProgressText.text = string.Format(scoreProgressFormat, currentScore, requiredScore);
        }
        else
        {
            scoreProgressText.text = string.Format(defeatCountProgressFormat, totalDefeatCount, requiredDefeatCount);
        }
    }

    private void UpdateSpecialPhotoText()
    {
        if (specialPhotoProgressText != null)
        {
            specialPhotoProgressText.text = string.Format(specialPhotoProgressFormat, specialGhostsPhotographed, requiredSpecialGhostPhotos);
        }
    }
}
