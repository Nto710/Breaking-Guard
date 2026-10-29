
using UnityEngine;

///===========================================
/// プレイヤーの攻撃状態を管理するステート
///===========================================
public class PlayerAttackState : IPlayerState
{
    //--------------------------------------
    // 攻撃の種類
    //--------------------------------------
    private enum AttackType
    {
        None,       // 攻撃なし
        Light,      // 弱攻撃
        Heavy       // 強攻撃
                    // 打ち上げやジャンプ子プ劇などを追加予定
    }

    //----------------------------------------------
    // Animatorに登録する攻撃ステート名
    // 実際のAnimator Controllerの名前に合わせる
    //---------------------------------------------
    private static readonly string[] WeakAttackStates =
    {
        "Attack_Weak_1",
        "Attack_Weak_2",
        "Attack_Weak_3",
        "Attack_Weak_4",
        "Attack_Weak_5"
    };

    private static readonly string[] HeavyAttackStates =
    {
        "Attack_Heavy_1",
        "Attack_Heavy_2",
        "Attack_Heavy_3"
    };


    // 次の攻撃によるキャンセルを受け付ける再生率。
    private const float AttackCancelStartTime = 0.75f;

    // 回避によるキャンセルを受け付ける再生率。
    private const float DodgeCancelStartTime = 0.65f;

    // Animatorがない場合に使用する仮の攻撃時間（秒）。
    private const float FallbackAttackDuration = 0.6f;

    //--------------------------------------
    // 現在の攻撃状態で使用するデータ
    //--------------------------------------

    // 直前に行った攻撃の種類
    private AttackType lastAttackType = AttackType.None;

    // 弱攻撃のコンボ段階
    private int weakComboStep = 0;

    // 強攻撃のコンボ段階
    private int heavyComboStep = 0;

    // 現在再生している攻撃アニメーション名
    private string currentAnimationName = "";

    // Animatorがない場合の経過時間
    private float attackElapsedTime = 0f;


    //--------------------------------------
    // 攻撃状態に入った瞬間に呼び出される
    //--------------------------------------
    public void Enter(PlayerController player)
    {
        // プレイヤーまたはステートマシンがない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        // コンボの段階を初期化する
        lastAttackType = AttackType.None;
        weakComboStep = 0;
        heavyComboStep = 0;

        // 攻撃時間を初期化する
        attackElapsedTime = 0f;

        // 入力されている攻撃を開始する
        if (!StartBufferedAttack(player))
        {
            // 攻撃入力を取得できなかった場合は待機状態へ戻る
            player.StateMachine.ChangeState(player, new PlayerIdleState());
        }
    }


    //---------------------------------------
    // 攻撃状態中、毎フレーム呼び出される
    //---------------------------------------
    public void UpdateState(PlayerController player)
    {
        // プレイヤーまたはステートマシンがない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        float normalizedTime;

        //----------------------------------
        // 攻撃アニメーションの進行状況を取得
        //----------------------------------
        if (player.Animator != null)
        {
            AnimatorStateInfo stateInfo = player.Animator.GetCurrentAnimatorStateInfo(0);

            // 再生中のステートが切り替わるまで待つ
            if (!stateInfo.IsName(currentAnimationName))
            {
                return;
            }

            // 1.0でアニメーション1回分の再生が完了
            normalizedTime = stateInfo.normalizedTime;
        }
        else
        {
            // Animatorがない場合は経過時間で代用する
            attackElapsedTime += Time.deltaTime;

            normalizedTime = attackElapsedTime / FallbackAttackDuration;
        }

        //----------------------------------
        // 回避による攻撃キャンセル
        //----------------------------------
        if (InputManager.Instance != null)
        {
            // 一定の再生率を超え、回避入力があれば回避へ遷移する。
            if (normalizedTime >= DodgeCancelStartTime && InputManager.Instance.DodgePressed)
            {
                player.StateMachine.ChangeState( player, new PlayerDodgeState());
                return;
            }

            //----------------------------------
            // 次の攻撃入力を確認する
            //----------------------------------
            if (normalizedTime >= AttackCancelStartTime)
            {
                // 次の攻撃入力があれば続けて攻撃する
                if (StartBufferedAttack(player))
                {
                    return;
                }
            }
        }

        //----------------------------------
        // 攻撃アニメーションの終了判定
        //----------------------------------
        if (normalizedTime >= 1.0f)
        {
            // 移動入力があれば移動状態へ戻る
            if (InputManager.Instance != null && InputManager.Instance.MoveInput.sqrMagnitude > 0.01f)
            {
                player.StateMachine.ChangeState(player, new PlayerMoveState());
            }
            else
            {
                // 移動入力がなければ待機状態へ戻る
                player.StateMachine.ChangeState(player, new PlayerIdleState());
            }
        }
    }


    //--------------------------------------
    // 攻撃入力を取得して攻撃を開始する
    //--------------------------------------
    private bool StartBufferedAttack(PlayerController player)
    {
        // 入力管理クラスが存在しない場合は攻撃を開始できない。
        if (player == null || InputManager.Instance == null)
        {
            return false;
        }

        AttackType selectedAttack;

        // 弱攻撃・強攻撃が両方入力されている場合は弱攻撃を優先する。
        if (InputManager.Instance.HasBufferedLightAttack())
        {
            InputManager.Instance.ConsumeLightAttackBuffer();

            selectedAttack = AttackType.Light;
        }
        else if (InputManager.Instance.HasBufferedHeavyAttack())
        {
            InputManager.Instance.ConsumeHeavyAttackBuffer();

            selectedAttack = AttackType.Heavy;
        }
        else
        {
            // 攻撃入力がなければ何もしない
            return false;
        }


        // 選択した攻撃の種類に応じてコンボ段階を決める。
        if (selectedAttack == AttackType.Light)
        {
            // 強攻撃から弱攻撃へ切り替えた場合は、弱攻撃のコンボを最初から開始する
            if (lastAttackType != AttackType.Light)
            {
                weakComboStep = 0;
            }

            // 弱攻撃5段目の次は新しい連続攻撃として扱う
            if (weakComboStep >= WeakAttackStates.Length)
            {
                weakComboStep = 0;
            }

            // 現在の弱攻撃アニメーションを設定する
            currentAnimationName = WeakAttackStates[weakComboStep];

            // 次の弱攻撃段階へ進める
            weakComboStep++;
        }
        // 強攻撃へ切り替えた場合
        else
        {
            // 弱攻撃から強攻撃へ切り替えた場合は、強攻撃のコンボを最初から開始する
            if (lastAttackType != AttackType.Heavy)
            {
                heavyComboStep = 0;
            }

            // 強攻撃3段目の次は新しい連続攻撃として扱う
            if (heavyComboStep >= HeavyAttackStates.Length)
            {
                heavyComboStep = 0;
            }

            // 現在の強攻撃アニメーションを設定する
            currentAnimationName = HeavyAttackStates[heavyComboStep];

            // 次の強攻撃段階へ進める
            heavyComboStep++;
        }

        // 直前に使用した攻撃を記録する
        lastAttackType = selectedAttack;

        // 次のアニメーションの進行時間を初期化する
        attackElapsedTime = 0f;

        // Animatorがあれば、選択した攻撃アニメーションを再生する。
        if (player.Animator != null)
        {
            player.Animator.CrossFade(
                currentAnimationName,
                0.1f
            );
        }

        // 現段階では攻撃開始時にコンボ数を加算する。
        // 実際の命中数を管理する場合は攻撃判定側との連携が必要。
        player.AddCombo();

        return true;
    }


    //------------------------------------------
    // 攻撃状態を終了する瞬間に呼び出される
    //------------------------------------------
    public void Exit(PlayerController player)
    {
        // 現時点では終了時の特別な処理はなし
    }
}
