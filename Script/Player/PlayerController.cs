using UnityEngine;

///=======================================================================
/// プレイヤーのステータス、コンポーネント、FSMを統括する本体クラス
///======================================================================
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // --------------------------------------------------
    // プレイヤーのステータス
    // --------------------------------------------------
    [Header("Player Status")]

    [Tooltip("プレイヤーの最大体力値")]
    [Min(1f)]
    public float maxHp = 100f;

    [Tooltip("プレイヤーの現在の体力値")]
    public float currentHp;

    [Tooltip("アクションや回避で使用する最大スタミナ値")]
    [Min(0f)]
    public float maxStamina = 100f;


    [Tooltip("プレイヤーの現在のスタミナ値")]
    public float currentStamina;

    [Tooltip("必殺技や特殊スキルで使用する最大SP値")]
    [Min(0f)]
    public float maxSp = 100f;

    [Tooltip("プレイヤーの現在のSP値")]
    public float currentSp;


    // --------------------------------------------------
    // 移動設定
    // --------------------------------------------------
    [Header("Movement Settings")]

    [Tooltip("プレイヤーの基本移動速度")]
    [Min(0f)]
    public float moveSpeed = 5f;

    [Tooltip("移動方向へ向き直る際の回転速度")]
    [Min(0f)]
    public float rotationSpeed = 15f;


    // --------------------------------------------------
    // 戦闘・コンボ設定
    // --------------------------------------------------
    [Header("Combat & Combo")]

    [Tooltip("現在の連続攻撃コンボ数")]
    public int currentComboCount = 0;

    [Tooltip("コンボが途切れるまでの猶予タイマー残り時間")]
    public float comboTimer = 0f;

    [Tooltip("次の攻撃入力がない場合にコンボがリセットされるまでの猶予時間（秒）")]
    [Min(0f)]
    [SerializeField] private float comboResetTime = 1.5f; // コンボが途切れるまでの猶予時間


    // --------------------------------------------------
    // コンポーネント参照
    // --------------------------------------------------
    // プレイヤーの移動や接地判定に使用するCharacterController。
    public CharacterController CharacterController { get; private set; }

    // プレイヤーのアニメーションを制御するAnimator。
    public Animator Animator { get; private set; }


    // カメラのTransformを保持する。
    // カメラが後から生成された場合にも取得できるよう、必要に応じて再検索する。
    private Transform camTransform;

    // カメラ基準の移動計算などに使用するカメラのTransform。
    public Transform CamTransform
    {
        get
        {
            // カメラ参照が未取得、または対象が破棄されている場合は再取得する。
            if (camTransform == null)
            {
                if (Camera.main != null)
                {
                    camTransform = Camera.main.transform;
                }
                else if (CameraManager.Instance != null)
                {
                    camTransform = CameraManager.Instance.transform;
                }
            }

            return camTransform;
        }
    }

    // --------------------------------------------------
    // ステートマシン
    // --------------------------------------------------

    // プレイヤーの現在の状態を管理するステートマシン。
    public PlayerStateMachine StateMachine { get; private set; }


    // --------------------------------------------------
    // 初期化
    // --------------------------------------------------

    // コンポーネントの取得と、プレイヤーの初期ステータスを設定する。
    private void Awake()
    {
        // CharacterControllerはRequireComponentによって必要な構成要素として指定している。
        CharacterController = GetComponent<CharacterController>();

        // 子オブジェクトも含めてAnimatorを取得する。
        Animator = GetComponentInChildren<Animator>();

        // Animatorが見つからない場合は、設定漏れを確認できるよう警告を出す。
        if (Animator == null)
        {
            Debug.LogWarning("PlayerController: Animatorが見つかりません。プレイヤーのモデル設定を確認してください。", this);
        }

        // Inspectorで設定された最大値を基準に初期ステータスを設定する。
        maxHp = Mathf.Max(1f, maxHp);
        maxStamina = Mathf.Max(0f, maxStamina);
        maxSp = Mathf.Max(0f, maxSp);

        currentHp = maxHp;
        currentStamina = maxStamina;

        // SPはゲーム開始時には0とする。
        currentSp = 0f;

        // ステートマシンを生成する。
        StateMachine = new PlayerStateMachine();
    }

    // 初期ステートを設定する。
    private void Start()
    {
        // 待機状態を最初のステートとして開始する。
        StateMachine.Initialize(this, new PlayerIdleState());
    }

    // --------------------------------------------------
    // 毎フレームの更新
    // --------------------------------------------------

    // コンボの時間管理と、現在のステートの更新を行う。
    private void Update()
    {
        // コンボが継続している場合は、残り時間を減らす。
        if (currentComboCount > 0)
        {
            comboTimer -= Time.deltaTime;

            // 制限時間を超えた場合はコンボを終了する。
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }

        // 現在のステートに応じた処理を実行する。
        if (StateMachine != null)
        {
            StateMachine.Update(this);
        }
    }

    // --------------------------------------------------
    // コンボ管理
    // --------------------------------------------------

    // コンボ攻撃が成立した際にコンボ数を加算し、コンボ継続時間を更新する。
    public void AddCombo()
    {
        currentComboCount++;

        // 次の攻撃までの猶予時間を設定する。
        comboTimer = comboResetTime;

        // ゲーム全体のプレイ結果に、最大コンボ数を記録する。
        if (GameManager.Instance != null && currentComboCount > GameManager.Instance.maxComboCount)
        {
            GameManager.Instance.maxComboCount = currentComboCount;
        }
    }

    // 現在のコンボ数と継続タイマーを初期状態に戻す。(記録済みの最大コンボ数は変更しない。)
    public void ResetCombo()
    {
        currentComboCount = 0;
        comboTimer = 0f;
    }

    // --------------------------------------------------
    // スタミナ管理
    // --------------------------------------------------

    // 指定された量のスタミナを消費する。
    // 消費できた場合はtrue、不足または不正な消費量の場合はfalseを返す。
    public bool ConsumeStamina(float amount)
    {
        // 負の値を指定すると回復処理になってしまうため、受け付けない。
        if (amount < 0f)
        {
            return false;
        }

        // スタミナが不足している場合は消費しない。
        if (currentStamina < amount)
        {
            return false;
        }

        // 消費後の値が下限を下回らないようにする。
        currentStamina = Mathf.Clamp(currentStamina - amount, 0f, maxStamina);

        return true;
    }
}