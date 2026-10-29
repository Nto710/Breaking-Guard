using UnityEngine;

///===========================================
/// プレイヤーの待機状態を管理するステート
///===========================================
public class PlayerIdleState : IPlayerState
{
    //--------------------------------------
    // 待機状態に入った瞬間に呼び出される
    //--------------------------------------
    public void Enter(PlayerController player)
    {
        // 待機アニメーションを再生する
        if (player.Animator != null)
        {
            player.Animator.CrossFade("Idle", 0.1f);
        }
    }

    //---------------------------------------
    // 待機状態中、毎フレーム呼び出される
    //---------------------------------------
    public void UpdateState(PlayerController player)
    {
        // 必要なオブジェクトが取得できていない場合は処理しない。
        if (player == null || player.StateMachine == null || InputManager.Instance == null)
        {
            return;
        }


        // 移動入力があったら移動ステートへ
        if (InputManager.Instance.MoveInput.sqrMagnitude > 0.01f)
        {
            player.StateMachine.ChangeState(player, new PlayerMoveState());
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

        // パリィ入力があればパリィ状態へ遷移する
        if (InputManager.Instance.ParryPressed)
        {
            player.StateMachine.ChangeState(player, new PlayerParryState());
        }

    }


    //------------------------------------------
    // 待機状態を終了する瞬間に呼び出される
    //-------------------------------------------
    public void Exit(PlayerController player)
    {
        // 現時点では終了時の特別な処理はなし
    }
}