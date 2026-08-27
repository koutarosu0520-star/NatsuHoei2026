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
    [Header("プレハブ")]
    [SerializeField] private Ghost normalGhostPrefab;
    [SerializeField] private Ghost specialGhostPrefab;

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

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true;

    private readonly List<Ghost> aliveNormalGhosts = new List<Ghost>();
    private int normalDefeatCount = 0;
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
