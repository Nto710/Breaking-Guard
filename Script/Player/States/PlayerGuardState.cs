
using UnityEngine;

///===========================================
/// プレイヤーのガード状態を管理するステート
///===========================================
public class PlayerGuardState : IPlayerState
{
    //--------------------------------------
    // ガード状態に入った瞬間に呼び出される
    //--------------------------------------
    public void Enter(PlayerController player)
    {
        // プレイヤーが取得できている場合、ガードアニメーションを再生する。
        if (player != null && player.Animator != null)
        {
            player.Animator.CrossFade("Guard", 0.1f);
        }
    }

    //---------------------------------------
    // ガード状態中、毎フレーム呼び出される
    //---------------------------------------
    public void UpdateState(PlayerController player)
    {
        // プレイヤーまたはステートマシンが取得できていない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        // InputManagerが存在しない場合は待機状態へ戻る
        if (InputManager.Instance == null)
        {
            player.StateMachine.ChangeState(player, new PlayerIdleState());
            return;
        }

        // パリィ入力があったらパリィステートへ
        if (InputManager.Instance.ParryPressed)
        {
            player.StateMachine.ChangeState(player, new PlayerParryState());
            return;
        }

        // 回避入力があったら回避ステートへ
        if (InputManager.Instance.DodgePressed)
        {
            player.StateMachine.ChangeState(player, new PlayerDodgeState());
            return;
        }

        // ガードボタンを離したら、次の状態を決める
        if (!InputManager.Instance.GuardHold)
        {
            // 移動入力がある場合は移動状態へ戻る
            if (InputManager.Instance.MoveInput.sqrMagnitude > 0.01f)
            {
                player.StateMachine.ChangeState(player, new PlayerMoveState());
            }
            else
            {
                // 移動入力がない場合は待機状態へ戻る
                player.StateMachine.ChangeState(player, new PlayerIdleState());
            }

            return;
        }

        // ガード中の移動は、現段階では行わない
        // ガード中のダメージ軽減処理は、敵の攻撃判定との連携時に実装する。
    }

    //------------------------------------------
    // ガード状態を終了する瞬間に呼び出される
    //------------------------------------------
    public void Exit(PlayerController player)
    {
        // 現時点では終了時の特別な処理はなし
    }
}
