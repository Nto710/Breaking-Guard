using UnityEngine;

///===============================================================
/// プレイヤーのステート（状態）の切り替えと実行を管理するクラス
/// 各ステートの具体的な処理は IPlayerState の実装クラスに任せ、
///このクラスでは現在のステートと実行タイミングを管理する。
///===============================================================
public class PlayerStateMachine
{
    // 現在プレイヤーが移行しているアクティブなステート（外部参照用プロパティ）
    public IPlayerState CurrentState { get; private set; }

    ///---------------------------------------------------------------------------
    /// ステートマシンを初期化し、最初のステート（Idleなど）を適用し開始する。
    ///--------------------------------------------------------------------------
    public void Initialize(PlayerController player, IPlayerState initialState)
    {
        // プレイヤーや初期ステートが指定されていない場合は初期化しない。
        if (player == null || initialState == null)
        {
            Debug.LogError("PlayerStateMachineの初期化に失敗しました。PlayerまたはInitialStateが未設定です。");
            return;
        }

        // 初期化済みの場合、現在のステートを意図せず上書きしない。
        if (CurrentState != null)
        {
            Debug.LogWarning("PlayerStateMachineはすでに初期化されています。");
            return;
        }

        // 最初のステートを設定し、開始時の処理を実行する。
        CurrentState = initialState;
        CurrentState.Enter(player);
    }

    ///------------------------------------------------------------------------
    /// 現在のステートを終了させ、指定した新しいステートへ安全に切り替えます。
    ///------------------------------------------------------------------------
    public void ChangeState(PlayerController player, IPlayerState newState)
    {
        // 不正な引数による状態遷移を防ぐ。
        if (player == null || newState == null)
        {
            Debug.LogError("PlayerStateMachineの状態遷移に失敗しました。PlayerまたはNewStateが未設定です。");
            return;
        }

        // 同じステートインスタンスへの切り替えは行わない。
        if (ReferenceEquals(CurrentState, newState))
        {
            return;
        }

        // 現在のステートが存在すれば、退出・終了処理（Exit）を呼び出す
        if (CurrentState != null)
        {
            CurrentState.Exit(player);
        }

        // 新しいステートへ切り替え、開始処理を実行する。
        CurrentState = newState;
        CurrentState.Enter(player);
    }

    ///--------------------------------------------------------------------------------------
    /// 毎フレーム呼び出され、現在アクティブなステートの更新処理（UpdateState）を進めます。
    ///-------------------------------------------------------------------------------------
    public void Update(PlayerController player)
    {
        // 現在のステートが存在する場合のみ更新処理を実行する。
        if (CurrentState != null && player != null)
        {
            CurrentState.UpdateState(player);
        }
    }
}