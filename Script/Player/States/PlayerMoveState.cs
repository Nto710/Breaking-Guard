using UnityEngine;

///===========================================
/// プレイヤーの移動状態を管理するステート
///===========================================
public class PlayerMoveState : IPlayerState
{
    //--------------------------------------
    // 移動状態に入った瞬間に呼び出される
    //--------------------------------------
    public void Enter(PlayerController player)
    {
        // 移動アニメーションを再生する。
        if (player != null && player.Animator != null)
        {
            player.Animator.CrossFade("Walk", 0.1f);
        }
    }


    //---------------------------------------
    // 移動状態中、毎フレーム呼び出される
    //---------------------------------------
    public void UpdateState(PlayerController player)
    {
        // プレイヤーまたはステートマシンが取得できていない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        if (InputManager.Instance == null)
        {
            player.StateMachine.ChangeState(player, new PlayerIdleState());
            return;
        }

        // 弱攻撃入力があったら攻撃ステートへ
        if (InputManager.Instance.HasBufferedLightAttack())
        {
            player.StateMachine.ChangeState(player, new PlayerAttackState());
            return;
        }

        // 強攻撃入力があったら攻撃ステートへ
        if (InputManager.Instance.HasBufferedHeavyAttack())
        {
            player.StateMachine.ChangeState(player, new PlayerAttackState());
            return;
        }

        // 回避入力があったら回避ステートへ
        if (InputManager.Instance.DodgePressed)
        {
            player.StateMachine.ChangeState(player, new PlayerDodgeState());
            return;
        }

        // ガードボタンが押されていたらガード状態へ
        if (InputManager.Instance.GuardHold)
        {
            player.StateMachine.ChangeState(player, new PlayerGuardState());
            return;
        }

        // パリィ入力があったらパリィステートへ
        if (InputManager.Instance.ParryPressed)
        {
            player.StateMachine.ChangeState(player, new PlayerParryState());
            return;
        }

        // 移動入力を取得する
        Vector2 moveInput = InputManager.Instance.MoveInput;

        // 入力がなくなったらIdleに戻る
        if (moveInput.sqrMagnitude <= 0.01f)
        {
            player.StateMachine.ChangeState(player, new PlayerIdleState());
            return;
        }

        // CharacterControllerが取得できていない場合は移動しない。
        if (player.CharacterController == null)
        {
            return;
        }

        // カメラ基準の移動方向を計算する
        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        // カメラが取得できている場合は、カメラ基準の方向を計算する。
        if (player.CamTransform != null)
        {
            // カメラの前方向を水平面に投影する
            cameraForward = player.CamTransform.forward;
            cameraForward.y = 0f;

            // 前方向を正規化する。方向が無効な場合はワールド前方向を使う。
            if (cameraForward.sqrMagnitude > 0.001f)
            {
                cameraForward.Normalize();
            }
            else
            {
                cameraForward = Vector3.forward;
            }

            // カメラの右方向を水平面に投影する
            cameraRight = player.CamTransform.right;
            cameraRight.y = 0f;

            // 右方向を正規化する。方向が無効な場合はワールド右方向を使う。
            if (cameraRight.sqrMagnitude > 0.001f)
            {
                cameraRight.Normalize();
            }
            else
            {
                cameraRight = Vector3.right;
            }
        }

        // 入力をカメラ基準の移動方向に変換する
        Vector3 moveDir = cameraForward * moveInput.y + cameraRight * moveInput.x;

        // 移動方向を正規化して移動速度を一定にする
        if (moveDir.sqrMagnitude > 0.001f)
        {
            moveDir.Normalize();
        }
        else
        {
            return;
        }

        // 移動方向にキャラクターの向きを合わせる
        Quaternion targetRotation = Quaternion.LookRotation(moveDir);

        player.transform.rotation = Quaternion.Slerp(player.transform.rotation, targetRotation, player.rotationSpeed * Time.deltaTime);

        // CharacterControllerを使って移動する
        if (player.CharacterController != null)
        {
            player.CharacterController.Move(moveDir * player.moveSpeed * Time.deltaTime);
        }
    }


    //------------------------------------------
    // 移動状態を終了する瞬間に呼び出される
    //------------------------------------------
    public void Exit(PlayerController player)
    {
        // 現時点では終了時の特別な処理はなし
    }
}


//＜追加要素＞
//
// ・Runを作る。