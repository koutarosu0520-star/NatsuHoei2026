using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 普通の幽霊と特別な幽霊を出現させるスポナー。
///
/// ・普通の幽霊は一定間隔で自動的に出現し続ける(同時出現数に上限あり)
/// ・普通の幽霊を一定数倒すと、特別な幽霊がまとめて数体出現する
///
/// 前提:
/// ・normalGhostPrefab / specialGhostPrefab には、Ghost.cs を付けたプレハブを設定し、
///   それぞれ Ghost Type を Normal / Special にしておくこと
/// ・Ghost.cs の OnGhostDefeated イベントを利用するため、Ghost.cs 側の対応も必要
/// </summary>
public class GhostSpawner : MonoBehaviour
{
    /// <summary>ボスの出現条件として、どの数値を見るかの選択肢</summary>
    public enum BossTriggerCondition
    {
        DefeatCount, // 撃破数のみで判定
        Score,       // スコアのみで判定
        Either,      // どちらか先に達成した方で判定(OR)
        Both         // 両方とも達成して初めて判定(AND)
    }

    [Header("ゲーム進行(GameController.csと連携)")]
    [SerializeField] private GameController gameController; // PlayState.Play の間だけ出現処理を行う

    [Header("プレハブ")]
    [SerializeField] private Ghost normalGhostPrefab;
    [SerializeField] private Ghost specialGhostPrefab;
    [SerializeField] private Ghost bossGhostPrefab; // Ghost Type = Boss にしたプレハブ

    [Header("出現エリア")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Vector2 spawnViewportMin = new Vector2(0.1f, 0.1f); // 画面内のどのあたりから出すか(0〜1)
    [SerializeField] private Vector2 spawnViewportMax = new Vector2(0.9f, 0.9f);

    [Header("普通の幽霊の出現設定")]
    [SerializeField] private float normalSpawnInterval = 2.5f; // 何秒おきに1体出すか
    [SerializeField] private int maxAliveNormalGhosts = 8;      // 同時に存在できる普通の幽霊の上限

    [Header("特別な幽霊の出現条件")]
    [SerializeField] private int normalDefeatsToTriggerSpecial = 5; // 普通の幽霊を何体倒したら特別な幽霊を出すか(仮の値、後で調整)
    [SerializeField] private int specialSpawnCount = 2;              // 一度にまとめて出す特別な幽霊の数

    [Header("ボスの出現条件(ステージに1体のみ)")]
    [SerializeField] private BossTriggerCondition bossTriggerCondition = BossTriggerCondition.Either;
    [SerializeField] private int totalDefeatsToTriggerBoss = 15; // 普通+特別の合計撃破数がこれに達したらボスを出す(仮の値)
    [SerializeField] private int scoreToTriggerBoss = 200;         // 合計スコアがこれに達したらボスを出す(仮の値)

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true;

    private readonly List<Ghost> aliveNormalGhosts = new List<Ghost>();
    private int normalDefeatCount = 0;
    private int totalDefeatCount = 0;
    private int totalScore = 0;
    private bool bossSpawned = false;
    private float normalSpawnTimer = 0f;

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
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        // Play中(ゲーム進行中)以外は出現処理を行わない
        if (gameController != null)
        {
            if (debugLog)
            {
                Debug.Log($"[GhostSpawner] GameController.CurrentState = {gameController.CurrentState}");
            }

            if (gameController.CurrentState != GameController.PlayState.Play)
            {
                return;
            }
        }
        else if (debugLog)
        {
            Debug.LogWarning("[GhostSpawner] Game Controller が未設定です(参照がnull)");
        }

        // リストの掃除(破棄済みの参照を除去)
        aliveNormalGhosts.RemoveAll(g => g == null);

        normalSpawnTimer -= Time.deltaTime;
        if (normalSpawnTimer <= 0f)
        {
            normalSpawnTimer = normalSpawnInterval;

            if (aliveNormalGhosts.Count < maxAliveNormalGhosts)
            {
                SpawnNormalGhost();
            }
        }
    }

    private void HandleGhostDefeated(Ghost ghost)
    {
        // ボスを倒したらステージクリア扱いにする
        if (ghost.Type == Ghost.GhostType.Boss)
        {
            if (debugLog)
            {
                Debug.Log("[GhostSpawner] ボスを撃破しました。ステージクリアにします");
            }

            if (gameController != null)
            {
                gameController.CurrentState = GameController.PlayState.Finish;
            }

            return; // ボスは合計撃破数のカウントや後続の出現判定に含めない
        }

        // ボスの出現条件は「普通+特別」の合計撃破数、または合計スコアでカウントする
        if (ghost.Type == Ghost.GhostType.Normal || ghost.Type == Ghost.GhostType.Special)
        {
            totalDefeatCount++;
            totalScore += ghost.ScoreValue;

            if (debugLog)
            {
                Debug.Log($"[GhostSpawner] 合計撃破数: {totalDefeatCount} / {totalDefeatsToTriggerBoss}、合計スコア: {totalScore} / {scoreToTriggerBoss}(ボス出現条件)");
            }

            bool defeatConditionMet = totalDefeatCount >= totalDefeatsToTriggerBoss;
            bool scoreConditionMet = totalScore >= scoreToTriggerBoss;

            bool shouldSpawnBoss = bossTriggerCondition switch
            {
                BossTriggerCondition.DefeatCount => defeatConditionMet,
                BossTriggerCondition.Score => scoreConditionMet,
                BossTriggerCondition.Either => defeatConditionMet || scoreConditionMet,
                BossTriggerCondition.Both => defeatConditionMet && scoreConditionMet,
                _ => false
            };

            if (!bossSpawned && shouldSpawnBoss)
            {
                SpawnBoss();
            }
        }

        if (ghost.Type != Ghost.GhostType.Normal) return;

        normalDefeatCount++;

        if (debugLog)
        {
            Debug.Log($"[GhostSpawner] 普通の幽霊を撃破: {normalDefeatCount} / {normalDefeatsToTriggerSpecial}");
        }

        if (normalDefeatCount >= normalDefeatsToTriggerSpecial)
        {
            normalDefeatCount = 0;
            SpawnSpecialGhosts(specialSpawnCount);
        }
    }

    private void SpawnNormalGhost()
    {
        if (normalGhostPrefab == null) return;

        Ghost ghost = Instantiate(normalGhostPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        aliveNormalGhosts.Add(ghost);

        if (debugLog)
        {
            Debug.Log($"[GhostSpawner] 普通の幽霊を出現: 現在 {aliveNormalGhosts.Count} 体");
        }
    }

    private void SpawnSpecialGhosts(int count)
    {
        if (specialGhostPrefab == null) return;

        for (int i = 0; i < count; i++)
        {
            Instantiate(specialGhostPrefab, GetRandomSpawnPosition(), Quaternion.identity);
        }

        if (debugLog)
        {
            Debug.Log($"[GhostSpawner] 特別な幽霊を {count} 体まとめて出現させました");
        }
    }

    private void SpawnBoss()
    {
        if (bossGhostPrefab == null)
        {
            Debug.LogWarning("[GhostSpawner] ボスの出現条件を満たしましたが、Boss Ghost Prefab が未設定(None)のため出現できません");
            return;
        }

        bossSpawned = true; // ステージに1体のみ。以後は出現条件を満たしても再度出さない
        Instantiate(bossGhostPrefab, GetRandomSpawnPosition(), Quaternion.identity);

        if (debugLog)
        {
            Debug.Log("[GhostSpawner] ボスを出現させました");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        if (targetCamera == null)
        {
            return Vector3.zero;
        }

        float vx = Random.Range(spawnViewportMin.x, spawnViewportMax.x);
        float vy = Random.Range(spawnViewportMin.y, spawnViewportMax.y);

        // カメラからの距離(奥行き)はカメラの初期zと同じ平面上に置く想定
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z);
        Vector3 worldPos = targetCamera.ViewportToWorldPoint(new Vector3(vx, vy, distanceFromCamera));
        worldPos.z = 0f;

        return worldPos;
    }
}
