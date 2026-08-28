using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ノルマ(スコア + 特別な幽霊の撮影数)を達成したらステージクリアにするマネージャー。
/// ボスを使わないステージ(LV1/LV2など、GhostSpawner の Boss Enabled = OFF)用。
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

    [Header("ゲーム進行(GameController.csと連携)")]
    [SerializeField] private GameController gameController;

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true;

    private int specialGhostsPhotographed = 0;
    private bool cleared = false;

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

    private void CheckClear()
    {
        if (!useQuotaForClear || cleared) return;

        int currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;

        bool scoreOk = currentScore >= requiredScore;
        bool specialPhotoOk = specialGhostsPhotographed >= requiredSpecialGhostPhotos;

        if (scoreOk && specialPhotoOk)
        {
            cleared = true;

            if (debugLog)
            {
                Debug.Log("[QuotaManager] ノルマ達成。ステージクリアにします");
            }

            if (gameController != null)
            {
                gameController.CurrentState = GameController.PlayState.Finish;
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
