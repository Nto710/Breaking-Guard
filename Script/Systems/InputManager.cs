using UnityEngine;
using UnityEngine.InputSystem;

///==================================================
// キーボード・マウス・ゲームパッドの入力を一括管理するクラス。
// 各入力を共通のプロパティとして提供し、Playerのステートなどから利用する。
// 弱攻撃・強攻撃には先行入力（入力バッファ）機能を持たせる
///==================================================
public class InputManager:MonoBehaviour
{
    // シングルトンインスタンス。他クラスから InputManager.Instance 経由でアクセス
    public static InputManager Instance { get; private set; }


    // --------------------------------------------------
    // 入力値
    // --------------------------------------------------
    [Header("Input Values")]

    [Tooltip("プレイヤーの移動入力。アナログスティックの入力強度も保持する")]
    public Vector2 MoveInput { get; private set; }

    [Tooltip("弱攻撃ボタンが押された瞬間フラグ")]
    public bool LightAttackPressed { get; private set; }

    [Tooltip("強攻撃ボタンが押された瞬間フラグ")]
    public bool HeavyAttackPressed { get; private set; }

    [Tooltip("回避ボタンが押された瞬間フラグ")]
    public bool DodgePressed { get; private set; }

    [Tooltip("パリィボタンが押された瞬間フラグ")]
    public bool ParryPressed { get; private set; }

    [Tooltip("ロックオンボタンが押された瞬間フラグ")]
    public bool LockOnPressed { get; private set; }

    [Tooltip("ガードボタンを押し続けているかどうかの長押し判定フラグ")]
    public bool GuardHold { get; private set; }

    // スキル入力値
    [Header("Skill Input Values")]

    [Tooltip("スキル1のボタンが押された瞬間フラグ")]
    public bool Skill1Pressed { get; private set; }

    [Tooltip("スキル2のボタンが押された瞬間フラグ")]
    public bool Skill2Pressed { get; private set; }

    [Tooltip("スキル3のボタンが押された瞬間フラグ")]
    public bool Skill3Pressed { get; private set; }

    
    //--------------------------------------------------
    // 入力バッファ設定
    //--------------------------------------------------
    [Header("Buffer Settings")]

    [Tooltip("攻撃の先行入力を受け付ける時間（秒）")]
    [Min(0f)]
    [SerializeField] private float inputBufferTime = 0.2f;

    // 各攻撃入力があと何秒間有効かを保持する。
    private float lightAttackBufferTimer;
    private float heavyAttackBufferTimer;

    //------------------------------------------------------------------
    // InputManagerを初期化し、シーン間で同じインスタンスを使用する。
    //-------------------------------------------------------------------
    private void Awake()
    {
        // シングルトンの初期化と重複インスタンスの破棄
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //------------------------------------------------------------------------------------------------
    // 現在の入力デバイスから入力を取得する。(各プロパティはこのフレームの入力状態に更新する。)
    //------------------------------------------------------------------------------------------------
    private void Update()
    {
        // 現在使用可能な入力デバイスを取得する。(接続されていないデバイスはnullになるため、個別にチェックする。)
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        Gamepad gamepad = Gamepad.current;

        // --------------------------------------------------
        // 移動入力
        // --------------------------------------------------
        Vector2 keyboardMove = Vector2.zero;

        if (keyboard != null)
        {
            float horizontal = 0f;
            float vertical = 0f;

            // WASDと方向キーの両方に対応する。
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            keyboardMove = new Vector2(horizontal, vertical);
        }

        // キーボード入力がある場合は、その入力を使用する。
        Vector2 moveInput = keyboardMove;

        if (gamepad != null)
        {
            // 左スティックの入力を優先する。
            Vector2 stickMove = gamepad.leftStick.ReadValue();

            if (stickMove.sqrMagnitude > 0.01f)
            {
                moveInput = stickMove;
            }
            // 左スティックが操作されていない場合は十字キーも使用できる。
            else
            {
                Vector2 dpadMove = gamepad.dpad.ReadValue();

                if (dpadMove.sqrMagnitude > 0f)
                {
                    moveInput = dpadMove;
                }
            }
        }

        // 斜め入力による移動量の増加を防ぎつつ、スティックの入力強度は維持する。
        MoveInput = Vector2.ClampMagnitude(moveInput, 1f);


        // --------------------------------------------------
        // 攻撃・回避・パリィ・ロックオン
        // --------------------------------------------------

        // 弱攻撃：Jキー / ゲームパッドの左側ボタン（X・□）
        LightAttackPressed = (keyboard != null && keyboard.jKey.wasPressedThisFrame) || (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);

        // 強攻撃：Kキー / ゲームパッドの上側ボタン（Y・△）
        HeavyAttackPressed = (keyboard != null && keyboard.kKey.wasPressedThisFrame) || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        // 回避：左Shiftキー・Lキー / ゲームパッドの右側ボタン（B・○）
        DodgePressed =(keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.lKey.wasPressedThisFrame)) || (gamepad != null && gamepad.buttonEast.wasPressedThisFrame);

        // パリィ：Eキー・Uキー / 左ショルダーボタン（LB・L1）
        ParryPressed = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.uKey.wasPressedThisFrame)) || (gamepad != null && gamepad.leftShoulder.wasPressedThisFrame);

        // ロックオン：Qキー・Spaceキー / 右スティック押し込み（R3）
        LockOnPressed = (keyboard != null && (keyboard.qKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) || (gamepad != null && gamepad.rightStickButton.wasPressedThisFrame);


        // --------------------------------------------------
        // ガード入力
        // --------------------------------------------------

        // ガードは押している間だけtrueになる。
        // Gキー・マウス右ボタン / 右ショルダーボタン（RB・R1）
        GuardHold = (keyboard != null && keyboard.gKey.isPressed) || (mouse != null && mouse.rightButton.isPressed) || (gamepad != null && gamepad.rightShoulder.isPressed);


        // --------------------------------------------------
        // スキル入力
        // --------------------------------------------------

        // スキル1：数字の1キー / L2 + □（Xbox：LT + X）
        // 左トリガーを押している間に、左側のボタンを押すと発動入力になる。
        Skill1Pressed =(keyboard != null && keyboard.digit1Key.wasPressedThisFrame) || (gamepad != null && gamepad.leftTrigger.isPressed && gamepad.buttonWest.wasPressedThisFrame);

        // スキル2：数字の2キー / L2 + △（Xbox：LT + Y）
        // 左トリガーを押している間に、上側のボタンを押すと発動入力になる。
        Skill2Pressed = (keyboard != null && keyboard.digit2Key.wasPressedThisFrame) || (gamepad != null && gamepad.leftTrigger.isPressed && gamepad.buttonNorth.wasPressedThisFrame);

        // スキル3：数字の3キー / L2 + ×（Xbox：LT + A）
        // 左トリガーを押している間に、下側のボタンを押すと発動入力になる。
        Skill3Pressed = (keyboard != null && keyboard.digit3Key.wasPressedThisFrame) || (gamepad != null && gamepad.leftTrigger.isPressed && gamepad.buttonSouth.wasPressedThisFrame);


        // --------------------------------------------------
        // 攻撃入力バッファの更新
        // --------------------------------------------------

        // 攻撃入力があれば有効時間を設定し、入力がなければ時間経過に応じて残り時間を減らす。
        if (LightAttackPressed)
        {
            lightAttackBufferTimer = Mathf.Max(0f, inputBufferTime);
        }
        else
        {
            lightAttackBufferTimer = Mathf.Max(0f, lightAttackBufferTimer - Time.deltaTime);
        }

        if (HeavyAttackPressed)
        {
            heavyAttackBufferTimer = Mathf.Max(0f, inputBufferTime);
        }
        else
        {
            heavyAttackBufferTimer = Mathf.Max(0f, heavyAttackBufferTimer - Time.deltaTime);
        }
    }

    //-----------------------------------------------
    // 弱攻撃の先行入力がまだ有効かを返す。
    //----------------------------------------------
    public bool HasBufferedLightAttack()
    {
        return lightAttackBufferTimer > 0f;
    }

    //-----------------------------------------------------------
    // 弱攻撃の先行入力を消費し、再使用されないようにする。
    //-----------------------------------------------------------
    public void ConsumeLightAttackBuffer()
    {
        lightAttackBufferTimer = 0f;
    }

    //--------------------------------------------------------
    // 強攻撃の先行入力がまだ有効かを返す。
    //--------------------------------------------------------
    public bool HasBufferedHeavyAttack()
    {
        return heavyAttackBufferTimer > 0f;
    }

    //-------------------------------------------------------
    // 強攻撃の先行入力を消費し、再使用されないようにする。
    //-------------------------------------------------------
    public void ConsumeHeavyAttackBuffer()
    {
        heavyAttackBufferTimer = 0f;
    }

    //--------------------------------------------------------------------
    // オブジェクト破棄時、自分が共有インスタンスなら参照を解除する。
    //--------------------------------------------------------------------
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}