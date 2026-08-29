using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ノルマ(スコア + 特別な幽霊の撮影数)の達成状況を追跡するマネージャー。
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
    [Header("クリア条件として使うかどうか")]
    [SerializeField] private bool useQuotaForClear = true; // OFFならこのスクリプトは判定を行わない(ボスクリアのステージ用)

    [Header("ノルマの内容")]
    [SerializeField] private int requiredScore = 200;               // 必要スコア(仮の値)
    [SerializeField] private int requiredSpecialGhostPhotos = 3;     // 必要な特別な幽霊の撮影数(仮の値)

    [Header("UI表示(任意)")]
    [SerializeField] private Text scoreProgressText;
    [SerializeField] private Text specialPhotoProgressText;
    [SerializeField] private string scoreProgressFormat = "Score: {0} / {1}";
    [SerializeField] private string specialPhotoProgressFormat = "Special Ghost: {0} / {1}";

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true;

    private int specialGhostsPhotographed = 0;

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
        UpdateScoreText(ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0);
        UpdateSpecialPhotoText();
    }

    private void HandleGhostDefeated(Ghost ghost)
    {
        if (ghost.Type != Ghost.GhostType.Special) return;

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
        UpdateScoreText(newScore);
        CheckClear();
    }

    /// <summary>
    /// 達成状況を更新するだけ。ゲームの終了処理は一切行わない。
    /// </summary>
    private void CheckClear()
    {
        if (!useQuotaForClear || IsCleared) return;

        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;

        bool scoreOk = currentScore >= requiredScore;
        bool specialPhotoOk = specialGhostsPhotographed >= requiredSpecialGhostPhotos;

        if (scoreOk && specialPhotoOk)
        {
            IsCleared = true;

            if (debugLog)
            {
                Debug.Log("[QuotaManager] ノルマ達成(まだゲームは終了しません。タイムアップ時にクリア扱いになります)");
            }
        }
    }

    private void UpdateScoreText(int currentScore)
    {
        if (scoreProgressText != null)
        {
            scoreProgressText.text = string.Format(scoreProgressFormat, currentScore, requiredScore);
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
