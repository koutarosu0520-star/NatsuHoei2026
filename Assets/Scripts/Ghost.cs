using System.Collections;
using UnityEngine;

/// <summary>
/// 幽霊(普通の幽霊/特別な幽霊)の共通スクリプト。
/// ・ランダムに徘徊移動する
/// ・普通の幽霊はスポットライトに当たり続けると体力が減り、0になると消滅する
/// ・特別な幽霊はライトでは減らず、写真モードでの撮影(KillByPhoto)でのみ消滅する
/// ・画面外に出ると演出なしで消滅する
///
/// 前提:
/// ・スポットライトの照射範囲を表すオブジェクトに Collider2D(IsTrigger = true)を付け、
///   タグを "Spotlight" にしておく(Inspector の spotlightTag で変更可)
/// ・このスクリプトを付けるGhost側にも Collider2D(IsTrigger推奨)と
///   Rigidbody2D(BodyType = Kinematic)を付けておくとトリガー判定が安定します
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Ghost : MonoBehaviour
{
    public enum GhostType
    {
        Normal,   // 普通の幽霊:ライトで倒せる
        Special   // 特別な幽霊:ライト無効、写真モードでのみ倒せる
    }

    [Header("種類")]
    [SerializeField] private GhostType ghostType = GhostType.Normal;

    [Header("移動設定")]
    [SerializeField] private float moveSpeedNormal = 1.5f;
    [SerializeField] private float moveSpeedSpecial = 2.2f; // 特別な幽霊はやや速め
    [SerializeField] private float wanderRadius = 3f;       // 現在地からどこまでランダムな目的地を選ぶか
    [SerializeField] private float wanderInterval = 2f;      // 何秒ごとに目的地を選び直すか

    [Header("徘徊範囲")]
    [SerializeField] private bool useCameraBoundsForArea = true; // trueならカメラの映る範囲を自動で徘徊範囲にする
    [SerializeField] private Vector2 areaViewportMargin = new Vector2(0.05f, 0.05f); // カメラ基準の場合、画面端からの余白(ビューポート比率)
    [SerializeField] private Vector2 areaMin = new Vector2(-8, -4); // useCameraBoundsForArea = false の場合に使う固定範囲
    [SerializeField] private Vector2 areaMax = new Vector2(8, 4);

    [Header("ライト被弾設定(普通の幽霊のみ)")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damagePerSecond = 70f; // ライトに当たっている間、1秒あたり何ポイント体力を減らすか
    [SerializeField] private string spotlightTag = "Spotlight";

    [Header("演出")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite deadSprite;
    [SerializeField] private float shakeAmount = 0.05f;
    [SerializeField] private float shakeSpeed = 25f;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("サウンド")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathSoundVolume = 1f;

    [Header("スコア")]
    [SerializeField] private int scoreValue = 10;

    [Header("画面外判定")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float offscreenMargin = 0.5f;

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true; // 原因調査用。安定したらfalseにしてOK

    private SpriteRenderer spriteRenderer;

    // 実際の(震えを含まない)論理位置。Wander()はここを動かす。
    // 見た目のtransform.positionは、これに震えオフセットを足したものになる。
    private Vector3 basePosition;

    private Vector2 currentTarget;
    private float wanderTimer;

    private bool isLit = false;
    private float currentHealth;
    private bool isDying = false;

    private float MoveSpeed => ghostType == GhostType.Special ? moveSpeedSpecial : moveSpeedNormal;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
        basePosition = transform.position;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (useCameraBoundsForArea && targetCamera != null)
        {
            RecalculateAreaFromCamera();
        }

        PickNewWanderTarget();
    }

    /// <summary>カメラの映る範囲(ビューポート)から徘徊範囲(ワールド座標)を計算する</summary>
    private void RecalculateAreaFromCamera()
    {
        float z = -targetCamera.transform.position.z; // カメラからこのオブジェクトのz=0平面までの距離
        Vector3 min = targetCamera.ViewportToWorldPoint(new Vector3(areaViewportMargin.x, areaViewportMargin.y, z));
        Vector3 max = targetCamera.ViewportToWorldPoint(new Vector3(1f - areaViewportMargin.x, 1f - areaViewportMargin.y, z));

        areaMin = new Vector2(min.x, min.y);
        areaMax = new Vector2(max.x, max.y);
    }

    private void Update()
    {
        if (isDying) return;

        Wander();

        if (IsOutsideScreen())
        {
            // 画面外に出た場合は演出なし・得点なしで即座に消滅する
            Destroy(gameObject);
            return;
        }

        if (isLit && ghostType == GhostType.Normal)
        {
            currentHealth -= damagePerSecond * Time.deltaTime;
            ApplyShake();

            if (debugLog)
            {
                Debug.Log($"[{gameObject.name}] HP = {currentHealth:F1} / {maxHealth}");
            }

            if (currentHealth <= 0f)
            {
                KillByLight();
            }
        }
        else
        {
            // ライトが当たっていない/対象外なら震えをリセット
            transform.position = basePosition;
        }
    }

    // ------------------------------
    // 移動(ランダム徘徊)
    // ------------------------------
    private void Wander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector2.Distance(basePosition, currentTarget) < 0.1f)
        {
            PickNewWanderTarget();
        }

        Vector2 pos = basePosition;
        Vector2 next = Vector2.MoveTowards(pos, currentTarget, MoveSpeed * Time.deltaTime);
        basePosition = new Vector3(next.x, next.y, basePosition.z);

        // ライトが当たっていない時はここで見た目にも反映する
        // (当たっている時はApplyShake側で震えを加えた位置を設定する)
        if (!isLit)
        {
            transform.position = basePosition;
        }
    }

    private void PickNewWanderTarget()
    {
        Vector2 origin = basePosition;
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        Vector2 candidate = origin + randomOffset;

        candidate.x = Mathf.Clamp(candidate.x, areaMin.x, areaMax.x);
        candidate.y = Mathf.Clamp(candidate.y, areaMin.y, areaMax.y);

        currentTarget = candidate;
        wanderTimer = wanderInterval;
    }

    // ------------------------------
    // 画面外判定
    // ------------------------------
    private bool IsOutsideScreen()
    {
        if (targetCamera == null) return false;

        Vector3 viewportPos = targetCamera.WorldToViewportPoint(transform.position);

        return viewportPos.x < -offscreenMargin
            || viewportPos.x > 1f + offscreenMargin
            || viewportPos.y < -offscreenMargin
            || viewportPos.y > 1f + offscreenMargin;
    }

    // ------------------------------
    // ライト(スポットライト)との接触判定
    // ------------------------------
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(spotlightTag))
        {
            isLit = true;
            if (debugLog) Debug.Log($"[{gameObject.name}] ライト接触開始: isLit = true");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(spotlightTag))
        {
            isLit = false;
            transform.position = basePosition;
            if (debugLog) Debug.Log($"[{gameObject.name}] ライト接触終了: isLit = false");
        }
    }

    // ------------------------------
    // 震え演出(ライトに当たっている間)
    // ------------------------------
    private void ApplyShake()
    {
        float offsetX = (Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) - 0.5f) * 2f * shakeAmount;
        float offsetY = (Mathf.PerlinNoise(0f, Time.time * shakeSpeed) - 0.5f) * 2f * shakeAmount;
        transform.position = basePosition + new Vector3(offsetX, offsetY, 0f);
    }

    // ------------------------------
    // 死亡処理
    // ------------------------------

    /// <summary>
    /// 普通の幽霊がライトで倒された時(体力が0になった時に呼ばれる)
    /// </summary>
    private void KillByLight()
    {
        if (isDying) return;
        StartCoroutine(DieRoutine(addScore: true));
    }

    /// <summary>
    /// 特別な幽霊が写真モードで撮影され撃破された時に、
    /// カメラ側のスクリプトから呼び出す想定のメソッド。
    /// </summary>
    public void KillByPhoto()
    {
        if (isDying) return;
        StartCoroutine(DieRoutine(addScore: true, instant: true));
    }

    private IEnumerator DieRoutine(bool addScore, bool instant = false)
    {
        isDying = true;

        if (addScore)
        {
            // TODO: ScoreManagerが実装されたら置き換える
            // ScoreManager.Instance.AddScore(scoreValue);
            Debug.Log($"[Ghost] +{scoreValue} スコア(ScoreManager未実装のため仮表示)");
        }

        // スポナー側などに「倒された」ことを知らせる
        OnGhostDefeated?.Invoke(this);

        // やられ音を再生(このオブジェクトが破棄されても音は最後まで鳴る)
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathSoundVolume);
        }

        if (deadSprite != null)
        {
            spriteRenderer.sprite = deadSprite;
        }

        if (!instant)
        {
            // 通常の幽霊:一瞬「やられ画像」を見せてからフェード開始
            yield return new WaitForSeconds(0.15f);
        }

        yield return StartCoroutine(FadeLikeFog());

        Destroy(gameObject);
    }

    /// <summary>
    /// 霧のようにスケールを広げながら透明にフェードアウトする演出。
    /// </summary>
    private IEnumerator FadeLikeFog()
    {
        float t = 0f;
        Color startColor = spriteRenderer.color;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * 1.4f; // 霧のように少し広がる

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float ratio = t / fadeDuration;

            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, ratio);
            spriteRenderer.color = c;

            transform.localScale = Vector3.Lerp(startScale, endScale, ratio);

            yield return null;
        }
    }

    // ------------------------------
    // 外部から参照したい情報
    // ------------------------------
    // 幽霊が倒された(ライトまたは写真で撃破された)ことを外部に知らせるイベント。
    // 画面外に出て消えた場合はここでは呼ばれない(倒した扱いにはしないため)。
    public static event System.Action<Ghost> OnGhostDefeated;
    public GhostType Type => ghostType;
    public bool IsLit => isLit;
    public bool IsDying => isDying;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
}
