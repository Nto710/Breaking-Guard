
using UnityEngine;

///===========================================
/// プレイヤーのスキル状態を管理するステート
///===========================================
public class PlayerSkillState : IPlayerState
{
    //--------------------------------------
    // スキルの基本設定（仮の数値）
    //--------------------------------------

    // スキル発動に必要なSP。
    private const float SkillSpCost = 20f;

    // スキル発動に必要なスタミナ。
    private const float SkillStaminaCost = 20f;


    // スキル状態の継続時間（秒） （実際のアニメーションに合わせて調整する。）
    private const float SkillDuration = 0.8f;


    //--------------------------------------
    // スキルの管理情報
    //--------------------------------------

    // 使用するスキルの装備枠番号
    // 0 = 1枠目、1 = 2枠目、2 = 3枠目
    private readonly int skillSlotIndex;

    // 現在使用するスキルの識別ID。（-1は未設定を表す。）
    public int CurrentSkillId { get; private set; }

    // スキル発動からの経過時間
    private float elapsedTime = 0f;

    // スキルが正常に発動できたかどうか。
    private bool skillStarted = false;


    //--------------------------------------
    // コンストラクタ
    //--------------------------------------

    // 使用するスキルの装備枠を指定する。
    public PlayerSkillState(int slotIndex = 0)
    {
        // 装備枠番号を保存する
        skillSlotIndex = slotIndex;
    }

    // --------------------------------------------------
    // ステート開始
    // --------------------------------------------------

    // スキル状態に入った瞬間に呼び出される
    // 装備スキルと必要なリソースを確認し、発動処理を行う。
    public void Enter(PlayerController player)
    {
        // 状態開始時の情報を初期化する。
        elapsedTime = 0f;
        CurrentSkillId = -1;
        skillStarted = false;

        // プレイヤーが正しく取得できていない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        // --------------------------------------------------
        // 使用するスキルIDを取得する
        // --------------------------------------------------
        GameManager gameManager = GameManager.Instance;

        // ゲーム管理クラスやスキル配列がなければ発動できない。
        if (gameManager == null || gameManager.selectedSkillIDs == null)
        {
            return;
        }

        // 指定した装備枠が配列の範囲内か確認する。
        if (skillSlotIndex < 0 || skillSlotIndex >= gameManager.selectedSkillIDs.Length)
        {
            Debug.LogWarning("指定されたスキル装備枠が存在しません。", player);
            return;
        }

        // 指定された装備枠のスキルIDを取得する。
        CurrentSkillId = gameManager.selectedSkillIDs[skillSlotIndex];

        // 負のIDは未設定として扱う。
        if (CurrentSkillId < 0)
        {
            Debug.LogWarning("スキルが装備されていないため、発動を中止しました。", player);
            return;
        }

        // --------------------------------------------------
        // 発動に必要なリソースを確認する
        // --------------------------------------------------

        // SPかスタミナのどちらかが不足していれば発動しない。
        // 先に両方の残量を確認し、片方だけ消費されることを防ぐ。
        if (player.currentSp < SkillSpCost || player.currentStamina < SkillStaminaCost)
        {
            return;
        }

        // スタミナを消費する。
        // 消費できなかった場合はSPも消費しない。
        if (!player.ConsumeStamina(SkillStaminaCost))
        {
            return;
        }

        // 両方のリソースを確保できたため、SPを消費する。
        player.currentSp = Mathf.Clamp(
            player.currentSp - SkillSpCost,
            0f,
            player.maxSp
        );

        // --------------------------------------------------
        // スキルを開始する
        // --------------------------------------------------

        skillStarted = true;

        // スキルアニメーションを再生する。
        if (player.Animator != null)
        {
            player.Animator.CrossFade("Skill", 0.1f);
        }
    }

    // --------------------------------------------------
    // 毎フレームの更新
    // --------------------------------------------------

    // スキル状態中、毎フレーム呼び出される。
    // 発動失敗時の復帰と、スキル状態の終了を管理する。
    public void UpdateState(PlayerController player)
    {
        // プレイヤーまたはステートマシンがない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        // スキルを発動できなかった場合は、次の状態へ戻る。
        // Enter内から直接ステートを変更しないことで、
        // 状態開始処理中の再帰的な遷移を避ける。
        if (!skillStarted)
        {
            ReturnToNextState(player);
            return;
        }

        // スキル状態の継続時間を計測する。
        elapsedTime += Time.deltaTime;

        // 指定時間が経過したらスキル状態を終了する。
        if (elapsedTime >= SkillDuration)
        {
            ReturnToNextState(player);
        }
    }

    // --------------------------------------------------
    // スキル終了後の状態遷移
    // --------------------------------------------------

    // スキル終了後の入力を確認し、次のステートを決定する。
    private void ReturnToNextState(PlayerController player)
    {
        // 入力管理クラスを取得する。
        InputManager input = InputManager.Instance;

        // 入力管理クラスがなければ待機状態へ戻る。
        if (input == null)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerIdleState()
            );
            return;
        }

        // ガード入力があればガード状態へ移行する。
        if (input.GuardHold)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerGuardState()
            );
            return;
        }

        // パリィ入力があればパリィ状態へ移行する。
        if (input.ParryPressed)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerParryState()
            );
            return;
        }

        // 回避入力があれば回避状態へ移行する。
        if (input.DodgePressed)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerDodgeState()
            );
            return;
        }

        // 攻撃入力が残っていれば攻撃状態へ移行する。
        if (input.HasBufferedLightAttack() ||
            input.HasBufferedHeavyAttack())
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerAttackState()
            );
            return;
        }

        // 移動入力があれば移動状態へ戻る。
        if (input.MoveInput.sqrMagnitude > 0.01f)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerMoveState()
            );
        }
        else
        {
            // 行動入力がなければ待機状態へ戻る。
            player.StateMachine.ChangeState(
                player,
                new PlayerIdleState()
            );
        }
    }

    // --------------------------------------------------
    // ステート終了
    // --------------------------------------------------

    // スキル状態を終了するときに呼び出される。
    public void Exit(PlayerController player)
    {
        // 現時点では終了時の特別な処理はない。
    }
}


//まだスキルを入力して発動する処理は接続していません。
//スキルの効果は未実装です。