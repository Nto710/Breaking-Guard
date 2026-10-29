
using UnityEngine;

///===========================================
/// プレイヤーの回避状態を管理するステート
///===========================================
public class PlayerDodgeState : IPlayerState
{
    //--------------------------------------
    // 回避の設定（仮の数値）
    //--------------------------------------

    // 回避状態の継続時間（秒）
    private const float DodgeDuration = 0.45f;

    // 回避中の無敵時間（秒）
    private const float InvulnerabilityDuration = 0.30f;

    // ジャスト回避の判定時間（秒）
    private const float JustDodgeWindow = 0.16f;

    // 回避する距離
    private const float DodgeDistance = 3.0f;

    // ジャスト回避成功時のスローモーション設定。
    private const float JustDodgeSlowScale = 0.3f;
    private const float JustDodgeSlowDuration = 0.8f;

    //--------------------------------------
    // 回避状態の管理
    //--------------------------------------

    // 回避開始からの経過時間
    private float elapsedTime = 0f;

    // 回避する方向
    private Vector3 dodgeDirection;

    // 現在、無敵時間中か
    public bool IsInvulnerable { get; private set; }

    // 敵の攻撃をジャスト回避できたか
    public bool IsJustDodge { get; private set; }

    // --------------------------------------------------
    // ステート開始
    // --------------------------------------------------


    // 回避状態に入った瞬間に呼び出される
    // タイマーと判定フラグを初期化し、回避方向を決定する。
    public void Enter(PlayerController player)
    {
        // プレイヤーが取得できていない場合は初期化しない。
        if (player == null)
        {
            return;
        }

        // 回避時間と判定フラグを初期化する。
        elapsedTime = 0f;
        IsInvulnerable = true;
        IsJustDodge = false;

        // 入力とカメラの向きから回避方向を決定する。
        dodgeDirection = GetDodgeDirection(player);

        // 回避アニメーションを再生する
        if (player.Animator != null)
        {
            player.Animator.CrossFade("Dodge", 0.05f);
        }
    }


    // --------------------------------------------------
    // 毎フレームの更新
    // --------------------------------------------------

    // 回避中の移動、無敵時間、終了判定を更新する。
    public void UpdateState(PlayerController player)
    {
        // プレイヤーまたはステートマシンがない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        // 回避開始からの経過時間を更新する
        elapsedTime += Time.deltaTime;

        // 無敵時間中かどうかを更新する
        IsInvulnerable = elapsedTime < InvulnerabilityDuration;


        // 回避時間が終了した場合は次のステートへ移行する。
        if (elapsedTime >= DodgeDuration)
        {
            // 回避終了後は無敵状態を解除する
            IsInvulnerable = false;

            // 次の状態へ移行する
            ReturnToNextState(player);
            return;
        }

        // CharacterControllerがない場合は移動処理を行わない。
        if (player.CharacterController == null)
        {
            return;
        }

        // 回避距離と継続時間から移動速度を求める。
        float dodgeSpeed = DodgeDistance / DodgeDuration;

        // 決定済みの回避方向へキャラクターを移動させる。
        player.CharacterController.Move(dodgeDirection * dodgeSpeed * Time.deltaTime);
    }


    //--------------------------------------
    // 回避方向の計算
    //--------------------------------------

    // 移動入力とカメラの向きをもとに回避方向を決定する。
    // 入力がない場合は、プレイヤーの前方へ回避する。
    private Vector3 GetDodgeDirection(PlayerController player)
    {
        Vector2 moveInput = Vector2.zero;

        // 移動入力を取得する
        if (InputManager.Instance != null)
        {
            moveInput = InputManager.Instance.MoveInput;
        }

        // 移動入力がない場合はキャラクターの前方へ回避する
        if (moveInput.sqrMagnitude <= 0.01f)
        {
            Vector3 forward = player.transform.forward;
            forward.y = 0f;

            return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        }

        // カメラ参照がない場合に使用する初期方向。
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        // PlayerControllerからカメラの参照を取得する。
        Transform camTransform = player.CamTransform;

        if (camTransform != null)
        {
            // カメラの前方向から上下成分を除去する。
            cameraForward = player.CamTransform.forward;
            cameraForward.y = 0f;

            if (cameraForward.sqrMagnitude > 0.001f)
            {
                cameraForward.Normalize();
            }
            else
            {
                cameraForward = Vector3.forward;
            }

            // カメラの右方向から上下成分を除去する。
            cameraRight = player.CamTransform.right;
            cameraRight.y = 0f;

            if (cameraRight.sqrMagnitude > 0.001f)
            {
                cameraRight.Normalize();
            }
            else
            {
                cameraRight = Vector3.right;
            }
        }

        // 入力をカメラ基準の方向へ変換する
        Vector3 direction = cameraForward * moveInput.y + cameraRight * moveInput.x;

        // 回避方向を正規化し、一定の移動速度を保つ。
        if (direction.sqrMagnitude > 0.001f)
        {
            return direction.normalized;
        }

        // 計算結果が無効な場合はプレイヤーの前方を使用する。
        Vector3 fallbackDirection = player.transform.forward;
        fallbackDirection.y = 0f;

        return fallbackDirection.sqrMagnitude > 0.001f ? fallbackDirection.normalized : Vector3.forward;
    }



    //--------------------------------------
    // 敵の攻撃を回避できるか判定する
    //--------------------------------------

    // 敵の攻撃がプレイヤーに接触したときに呼び出す。
    // 無敵時間内ならtrueを返し、ジャスト回避が成立した場合はスローモーションを発動する。
    public bool TryAvoidAttack()
    {
        // 無敵時間外なら回避できない
        if (!IsInvulnerable)
        {
            return false;
        }

        // ジャスト回避の受付時間内で、まだ成功していない場合。
        if (elapsedTime <= JustDodgeWindow && !IsJustDodge)
        {
            // ジャスト回避の成功を記録する。
            IsJustDodge = true;

            // 時間管理クラスが存在する場合はスローモーションを発動する。
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.DoSlowMotion(JustDodgeSlowScale, JustDodgeSlowDuration);
            }
        }

        // 無敵時間内なので、通常のダメージ処理を行わないよう通知する。
        return true;
    }



    //--------------------------------------
    // 回避終了後の状態を決定する
    //--------------------------------------

    // 回避終了後の入力を確認し、次のステートを決定する。
    private void ReturnToNextState(PlayerController player)
    {
        // 入力管理クラスがない場合は待機状態へ戻る。
        InputManager input = InputManager.Instance;

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
            player.StateMachine.ChangeState(
                player,
                new PlayerParryState()
            );
            return;
        }

        // 弱攻撃または強攻撃が入力されていれば攻撃状態へ移行する。
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

    /// <summary>
    /// 回避状態を終了するときに呼び出される。
    /// 無敵状態を解除する。
    /// </summary>
    public void Exit(PlayerController player)
    {
        IsInvulnerable = false;
    }
}