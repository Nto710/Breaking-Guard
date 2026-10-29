using System.Collections;
using UnityEngine;

///=========================================================================
/// ゲーム内時間（Time.timeScale）を操作し、
/// スローモーションやヒットストップ（フリーズ演出）を管理するクラス
///==========================================================================
public class TimeManager : MonoBehaviour
{
    // シングルトンインスタンス。他クラスから TimeManager.Instance 経由でアクセス
    public static TimeManager Instance { get; private set; }

    // --------------------------------------------------
    // 時間設定
    // --------------------------------------------------

    // 通常時の時間倍率。
    private const float NormalTimeScale = 1.0f;

    // 通常時の物理演算フレーム間隔を保持する。
    private float defaultFixedDeltaTime;

    [Header("Time Effect Settings")]

    [Tooltip("ヒットストップ中の時間倍率")]
    [Range(0.01f, 1.0f)]
    [SerializeField] private float hitStopTimeScale = 0.05f;


    // --------------------------------------------------
    // 時間演出の管理
    // --------------------------------------------------

    // 現在実行中の時間演出コルーチン。
    // スローモーションとヒットストップで共通して使用する。
    private Coroutine currentTimeEffectCoroutine;


    // TimeManagerを初期化する。(最初のインスタンスをシーン間で維持し、通常時の物理演算フレーム間隔を保存する。)
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // シーンが切り替わっても時間管理を継続する。
            DontDestroyOnLoad(gameObject);

            // 時間倍率を変更する前の物理演算フレーム間隔を保存する。
            defaultFixedDeltaTime = Time.fixedDeltaTime;
        }
        else if (Instance != this)
        {
            // すでに別のインスタンスが存在する場合は重複を防ぐ。
            Destroy(gameObject);
        }
    }

    
    // --------------------------------------------------
    // スローモーション
    // --------------------------------------------------

    // 指定した時間倍率でスローモーションを実行する。(すでに別の時間演出が実行中の場合は、新しい演出に置き換える。)
    public void DoSlowMotion(float slowScale, float duration)
    {
        // 時間倍率は0.01～1.0の範囲に制限する。
        // 0にすると物理演算のフレーム間隔も0になるため、使用しない。
        slowScale = Mathf.Clamp(slowScale, 0.01f, NormalTimeScale);

        StartTimeEffect(slowScale, duration);
    }

    
    // --------------------------------------------------
    // ヒットストップ
    // --------------------------------------------------

    // 攻撃がヒットした瞬間などに、短時間のヒットストップを実行する。
    public void DoHitStop(float duration)
    {
        StartTimeEffect(hitStopTimeScale, duration);
    }


    // --------------------------------------------------
    // 時間演出の共通処理
    // --------------------------------------------------

    // 時間演出を開始する。(実行中の演出があれば停止し、新しい演出を優先する。)
    private void StartTimeEffect(float timeScale, float duration)
    {
        // 0秒以下の演出要求は受け付けない。
        if (duration <= 0f)
        {
            Debug.LogWarning("時間演出の継続時間には0より大きい値を指定してください。");
            return;
        }

        // 以前の演出を停止する。
        // 新しい演出の時間倍率は、共通コルーチン内で設定する。
        if (currentTimeEffectCoroutine != null)
        {
            StopCoroutine(currentTimeEffectCoroutine);
            currentTimeEffectCoroutine = null;
        }

        // 新しい時間演出を開始する。
        currentTimeEffectCoroutine = StartCoroutine(TimeEffectRoutine(timeScale, duration));
    }

    // 時間倍率を変更し、指定時間の経過後に通常状態へ戻す。
    private IEnumerator TimeEffectRoutine(float timeScale, float duration)
    {
        // ゲーム内時間と物理演算の時間倍率を変更する。
        ApplyTimeScale(timeScale);

        // 実時間で待機するため、スローモーション中でも
        // 指定した時間が経過すると演出が終了する。
        yield return new WaitForSecondsRealtime(duration);

        // 時間倍率と物理演算の設定を通常状態へ戻す。
        RestoreTimeScale();

        // 演出が終了したことを記録する。
        currentTimeEffectCoroutine = null;
    }

    // 指定した時間倍率を適用する。
    // 物理演算のフレーム間隔も時間倍率に合わせて調整する。
    
    private void ApplyTimeScale(float timeScale)
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * timeScale;
    }

    // 時間倍率と物理演算のフレーム間隔を通常状態に戻す。
    private void RestoreTimeScale()
    {
        Time.timeScale = NormalTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }

    // 管理オブジェクトが破棄される場合、実行中の時間演出を停止して時間設定を復元する。
    private void OnDestroy()
    {
        // 自分が現在の共有インスタンスでない場合は何もしない。
        if (Instance != this)
        {
            return;
        }

        if (currentTimeEffectCoroutine != null)
        {
            StopCoroutine(currentTimeEffectCoroutine);
            currentTimeEffectCoroutine = null;

            RestoreTimeScale();
        }

        Instance = null;
    }
}