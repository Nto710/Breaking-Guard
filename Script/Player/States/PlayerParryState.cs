
using UnityEngine;
using UnityEngine.Windows;

///===========================================
/// プレイヤーのパリィ状態を管理するステート
///===========================================
public class PlayerParryState : IPlayerState
{
    //--------------------------------------
    // パリィの設定
    //--------------------------------------

    // パリィの成功判定を受け付ける時間（秒）
    private const float ParryWindowDuration = 0.20f;

    // パリィ状態全体の継続時間（秒）
    private const float ParryStateDuration = 0.45f;


    //--------------------------------------
    // パリィの状態管理
    //--------------------------------------

    // パリィ状態に入ってからの経過時間
    private float elapsedTime = 0f;

    // 現在、パリィ成功判定を受け付けているか
    public bool IsParryWindowActive { get; private set; }

    // パリィに成功したか
    public bool IsParrySuccessful { get; private set; }


    // --------------------------------------------------
    // ステート開始
    // --------------------------------------------------

    // パリィ状態に入ったときに呼び出される。
    // タイマーと判定フラグを初期化し、アニメーションを再生する。
    public void Enter(PlayerController player)
    {
        // 経過時間を初期化する
        elapsedTime = 0f;

        // パリィの成功フラグを初期化する
        IsParrySuccessful = false;

        // パリィ判定を開始する
        IsParryWindowActive = true;

        // パリィアニメーションを再生する
        if (player.Animator != null)
        {
            player.Animator.CrossFade("Parry", 0.05f);
        }
    }


    // --------------------------------------------------
    // 毎フレームの更新
    // --------------------------------------------------

    // パリィ状態中、毎フレーム呼び出される。
    // 受付時間と状態の継続時間を管理する。
    public void UpdateState(PlayerController player)
    {
        // プレイヤーまたはステートマシンがない場合は処理しない。
        if (player == null || player.StateMachine == null)
        {
            return;
        }

        // 経過時間を更新する
        elapsedTime += Time.deltaTime;

        // 受付時間を過ぎた場合、パリィ判定を終了する。
        if (elapsedTime >= ParryWindowDuration)
        {
            IsParryWindowActive = false;
        }

        // パリィ状態全体の終了時間に達していなければ継続する。
        if (elapsedTime < ParryStateDuration)
        {
            return;
        }

        // 入力管理クラスがない場合は待機状態へ戻る。
        if (InputManager.Instance == null)
        {
            player.StateMachine.ChangeState(player, new PlayerIdleState());
            return;
        }

        // ガードボタンを押し続けている場合はガード状態へ戻る。
        if (InputManager.Instance.GuardHold)
        {
            player.StateMachine.ChangeState(player, new PlayerGuardState());
            return;
        }

        // 攻撃入力が残っている場合は攻撃状態へ移行する。
        if (InputManager.Instance.HasBufferedLightAttack() || InputManager.Instance.HasBufferedHeavyAttack())
        {
            player.StateMachine.ChangeState(player, new PlayerAttackState());
            return;
        }

        // 移動入力があれば移動状態へ戻る。
        if (InputManager.Instance.MoveInput.sqrMagnitude > 0.01f)
        {
            player.StateMachine.ChangeState(player, new PlayerMoveState());
        }
        else
        {
            // それ以外の場合は待機状態へ戻る。
            player.StateMachine.ChangeState(player, new PlayerIdleState());
        }
    }


    //--------------------------------------
    // パリィ成功判定
    //--------------------------------------
    public bool TryParry()
    {
        // 判定時間外、または成功済みなら失敗
        if (!IsParryWindowActive || IsParrySuccessful)
        {
            return false;
        }

        // パリィ成功を記録する
        IsParrySuccessful = true;

        // 同じパリィで複数回成功しないようにする
        IsParryWindowActive = false;

        return true;
    }


    //------------------------------------------
    // パリィ状態を終了する瞬間に呼び出される
    //------------------------------------------

    // 敵の攻撃がパリィ受付時間内に接触した場合に呼び出す。
    // 成功した場合はtrueを返し、同じ受付時間内の再成功を防ぐ。
    public void Exit(PlayerController player)
    {
        // 現時点では終了時の特別な処理はなし
    }
}


// <注意事項>
//
//パリィ成功と敵への効果は、まだ別の処理です。
//