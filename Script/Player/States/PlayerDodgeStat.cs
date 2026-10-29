
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


    //--------------------------------------
    // 回避状態に入った瞬間に呼び出される
    //--------------------------------------
    public void Enter(PlayerController player)
    {
        // 回避時間を初期化する
        elapsedTime = 0f;

        // 判定フラグを初期化する
        IsInvulnerable = true;
        IsJustDodge = false;

        // 回避方向を決定する
        dodgeDirection = GetDodgeDirection(player);

        // 回避アニメーションを再生する
        if (player.Animator != null)
        {
            player.Animator.CrossFade("Dodge", 0.05f);
        }
    }


    //---------------------------------------
    // 回避状態中、毎フレーム呼び出される
    //---------------------------------------
    public void UpdateState(PlayerController player)
    {
        // 回避開始からの経過時間を更新する
        elapsedTime += Time.deltaTime;

        // 無敵時間中かどうかを更新する
        IsInvulnerable =
            elapsedTime < InvulnerabilityDuration;

        //----------------------------------
        // 回避状態の終了判定
        //----------------------------------
        if (elapsedTime >= DodgeDuration)
        {
            // 回避終了後は無敵状態を解除する
            IsInvulnerable = false;

            // 次の状態へ移行する
            ReturnToNextState(player);
            return;
        }

        //----------------------------------
        // 回避移動
        //----------------------------------
        if (player.CharacterController != null)
        {
            // 一定の距離を一定時間で移動する
            float dodgeSpeed =
                DodgeDistance / DodgeDuration;

            player.CharacterController.Move(
                dodgeDirection *
                dodgeSpeed *
                Time.deltaTime
            );
        }
    }


    //--------------------------------------
    // 回避方向を決定する
    //--------------------------------------
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

            return forward.sqrMagnitude > 0.001f
                ? forward.normalized
                : Vector3.forward;
        }

        //----------------------------------
        // カメラ基準の回避方向を計算する
        //----------------------------------
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        if (player.CamTransform != null)
        {
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
        Vector3 direction =
            cameraForward * moveInput.y +
            cameraRight * moveInput.x;

        // 回避方向を正規化する
        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();
        }
        else
        {
            direction = player.transform.forward;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
            }
            else
            {
                direction = Vector3.forward;
            }
        }

        return direction;
    }


    //--------------------------------------
    // 敵の攻撃を回避できるか判定する
    //--------------------------------------
    public bool TryAvoidAttack()
    {
        // 無敵時間外なら回避できない
        if (!IsInvulnerable)
        {
            return false;
        }

        // 回避開始直後ならジャスト回避として記録する
        if (elapsedTime <= JustDodgeWindow)
        {
            IsJustDodge = true;
        }

        // 無敵時間中なので攻撃を回避できた
        return true;
    }


    //--------------------------------------
    // 回避終了後の状態を決定する
    //--------------------------------------
    private void ReturnToNextState(PlayerController player)
    {
        if (InputManager.Instance == null)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerIdleState()
            );
            return;
        }

        // ガードボタンを押していればガードへ
        if (InputManager.Instance.GuardHold)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerGuardState()
            );
            return;
        }

        // パリィ入力があればパリィへ
        if (InputManager.Instance.ParryPressed)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerParryState()
            );
            return;
        }

        // 攻撃入力があれば攻撃へ
        if (InputManager.Instance.HasBufferedLightAttack() ||
            InputManager.Instance.HasBufferedHeavyAttack())
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerAttackState()
            );
            return;
        }

        // 移動入力があれば移動へ
        if (InputManager.Instance.MoveInput.sqrMagnitude > 0.01f)
        {
            player.StateMachine.ChangeState(
                player,
                new PlayerMoveState()
            );
        }
        else
        {
            // 入力がなければ待機へ
            player.StateMachine.ChangeState(
                player,
                new PlayerIdleState()
            );
        }
    }


    //------------------------------------------
    // 回避状態を終了する瞬間に呼び出される
    //------------------------------------------
    public void Exit(PlayerController player)
    {
        // 回避状態を抜けたら無敵状態を解除する
        IsInvulnerable = false;
    }
}
