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
        Special,  // 特別な幽霊:ライト無効、写真モードでのみ倒せる
        Boss      // ボス:ライトで弱らせてスタンさせ、写真で1ヒット。これを既定回数繰り返すと撃破
    }

    private enum BossPhase
    {
        LightPhase, // ライトで体力を削る段階
        Stunned     // 体力が尽きてスタン中。写真で撃てる
    }

    [Header("種類")]
    [SerializeField] private GhostType ghostType = GhostType.Normal;

    [Header("移動設定")]
    [SerializeField] private float moveSpeedNormal = 1.5f;
    [SerializeField] private float moveSpeedSpecial = 2.2f; // 特別な幽霊はやや速め
    [SerializeField] private float wanderRadius = 3f;       // 現在地からどこまでランダムな目的地を選ぶか
    [SerializeField] private float wanderInterval = 2f;      // 何秒ごとに目的地を選び直すか

    [Header("ライトから逃げる動き(ステージごとにON/OFF可能)")]
    [SerializeField] private bool fleeFromLightWhenLit = false; // ONにすると、ライトに当たっている間は光源から逃げる方向へ移動する(普通/特別どちらの幽霊にも設定可能)
    [SerializeField] private float fleeSpeedMultiplier = 1.4f;  // 逃げている間、通常の移動速度に対して何倍の速さで逃げるか

    [Header("徘徊範囲")]
    [SerializeField] private bool useCameraBoundsForArea = true; // trueならカメラの映る範囲を自動で徘徊範囲にする
    [SerializeField] private Vector2 areaViewportMargin = new Vector2(0.05f, 0.05f); // カメラ基準の場合、画面端からの余白(ビューポート比率)
    [SerializeField] private Vector2 areaMin = new Vector2(-8, -4); // useCameraBoundsForArea = false の場合に使う固定範囲
    [SerializeField] private Vector2 areaMax = new Vector2(8, 4);

    [Header("ライト被弾設定(普通の幽霊・ボスのライトフェーズ)")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damagePerSecond = 70f; // ライトに当たっている間、1秒あたり何ポイント体力を減らすか
    [SerializeField] private string spotlightTag = "Spotlight";

    [Header("ボス設定(Ghost Type = Boss の時のみ使用)")]
    [SerializeField] private float bossScaleMultiplier = 1.8f;   // 通常の幽霊より大きく表示する倍率
    [SerializeField] private int requiredPhotoHits = 3;           // 撃破に必要な写真ヒット数
    [SerializeField] private float stunDuration = 5f;             // スタン(写真で撃てる状態)の持続時間
    [SerializeField] private float vanishFadeDuration = 0.5f;     // スタン復帰時、姿を消す/現すフェードの時間
    [SerializeField] private float offscreenReappearDelay = 1f;   // 姿を消してから画面外に移動し、再登場するまでの待機時間

    [Header("演出")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite deadSprite;
    [SerializeField] private float shakeAmount = 0.05f;
    [SerializeField] private float shakeSpeed = 25f;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("サウンド")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private float spawnSoundVolume = 1f;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float deathSoundVolume = 1f;

    [Header("スコア")]
    [SerializeField] private int scoreValue = 10;

    [Header("画面外判定")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float offscreenMargin = 0.5f;

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = true; // 原因調査用。安定したらfalseにしてOK

    [Header("ゲーム進行(GameController.csと連携)")]
    [SerializeField] private GameController gameController; // PlayState.Play の間だけ行動する(未設定なら常に動作する)

    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider; // ボスが消えている間、当たり判定を切るために使う

    // 実際の(震えを含まない)論理位置。Wander()はここを動かす。
    // 見た目のtransform.positionは、これに震えオフセットを足したものになる。
    private Vector3 basePosition;

    private Vector2 currentTarget;
    private float wanderTimer;

    private bool isLit = false;
    private Transform litSpotlightTransform; // 現在当たっているライトの位置(逃げる方向の計算に使う)
    private float currentHealth;
    private bool isDying = false;

    // ボス専用の状態
    private BossPhase bossPhase = BossPhase.LightPhase;
    private int bossHitsTaken = 0;
    private float stunTimer = 0f;
    private bool isVanished = false; // 消失→画面外移動→再登場の演出中はtrue(移動処理を止める)

    private float MoveSpeed => ghostType == GhostType.Special ? moveSpeedSpecial : moveSpeedNormal;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        if (normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
        basePosition = transform.position;
        currentHealth = maxHealth;

        if (ghostType == GhostType.Boss)
        {
            transform.localScale *= bossScaleMultiplier;
        }
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

        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, transform.position, spawnSoundVolume);
        }
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

        // Play中(ゲーム進行中)以外は行動を停止する(死亡演出中は止めない)
        if (gameController != null && gameController.CurrentState != GameController.PlayState.Play)
        {
            return;
        }

        if (isVanished)
        {
            // 消失→画面外移動→再登場の演出中は、それ以外の処理(移動・ライト判定・画面外消滅)を行わない
            return;
        }

        Wander();

        if (IsOutsideScreen())
        {
            // 画面外に出た場合は演出なし・得点なしで即座に消滅する
            Destroy(gameObject);
            return;
        }

        // ボスのスタン中はタイマーを進める(写真で撃たれず時間切れになったらライトフェーズへ戻す)
        if (ghostType == GhostType.Boss && bossPhase == BossPhase.Stunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                EndStunWithoutHit();
            }
        }

        bool canTakeLightDamage =
            (ghostType == GhostType.Normal) ||
            (ghostType == GhostType.Boss && bossPhase == BossPhase.LightPhase);

        if (isLit && canTakeLightDamage)
        {
            currentHealth -= damagePerSecond * Time.deltaTime;
            ApplyShake();

            if (debugLog)
            {
                Debug.Log($"[{gameObject.name}] HP = {currentHealth:F1} / {maxHealth}");
            }

            if (currentHealth <= 0f)
            {
                if (ghostType == GhostType.Boss)
                {
                    EnterStunPhase();
                }
                else
                {
                    KillByLight();
                }
            }
        }
        else
        {
            // ライトが当たっていない/対象外なら震えをリセット
            transform.position = basePosition;
        }
    }

    // ------------------------------
    // 移動(ランダム徘徊 / ライトから逃げる)
    // ------------------------------
    private void Wander()
    {
        bool shouldFlee = fleeFromLightWhenLit && isLit && litSpotlightTransform != null;

        if (shouldFlee)
        {
            FleeFromLight();
        }
        else
        {
            NormalWander();
        }

        // ライトが当たっていない時はここで見た目にも反映する
        // (当たっている時はApplyShake側で震えを加えた位置を設定する)
        if (!isLit)
        {
            transform.position = basePosition;
        }
    }

    private void NormalWander()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f || Vector2.Distance(basePosition, currentTarget) < 0.1f)
        {
            PickNewWanderTarget();
        }

        Vector2 pos = basePosition;
        Vector2 next = Vector2.MoveTowards(pos, currentTarget, MoveSpeed * Time.deltaTime);
        basePosition = new Vector3(next.x, next.y, basePosition.z);
    }

    /// <summary>ライトが当たっている間、光源から遠ざかる方向へ移動する(LV2以降で使用)</summary>
    private void FleeFromLight()
    {
        Vector2 fleeDirection = ((Vector2)basePosition - (Vector2)litSpotlightTransform.position);

        // ちょうど重なっている等で方向が定まらない場合は、ランダムな方向に逃がす
        if (fleeDirection.sqrMagnitude < 0.0001f)
        {
            fleeDirection = Random.insideUnitCircle;
        }
        fleeDirection.Normalize();

        float fleeSpeed = MoveSpeed * fleeSpeedMultiplier;
        Vector2 next = (Vector2)basePosition + fleeDirection * fleeSpeed * Time.deltaTime;

        // 徘徊範囲の外に逃げすぎないようクランプ
        next.x = Mathf.Clamp(next.x, areaMin.x, areaMax.x);
        next.y = Mathf.Clamp(next.y, areaMin.y, areaMax.y);

        basePosition = new Vector3(next.x, next.y, basePosition.z);

        // 逃走中は、ライトが外れた後の徘徊が変な方向へ飛ばないよう、
        // 現在地を新しい目的地として同期しておく
        currentTarget = next;
        wanderTimer = wanderInterval;
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
            litSpotlightTransform = other.transform;
            if (debugLog) Debug.Log($"[{gameObject.name}] ライト接触開始: isLit = true");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(spotlightTag))
        {
            isLit = false;
            litSpotlightTransform = null;
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
    /// 特別な幽霊 / ボスが写真モードで撮影された時に、
    /// カメラ側のスクリプトから呼び出す想定のメソッド。
    /// ・特別な幽霊:即座に撃破
    /// ・ボス:スタン中のみ有効。既定回数ヒットしたら撃破、それ未満なら次のライトフェーズへ
    /// ・普通の幽霊:何もしない
    /// </summary>
    public void KillByPhoto()
    {
        if (isDying) return;

        if (ghostType == GhostType.Special)
        {
            StartCoroutine(DieRoutine(addScore: true, instant: true));
        }
        else if (ghostType == GhostType.Boss)
        {
            if (bossPhase != BossPhase.Stunned) return; // スタン中でなければ撮影しても効果なし

            bossHitsTaken++;
            if (debugLog)
            {
                Debug.Log($"[{gameObject.name}] ボスに写真ヒット: {bossHitsTaken} / {requiredPhotoHits}");
            }

            if (bossHitsTaken >= requiredPhotoHits)
            {
                StartCoroutine(DieRoutine(addScore: true, instant: true));
            }
            else
            {
                // まだ撃破に至らない場合は、次のライトフェーズへ戻る(姿を消して画面外から再登場)
                RecoverFromStun();
            }
        }
    }

    /// <summary>ボス:体力が尽きた時にスタン状態へ移行する(ライト無効、写真で撃てる)</summary>
    private void EnterStunPhase()
    {
        bossPhase = BossPhase.Stunned;
        stunTimer = stunDuration;

        if (debugLog)
        {
            Debug.Log($"[{gameObject.name}] ボスがスタンしました(残り{stunDuration}秒以内に撮影してください)");
        }
    }

    /// <summary>ボス:スタン中に撮影されないまま時間切れになった場合、ヒット無しでライトフェーズへ戻す</summary>
    private void EndStunWithoutHit()
    {
        if (debugLog)
        {
            Debug.Log($"[{gameObject.name}] ボスのスタンが時間切れ。ライトフェーズに戻ります");
        }

        RecoverFromStun();
    }

    /// <summary>
    /// スタンから復帰してライトフェーズへ戻る共通処理。
    /// 体力を回復し、その場では復帰させず、一旦姿を消して画面外へ移動し、そこから再登場する。
    /// </summary>
    private void RecoverFromStun()
    {
        bossPhase = BossPhase.LightPhase;
        currentHealth = maxHealth;

        // 光源との接触状態をリセットしておく(コライダーを切るため)
        isLit = false;
        litSpotlightTransform = null;

        StartCoroutine(VanishAndReappearRoutine());
    }

    /// <summary>ボス演出:姿を消す → 画面外へ移動 → 一定時間待つ → 姿を現す</summary>
    private IEnumerator VanishAndReappearRoutine()
    {
        isVanished = true;

        if (bodyCollider != null)
        {
            bodyCollider.enabled = false;
        }

        // フェードアウト
        yield return StartCoroutine(FadeSpriteAlpha(1f, 0f, vanishFadeDuration));

        // 画面外の位置へ瞬間移動
        basePosition = GetRandomOffscreenPosition();
        transform.position = basePosition;

        yield return new WaitForSeconds(offscreenReappearDelay);

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
        }

        // フェードイン
        yield return StartCoroutine(FadeSpriteAlpha(0f, 1f, vanishFadeDuration));

        // 画面内に向かって歩き出すよう、新しい徘徊目標を選び直す
        PickNewWanderTarget();

        isVanished = false;
    }

    /// <summary>SpriteRendererのアルファ値を指定時間かけて変化させる汎用コルーチン</summary>
    private IEnumerator FadeSpriteAlpha(float from, float to, float duration)
    {
        float t = 0f;
        Color c = spriteRenderer.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            spriteRenderer.color = c;
            yield return null;
        }

        c.a = to;
        spriteRenderer.color = c;
    }

    /// <summary>カメラの映る範囲のすぐ外側(画面端の外)にランダムな位置を1つ返す</summary>
    private Vector3 GetRandomOffscreenPosition()
    {
        if (targetCamera == null)
        {
            return basePosition;
        }

        float z = -targetCamera.transform.position.z;

        // 上下左右どこから登場するかをランダムに決める
        int side = Random.Range(0, 4); // 0:左 1:右 2:下 3:上
        float edgeOffset = 0.15f; // 画面端からどれだけ外側に出すか(ビューポート比率)

        float vx, vy;
        switch (side)
        {
            case 0: vx = -edgeOffset; vy = Random.Range(0f, 1f); break;
            case 1: vx = 1f + edgeOffset; vy = Random.Range(0f, 1f); break;
            case 2: vx = Random.Range(0f, 1f); vy = -edgeOffset; break;
            default: vx = Random.Range(0f, 1f); vy = 1f + edgeOffset; break;
        }

        Vector3 worldPos = targetCamera.ViewportToWorldPoint(new Vector3(vx, vy, z));
        worldPos.z = 0f;
        return worldPos;
    }

    private IEnumerator DieRoutine(bool addScore, bool instant = false)
    {
        isDying = true;

        if (addScore)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(scoreValue);
            }
            else if (debugLog)
            {
                Debug.LogWarning("[Ghost] ScoreManager が見つからないため、スコアを加算できませんでした");
            }
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
    public int ScoreValue => scoreValue;
    public bool IsLit => isLit;
    public bool IsVulnerableToPhoto => ghostType == GhostType.Special || (ghostType == GhostType.Boss && bossPhase == BossPhase.Stunned);
    public bool IsDying => isDying;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
}
