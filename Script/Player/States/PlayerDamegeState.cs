
using UnityEngine;

///===========================================
/// プレイヤーの被ダメージ状態を管理するステート
///===========================================
public class PlayerDamageState : IPlayerState
{
    //--------------------------------------
    // 被ダメージの設定（仮の数値）
    //--------------------------------------

    // 被ダメージ状態を維持する時間（秒）（実際のアニメーションに合わせて調整する。）
    private const float DamageStateDuration = 0.5f;


    //--------------------------------------
    // 被ダメージの管理情報
    //--------------------------------------

    // 今回受けるダメージ量
    private readonly float damageAmount;

    // 攻撃してきた相手の位置
    // 今後、ノックバックや被弾方向の制御に使用するため保持する。
    public Vector3 AttackerPosition { get; private set; }


    // 被ダメージ状態に入ってからの経過時間
    private float elapsedTime = 0f;


    //--------------------------------------
    // コンストラクタ
    //--------------------------------------

    // 受けるダメージ量と、攻撃してきた相手の位置を設定する。
    public PlayerDamageState(float damage, Vector3 attackerPosition)
    {
        // ダメージ量が負にならないようにする。
        damageAmount = Mathf.Max(0f, damage);

        // 攻撃者の位置を保存する。
        AttackerPosition = attackerPosition;
    }

    // --------------------------------------------------
    // ステート開始
    // --------------------------------------------------

    // 被ダメージ状態に入った瞬間に呼び出される
    // HPを減少させ、被弾アニメーションを再生する。
    public void Enter(PlayerController player)
    {
        // プレイヤーが取得できていない場合は処理しない。
        if (player == null)
        {
            return;

        }

        // 被ダメージ状態の経過時間を初期化する。
        elapsedTime = 0f;

        // ダメージを適用し、HPを0以上、最大HP以下に制限する。
        player.currentHp = Mathf.Clamp(player.currentHp - damageAmount, 0f, player.maxHp);

        // 被弾アニメーションを再生する。
        if (player.Animator != null)
        {
            player.Animator.CrossFade("Damage", 0.05f);
        }

        // HPが0になった場合はゲームオーバーフラグを設定する。
        if (player.currentHp <= 0f)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.isGameOver = true;
            }
        }
    }

    // --------------------------------------------------
    // 毎フレームの更新
    // --------------------------------------------------

    // 被ダメージ状態中、毎フレーム呼び出される
    // 行動不能時間と、被弾後の復帰を管理する。
    public void UpdateState(PlayerController player)
    {
        // プレイヤーまたはステートマシンがない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        // HPが0の場合は通常の行動へ復帰しない。
        // ゲームオーバー後の画面遷移は別の管理処理に任せる。
        if (player.currentHp <= 0f)
        {
            return;
        }

        // 被ダメージ状態の経過時間を更新する
        elapsedTime += Time.deltaTime;

        // 行動不能時間が終了するまでは何もしない
        if (elapsedTime < DamageStateDuration)
        {
            return;
        }

        // 行動不能時間が終了したら次のステートへ移行する。
        ReturnToNextState(player);
    }


    //--------------------------------------
    // 被ダメージ終了後のステート遷移
    //--------------------------------------

    // 被ダメージ終了後の入力を確認し、次のステートを決定する。
    private void ReturnToNextState(PlayerController player)
    {
        // 現在の入力管理クラスを取得する。
        InputManager input = InputManager.Instance;

        // 入力管理クラスが存在しない場合は待機状態へ戻る。
        if (input == null)
        {
            player.StateMachine.ChangeState(player, new PlayerIdleState());
            return;
        }

        // ガードボタンが押されていればガード状態へ移行する。
        if (input.GuardHold)
        {
            player.StateMachine.ChangeState(player, new PlayerGuardState());
            return;
        }

        // パリィ入力があればパリィ状態へ移行する。
        if (input.ParryPressed)
        {
            player.StateMachine.ChangeState(player, new PlayerParryState());
            return;
        }

        // 回避入力があれば回避状態へ移行する。
        if (input.DodgePressed)
        {
            player.StateMachine.ChangeState(player, new PlayerDodgeState());
            return;
        }

        // 攻撃入力が残っていれば攻撃状態へ移行する。
        if (input.HasBufferedLightAttack() || input.HasBufferedHeavyAttack())
        {
            player.StateMachine.ChangeState(player, new PlayerAttackState());
            return;
        }

        // 移動入力があれば移動状態へ戻る。
        if (input.MoveInput.sqrMagnitude > 0.01f)
        {
            player.StateMachine.ChangeState(player, new PlayerMoveState());
        }
        else
        {
            // 行動入力がなければ待機状態へ戻る。
            player.StateMachine.ChangeState(player, new PlayerIdleState());
        }
    }

    // --------------------------------------------------
    // ステート終了
    // --------------------------------------------------

    // 被ダメージ状態を終了するときに呼び出される。
    public void Exit(PlayerController player)
    {
        // 現時点では終了時の特別な処理はない。
    }
}

// <注意事項>
//
//・Animator Controller内に Damage というステートが必要です。実際の名前に合わせて変更してください。
//
//・DamageStateDuration の0.5秒は仮設定です。アニメーションの長さや操作感に合わせて調整します。
//
//・今回はHP減少と行動不能時間を実装していますが、ノックバック、被弾エフェクト、効果音、連続被弾の制御はまだありません。
//
//・isGameOver はフラグを設定するだけです。タイトル画面への移動やリザルト画面への遷移は、既存のゲーム全体の管理処理と接続する必要があります。
